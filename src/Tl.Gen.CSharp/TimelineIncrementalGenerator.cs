using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Tl.Gen.CSharp.Analysis;

namespace Tl.Gen.CSharp;

#if NETSTANDARD2_0
[Generator(LanguageNames.CSharp)]
#endif
public sealed class TimelineIncrementalGenerator : IIncrementalGenerator
{
    internal const string TypeStep = "Tl.Type";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax syntax && TypeAnalyzer.IsCandidate(syntax),
                static (syntax, cancellation) => TypeAnalyzer.Analyze(syntax.SemanticModel, (TypeDeclarationSyntax)syntax.Node, cancellation))
            .WithTrackingName(TypeStep);
        var analysis = types.Collect()
            .Select(static (models, cancellation) => Analyze(models, cancellation))
            .WithComparer(AnalysisComparer.Instance)
            .WithTrackingName("Tl.Analysis");
        context.RegisterSourceOutput(analysis, static (production, result) => Produce(production, result));
    }

    private static Analysis Analyze(ImmutableArray<TypeAnalysis?> models, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var model = JobReader.Merge(models.Where(static analysis => analysis is not null).Select(static analysis => analysis!).ToList());
        var artifacts = model.Diagnostics.Count == 0
            ? JobEmitter.Emit(model).OrderBy(static artifact => artifact.RelativePath, StringComparer.Ordinal).ToArray()
            : [];
        var fingerprint = string.Join("\0", model.Diagnostics.Select(DiagnosticFingerprint).Concat(artifacts.Select(static artifact => artifact.RelativePath + "\0" + artifact.Content)));
        return new Analysis(model.Diagnostics.ToArray(), artifacts, fingerprint);
    }

    private static void Produce(SourceProductionContext context, Analysis analysis)
    {
        foreach (var diagnostic in analysis.Diagnostics)
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(diagnostic.Code, "Invalid timeline declaration", "{0}", "Tl.Generation", DiagnosticSeverity.Error, true),
                Location(diagnostic), diagnostic.Message));
        foreach (var artifact in analysis.Artifacts)
            context.AddSource(artifact.RelativePath, SourceText.From(JobEmitter.Normalize(artifact.Content), new UTF8Encoding(false)));
    }

    internal static Location Location(DeclarationDiagnostic diagnostic)
        => diagnostic.SpanStart < 0
            ? Microsoft.CodeAnalysis.Location.None
            : Microsoft.CodeAnalysis.Location.Create(
                diagnostic.File,
                new TextSpan(diagnostic.SpanStart, diagnostic.SpanLength),
                new LinePositionSpan(
                    new LinePosition(diagnostic.Line - 1, diagnostic.Column - 1),
                    new LinePosition(diagnostic.EndLine - 1, diagnostic.EndColumn - 1)));

    private static string DiagnosticFingerprint(DeclarationDiagnostic diagnostic)
        => diagnostic.File + "\0" + diagnostic.Line + "\0" + diagnostic.Column + "\0" + diagnostic.Code + "\0" + diagnostic.Message;

    internal sealed record Analysis(
        IReadOnlyList<DeclarationDiagnostic> Diagnostics,
        IReadOnlyList<CompileArtifact> Artifacts,
        string Fingerprint);

    internal sealed class AnalysisComparer : IEqualityComparer<Analysis>
    {
        internal static readonly AnalysisComparer Instance = new();
        public bool Equals(Analysis? left, Analysis? right) => ReferenceEquals(left, right) || left is not null && right is not null && left.Fingerprint == right.Fingerprint;
        public int GetHashCode(Analysis value) => StringComparer.Ordinal.GetHashCode(value.Fingerprint);
    }
}
