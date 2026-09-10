using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

#if NETSTANDARD2_0
[Generator(LanguageNames.CSharp)]
#endif
public sealed class TimelineIncrementalGenerator : IIncrementalGenerator
{
    private const string Category = "Tl.Generation";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax { BaseList.Types.Count: > 0 },
                static (syntax, cancellationToken) => ReadCandidate(syntax, cancellationToken))
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(CandidateComparer.Instance)
            .WithTrackingName("Tl.Candidates");
        var environment = context.CompilationProvider
            .Combine(context.ParseOptionsProvider)
            .Select(static (pair, cancellationToken) => EnvironmentInput.Create(pair.Left, pair.Right, cancellationToken))
            .WithComparer(EnvironmentComparer.Instance)
            .WithTrackingName("Tl.Environment");
        var compilation = candidates
            .Collect()
            .Combine(environment)
            .Select(static (pair, cancellationToken) => CompilationInput.Create(pair.Left, pair.Right, cancellationToken))
            .WithComparer(CompilationComparer.Instance)
            .WithTrackingName("Tl.Compilation");
        context.RegisterSourceOutput(compilation, static (production, input) => Produce(production, input));
    }

    private static CandidateInput? ReadCandidate(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var syntax = (TypeDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(syntax, cancellationToken);
        if (symbol is null)
            return null;
        var contract = context.SemanticModel.Compilation.GetTypeByMetadataName("Tl.ITimeline");
        if (contract is null)
        {
            if (!syntax.BaseList!.Types.Any(static type => type.Type.ToString().IndexOf("ITimeline", StringComparison.Ordinal) >= 0))
                return null;
        }
        else if (!symbol.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, contract)))
            return null;
        var identity = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return new CandidateInput(identity);
    }

    private static void Produce(SourceProductionContext context, CompilationInput input)
    {
        foreach (var diagnostic in input.Diagnostics)
            context.ReportDiagnostic(Diagnostic.Create(Descriptor(diagnostic.Code), Location(diagnostic), diagnostic.Message));
        if (input.Timelines.Length > ushort.MaxValue + 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptor("TLGEN49"), Microsoft.CodeAnalysis.Location.None, "A compilation may contain at most 65,536 timelines."));
            return;
        }
        if (input.Diagnostics.Length != 0)
            return;
        var artifacts = HeterogeneousEmitter.EmitCompilation(input.Timelines);
        foreach (var artifact in artifacts.OrderBy(static artifact => artifact.RelativePath, StringComparer.Ordinal))
            context.AddSource(artifact.RelativePath, SourceText.From(HeterogeneousEmitter.NormalizeSource(artifact.Content), new UTF8Encoding(false)));
    }

    private static DiagnosticDescriptor Descriptor(string code)
        => new(code, "Invalid timeline declaration", "{0}", Category, DiagnosticSeverity.Error, true);

    private static Location Location(DeclarationDiagnostic diagnostic)
    {
        if (diagnostic.SpanStart < 0)
            return Microsoft.CodeAnalysis.Location.None;
        return Microsoft.CodeAnalysis.Location.Create(
            diagnostic.File,
            new TextSpan(diagnostic.SpanStart, diagnostic.SpanLength),
            new LinePositionSpan(
                new LinePosition(diagnostic.Line - 1, diagnostic.Column - 1),
                new LinePosition(diagnostic.EndLine - 1, diagnostic.EndColumn - 1)));
    }

    private sealed class CandidateInput
    {
        internal CandidateInput(string identity) => Identity = identity;

        internal string Identity { get; }
        internal string Key => Identity;
    }

    private sealed class EnvironmentInput
    {
        private EnvironmentInput(ImmutableArray<HeterogeneousTimeline> timelines, ImmutableArray<DeclarationDiagnostic> diagnostics, string key)
        {
            Timelines = timelines;
            Diagnostics = diagnostics;
            Key = key;
        }

        internal ImmutableArray<HeterogeneousTimeline> Timelines { get; }
        internal ImmutableArray<DeclarationDiagnostic> Diagnostics { get; }
        internal string Key { get; }

        internal static EnvironmentInput Create(Compilation compilation, ParseOptions parseOptions, CancellationToken cancellationToken)
        {
            var key = new StringBuilder();
            if (parseOptions is CSharpParseOptions csharp)
            {
                Append(key, csharp.LanguageVersion.ToString());
                Append(key, csharp.DocumentationMode.ToString());
                Append(key, csharp.Kind.ToString());
                foreach (var symbol in csharp.PreprocessorSymbolNames.OrderBy(static value => value, StringComparer.Ordinal))
                    Append(key, symbol);
                foreach (var feature in csharp.Features.OrderBy(static value => value.Key, StringComparer.Ordinal))
                {
                    Append(key, feature.Key);
                    Append(key, feature.Value);
                }
            }
            if (compilation.Options is CSharpCompilationOptions options)
            {
                Append(key, options.OutputKind.ToString());
                Append(key, options.OptimizationLevel.ToString());
                Append(key, options.CheckOverflow);
                Append(key, options.AllowUnsafe);
                Append(key, options.Platform.ToString());
                Append(key, options.NullableContextOptions.ToString());
                Append(key, options.WarningLevel);
                foreach (var diagnostic in options.SpecificDiagnosticOptions.OrderBy(static value => value.Key, StringComparer.Ordinal))
                {
                    Append(key, diagnostic.Key);
                    Append(key, diagnostic.Value.ToString());
                }
            }
            foreach (var reference in compilation.References)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Append(key, reference.Display ?? "");
                Append(key, reference.Properties.Kind.ToString());
                Append(key, reference.Properties.EmbedInteropTypes);
                foreach (var alias in reference.Properties.Aliases)
                    Append(key, alias);
                var symbol = compilation.GetAssemblyOrModuleSymbol(reference);
                Append(key, symbol is IAssemblySymbol assembly ? assembly.Identity.ToString() : symbol?.Name ?? "");
            }
            var result = HeterogeneousReader.ReadCompilation((CSharpCompilation)compilation);
            cancellationToken.ThrowIfCancellationRequested();
            var timelines = result.Timelines
                .OrderBy(static timeline => timeline.Namespace, StringComparer.Ordinal)
                .ThenBy(static timeline => timeline.Name, StringComparer.Ordinal)
                .ToImmutableArray();
            var diagnostics = result.Diagnostics
                .GroupBy(static diagnostic => DiagnosticKey(diagnostic), StringComparer.Ordinal)
                .Select(static group => group.First())
                .OrderBy(static diagnostic => diagnostic.File, StringComparer.Ordinal)
                .ThenBy(static diagnostic => diagnostic.SpanStart)
                .ThenBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ToImmutableArray();
            foreach (var timeline in timelines)
                Append(key, HeterogeneousEmitter.NormalizeSource(HeterogeneousEmitter.Emit(timeline)));
            foreach (var diagnostic in diagnostics)
                Append(key, DiagnosticKey(diagnostic));
            return new EnvironmentInput(timelines, diagnostics, key.ToString());
        }
    }

    private sealed class CompilationInput
    {
        private CompilationInput(ImmutableArray<HeterogeneousTimeline> timelines, ImmutableArray<DeclarationDiagnostic> diagnostics, string key)
        {
            Timelines = timelines;
            Diagnostics = diagnostics;
            Key = key;
        }

        internal ImmutableArray<HeterogeneousTimeline> Timelines { get; }
        internal ImmutableArray<DeclarationDiagnostic> Diagnostics { get; }
        internal string Key { get; }

        internal static CompilationInput Create(ImmutableArray<CandidateInput> candidates, EnvironmentInput environment, CancellationToken cancellationToken)
        {
            var unique = candidates
                .OrderBy(static candidate => candidate.Identity, StringComparer.Ordinal)
                .GroupBy(static candidate => candidate.Identity, StringComparer.Ordinal)
                .Select(static group => group.First())
                .ToArray();
            var key = new StringBuilder(environment.Key);
            foreach (var candidate in unique)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Append(key, candidate.Key);
            }
            return new CompilationInput(environment.Timelines, environment.Diagnostics, key.ToString());
        }
    }

    private sealed class CandidateComparer : IEqualityComparer<CandidateInput>
    {
        internal static readonly CandidateComparer Instance = new();
        public bool Equals(CandidateInput? x, CandidateInput? y) => ReferenceEquals(x, y) || x is not null && y is not null && x.Key == y.Key;
        public int GetHashCode(CandidateInput obj) => StringComparer.Ordinal.GetHashCode(obj.Key);
    }

    private sealed class EnvironmentComparer : IEqualityComparer<EnvironmentInput>
    {
        internal static readonly EnvironmentComparer Instance = new();
        public bool Equals(EnvironmentInput? x, EnvironmentInput? y) => ReferenceEquals(x, y) || x is not null && y is not null && x.Key == y.Key;
        public int GetHashCode(EnvironmentInput obj) => StringComparer.Ordinal.GetHashCode(obj.Key);
    }

    private sealed class CompilationComparer : IEqualityComparer<CompilationInput>
    {
        internal static readonly CompilationComparer Instance = new();
        public bool Equals(CompilationInput? x, CompilationInput? y) => ReferenceEquals(x, y) || x is not null && y is not null && x.Key == y.Key;
        public int GetHashCode(CompilationInput obj) => StringComparer.Ordinal.GetHashCode(obj.Key);
    }

    private static string DiagnosticKey(DeclarationDiagnostic diagnostic)
        => diagnostic.File + "\0" + diagnostic.SpanStart + "\0" + diagnostic.SpanLength + "\0" + diagnostic.Code + "\0" + diagnostic.Message;

    private static void Append(StringBuilder writer, string value) => writer.Append(value.Length).Append(':').Append(value);
    private static void Append(StringBuilder writer, int value) => Append(writer, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static void Append(StringBuilder writer, bool value) => writer.Append(value ? "1:" : "0:");
}
