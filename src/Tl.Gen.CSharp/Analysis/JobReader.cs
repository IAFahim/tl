using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

public static class JobReader
{
    public static JobReadResult Read(CSharpCompilation compilation)
    {
        if (compilation is null)
            throw new ArgumentNullException(nameof(compilation));
        var errors = new List<DeclarationDiagnostic>();
        if (Contract(compilation) is not { } contracts)
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
                if (pairs.Add(consumer.Job.TypeName + "\0" + consumer.TrackTypeName + "\0" + consumer.ClipTypeName))
                    consumers.Add(consumer);
        }
        var bakes = BakeReader.Read(compilation, consumers, errors);
        return new(consumers, errors, bakes);
    }

    private sealed class Reader(CSharpCompilation compilation, List<DeclarationDiagnostic> errors)
    {
        public JobDefinition? Execute(INamedTypeSymbol type, ITypeSymbol frame, ISymbol owner, SyntaxNode site)
        {
            var name = Symbols.Name(type);
            var frameName = Symbols.Name(frame);
            var memos = Candidates(type, "OnMemo", frame, owner);
            var actives = Candidates(type, "OnActive", frame, owner);
            if (memos.Length > 1 || actives.Length > 1)
                return Err(site, "TLGEN66", $"'{name}' declares multiple OnMemo or OnActive.");
            var memo = memos.Length == 1 ? memos[0] : null;
            var active = actives.Length == 1 ? actives[0] : null;
            if (memo is null && active is null)
                return Err(site, "TLGEN66", $"'{name}' needs one static void OnMemo(in {frameName}) or OnActive.");

            var slots = new List<TimelineSlot>();
            if (memo is not null)
            {
                if (!Framed(memo, frame))
                    return Err(Site(memo, site), "TLGEN75", $"'{name}.OnMemo' must begin with in {frameName}.");
                foreach (var p in memo.Parameters.Skip(1))
                {
                    if (p.IsOptional || p.IsParams || !p.Type.IsUnmanagedType || p.RefKind is not (RefKind.Ref or RefKind.Out))
                        return Err(Site(p, site), "TLGEN76", $"'{name}.OnMemo' runs at fold; only unmanaged 'out'/'ref' results exist — 'in' has no caller.");
                    var typeName = Symbols.Name(p.Type);
                    var size = ResultSize(p.Type);
                    if (size == 0 || slots.Count >= 4)
                        return Err(Site(p, site), "TLGEN79", $"'{name}.OnMemo' >4-byte/5+ results — pending.");
                    slots.Add(new(p.Name, typeName, p.RefKind == RefKind.Out ? SlotMode.Output : SlotMode.Reference, size));
                }
                if (slots.Count == 0)
                    return Err(Site(memo, site), "TLGEN76", $"'{name}.OnMemo' produces no result; dispatch-only is OnActive.");
            }

            var dispatch = false;
            var liveFrame = false;
            List<TimelineSlot>? live = null;
            if (active is not null)
            {
                var framed = Framed(active, frame);
                var start = framed ? 1 : 0;
                var gameplay = active.Parameters.Length - start;
                var p1 = framed && active.Parameters.Length > 1 ? active.Parameters[1] : null;
                var sugar = p1 is not null && memo is null && gameplay == 1 && p1.RefKind is RefKind.Ref
                    && p1.Type.IsUnmanagedType && !p1.IsOptional && !p1.IsParams;
                if (sugar)
                {
                    var only = active.Parameters[1];
                    slots.Add(new(only.Name, Symbols.Name(only.Type), SlotMode.Reference));
                }
                else if (gameplay == 0)
                {
                    dispatch = true;
                    liveFrame = framed;
                }
                else
                {
                    dispatch = true;
                    liveFrame = framed;
                    live = [];
                    var outs = 0;
                    for (var i = 0; i < slots.Count; i++) if (slots[i].Mode == SlotMode.Output) outs++;
                    var feed = 0;
                    var index = start;
                    if (memo is not null)
                        while (index < active.Parameters.Length && feed < outs && active.Parameters[index].RefKind is RefKind.In
                            && active.Parameters[index].Type.IsUnmanagedType && !active.Parameters[index].IsOptional && !active.Parameters[index].IsParams
                            && Symbols.Name(active.Parameters[index].Type) == ResultName(feed))
                        {
                            var fed = active.Parameters[index++];
                            live.Add(new(fed.Name, Symbols.Name(fed.Type), SlotMode.MemoFeed));
                            feed++;
                        }
                    for (; index < active.Parameters.Length; index++)
                    {
                        var p = active.Parameters[index];
                        if (p.IsOptional || p.IsParams || !p.Type.IsUnmanagedType || p.RefKind is not (RefKind.In or RefKind.Ref))
                            return Err(Site(p, site), "TLGEN78", $"'{name}.OnActive' columns must be required unmanaged 'in'/'ref'; '{Symbols.Name(p.Type)} {p.Name}' is not.");
                        live.Add(new(p.Name, Symbols.Name(p.Type), p.RefKind == RefKind.Ref ? SlotMode.Reference : SlotMode.Input));
                    }
                    if (live.Count > 4)
                        return Err(Site(active.Parameters[start + 4], site), "TLGEN68", $"'{name}.OnActive' declares {gameplay} gameplay parameters; the consumer ABI reserves 4 pointer slots per registered consumer, so a fifth parameter binds into the next consumer's slots; declare at most 4 gameplay parameters.");
                }
            }

            return new(name, slots, dispatch, liveFrame, memo is not null, live);

            string ResultName(int ordinal)
            {
                var seen = -1;
                for (var i = 0; i < slots.Count; i++)
                    if (slots[i].Mode == SlotMode.Output && ++seen == ordinal) return slots[i].TypeName;
                return "";
            }
        }

        private JobDefinition? Err(SyntaxNode site, string code, string message)
        {
            Symbols.Error(errors, site, code, message);
            return null;
        }

        private static byte ResultSize(ITypeSymbol t)
            => t switch
            {
                INamedTypeSymbol { EnumUnderlyingType: { } u } => ResultSize(u),
                { SpecialType: SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte } => 1,
                { SpecialType: SpecialType.System_Char or SpecialType.System_Int16 or SpecialType.System_UInt16 } => 2,
                { SpecialType: SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single } => 4,
                _ => 0,
            };

        private static bool Framed(IMethodSymbol method, ITypeSymbol frame)
            => method.Parameters.Length > 0 && method.Parameters[0].RefKind == RefKind.In && Symbols.Same(method.Parameters[0].Type, frame);

        private IMethodSymbol[] Candidates(INamedTypeSymbol type, string name, ITypeSymbol frame, ISymbol owner)
            => type.GetMembers(name).OfType<IMethodSymbol>().Where(m => !m.IsImplicitlyDeclared
                && m.IsStatic && m.ReturnsVoid && m.Arity == 0
                && compilation.IsSymbolAccessibleWithin(m, owner)
                && (m.Parameters.Length == 0
                    || !Symbols.Same(m.Parameters[0].Type.OriginalDefinition, frame.OriginalDefinition)
                    || Symbols.Same(m.Parameters[0].Type, frame))).ToArray();
    }

    private static (INamedTypeSymbol Job, INamedTypeSymbol Frame, INamedTypeSymbol Blend)? Contract(Compilation compilation)
    {
        var types = new INamedTypeSymbol?[] { compilation.GetTypeByMetadataName("Tl.ITrack`2"), compilation.GetTypeByMetadataName("Tl.Frame`2"), compilation.GetTypeByMetadataName("Tl.IBlend`1") };
        return types.Any(static type => type is null) ? null : (types[0]!, types[1]!, types[2]!);
    }

    private static SyntaxNode Site(ISymbol symbol, SyntaxNode fallback) => symbol.DeclaringSyntaxReferences.Select(static reference => reference.GetSyntax()).FirstOrDefault() ?? fallback;
}
