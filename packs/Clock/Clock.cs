using System;

namespace Tl.Pack.Clock
{
    public struct FixedSteps
    {
        const long Second = 1_000_000_000L;
        readonly long _hz;
        readonly int _cap;
        long _owed;

        public FixedSteps(int hz, int maxStepsPerAdvance)
        {
            if (hz <= 0) throw new ArgumentOutOfRangeException(nameof(hz));
            if (maxStepsPerAdvance <= 0) throw new ArgumentOutOfRangeException(nameof(maxStepsPerAdvance));
            _hz = hz;
            _cap = maxStepsPerAdvance;
            _owed = 0;
            Dropped = 0;
        }

        public long Dropped { get; private set; }

        public double Alpha => (double)_owed / Second;

        public int Advance(double seconds) => Advance((long)Math.Round(seconds * Second));

        public int Advance(TimeSpan elapsed) => Advance(elapsed.Ticks * 100L);

        public int Advance(long elapsedNanoseconds)
        {
            if (elapsedNanoseconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedNanoseconds));
            if (_hz == 0) throw new InvalidOperationException("FixedSteps needs a rate; construct it with new FixedSteps(hz, maxStepsPerAdvance).");
            var whole = elapsedNanoseconds / Second;
            var units = _owed + elapsedNanoseconds % Second * _hz;
            var steps = units / Second;
            _owed = units - steps * Second;
            steps = whole > (long.MaxValue - steps) / _hz ? long.MaxValue : steps + whole * _hz;
            if (steps <= _cap) return (int)steps;
            var over = steps - _cap;
            Dropped = over > long.MaxValue - Dropped ? long.MaxValue : Dropped + over;
            return _cap;
        }

        public void Reset()
        {
            _owed = 0;
            Dropped = 0;
        }
    }
}
