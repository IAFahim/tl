using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;


internal sealed record TypeAnalysis(
    string TypeName,
    DeclarationDiagnostic? ContractSite,
    IReadOnlyList<JobConsumer> Consumers,
    IReadOnlyList<DeclarationDiagnostic> JobDiagnostics)
{
    public bool Equals(TypeAnalysis? other) => other is not null && Fingerprint == other.Fingerprint;
    public override int GetHashCode() => Fingerprint.GetHashCode();

    private string? _fingerprint;
    internal string Fingerprint => _fingerprint ??= Build();

    private string Build()
    {
        var builder = new StringBuilder(TypeName);
        Append(builder, ContractSite is null ? "" : Site(ContractSite));
        foreach (var consumer in Consumers)
        {
            Append(builder, consumer.TrackTypeName);
            Append(builder, consumer.ClipTypeName);
            Append(builder, consumer.Identity);
            Append(builder, consumer.Layout.ToString());
            Append(builder, string.Join("\0", consumer.LayoutTypes ?? []));
            Append(builder, consumer.Job.TypeName);
        }
        foreach (var diagnostic in JobDiagnostics)
            Append(builder, Diagnostic(diagnostic));
        return builder.ToString();

        static void Append(StringBuilder builder, string value) => builder.Append('\0').Append(value);
        static string Diagnostic(DeclarationDiagnostic diagnostic) => $"{diagnostic.File}\0{diagnostic.Line}\0{diagnostic.Column}\0{diagnostic.Code}\0{diagnostic.Message}\0{diagnostic.SpanStart}\0{diagnostic.SpanLength}\0{diagnostic.EndLine}\0{diagnostic.EndColumn}";
        static string Site(DeclarationDiagnostic site) => $"{site.File}\0{site.Line}\0{site.Column}\0{site.SpanStart}\0{site.SpanLength}\0{site.EndLine}\0{site.EndColumn}";
    }
}

internal static class TypeAnalyzer
{

    internal static bool IsCandidate(TypeDeclarationSyntax syntax)
        => syntax.BaseList is not null || syntax.Identifier.ValueText == "ITrack";

    internal static TypeAnalysis? Analyze(SemanticModel model, TypeDeclarationSyntax syntax, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (model.GetDeclaredSymbol(syntax) is not { } type)
            return null;
        var compilation = model.Compilation;
        var name = Symbols.Name(type);
        if (Contract(compilation) is not { } contracts)
            return new TypeAnalysis(name, ContractSite(syntax), [], []);
        if (type.TypeKind == TypeKind.Interface)
            return null;
        if (IsNewStyle(type))
            return new TypeAnalysis(name, null, [], []);
        var job = Job(type, compilation, contracts, syntax, name);
        return job is null
            ? null
            : new TypeAnalysis(name, null, job.Value.Consumers, job.Value.Diagnostics);
    }

    private static DeclarationDiagnostic? ContractSite(TypeDeclarationSyntax syntax)
    {
        foreach (var token in syntax.DescendantTokens())
            if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "ITrack")
                return Symbols.Site(syntax);
        return null;
    }

    private static (IReadOnlyList<JobConsumer> Consumers, IReadOnlyList<DeclarationDiagnostic> Diagnostics)? Job(
        INamedTypeSymbol type,
        Compilation compilation,
        (INamedTypeSymbol Job, INamedTypeSymbol Frame, INamedTypeSymbol Blend) contracts,
        SyntaxNode syntax,
        string name)
    {
        var markers = type.AllInterfaces.Where(item => Symbols.Same(item.OriginalDefinition, contracts.Job)).ToArray();
        if (markers.Length == 0)
            return null;
        var errors = new List<DeclarationDiagnostic>();
        void Err(string msg) => Symbols.Error(errors, syntax, "TLGEN65", $"Job '{name}' {msg}");
        if (type.IsAbstract) { Err("cannot be abstract."); return ([], errors); }
        if (type.Arity > 0 || type.TypeParameters.Length > 0) { Err("cannot be generic; open type parameters cannot be registered as timeline jobs."); return ([], errors); }
        var identity = compilation.AssemblyName + "\0" + name;
        var discovered = new List<JobConsumer>();
        var valid = true;
        foreach (var marker in markers)
        {
            var track = marker.TypeArguments[0];
            var clip = marker.TypeArguments[1];
            if (track.TypeKind == TypeKind.TypeParameter || clip.TypeKind == TypeKind.TypeParameter) { Err("cannot have open type parameters for its track or clip pairing."); valid = false; continue; }
            var ok = true;
            if (!track.IsUnmanagedType) { Err($"track type '{Symbols.Name(track)}' must be an unmanaged type."); ok = false; }
            if (!clip.IsUnmanagedType) { Err($"clip type '{Symbols.Name(clip)}' must be an unmanaged type."); ok = false; }
            if (ok && track is INamedTypeSymbol { IsReadOnly: false }) { Err($"track type '{Symbols.Name(track)}' must be a readonly struct; a mutating Blend would write interned asset bytes."); ok = false; }
            if (ok && !track.AllInterfaces.Any(item => Symbols.Same(item.OriginalDefinition, contracts.Blend) && item.TypeArguments.Length == 1 && Symbols.Same(item.TypeArguments[0], clip))) { Err($"track type '{Symbols.Name(track)}' must implement Tl.IBlend<{Symbols.Name(clip)}>."); ok = false; }
            if (!ok) { valid = false; continue; }
            discovered.Add(new(Symbols.Name(track), Symbols.Name(clip), new JobDefinition(name), identity, PairLayout.Of(track, clip), PairLayout.NestedTypes(track, clip)));
        }
        if (!valid)
            return ([], errors);
        return (discovered.OrderBy(static item => item.TrackTypeName + "\0" + item.ClipTypeName, StringComparer.Ordinal).ToArray(), errors);
    }

    internal static bool IsNewStyle(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol { IsStatic: true } attributed)
                foreach (var attribute in attributed.GetAttributes())
                    if (attribute.AttributeClass is { Name: "FeedsAttribute" or "PulseAttribute", ContainingNamespace: { Name: "Tl", ContainingNamespace.IsGlobalNamespace: true } })
                        return true;
            if (member is not IMethodSymbol method || !method.IsStatic || method.Arity != 0 || !method.ReturnsVoid)
                continue;
            if (method.Name is "ExecuteBatch" or "OnClipStart" or "OnClipEnd" or "OnMarker")
                return true;
            if (method.Name == "ExecuteActive")
                foreach (var parameter in method.Parameters)
                {
                    var origin = parameter.Type.OriginalDefinition;
                    if (origin is INamedTypeSymbol named
                        && named.ContainingNamespace?.ToDisplayString() == "Tl"
                        && named.Name is "Requests" or "Events" or "BatchItem")
                        return true;
                }
        }
        return false;
    }

    private static (INamedTypeSymbol Job, INamedTypeSymbol Frame, INamedTypeSymbol Blend)? Contract(Compilation compilation)
    {
        INamedTypeSymbol?[] types = [compilation.GetTypeByMetadataName("Tl.ITrack`2"), compilation.GetTypeByMetadataName("Tl.Frame`2"), compilation.GetTypeByMetadataName("Tl.IBlend`1")];
        return types.Any(static type => type is null) ? null : (types[0]!, types[1]!, types[2]!);
    }
}
