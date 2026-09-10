using System;

namespace UnifiedTimelineProof
{
    public readonly struct TimelineAsset
    {
        internal readonly byte Kind;
        internal TimelineAsset(byte kind) => Kind = kind;
        public uint Duration => Kind == 0 ? 0u : Kind == 4 ? 1u : 4u;
        public bool Looping => Kind == 3;
        internal bool DamageOnly => Kind == 4;
    }

    public static class Attack { public static TimelineAsset Asset => new TimelineAsset(1); }
    public static class HeavyAttack { public static TimelineAsset Asset => new TimelineAsset(2); }
    public static class LoopingAttack { public static TimelineAsset Asset => new TimelineAsset(3); }
    public static class DamageOnlyAttack { public static TimelineAsset Asset => new TimelineAsset(4); }

    public readonly struct Playback
    {
        public readonly uint Position;
        internal Playback(uint position) => Position = position;
    }

    [Flags]
    internal enum SelectionFlags : byte { None = 0, Advance = 1, Reverse = 2, Damage = 4, Animation = 8 }

    public readonly struct Selection
    {
        internal readonly uint NextPosition;
        internal readonly SelectionFlags Flags;
        public readonly uint TimelineTick;
        public readonly uint GameTick;

        internal Selection(uint nextPosition, uint timelineTick, uint gameTick, SelectionFlags flags)
        {
            NextPosition = nextPosition;
            TimelineTick = timelineTick;
            GameTick = gameTick;
            Flags = flags;
        }

        public bool Advances => (Flags & SelectionFlags.Advance) != 0;
        public bool Reverse => (Flags & SelectionFlags.Reverse) != 0;
        public bool Damage => (Flags & SelectionFlags.Damage) != 0;
        public bool Animation => (Flags & SelectionFlags.Animation) != 0;
    }

    public readonly struct TimelineState
    {
        public readonly TimelineAsset Asset;
        public readonly Playback Playback;
        public readonly Selection Selection;

        public TimelineState(TimelineAsset asset) : this(asset, default, default) { }

        internal TimelineState(TimelineAsset asset, Playback playback, Selection selection)
        {
            Asset = asset;
            Playback = playback;
            Selection = selection;
        }
    }

    public static class Timeline
    {
        public static TimelineState Select(in TimelineState timeline, uint gameTick, int direction)
        {
            var duration = timeline.Asset.Duration;
            if (duration == 0)
                return new TimelineState(timeline.Asset);
            var position = timeline.Asset.Looping
                ? timeline.Playback.Position % duration
                : Math.Min(timeline.Playback.Position, duration);
            var unchanged = new TimelineState(timeline.Asset, new Playback(position), default);
            if (direction == 0 || !timeline.Asset.Looping && (direction > 0 ? position == duration : position == 0))
                return unchanged;
            var frameTick = direction > 0 ? position : position == 0 ? duration - 1 : position - 1;
            var next = direction < 0 ? frameTick : frameTick + 1;
            if (timeline.Asset.Looping && next == duration)
                next = 0;
            var flags = SelectionFlags.Advance | (timeline.Asset.DamageOnly ? SelectionFlags.Damage : SelectionFlags.Animation);
            if (direction < 0)
                flags |= SelectionFlags.Reverse;
            if (!timeline.Asset.DamageOnly && frameTick == 2)
                flags |= SelectionFlags.Damage;
            return new TimelineState(timeline.Asset, new Playback(position), new Selection(next, frameTick, gameTick, flags));
        }

        public static TimelineState Complete(in TimelineState timeline)
            => timeline.Selection.Advances
                ? new TimelineState(timeline.Asset, new Playback(timeline.Selection.NextPosition), default)
                : timeline;

        internal static TimelineState ClearSelection(in TimelineState timeline)
            => new TimelineState(timeline.Asset, timeline.Playback, default);
    }
}
