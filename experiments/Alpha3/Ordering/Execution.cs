using System;

namespace OperationOrderProof
{
    internal static class Selector
    {
        internal static ushort Select(Schedule schedule, Span<EntityState> entities, int direction)
        {
            ushort maximumCount = 0;
            for (var entity = 0; entity < entities.Length; entity++)
            {
                ref var state = ref entities[entity];
                ref readonly var asset = ref schedule.Assets[state.AssetIndex];
                if (direction == 0 || asset.Duration == 0
                    || direction > 0 && state.Position >= asset.Duration
                    || direction < 0 && state.Position == 0)
                {
                    state.Selection = default;
                    continue;
                }

                var frame = direction > 0 ? state.Position : state.Position - 1;
                ref readonly var slice = ref schedule.Frames[asset.FrameOffset + frame];
                var next = direction > 0 ? state.Position + 1 : state.Position - 1;
                state.Selection = new Selection(
                    slice.Offset,
                    next,
                    slice.Count,
                    direction > 0 ? (sbyte)1 : (sbyte)-1,
                    SelectionFlags.Advances);
                if (slice.Count > maximumCount)
                    maximumCount = slice.Count;
            }
            return maximumCount;
        }

        internal static void Commit(Span<EntityState> entities)
        {
            for (var entity = 0; entity < entities.Length; entity++)
            {
                ref var state = ref entities[entity];
                if (!state.Selection.Advances)
                    continue;
                state.Position = state.Selection.NextPosition;
                state.Selection = default;
            }
        }
    }

    internal static class StageDispatcher
    {
        internal static void Tick(
            Schedule schedule,
            Span<EntityState> entities,
            int direction,
            Trace trace,
            GlobalTrace global = null)
        {
            var stageCount = Selector.Select(schedule, entities, direction);
            for (var stage = 0; stage < stageCount; stage++)
            {
                Execute(schedule, entities, stage, OperationId.A, trace, global);
                Execute(schedule, entities, stage, OperationId.B, trace, global);
            }
            Selector.Commit(entities);
        }

        private static void Execute(
            Schedule schedule,
            Span<EntityState> entities,
            int stage,
            OperationId operation,
            Trace trace,
            GlobalTrace global)
        {
            for (var entity = 0; entity < entities.Length; entity++)
            {
                ref readonly var selection = ref entities[entity].Selection;
                if (!selection.Advances || stage >= selection.Count)
                    continue;
                ref readonly var occurrence = ref schedule.Get(in selection, stage);
                if (occurrence.Operation != operation)
                    continue;
                trace.Append(entity, occurrence.Token);
                global?.Append(entity, occurrence.Token);
            }
        }
    }

    internal static class ScalarOracle
    {
        internal static void Tick(
            Schedule schedule,
            Span<EntityState> entities,
            int direction,
            Trace trace,
            GlobalTrace global = null)
        {
            for (var entity = 0; entity < entities.Length; entity++)
            {
                ref var state = ref entities[entity];
                ref readonly var asset = ref schedule.Assets[state.AssetIndex];
                if (direction == 0 || asset.Duration == 0
                    || direction > 0 && state.Position >= asset.Duration
                    || direction < 0 && state.Position == 0)
                    continue;
                var frame = direction > 0 ? state.Position : state.Position - 1;
                ref readonly var slice = ref schedule.Frames[asset.FrameOffset + frame];
                for (var stage = 0; stage < slice.Count; stage++)
                {
                    var ordinal = direction > 0 ? stage : slice.Count - 1 - stage;
                    ref readonly var occurrence = ref schedule.Occurrences[slice.Offset + ordinal];
                    trace.Append(entity, occurrence.Token);
                    global?.Append(entity, occurrence.Token);
                }
                state.Position = direction > 0 ? state.Position + 1 : state.Position - 1;
                state.Selection = default;
            }
        }
    }

    internal sealed class Trace
    {
        private readonly ulong[] _tokens;
        private readonly ushort[] _counts;
        private readonly int _capacity;

        internal Trace(int entities, int capacity)
        {
            _tokens = new ulong[entities * capacity];
            _counts = new ushort[entities];
            _capacity = capacity;
        }

        internal ushort Count(int entity) => _counts[entity];

        internal ulong Get(int entity, int ordinal) => _tokens[entity * _capacity + ordinal];

        internal void Append(int entity, ulong token)
        {
            var count = _counts[entity];
            if (count >= _capacity)
                throw new InvalidOperationException("Trace capacity is a proof harness limit, not a scheduler queue.");
            _tokens[entity * _capacity + count] = token;
            _counts[entity] = (ushort)(count + 1);
        }

        internal void Clear() => Array.Clear(_counts);
    }

    internal sealed class GlobalTrace
    {
        private readonly ulong[] _tokens;
        private int _count;

        internal GlobalTrace(int capacity) => _tokens = new ulong[capacity];

        internal int Count => _count;

        internal ulong Get(int ordinal) => _tokens[ordinal];

        internal void Append(int entity, ulong occurrence)
            => _tokens[_count++] = (ulong)(uint)entity << 56 | occurrence;
    }
}
