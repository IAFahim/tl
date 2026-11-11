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
        IncrementalValuesProvider<SystemTypeModel?> TypeAttr(string name, Func<GeneratorAttributeSyntaxContext, CancellationToken, SystemTypeModel?> turn)
            => context.SyntaxProvider.ForAttributeWithMetadataName(name, static (node, _) => node is TypeDeclarationSyntax, turn).WithTrackingName(TypeStep);
        IncrementalValuesProvider<SystemTypeModel?> MethodAttr(string name, Func<GeneratorAttributeSyntaxContext, CancellationToken, SystemTypeModel?> turn)
            => context.SyntaxProvider.ForAttributeWithMetadataName(name, static (node, _) => node is MethodDeclarationSyntax, turn).WithTrackingName(TypeStep);
        var kinds = TypeAttr("Tl.TlKindAttribute", static (ctx, ct) => SystemReader.KindOf(ctx, ct));
        var kindsLegacy = TypeAttr("Tl.TimelineKindAttribute", static (ctx, ct) => SystemReader.KindOf(ctx, ct));
        var systems = TypeAttr("Tl.TimelineSystemAttribute", static (ctx, ct) => SystemReader.SystemOf(ctx, ct));
        var states = TypeAttr("Tl.TlStateAttribute", static (ctx, ct) => SystemReader.StateOf(ctx, ct));
        var feeds = MethodAttr("Tl.FeedsAttribute", static (ctx, ct) => SystemReader.FeederOf(ctx, ct));
        var pulses = MethodAttr("Tl.PulseAttribute", static (ctx, ct) => SystemReader.FeederOf(ctx, ct));
        var tracks = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax syntax && syntax.BaseList is not null && syntax.BaseList.Types.Any(candidate => Base(candidate.Type) is "ITlTrack" or "ITrack" or "IMarker" or "ITlModule"),
                static (syntax, cancellation) => SystemReader.TrackOf(syntax, cancellation))
            .WithTrackingName(TypeStep);
        var models = kinds.Collect().Combine(kindsLegacy.Collect()).Combine(systems.Collect()).Combine(states.Collect())
            .Combine(feeds.Collect()).Combine(pulses.Collect()).Combine(tracks.Collect())
            .Select(static (tuple, cancellation) =>
            {
                cancellation.ThrowIfCancellationRequested();
                var a = tuple.Left.Left.Left.Left.Left;
                var b = tuple.Left.Left.Left.Left.Right;
                var c = tuple.Left.Left.Left.Right;
                var d = tuple.Left.Left.Right;
                var e = tuple.Left.Right;
                return a.Item1.AddRange(a.Item2).AddRange(b).AddRange(c).AddRange(d).AddRange(e).AddRange(tuple.Right);
            })
            .WithTrackingName("Tl.Models");
        var systemsOutput = models.Select(static (models, cancellation) => AnalyzeModels(models, cancellation))
            .WithComparer(AnalysisComparer.Instance)
            .WithTrackingName("Tl.Systems");
        context.RegisterSourceOutput(systemsOutput, static (production, result) => Produce(production, result));
    }

    static string Base(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
        GenericNameSyntax generic => generic.Identifier.Text,
        _ => "",
    };
    private static readonly Analysis Empty = new([], [], "");

    private static Analysis AnalyzeModels(ImmutableArray<SystemTypeModel?> models, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var ordered = models.Where(static model => model is not null).Select(static model => model!)
            .OrderBy(static model => model.TypeName, StringComparer.Ordinal).ThenBy(static model => (byte)model.Kind).ToList();
        if (ordered.Count == 0) return Empty;
        if (!ordered.Any(static model => model.Kind == SystemModelKind.Kind || model.Kind == SystemModelKind.Consumer && model.Events is not null)) return Empty;
        var assemblyName = ordered[0].AssemblyName;
        var id = string.Concat((assemblyName.Length == 0 ? "Game" : assemblyName).Select(static c => char.IsLetterOrDigit(c) ? c : '_'));
        if (ordered.Any(model => model.Kind == SystemModelKind.Module && model.TypeName == "global::TlSystems.TlModule_" + id)) return Empty;
        var read = SystemReader.AssignModels(ordered, id);
        var artifacts = SystemGeneration.ArtifactsFor(read.Assignments, read.Diagnostics, read.AutoSource, assemblyName, id);
        var fingerprint = string.Join("\0", read.Diagnostics.Select(DiagnosticFingerprint).Concat(artifacts.Select(static artifact => artifact.RelativePath + "\0" + artifact.Content)));
        return new Analysis(read.Diagnostics, artifacts, fingerprint);
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
                new DiagnosticDescriptor(diagnostic.Code, "Invalid timeline declaration", "{0}", "Tl.Generation", diagnostic.Code is "TLGEN98" or "TLGEN107" ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error, true),
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
