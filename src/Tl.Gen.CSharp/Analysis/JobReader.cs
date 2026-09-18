using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

public static class JobReader
{
    private sealed record Contracts(INamedTypeSymbol Job, INamedTypeSymbol Frame, INamedTypeSymbol Blend);

    public static JobReadResult Read(CSharpCompilation compilation)
    {
        if (compilation is null)
            throw new ArgumentNullException(nameof(compilation));
        var errors = new List<DeclarationDiagnostic>();
        var contracts = Contract(compilation);
        if (contracts is null)
        {
            var site = compilation.SyntaxTrees.SelectMany(static tree => tree.GetRoot().DescendantNodes())
                .FirstOrDefault(static node => node.ToString().Contains("ITrack<", StringComparison.Ordinal));
            if (site is not null)
                Symbols.Error(errors, site, "TLGEN60", "The exact Tl job declaration contracts could not be resolved.");
            return new([], errors, []);
        }
        var entries = Symbols.Entries(compilation).OrderBy(static entry => Symbols.Name(entry.Type), StringComparer.Ordinal).ToArray();
        var consumers = new List<JobConsumer>();
        var pairs = new HashSet<string>(StringComparer.Ordinal);
        var reader = new Reader(compilation, errors);
        foreach (var entry in entries)
        {
            if (entry.Type.TypeKind == TypeKind.Interface) continue;
            var markers = entry.Type.AllInterfaces.Where(item => Symbols.Same(item.OriginalDefinition, contracts.Job)).ToArray();
            if (markers.Length == 0) continue;
            void Err(string msg) => Symbols.Error(errors, entry.Syntax, "TLGEN65", $"Job '{Symbols.Name(entry.Type)}' {msg}");
            if (entry.Type.IsAbstract) { Err("cannot be abstract."); continue; }
            if (entry.Type.Arity > 0 || entry.Type.TypeParameters.Length > 0) { Err("cannot be generic; open type parameters cannot be registered as timeline jobs."); continue; }
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
                if (ok && !track.AllInterfaces.Any(item => Symbols.Same(item.OriginalDefinition, contracts.Blend) && item.TypeArguments.Length == 1 && Symbols.Same(item.TypeArguments[0], clip))) { Err($"track type '{Symbols.Name(track)}' must implement Tl.IBlend<{Symbols.Name(clip)}>."); ok = false; }
                if (!ok) { valid = false; continue; }
                var definition = reader.Execute(entry.Type, contracts.Frame.Construct(track, clip), compilation.Assembly, entry.Syntax);
                if (definition is null) { valid = false; continue; }
                discovered.Add(new(Symbols.Name(track), Symbols.Name(clip), definition));
            }
            if (!valid) continue;
            foreach (var consumer in discovered.OrderBy(static item => item.TrackTypeName + "\0" + item.ClipTypeName, StringComparer.Ordinal))
                if (pairs.Add(PairKey(consumer.Job.TypeName, consumer.TrackTypeName, consumer.ClipTypeName)))
                    consumers.Add(consumer);
        }
        var bakes = BakeReader.Read(compilation, consumers, errors);
        return new(consumers, errors, bakes);
    }

    private sealed class Reader(CSharpCompilation compilation, List<DeclarationDiagnostic> errors)
    {
        public JobDefinition? Execute(INamedTypeSymbol type, ITypeSymbol frame, ISymbol owner, SyntaxNode site)
        {
            var candidates = type.GetMembers("Execute").OfType<IMethodSymbol>().Where(method => !method.IsImplicitlyDeclared
                && method.IsStatic && method.ReturnsVoid && method.Arity == 0 && method.MethodKind == MethodKind.Ordinary
                && method.Parameters.Length > 0 && method.Parameters[0].RefKind == RefKind.In
                && Symbols.Same(method.Parameters[0].Type, frame)
                && compilation.IsSymbolAccessibleWithin(method, owner)).ToArray();
            var method = candidates.Length == 1 ? candidates[0] : null;
            if (method is null)
            {
                Symbols.Error(errors, site, "TLGEN66", $"'{Symbols.Name(type)}' must declare one accessible static void Execute beginning with in {Symbols.Name(frame)}.");
                return null;
            }
            if (method.Parameters.Length > 5)
            {
                Symbols.Error(errors, Site(method.Parameters[5], site), "TLGEN68", $"'{Symbols.Name(type)}.Execute' declares {method.Parameters.Length - 1} gameplay parameters; the consumer ABI reserves 4 pointer slots per registered consumer, so a fifth parameter binds into the next consumer's slots; declare at most 4 gameplay parameters.");
                return null;
            }
            var slots = new List<TimelineSlot>();
            foreach (var parameter in method.Parameters.Skip(1))
            {
                if (parameter.RefKind is not (RefKind.In or RefKind.Ref) || parameter.IsOptional || parameter.IsParams || !parameter.Type.IsUnmanagedType)
                {
                    Symbols.Error(errors, Site(parameter, site), "TLGEN67", $"Every gameplay parameter of '{Symbols.Name(type)}.Execute' must be a required unmanaged in or ref parameter; out is unsupported.");
                    return null;
                }
                slots.Add(new(parameter.Name, Symbols.Name(parameter.Type), parameter.RefKind == RefKind.In ? SlotMode.Input : SlotMode.Reference));
            }
            return new(Symbols.Name(type), slots);
        }
    }

    private static Contracts? Contract(Compilation compilation)
    {
        string[] names = ["Tl.ITrack`2", "Tl.Frame`2", "Tl.IBlend`1"];
        var types = names.Select(compilation.GetTypeByMetadataName).ToArray();
        return types.Any(static type => type is null) ? null : new(types[0]!, types[1]!, types[2]!);
    }

    private static SyntaxNode Site(ISymbol symbol, SyntaxNode fallback) => symbol.DeclaringSyntaxReferences.Select(static reference => reference.GetSyntax()).FirstOrDefault() ?? fallback;
    private static string PairKey(string job, string track, string clip) => job + "\0" + track + "\0" + clip;
}
