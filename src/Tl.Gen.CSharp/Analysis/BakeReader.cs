using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

public static class BakeReader
{
    private sealed record Entry(INamedTypeSymbol Type, TypeDeclarationSyntax Syntax);

    public static IReadOnlyList<BakeDeclaration> Read(CSharpCompilation compilation, IReadOnlyList<JobConsumer> consumers, List<DeclarationDiagnostic> errors)
    {
        if (compilation is null) throw new ArgumentNullException(nameof(compilation));
        var markers = Markers(compilation);
        if (markers is null) return [];
        var pairings = new Dictionary<string, List<JobConsumer>>(StringComparer.Ordinal);
        foreach (var consumer in consumers)
        {
            if (!pairings.TryGetValue(consumer.Job.TypeName, out var list)) pairings[consumer.Job.TypeName] = list = [];
            list.Add(consumer);
        }
        var bakes = new List<BakeDeclaration>();
        foreach (var entry in Entries(compilation).OrderBy(static entry => Name(entry.Type), StringComparer.Ordinal))
        {
            if (entry.Type.TypeKind == TypeKind.Interface) continue;
            var declared = entry.Type.AllInterfaces.Where(item => markers.Any(marker => Same(item.OriginalDefinition, marker)))
                .OrderBy(static item => string.Join("\0", item.TypeArguments.Select(static argument => Name(argument))), StringComparer.Ordinal).ToArray();
            if (declared.Length == 0) continue;
            void Err(string message) => Error(errors, entry.Syntax, "TLGEN70", $"Bake '{Name(entry.Type)}' {message}");
            if (entry.Type.IsAbstract) { Err("cannot be abstract."); continue; }
            if (entry.Type.Arity > 0 || entry.Type.TypeParameters.Length > 0) { Err("cannot be generic; open type parameters cannot be registered as bakes."); continue; }
            if (entry.Type.TypeKind != TypeKind.Struct) { Err("must be a struct; bakes are declared on structs."); continue; }
            foreach (var marker in declared)
            {
                var consumer = marker.TypeArguments[0];
                var contexts = marker.TypeArguments.Skip(1).ToArray();
                if (consumer.TypeKind == TypeKind.TypeParameter || contexts.Any(static context => context.TypeKind == TypeKind.TypeParameter))
                {
                    Err("cannot have open type parameters for its consumer or context types.");
                    continue;
                }
                if (!pairings.TryGetValue(Name(consumer), out var pairs))
                {
                    Error(errors, entry.Syntax, "TLGEN72", $"Bake '{Name(entry.Type)}' binds consumer '{Name(consumer)}', which is not a registered Tl.ITrack consumer; declare the consumer with its ITrack pairing before binding a bake to it.");
                    continue;
                }
                var accessible = true;
                foreach (var context in contexts)
                    if (!compilation.IsSymbolAccessibleWithin(context, compilation.Assembly))
                    {
                        Error(errors, entry.Syntax, "TLGEN73", $"Bake '{Name(entry.Type)}' context type '{Name(context)}' must be accessible from this compilation; the generated binding references it from generated source.");
                        accessible = false;
                    }
                if (!accessible) continue;
                if (!ReadMethod(entry.Type, consumer, contexts, compilation, entry.Syntax, errors)) continue;
                bakes.Add(new(Name(entry.Type), Name(consumer), contexts.Select(Name).ToArray(), pairs));
            }
        }
        return bakes
            .OrderBy(static bake => bake.ConsumerTypeName, StringComparer.Ordinal)
            .ThenBy(static bake => string.Join("\0", bake.ContextTypeNames), StringComparer.Ordinal)
            .ThenBy(static bake => bake.TypeName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool ReadMethod(INamedTypeSymbol type, ITypeSymbol consumer, ITypeSymbol[] contexts, CSharpCompilation compilation, SyntaxNode site, List<DeclarationDiagnostic> errors)
    {
        var candidates = type.GetMembers("Bake").OfType<IMethodSymbol>().Where(method => !method.IsImplicitlyDeclared
            && method.IsStatic && method.ReturnsVoid && method.Arity == 0 && method.MethodKind == MethodKind.Ordinary
            && method.Parameters.Length == contexts.Length + 1
            && compilation.IsSymbolAccessibleWithin(method, compilation.Assembly)
            && method.Parameters[0].RefKind == RefKind.None && Same(method.Parameters[0].Type, consumer)
            && method.Parameters.Skip(1).Select(static parameter => parameter.Type).SequenceEqual(contexts, SymbolEqualityComparer.Default)
            && method.Parameters.All(static parameter => parameter.RefKind == RefKind.None)).ToArray();
        if (candidates.Length == 1) return true;
        Error(errors, site, "TLGEN71", $"Bake '{Name(type)}' must declare one accessible static void Bake whose first parameter is the consumer type '{Name(consumer)}' by value, followed by one by-value parameter per declared context type in order.");
        return false;
    }

    private static INamedTypeSymbol[]? Markers(Compilation compilation)
    {
        var markers = Enumerable.Range(1, 5).Select(arity => compilation.GetTypeByMetadataName($"Tl.IBake`{arity}")).ToArray();
        return markers.Any(static marker => marker is null) ? null : markers.Select(static marker => marker!).ToArray();
    }

    private static IReadOnlyList<Entry> Entries(CSharpCompilation compilation)
    {
        var result = new List<Entry>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var tree in compilation.SyntaxTrees)
        foreach (var syntax in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            if (compilation.GetSemanticModel(tree).GetDeclaredSymbol(syntax) is { } type && seen.Add(type))
                result.Add(new(type, syntax));
        return result;
    }

    private static bool Same(ISymbol? left, ISymbol? right) => SymbolEqualityComparer.Default.Equals(left, right);
    private static string Name(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    private static void Error(ICollection<DeclarationDiagnostic> errors, SyntaxNode node, string code, string message)
    {
        var span = node.GetLocation().GetLineSpan();
        errors.Add(new(span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, code, message)
        {
            SpanStart = node.SpanStart, SpanLength = node.Span.Length,
            EndLine = span.EndLinePosition.Line + 1, EndColumn = span.EndLinePosition.Character + 1,
        });
    }
}
