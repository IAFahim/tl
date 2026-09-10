using System.Collections.Immutable;
using System.Security.Cryptography;
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
        var inputs = context.CompilationProvider
            .Combine(context.ParseOptionsProvider)
            .Select(static (pair, cancellationToken) => CompilerInput.Create(pair.Left, pair.Right, cancellationToken))
            .WithComparer(CompilerInputComparer.Instance)
            .WithTrackingName("Tl.Inputs");
        var analysis = candidates
            .Collect()
            .Combine(inputs)
            .Select(static (pair, cancellationToken) => AnalysisInput.Create(pair.Left, pair.Right, cancellationToken))
            .WithComparer(AnalysisComparer.Instance)
            .WithTrackingName("Tl.Analysis");
        context.RegisterSourceOutput(analysis, static (production, input) => Produce(production, input));
    }

    private static CandidateInput? ReadCandidate(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var syntax = (TypeDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(syntax, cancellationToken);
        if (symbol is null)
            return null;
        var compilation = (CSharpCompilation)context.SemanticModel.Compilation;
        var contract = compilation.GetTypeByMetadataName("Tl.ITimeline");
        if (contract is null)
        {
            if (!syntax.BaseList!.Types.Any(static type => type.Type.ToString().IndexOf("ITimeline", StringComparison.Ordinal) >= 0))
                return null;
        }
        else if (!symbol.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, contract)))
            return null;
        var sources = RelevantSources(compilation, symbol, cancellationToken);
        var identity = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var key = new StringBuilder();
        Append(key, identity);
        foreach (var source in sources)
        {
            Append(key, source.Path);
            Append(key, source.Content);
        }
        return new CandidateInput(identity, sources, key.ToString());
    }

    private static ImmutableArray<SourceInput> RelevantSources(
        CSharpCompilation compilation,
        INamedTypeSymbol timeline,
        CancellationToken cancellationToken)
    {
        var queued = new HashSet<SyntaxTree>();
        var queue = new Queue<SyntaxTree>();
        void AddTree(SyntaxTree tree)
        {
            if (queued.Add(tree))
                queue.Enqueue(tree);
        }
        void AddSymbol(ISymbol? symbol)
        {
            if (symbol is null)
                return;
            foreach (var reference in symbol.DeclaringSyntaxReferences)
                AddTree(reference.SyntaxTree);
            switch (symbol)
            {
                case IMethodSymbol method:
                    AddSymbol(method.ContainingType);
                    AddSymbol(method.ReturnType);
                    foreach (var parameter in method.Parameters)
                        AddSymbol(parameter.Type);
                    foreach (var argument in method.TypeArguments)
                        AddSymbol(argument);
                    break;
                case IPropertySymbol property:
                    AddSymbol(property.ContainingType);
                    AddSymbol(property.Type);
                    break;
                case IFieldSymbol field:
                    AddSymbol(field.ContainingType);
                    AddSymbol(field.Type);
                    break;
                case IEventSymbol eventSymbol:
                    AddSymbol(eventSymbol.ContainingType);
                    AddSymbol(eventSymbol.Type);
                    break;
                case ILocalSymbol local:
                    AddSymbol(local.Type);
                    break;
                case IParameterSymbol parameter:
                    AddSymbol(parameter.Type);
                    break;
                case IArrayTypeSymbol array:
                    AddSymbol(array.ElementType);
                    break;
                case IPointerTypeSymbol pointer:
                    AddSymbol(pointer.PointedAtType);
                    break;
                case INamedTypeSymbol type:
                    foreach (var argument in type.TypeArguments)
                        AddSymbol(argument);
                    break;
            }
        }
        AddSymbol(timeline);
        foreach (var tree in compilation.SyntaxTrees)
            if (tree.GetRoot(cancellationToken).DescendantNodes().OfType<UsingDirectiveSyntax>().Any(static usingDirective => usingDirective.GlobalKeyword.RawKind != 0))
                AddTree(tree);
        while (queue.Count != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tree = queue.Dequeue();
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot(cancellationToken).DescendantNodesAndSelf())
            {
                var symbol = model.GetSymbolInfo(node, cancellationToken);
                AddSymbol(symbol.Symbol);
                foreach (var candidate in symbol.CandidateSymbols)
                    AddSymbol(candidate);
                var type = model.GetTypeInfo(node, cancellationToken);
                AddSymbol(type.Type);
                AddSymbol(type.ConvertedType);
            }
        }
        return queued
            .Select(tree => new SourceInput(tree.FilePath, tree.GetText(cancellationToken).ToString()))
            .OrderBy(static source => source.Path, StringComparer.Ordinal)
            .ThenBy(static source => source.Content, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static void Produce(SourceProductionContext context, AnalysisInput input)
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

    private sealed record SourceInput(string Path, string Content);

    private sealed class CandidateInput(
        string identity,
        ImmutableArray<SourceInput> sources,
        string key)
    {
        internal string Identity { get; } = identity;
        internal ImmutableArray<SourceInput> Sources { get; } = sources;
        internal string Key { get; } = key;
    }

    private sealed class ReferenceInput(
        string path,
        ImmutableArray<string> aliases,
        bool embedInteropTypes,
        MetadataImageKind kind,
        string key)
    {
        internal string Path { get; } = path;
        internal ImmutableArray<string> Aliases { get; } = aliases;
        internal bool EmbedInteropTypes { get; } = embedInteropTypes;
        internal MetadataImageKind Kind { get; } = kind;
        internal string Key { get; } = key;
    }

    private sealed class CompilerInput(
        string assemblyName,
        LanguageVersion languageVersion,
        DocumentationMode documentationMode,
        SourceCodeKind sourceKind,
        ImmutableArray<string> symbols,
        bool allowUnsafe,
        bool checkOverflow,
        NullableContextOptions nullable,
        ImmutableArray<ReferenceInput> references,
        string key)
    {
        internal string AssemblyName { get; } = assemblyName;
        internal LanguageVersion LanguageVersion { get; } = languageVersion;
        internal DocumentationMode DocumentationMode { get; } = documentationMode;
        internal SourceCodeKind SourceKind { get; } = sourceKind;
        internal ImmutableArray<string> Symbols { get; } = symbols;
        internal bool AllowUnsafe { get; } = allowUnsafe;
        internal bool CheckOverflow { get; } = checkOverflow;
        internal NullableContextOptions Nullable { get; } = nullable;
        internal ImmutableArray<ReferenceInput> References { get; } = references;
        internal string Key { get; } = key;

        internal static CompilerInput Create(Compilation compilation, ParseOptions parseOptions, CancellationToken cancellationToken)
        {
            var csharp = (CSharpParseOptions)parseOptions;
            var options = (CSharpCompilationOptions)compilation.Options;
            var references = compilation.References
                .OfType<PortableExecutableReference>()
                .Select(reference => ReadReference(reference, cancellationToken))
                .Where(static reference => reference is not null)
                .Select(static reference => reference!)
                .ToImmutableArray();
            var symbols = csharp.PreprocessorSymbolNames.OrderBy(static symbol => symbol, StringComparer.Ordinal).ToImmutableArray();
            var key = new StringBuilder();
            Append(key, compilation.AssemblyName ?? "Tl.Generated.Analysis");
            Append(key, csharp.LanguageVersion.ToString());
            Append(key, csharp.DocumentationMode.ToString());
            Append(key, csharp.Kind.ToString());
            foreach (var symbol in symbols)
                Append(key, symbol);
            Append(key, options.AllowUnsafe ? "true" : "false");
            Append(key, options.CheckOverflow ? "true" : "false");
            Append(key, options.NullableContextOptions.ToString());
            foreach (var reference in references)
                Append(key, reference.Key);
            return new CompilerInput(
                compilation.AssemblyName ?? "Tl.Generated.Analysis",
                csharp.LanguageVersion,
                csharp.DocumentationMode,
                csharp.Kind,
                symbols,
                options.AllowUnsafe,
                options.CheckOverflow,
                options.NullableContextOptions,
                references,
                key.ToString());
        }

        private static ReferenceInput? ReadReference(PortableExecutableReference reference, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = reference.FilePath ?? reference.Display;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;
            path = Path.GetFullPath(path);
            using var hash = SHA256.Create();
            using var stream = File.OpenRead(path);
            var contentHash = Hex(hash.ComputeHash(stream));
            var aliases = reference.Properties.Aliases;
            var key = new StringBuilder();
            Append(key, path);
            Append(key, contentHash);
            Append(key, reference.Properties.Kind.ToString());
            Append(key, reference.Properties.EmbedInteropTypes ? "true" : "false");
            foreach (var alias in aliases)
                Append(key, alias);
            return new ReferenceInput(path, aliases, reference.Properties.EmbedInteropTypes, reference.Properties.Kind, key.ToString());
        }
    }

    private sealed class AnalysisInput
    {
        private AnalysisInput(
            ImmutableArray<HeterogeneousTimeline> timelines,
            ImmutableArray<DeclarationDiagnostic> diagnostics,
            string key)
        {
            Timelines = timelines;
            Diagnostics = diagnostics;
            Key = key;
        }

        internal ImmutableArray<HeterogeneousTimeline> Timelines { get; }
        internal ImmutableArray<DeclarationDiagnostic> Diagnostics { get; }
        internal string Key { get; }

        internal static AnalysisInput Create(
            ImmutableArray<CandidateInput> candidates,
            CompilerInput compiler,
            CancellationToken cancellationToken)
        {
            var unique = candidates
                .OrderBy(static candidate => candidate.Identity, StringComparer.Ordinal)
                .GroupBy(static candidate => candidate.Identity, StringComparer.Ordinal)
                .Select(static group => group.First())
                .ToArray();
            var sources = unique
                .SelectMany(static candidate => candidate.Sources)
                .GroupBy(static source => source.Path + "\0" + source.Content, StringComparer.Ordinal)
                .Select(static group => group.First())
                .OrderBy(static source => source.Path, StringComparer.Ordinal)
                .ThenBy(static source => source.Content, StringComparer.Ordinal)
                .ToArray();
            var parseOptions = CSharpParseOptions.Default
                .WithLanguageVersion(compiler.LanguageVersion)
                .WithDocumentationMode(compiler.DocumentationMode)
                .WithKind(compiler.SourceKind)
                .WithPreprocessorSymbols(compiler.Symbols);
            var trees = sources.Select(source => CSharpSyntaxTree.ParseText(source.Content, parseOptions, source.Path)).ToArray();
            var references = compiler.References.Select(reference => MetadataReference.CreateFromFile(
                reference.Path,
                new MetadataReferenceProperties(reference.Kind, reference.Aliases, reference.EmbedInteropTypes)));
            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: compiler.AllowUnsafe,
                checkOverflow: compiler.CheckOverflow,
                nullableContextOptions: compiler.Nullable);
            var compilation = CSharpCompilation.Create(compiler.AssemblyName, trees, references, options);
            var result = HeterogeneousReader.ReadCompilation(compilation);
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
            var key = new StringBuilder();
            foreach (var timeline in timelines)
                Append(key, HeterogeneousEmitter.NormalizeSource(HeterogeneousEmitter.Emit(timeline)));
            foreach (var diagnostic in diagnostics)
                Append(key, DiagnosticKey(diagnostic));
            return new AnalysisInput(timelines, diagnostics, key.ToString());
        }
    }

    private sealed class CandidateComparer : IEqualityComparer<CandidateInput>
    {
        internal static readonly CandidateComparer Instance = new();
        public bool Equals(CandidateInput? x, CandidateInput? y) => ReferenceEquals(x, y) || x is not null && y is not null && x.Key == y.Key;
        public int GetHashCode(CandidateInput obj) => StringComparer.Ordinal.GetHashCode(obj.Key);
    }

    private sealed class CompilerInputComparer : IEqualityComparer<CompilerInput>
    {
        internal static readonly CompilerInputComparer Instance = new();
        public bool Equals(CompilerInput? x, CompilerInput? y) => ReferenceEquals(x, y) || x is not null && y is not null && x.Key == y.Key;
        public int GetHashCode(CompilerInput obj) => StringComparer.Ordinal.GetHashCode(obj.Key);
    }

    private sealed class AnalysisComparer : IEqualityComparer<AnalysisInput>
    {
        internal static readonly AnalysisComparer Instance = new();
        public bool Equals(AnalysisInput? x, AnalysisInput? y) => ReferenceEquals(x, y) || x is not null && y is not null && x.Key == y.Key;
        public int GetHashCode(AnalysisInput obj) => StringComparer.Ordinal.GetHashCode(obj.Key);
    }

    private static string DiagnosticKey(DeclarationDiagnostic diagnostic)
        => diagnostic.File + "\0" + diagnostic.Line + "\0" + diagnostic.Column + "\0" + diagnostic.EndLine + "\0" + diagnostic.EndColumn + "\0" + diagnostic.SpanStart + "\0" + diagnostic.SpanLength + "\0" + diagnostic.Code + "\0" + diagnostic.Message;

    private static string Hex(byte[] bytes)
    {
        var writer = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
            writer.Append(value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
        return writer.ToString();
    }

    private static void Append(StringBuilder writer, string value) => writer.Append(value.Length).Append(':').Append(value);
}
