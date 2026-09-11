using System;
using System.Runtime.InteropServices;

namespace UnifiedTimelineProof
{
    public readonly ref struct CombatQuery
    {
        private readonly Span<TimelineState> _timelines;
        private readonly ReadOnlySpan<Resistance> _resistance;
        private readonly Span<Health> _health;
        private readonly Span<Pose> _poses;

        public CombatQuery(Span<TimelineState> timelines, ReadOnlySpan<Resistance> resistance, Span<Health> health, Span<Pose> poses)
        {
            if (resistance.Length != timelines.Length || health.Length != timelines.Length || poses.Length != timelines.Length)
                throw new ArgumentException("Component columns must describe the same number of entities.");
            var states = MemoryMarshal.AsBytes(timelines);
            var inputs = MemoryMarshal.AsBytes(resistance);
            var outputs = MemoryMarshal.AsBytes(health);
            var positions = MemoryMarshal.AsBytes(poses);
            if (states.Overlaps(inputs) || states.Overlaps(outputs) || states.Overlaps(positions)
                || inputs.Overlaps(outputs) || inputs.Overlaps(positions) || outputs.Overlaps(positions))
                throw new ArgumentException("Independent component columns must not overlap in memory.");
            _timelines = timelines;
            _resistance = resistance;
            _health = health;
            _poses = poses;
        }

        public void Tick(uint gameTick, int delta = 1)
        {
            if (_timelines.IsEmpty)
                return;
            var direction = delta < 0 ? -1 : 1;
            for (var remaining = delta; remaining != 0; remaining -= direction)
            {
                if (direction < 0)
                    gameTick = unchecked(gameTick - 1);
                var advances = false;
                for (var entity = 0; entity < _timelines.Length; entity++)
                {
                    _timelines[entity] = Timeline.Select(in _timelines[entity], gameTick, direction);
                    advances |= _timelines[entity].Selection.Advances;
                }
                if (!advances)
                    break;
                if (direction > 0)
                {
                    Damage();
                    Animation();
                }
                else
                {
                    Animation();
                    Damage();
                }
                for (var entity = 0; entity < _timelines.Length; entity++)
                    _timelines[entity] = Timeline.Complete(in _timelines[entity]);
                if (direction > 0)
                    gameTick = unchecked(gameTick + 1);
            }
        }

        private void Damage()
        {
            for (var entity = 0; entity < _timelines.Length; entity++)
                DamageJob.Execute(in _timelines[entity], in _resistance[entity], ref _health[entity]);
        }

        private void Animation()
        {
            for (var entity = 0; entity < _timelines.Length; entity++)
                AnimationJob.Execute(in _timelines[entity], in _health[entity], ref _poses[entity]);
        }
    }
}
