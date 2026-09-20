using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

public static class BakeReader
{
    public static IReadOnlyList<BakeDeclaration> Read(CSharpCompilation compilation, IReadOnlyList<JobConsumer> consumers, List<DeclarationDiagnostic> errors)
    {
        if (compilation is null) throw new ArgumentNullException(nameof(compilation));
        var marker = compilation.GetTypeByMetadataName("Tl.IBake`1");
        if (marker is null) return [];
        var pairings = new Dictionary<string, List<JobConsumer>>(StringComparer.Ordinal);
        foreach (var consumer in consumers)
        {
            if (!pairings.TryGetValue(consumer.Job.TypeName, out var list)) pairings[consumer.Job.TypeName] = list = [];
            list.Add(consumer);
        }
        var discovered = new List<(BakeDeclaration Bake, SyntaxNode Syntax, string Signature)>();
        foreach (var entry in Symbols.Entries(compilation).OrderBy(static entry => Symbols.Name(entry.Type), StringComparer.Ordinal))
        {
            if (entry.Type.TypeKind == TypeKind.Interface) continue;
            var declared = entry.Type.AllInterfaces.Where(item => Symbols.Same(item.OriginalDefinition, marker))
                .OrderBy(static item => Symbols.Name(item.TypeArguments[0]), StringComparer.Ordinal).ToArray();
            if (declared.Length == 0) continue;
            void Err(string message) => Symbols.Error(errors, entry.Syntax, "TLGEN70", $"Bake '{Symbols.Name(entry.Type)}' {message}");
            if (entry.Type.IsAbstract) { Err("cannot be abstract."); continue; }
            if (entry.Type.Arity > 0 || entry.Type.TypeParameters.Length > 0) { Err("cannot be generic; open type parameters cannot be registered as bakes."); continue; }
            if (entry.Type.TypeKind != TypeKind.Struct) { Err("must be a struct; bakes are declared on structs."); continue; }
            foreach (var instance in declared)
            {
                var consumer = instance.TypeArguments[0];
                if (consumer.TypeKind == TypeKind.TypeParameter)
                {
                    Err("cannot have an open type parameter as its consumer type.");
                    continue;
                }
                if (!pairings.TryGetValue(Symbols.Name(consumer), out var pairs))
                {
                    Symbols.Error(errors, entry.Syntax, "TLGEN72", $"Bake '{Symbols.Name(entry.Type)}' binds consumer '{Symbols.Name(consumer)}', which is not a registered Tl.ITrack consumer; declare the consumer with its ITrack pairing before binding a bake to it.");
                    continue;
                }
                if (!TryReadMethod(entry.Type, consumer, compilation, entry.Syntax, errors, out var method)) continue;
                var parameters = new List<BakeParameter>();
                var accessible = true;
                foreach (var parameter in method.Parameters)
                {
                    if (!compilation.IsSymbolAccessibleWithin(parameter.Type, compilation.Assembly))
                    {
                        Symbols.Error(errors, Site(parameter, entry.Syntax), "TLGEN73", $"Bake '{Symbols.Name(entry.Type)}' parameter type '{Symbols.Name(parameter.Type)}' must be accessible from this compilation; the generated binding references it from generated source.");
                        accessible = false;
                        continue;
                    }
                    parameters.Add(new(Symbols.Name(parameter.Type), parameter.RefKind switch
                    {
                        RefKind.In => BakeModifier.In,
                        RefKind.Ref => BakeModifier.Ref,
                        _ => BakeModifier.Value,
                    }, Symbols.Same(parameter.Type, consumer)));
                }
                if (!accessible) continue;
                discovered.Add((new BakeDeclaration(Symbols.Name(entry.Type), Symbols.Name(consumer), parameters, pairs), entry.Syntax, Signature(method)));
            }
        }
        var chains = new Dictionary<string, (BakeDeclaration Bake, string Signature)>(StringComparer.Ordinal);
        var accepted = new List<(BakeDeclaration Bake, SyntaxNode Syntax, string Signature)>();
        foreach (var item in discovered)
        {
            var conflict = false;
            foreach (var pair in item.Bake.Pairs)
            {
                var key = pair.TrackTypeName + "\0" + pair.ClipTypeName;
                if (!chains.TryGetValue(key, out var first))
                    chains[key] = (item.Bake, item.Signature);
                else if (first.Bake.SignatureKey != item.Bake.SignatureKey)
                {
                    Symbols.Error(errors, item.Syntax, "TLGEN74", $"Bakes '{first.Bake.TypeName}' and '{item.Bake.TypeName}' bind pair ({pair.TrackTypeName}, {pair.ClipTypeName}) with different bake parameter lists: '{first.Signature}' and '{item.Signature}'; every bake registered to one pair must declare an identical parameter list.");
                    conflict = true;
                }
            }
            if (!conflict) accepted.Add(item);
        }
        return accepted
            .OrderBy(static item => item.Bake.ConsumerTypeName, StringComparer.Ordinal)
            .ThenBy(static item => item.Bake.SignatureKey, StringComparer.Ordinal)
            .ThenBy(static item => item.Bake.TypeName, StringComparer.Ordinal)
            .Select(static item => item.Bake)
            .ToArray();
    }

    private static bool TryReadMethod(INamedTypeSymbol type, ITypeSymbol consumer, CSharpCompilation compilation, SyntaxNode site, List<DeclarationDiagnostic> errors, out IMethodSymbol method)
    {
        var candidates = type.GetMembers("Bake").OfType<IMethodSymbol>().Where(candidate => !candidate.IsImplicitlyDeclared
            && candidate.IsStatic && candidate.ReturnsVoid && candidate.Arity == 0 && candidate.MethodKind == MethodKind.Ordinary
            && compilation.IsSymbolAccessibleWithin(candidate, compilation.Assembly)
            && candidate.Parameters.All(static parameter => parameter.RefKind != RefKind.Out && !parameter.IsOptional && !parameter.IsParams)).ToArray();
        if (candidates.Length == 1)
        {
            method = candidates[0];
            return true;
        }
        Symbols.Error(errors, site, "TLGEN71", $"Bake '{Symbols.Name(type)}' must declare exactly one accessible static void Bake; every parameter may be by value, in, or ref of any type (including Span<T> and ReadOnlySpan<T>), and a parameter whose type is exactly the consumer type '{Symbols.Name(consumer)}' is bound to default(TConsumer), because consumers are static and the parameter participates only in the per-pair bake signature match; out, optional, and params parameters are unsupported.");
        method = null!;
        return false;
    }

    private static SyntaxNode Site(ISymbol symbol, SyntaxNode fallback)
        => symbol.DeclaringSyntaxReferences.Select(static reference => reference.GetSyntax()).FirstOrDefault() ?? fallback;

    private static string Signature(IMethodSymbol method)
        => $"{Symbols.Name(method.ContainingType)}.Bake({string.Join(", ", method.Parameters.Select(Format))})";

    private static string Format(IParameterSymbol parameter)
    {
        var modifier = parameter.RefKind == RefKind.In ? "in " : parameter.RefKind == RefKind.Ref ? "ref " : "";
        var name = string.IsNullOrEmpty(parameter.Name) ? "" : " " + parameter.Name;
        return modifier + Symbols.Name(parameter.Type) + name;
    }
}
