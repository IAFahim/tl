namespace Tl;

public static class Timeline
{
    public static FrameQuery<TTrack, TClip> Query<TTrack, TClip>(in TimelineComponent component) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged => new(component);

    public static void Bake(ushort timeline)
        => Dispatch(timeline, ReadOnlySpan<ulong>.Empty, Array.Empty<object>());

    public static void Bake<T0>(ushort timeline, T0 context0)
        => Dispatch(timeline, [TypeKey<T0>.Value], [(object)context0!]);

    public static void Bake<T0, T1>(ushort timeline, T0 context0, T1 context1)
        => Dispatch(timeline, [TypeKey<T0>.Value, TypeKey<T1>.Value], [(object)context0!, (object)context1!]);

    public static void Bake<T0, T1, T2>(ushort timeline, T0 context0, T1 context1, T2 context2)
        => Dispatch(timeline, [TypeKey<T0>.Value, TypeKey<T1>.Value, TypeKey<T2>.Value], [(object)context0!, (object)context1!, (object)context2!]);

    public static void Bake<T0, T1, T2, T3>(ushort timeline, T0 context0, T1 context1, T2 context2, T3 context3)
        => Dispatch(timeline, [TypeKey<T0>.Value, TypeKey<T1>.Value, TypeKey<T2>.Value, TypeKey<T3>.Value], [(object)context0!, (object)context1!, (object)context2!, (object)context3!]);

    static unsafe void Dispatch(ushort timeline, ReadOnlySpan<ulong> present, object[] arguments)
    {
        var reference = TimelineTable.Reference(timeline);
        var pairs = reference.Pairs;
        Span<int> binding = stackalloc int[4];
        var count = (int)reference.PairCount;
        for (var pair = 0; pair < count; pair++)
        {
            var key = pairs[pair].Key;
            for (var entry = BakeTable.Head(key); entry >= 0; entry = BakeTable.EntryAt[entry].Next)
            {
                var bake = BakeTable.EntryAt + entry;
                if (!Satisfied(bake, present, binding)) continue;
                if (bake->ContextCount == 0)
                {
                    bake->Invoke(Array.Empty<object>());
                    continue;
                }
                var invoke = new object[bake->ContextCount];
                for (var context = 0; context < bake->ContextCount; context++) invoke[context] = arguments[binding[context]];
                bake->Invoke(invoke);
            }
        }
    }

    static unsafe bool Satisfied(BakeTable.Entry* bake, ReadOnlySpan<ulong> present, Span<int> binding)
    {
        for (var context = 0; context < bake->ContextCount; context++)
        {
            var bound = -1;
            for (var argument = 0; argument < present.Length; argument++)
                if (present[argument] == bake->Contexts[context])
                {
                    bound = argument;
                    break;
                }
            if (bound < 0) return false;
            binding[context] = bound;
        }
        return true;
    }
}
