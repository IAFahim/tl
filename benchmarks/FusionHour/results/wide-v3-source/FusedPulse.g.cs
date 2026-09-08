#nullable enable
using System;
using System.Runtime.CompilerServices;
using Tl;

namespace Tl.FusionExperiment;

public static class FusedPulse
{
    public const uint Duration = 600u;
    public const bool Loops = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Start(uint tick = 0)
        => Mint(tick, 0, PlaybackFlags.Started);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Stop(in Playback playback)
    {
        if (!playback.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Cannot stop a playback that was never started.");
        return Mint(playback.Tick, playback.Cycles, playback.Flags | PlaybackFlags.Stopped);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Forward(in Playback from, ref float sum, uint tick)
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        return ForwardOne(in from, ref sum, tick);
    }

    public static Playback Forward(in Playback from, ref float sum, ReadOnlySpan<uint> ticks)
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        if (ticks.IsEmpty)
            return from;
        var stateTick = from.Tick;
        var stateCycles = from.Cycles;
        var previousEffective = stateTick % Duration;
        var previousQuotient = stateTick / Duration;
        var index = 0;
        var effective = ticks[0] % Duration;
        while ((uint)index < (uint)ticks.Length)
        {
            if (effective < 47u)
            {
                if (effective < 11u)
                {
                    if (effective < 3u)
                    {
                        while ((uint)index < (uint)ticks.Length && effective >= 0u && effective < 3u)
                        {
                            var tick = ticks[index];
                            var quotient = tick / Duration;
                            uint cycles;
                            if (tick >= stateTick)
                                cycles = quotient - previousQuotient;
                            else
                                cycles = effective < previousEffective ? 1u : 0u;
                            if (cycles > ushort.MaxValue - stateCycles)
                                throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                            stateCycles = (ushort)(stateCycles + cycles);
                            sum += 1f;
                            stateTick = tick;
                            previousEffective = effective;
                            previousQuotient = quotient;
                            index++;
                            if ((uint)index < (uint)ticks.Length)
                                effective = ticks[index] % Duration;
                        }
                    }
                    else
                    {
                        if (effective < 7u)
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 3u && effective < 7u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick >= stateTick)
                                    cycles = quotient - previousQuotient;
                                else
                                    cycles = effective < previousEffective ? 1u : 0u;
                                if (cycles > ushort.MaxValue - stateCycles)
                                    throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                                stateCycles = (ushort)(stateCycles + cycles);
                                sum += 1f;
                                var factor_1_1 = (effective - 3u) / 3f;
                                var difference_1_1 = 3f - 2f;
                                var blended_1_1 = 2f + difference_1_1 * factor_1_1;
                                sum += blended_1_1;
                                sum += 5f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                        else
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 7u && effective < 11u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick >= stateTick)
                                    cycles = quotient - previousQuotient;
                                else
                                    cycles = effective < previousEffective ? 1u : 0u;
                                if (cycles > ushort.MaxValue - stateCycles)
                                    throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                                stateCycles = (ushort)(stateCycles + cycles);
                                sum += 3f;
                                sum += 5f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                    }
                }
                else
                {
                    if (effective < 18u)
                    {
                        while ((uint)index < (uint)ticks.Length && effective >= 11u && effective < 18u)
                        {
                            var tick = ticks[index];
                            var quotient = tick / Duration;
                            uint cycles;
                            if (tick >= stateTick)
                                cycles = quotient - previousQuotient;
                            else
                                cycles = effective < previousEffective ? 1u : 0u;
                            if (cycles > ushort.MaxValue - stateCycles)
                                throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                            stateCycles = (ushort)(stateCycles + cycles);
                            stateTick = tick;
                            previousEffective = effective;
                            previousQuotient = quotient;
                            index++;
                            if ((uint)index < (uint)ticks.Length)
                                effective = ticks[index] % Duration;
                        }
                    }
                    else
                    {
                        if (effective < 29u)
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 18u && effective < 29u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick >= stateTick)
                                    cycles = quotient - previousQuotient;
                                else
                                    cycles = effective < previousEffective ? 1u : 0u;
                                if (cycles > ushort.MaxValue - stateCycles)
                                    throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                                stateCycles = (ushort)(stateCycles + cycles);
                                sum += 8f;
                                sum += 5f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                        else
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 29u && effective < 47u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick >= stateTick)
                                    cycles = quotient - previousQuotient;
                                else
                                    cycles = effective < previousEffective ? 1u : 0u;
                                if (cycles > ushort.MaxValue - stateCycles)
                                    throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                                stateCycles = (ushort)(stateCycles + cycles);
                                var factor_5_0 = (effective - 29u) / 17f;
                                var difference_5_0 = 21f - 13f;
                                var blended_5_0 = 13f + difference_5_0 * factor_5_0;
                                sum += blended_5_0;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                    }
                }
            }
            else
            {
                if (effective < 200u)
                {
                    if (effective < 76u)
                    {
                        while ((uint)index < (uint)ticks.Length && effective >= 47u && effective < 76u)
                        {
                            var tick = ticks[index];
                            var quotient = tick / Duration;
                            uint cycles;
                            if (tick >= stateTick)
                                cycles = quotient - previousQuotient;
                            else
                                cycles = effective < previousEffective ? 1u : 0u;
                            if (cycles > ushort.MaxValue - stateCycles)
                                throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                            stateCycles = (ushort)(stateCycles + cycles);
                            sum += 13f;
                            sum += 2f;
                            stateTick = tick;
                            previousEffective = effective;
                            previousQuotient = quotient;
                            index++;
                            if ((uint)index < (uint)ticks.Length)
                                effective = ticks[index] % Duration;
                        }
                    }
                    else
                    {
                        if (effective < 123u)
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 76u && effective < 123u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick >= stateTick)
                                    cycles = quotient - previousQuotient;
                                else
                                    cycles = effective < previousEffective ? 1u : 0u;
                                if (cycles > ushort.MaxValue - stateCycles)
                                    throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                                stateCycles = (ushort)(stateCycles + cycles);
                                sum += 8f;
                                var factor_7_1 = (effective - 76u) / 46f;
                                var difference_7_1 = 55f - 34f;
                                var blended_7_1 = 34f + difference_7_1 * factor_7_1;
                                sum += blended_7_1;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                        else
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 123u && effective < 200u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick >= stateTick)
                                    cycles = quotient - previousQuotient;
                                else
                                    cycles = effective < previousEffective ? 1u : 0u;
                                if (cycles > ushort.MaxValue - stateCycles)
                                    throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                                stateCycles = (ushort)(stateCycles + cycles);
                                sum += 2f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                    }
                }
                else
                {
                    if (effective < 321u)
                    {
                        while ((uint)index < (uint)ticks.Length && effective >= 200u && effective < 321u)
                        {
                            var tick = ticks[index];
                            var quotient = tick / Duration;
                            uint cycles;
                            if (tick >= stateTick)
                                cycles = quotient - previousQuotient;
                            else
                                cycles = effective < previousEffective ? 1u : 0u;
                            if (cycles > ushort.MaxValue - stateCycles)
                                throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                            stateCycles = (ushort)(stateCycles + cycles);
                            sum += 2f;
                            sum += 5f;
                            stateTick = tick;
                            previousEffective = effective;
                            previousQuotient = quotient;
                            index++;
                            if ((uint)index < (uint)ticks.Length)
                                effective = ticks[index] % Duration;
                        }
                    }
                    else
                    {
                        if (effective < 515u)
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 321u && effective < 515u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick >= stateTick)
                                    cycles = quotient - previousQuotient;
                                else
                                    cycles = effective < previousEffective ? 1u : 0u;
                                if (cycles > ushort.MaxValue - stateCycles)
                                    throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                                stateCycles = (ushort)(stateCycles + cycles);
                                sum += 1f;
                                sum += 8f;
                                sum += 5f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                        else
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 515u && effective < 600u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick >= stateTick)
                                    cycles = quotient - previousQuotient;
                                else
                                    cycles = effective < previousEffective ? 1u : 0u;
                                if (cycles > ushort.MaxValue - stateCycles)
                                    throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
                                stateCycles = (ushort)(stateCycles + cycles);
                                sum += 3f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                    }
                }
            }
        }
        var flags = PlaybackFlags.Started;
        if (previousEffective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        return Mint(stateTick, stateCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Backward(in Playback from, ref float sum, uint tick)
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        return BackwardOne(in from, ref sum, tick);
    }

    public static Playback Backward(in Playback from, ref float sum, ReadOnlySpan<uint> ticks)
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        if (ticks.IsEmpty)
            return from;
        var stateTick = from.Tick;
        var stateCycles = from.Cycles;
        var previousEffective = stateTick % Duration;
        var previousQuotient = stateTick / Duration;
        var index = 0;
        var effective = ticks[0] % Duration;
        while ((uint)index < (uint)ticks.Length)
        {
            if (effective < 47u)
            {
                if (effective < 11u)
                {
                    if (effective < 3u)
                    {
                        while ((uint)index < (uint)ticks.Length && effective >= 0u && effective < 3u)
                        {
                            var tick = ticks[index];
                            var quotient = tick / Duration;
                            uint cycles;
                            if (tick <= stateTick)
                                cycles = previousQuotient - quotient;
                            else
                                cycles = effective > previousEffective ? 1u : 0u;
                            stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                            sum -= 1f;
                            stateTick = tick;
                            previousEffective = effective;
                            previousQuotient = quotient;
                            index++;
                            if ((uint)index < (uint)ticks.Length)
                                effective = ticks[index] % Duration;
                        }
                    }
                    else
                    {
                        if (effective < 7u)
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 3u && effective < 7u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick <= stateTick)
                                    cycles = previousQuotient - quotient;
                                else
                                    cycles = effective > previousEffective ? 1u : 0u;
                                stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                                sum -= 1f;
                                var factor_1_1 = (effective - 3u) / 3f;
                                var difference_1_1 = 3f - 2f;
                                var blended_1_1 = 2f + difference_1_1 * factor_1_1;
                                sum -= blended_1_1;
                                sum -= 5f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                        else
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 7u && effective < 11u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick <= stateTick)
                                    cycles = previousQuotient - quotient;
                                else
                                    cycles = effective > previousEffective ? 1u : 0u;
                                stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                                sum -= 3f;
                                sum -= 5f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                    }
                }
                else
                {
                    if (effective < 18u)
                    {
                        while ((uint)index < (uint)ticks.Length && effective >= 11u && effective < 18u)
                        {
                            var tick = ticks[index];
                            var quotient = tick / Duration;
                            uint cycles;
                            if (tick <= stateTick)
                                cycles = previousQuotient - quotient;
                            else
                                cycles = effective > previousEffective ? 1u : 0u;
                            stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                            stateTick = tick;
                            previousEffective = effective;
                            previousQuotient = quotient;
                            index++;
                            if ((uint)index < (uint)ticks.Length)
                                effective = ticks[index] % Duration;
                        }
                    }
                    else
                    {
                        if (effective < 29u)
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 18u && effective < 29u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick <= stateTick)
                                    cycles = previousQuotient - quotient;
                                else
                                    cycles = effective > previousEffective ? 1u : 0u;
                                stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                                sum -= 8f;
                                sum -= 5f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                        else
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 29u && effective < 47u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick <= stateTick)
                                    cycles = previousQuotient - quotient;
                                else
                                    cycles = effective > previousEffective ? 1u : 0u;
                                stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                                var factor_5_0 = (effective - 29u) / 17f;
                                var difference_5_0 = 21f - 13f;
                                var blended_5_0 = 13f + difference_5_0 * factor_5_0;
                                sum -= blended_5_0;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                    }
                }
            }
            else
            {
                if (effective < 200u)
                {
                    if (effective < 76u)
                    {
                        while ((uint)index < (uint)ticks.Length && effective >= 47u && effective < 76u)
                        {
                            var tick = ticks[index];
                            var quotient = tick / Duration;
                            uint cycles;
                            if (tick <= stateTick)
                                cycles = previousQuotient - quotient;
                            else
                                cycles = effective > previousEffective ? 1u : 0u;
                            stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                            sum -= 13f;
                            sum -= 2f;
                            stateTick = tick;
                            previousEffective = effective;
                            previousQuotient = quotient;
                            index++;
                            if ((uint)index < (uint)ticks.Length)
                                effective = ticks[index] % Duration;
                        }
                    }
                    else
                    {
                        if (effective < 123u)
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 76u && effective < 123u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick <= stateTick)
                                    cycles = previousQuotient - quotient;
                                else
                                    cycles = effective > previousEffective ? 1u : 0u;
                                stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                                sum -= 8f;
                                var factor_7_1 = (effective - 76u) / 46f;
                                var difference_7_1 = 55f - 34f;
                                var blended_7_1 = 34f + difference_7_1 * factor_7_1;
                                sum -= blended_7_1;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                        else
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 123u && effective < 200u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick <= stateTick)
                                    cycles = previousQuotient - quotient;
                                else
                                    cycles = effective > previousEffective ? 1u : 0u;
                                stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                                sum -= 2f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                    }
                }
                else
                {
                    if (effective < 321u)
                    {
                        while ((uint)index < (uint)ticks.Length && effective >= 200u && effective < 321u)
                        {
                            var tick = ticks[index];
                            var quotient = tick / Duration;
                            uint cycles;
                            if (tick <= stateTick)
                                cycles = previousQuotient - quotient;
                            else
                                cycles = effective > previousEffective ? 1u : 0u;
                            stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                            sum -= 2f;
                            sum -= 5f;
                            stateTick = tick;
                            previousEffective = effective;
                            previousQuotient = quotient;
                            index++;
                            if ((uint)index < (uint)ticks.Length)
                                effective = ticks[index] % Duration;
                        }
                    }
                    else
                    {
                        if (effective < 515u)
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 321u && effective < 515u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick <= stateTick)
                                    cycles = previousQuotient - quotient;
                                else
                                    cycles = effective > previousEffective ? 1u : 0u;
                                stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                                sum -= 1f;
                                sum -= 8f;
                                sum -= 5f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                        else
                        {
                            while ((uint)index < (uint)ticks.Length && effective >= 515u && effective < 600u)
                            {
                                var tick = ticks[index];
                                var quotient = tick / Duration;
                                uint cycles;
                                if (tick <= stateTick)
                                    cycles = previousQuotient - quotient;
                                else
                                    cycles = effective > previousEffective ? 1u : 0u;
                                stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
                                sum -= 3f;
                                stateTick = tick;
                                previousEffective = effective;
                                previousQuotient = quotient;
                                index++;
                                if ((uint)index < (uint)ticks.Length)
                                    effective = ticks[index] % Duration;
                            }
                        }
                    }
                }
            }
        }
        var flags = PlaybackFlags.Started;
        if (previousEffective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        return Mint(stateTick, stateCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Playback ForwardOne(in Playback from, ref float sum, uint tick)
    {
        var previousEffective = from.Tick % Duration;
        var effective = tick % Duration;
        uint cycles;
        if (tick >= from.Tick)
            cycles = tick / Duration - from.Tick / Duration;
        else
            cycles = effective < previousEffective ? 1u : 0u;
        if (cycles > ushort.MaxValue - from.Cycles)
            throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
        var newCycles = (ushort)(from.Cycles + cycles);
        var flags = PlaybackFlags.Started;
        if (effective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        if (effective < 47u)
        {
            if (effective < 11u)
            {
                if (effective < 3u)
                {
                    sum += 1f;
                }
                else
                {
                    if (effective < 7u)
                    {
                        sum += 1f;
                        var factor_1_1 = (effective - 3u) / 3f;
                        var difference_1_1 = 3f - 2f;
                        var blended_1_1 = 2f + difference_1_1 * factor_1_1;
                        sum += blended_1_1;
                        sum += 5f;
                    }
                    else
                    {
                        sum += 3f;
                        sum += 5f;
                    }
                }
            }
            else
            {
                if (effective < 18u)
                {
                }
                else
                {
                    if (effective < 29u)
                    {
                        sum += 8f;
                        sum += 5f;
                    }
                    else
                    {
                        var factor_5_0 = (effective - 29u) / 17f;
                        var difference_5_0 = 21f - 13f;
                        var blended_5_0 = 13f + difference_5_0 * factor_5_0;
                        sum += blended_5_0;
                    }
                }
            }
        }
        else
        {
            if (effective < 200u)
            {
                if (effective < 76u)
                {
                    sum += 13f;
                    sum += 2f;
                }
                else
                {
                    if (effective < 123u)
                    {
                        sum += 8f;
                        var factor_7_1 = (effective - 76u) / 46f;
                        var difference_7_1 = 55f - 34f;
                        var blended_7_1 = 34f + difference_7_1 * factor_7_1;
                        sum += blended_7_1;
                    }
                    else
                    {
                        sum += 2f;
                    }
                }
            }
            else
            {
                if (effective < 515u)
                {
                    if (effective < 321u)
                    {
                        sum += 2f;
                        sum += 5f;
                    }
                    else
                    {
                        sum += 1f;
                        sum += 8f;
                        sum += 5f;
                    }
                }
                else
                {
                    if (effective < 600u)
                    {
                        sum += 3f;
                    }
                    else
                    {
                    }
                }
            }
        }
        return Mint(tick, newCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Playback BackwardOne(in Playback from, ref float sum, uint tick)
    {
        var previousEffective = from.Tick % Duration;
        var effective = tick % Duration;
        uint cycles;
        if (tick <= from.Tick)
            cycles = from.Tick / Duration - tick / Duration;
        else
            cycles = effective > previousEffective ? 1u : 0u;
        var newCycles = (ushort)(from.Cycles - Math.Min(from.Cycles, cycles));
        var flags = PlaybackFlags.Started;
        if (effective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        if (effective < 47u)
        {
            if (effective < 11u)
            {
                if (effective < 3u)
                {
                    sum -= 1f;
                }
                else
                {
                    if (effective < 7u)
                    {
                        sum -= 1f;
                        var factor_1_1 = (effective - 3u) / 3f;
                        var difference_1_1 = 3f - 2f;
                        var blended_1_1 = 2f + difference_1_1 * factor_1_1;
                        sum -= blended_1_1;
                        sum -= 5f;
                    }
                    else
                    {
                        sum -= 3f;
                        sum -= 5f;
                    }
                }
            }
            else
            {
                if (effective < 18u)
                {
                }
                else
                {
                    if (effective < 29u)
                    {
                        sum -= 8f;
                        sum -= 5f;
                    }
                    else
                    {
                        var factor_5_0 = (effective - 29u) / 17f;
                        var difference_5_0 = 21f - 13f;
                        var blended_5_0 = 13f + difference_5_0 * factor_5_0;
                        sum -= blended_5_0;
                    }
                }
            }
        }
        else
        {
            if (effective < 200u)
            {
                if (effective < 76u)
                {
                    sum -= 13f;
                    sum -= 2f;
                }
                else
                {
                    if (effective < 123u)
                    {
                        sum -= 8f;
                        var factor_7_1 = (effective - 76u) / 46f;
                        var difference_7_1 = 55f - 34f;
                        var blended_7_1 = 34f + difference_7_1 * factor_7_1;
                        sum -= blended_7_1;
                    }
                    else
                    {
                        sum -= 2f;
                    }
                }
            }
            else
            {
                if (effective < 515u)
                {
                    if (effective < 321u)
                    {
                        sum -= 2f;
                        sum -= 5f;
                    }
                    else
                    {
                        sum -= 1f;
                        sum -= 8f;
                        sum -= 5f;
                    }
                }
                else
                {
                    if (effective < 600u)
                    {
                        sum -= 3f;
                    }
                    else
                    {
                    }
                }
            }
        }
        return Mint(tick, newCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Playback Mint(uint tick, ushort cycles, PlaybackFlags flags)
        => Unsafe.BitCast<ulong, Playback>(tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);
}
