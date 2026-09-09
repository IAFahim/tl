#nullable enable
using System;
using System.Runtime.CompilerServices;
using Tl;

namespace Tl.ConsumerFusion;

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
    public static Playback Forward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, uint tick)
        where TInput : unmanaged
        where TResult : unmanaged, IWorkOperation<TInput, TResult>
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        var previousQuotient = from.Tick / Duration;
        var previousEffective = from.Tick - previousQuotient * Duration;
        var quotient = tick / Duration;
        var effective = tick - quotient * Duration;
        uint cycles;
        if (tick >= from.Tick)
            cycles = quotient - previousQuotient;
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
                    var state_0_0 = cycles != 0 ? ClipState.Enter : ClipState.Stay;
                    TResult.Forward(0, 1, 1f, state_0_0, effective, in input, ref result);
                }
                else
                {
                    if (effective < 7u)
                    {
                        var state_1_0 = effective == 6u
                            ? ClipState.Exit
                            : cycles != 0 ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(0, 1, 1f, state_1_0, effective, in input, ref result);
                        var state_1_1 = cycles != 0 || previousEffective < 3u ? ClipState.Enter : ClipState.Stay;
                        var factor_1_1 = (effective - 3u) / 3f;
                        var difference_1_1 = 3f - 2f;
                        var amount_1_1 = 2f + difference_1_1 * factor_1_1;
                        TResult.Forward(1, 2, amount_1_1, state_1_1, effective, in input, ref result);
                        TResult.Forward(3, 4, 5f, state_1_1, effective, in input, ref result);
                    }
                    else
                    {
                        var state_2_0 = effective == 10u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 3u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(1, 2, 3f, state_2_0, effective, in input, ref result);
                        TResult.Forward(3, 4, 5f, state_2_0, effective, in input, ref result);
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
                        var state_4_0 = effective == 28u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 18u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(1, 2, 8f, state_4_0, effective, in input, ref result);
                        TResult.Forward(3, 4, 5f, state_4_0, effective, in input, ref result);
                    }
                    else
                    {
                        var state_5_0 = effective == 46u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 29u ? ClipState.Enter : ClipState.Stay;
                        var factor_5_0 = (effective - 29u) / 17f;
                        var difference_5_0 = 21f - 13f;
                        var amount_5_0 = 13f + difference_5_0 * factor_5_0;
                        TResult.Forward(0, 1, amount_5_0, state_5_0, effective, in input, ref result);
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
                    var state_6_0 = effective == 75u
                        ? ClipState.Exit
                        : cycles != 0 || previousEffective < 47u ? ClipState.Enter : ClipState.Stay;
                    TResult.Forward(0, 1, 13f, state_6_0, effective, in input, ref result);
                    TResult.Forward(2, 3, 2f, state_6_0, effective, in input, ref result);
                }
                else
                {
                    if (effective < 123u)
                    {
                        var state_7_0 = effective == 122u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 76u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(1, 2, 8f, state_7_0, effective, in input, ref result);
                        var factor_7_1 = (effective - 76u) / 46f;
                        var difference_7_1 = 55f - 34f;
                        var amount_7_1 = 34f + difference_7_1 * factor_7_1;
                        TResult.Forward(3, 4, amount_7_1, state_7_0, effective, in input, ref result);
                    }
                    else
                    {
                        var state_8_0 = cycles != 0 || previousEffective < 123u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(2, 3, 2f, state_8_0, effective, in input, ref result);
                    }
                }
            }
            else
            {
                if (effective < 321u)
                {
                    var state_9_0 = effective == 320u
                        ? ClipState.Exit
                        : cycles != 0 || previousEffective < 123u ? ClipState.Enter : ClipState.Stay;
                    TResult.Forward(2, 3, 2f, state_9_0, effective, in input, ref result);
                    var state_9_1 = effective == 320u
                        ? ClipState.Exit
                        : cycles != 0 || previousEffective < 200u ? ClipState.Enter : ClipState.Stay;
                    TResult.Forward(3, 4, 5f, state_9_1, effective, in input, ref result);
                }
                else
                {
                    if (effective < 515u)
                    {
                        var state_10_0 = effective == 514u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 321u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(0, 1, 1f, state_10_0, effective, in input, ref result);
                        TResult.Forward(2, 3, 8f, state_10_0, effective, in input, ref result);
                        TResult.Forward(3, 4, 5f, state_10_0, effective, in input, ref result);
                    }
                    else
                    {
                        var state_11_0 = effective == 599u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 515u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(1, 2, 3f, state_11_0, effective, in input, ref result);
                    }
                }
            }
        }
        return Mint(tick, newCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Forward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        where TInput : unmanaged
        where TResult : unmanaged, IWorkOperation<TInput, TResult>
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        if (ticks.IsEmpty)
            return from;
        var stateTick = from.Tick;
        var stateCycles = from.Cycles;
        var previousQuotient = stateTick / Duration;
        var previousEffective = stateTick - previousQuotient * Duration;
        foreach (var tick in ticks)
        {
            var quotient = tick / Duration;
            var effective = tick - quotient * Duration;
            uint cycles;
            if (tick >= stateTick)
                cycles = quotient - previousQuotient;
            else
                cycles = effective < previousEffective ? 1u : 0u;
            if (cycles > ushort.MaxValue - stateCycles)
                throw new ArgumentOutOfRangeException("ticks", "Playback cycle capacity exceeded.");
            stateCycles = (ushort)(stateCycles + cycles);
            if (effective < 47u)
            {
                if (effective < 11u)
                {
                    if (effective < 3u)
                    {
                        var state_0_0 = cycles != 0 ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(0, 1, 1f, state_0_0, effective, in input, ref result);
                    }
                    else
                    {
                        if (effective < 7u)
                        {
                            var state_1_0 = effective == 6u
                                ? ClipState.Exit
                                : cycles != 0 ? ClipState.Enter : ClipState.Stay;
                            TResult.Forward(0, 1, 1f, state_1_0, effective, in input, ref result);
                            var state_1_1 = cycles != 0 || previousEffective < 3u ? ClipState.Enter : ClipState.Stay;
                            var factor_1_1 = (effective - 3u) / 3f;
                            var difference_1_1 = 3f - 2f;
                            var amount_1_1 = 2f + difference_1_1 * factor_1_1;
                            TResult.Forward(1, 2, amount_1_1, state_1_1, effective, in input, ref result);
                            TResult.Forward(3, 4, 5f, state_1_1, effective, in input, ref result);
                        }
                        else
                        {
                            var state_2_0 = effective == 10u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective < 3u ? ClipState.Enter : ClipState.Stay;
                            TResult.Forward(1, 2, 3f, state_2_0, effective, in input, ref result);
                            TResult.Forward(3, 4, 5f, state_2_0, effective, in input, ref result);
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
                            var state_4_0 = effective == 28u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective < 18u ? ClipState.Enter : ClipState.Stay;
                            TResult.Forward(1, 2, 8f, state_4_0, effective, in input, ref result);
                            TResult.Forward(3, 4, 5f, state_4_0, effective, in input, ref result);
                        }
                        else
                        {
                            var state_5_0 = effective == 46u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective < 29u ? ClipState.Enter : ClipState.Stay;
                            var factor_5_0 = (effective - 29u) / 17f;
                            var difference_5_0 = 21f - 13f;
                            var amount_5_0 = 13f + difference_5_0 * factor_5_0;
                            TResult.Forward(0, 1, amount_5_0, state_5_0, effective, in input, ref result);
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
                        var state_6_0 = effective == 75u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 47u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(0, 1, 13f, state_6_0, effective, in input, ref result);
                        TResult.Forward(2, 3, 2f, state_6_0, effective, in input, ref result);
                    }
                    else
                    {
                        if (effective < 123u)
                        {
                            var state_7_0 = effective == 122u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective < 76u ? ClipState.Enter : ClipState.Stay;
                            TResult.Forward(1, 2, 8f, state_7_0, effective, in input, ref result);
                            var factor_7_1 = (effective - 76u) / 46f;
                            var difference_7_1 = 55f - 34f;
                            var amount_7_1 = 34f + difference_7_1 * factor_7_1;
                            TResult.Forward(3, 4, amount_7_1, state_7_0, effective, in input, ref result);
                        }
                        else
                        {
                            var state_8_0 = cycles != 0 || previousEffective < 123u ? ClipState.Enter : ClipState.Stay;
                            TResult.Forward(2, 3, 2f, state_8_0, effective, in input, ref result);
                        }
                    }
                }
                else
                {
                    if (effective < 321u)
                    {
                        var state_9_0 = effective == 320u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 123u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(2, 3, 2f, state_9_0, effective, in input, ref result);
                        var state_9_1 = effective == 320u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective < 200u ? ClipState.Enter : ClipState.Stay;
                        TResult.Forward(3, 4, 5f, state_9_1, effective, in input, ref result);
                    }
                    else
                    {
                        if (effective < 515u)
                        {
                            var state_10_0 = effective == 514u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective < 321u ? ClipState.Enter : ClipState.Stay;
                            TResult.Forward(0, 1, 1f, state_10_0, effective, in input, ref result);
                            TResult.Forward(2, 3, 8f, state_10_0, effective, in input, ref result);
                            TResult.Forward(3, 4, 5f, state_10_0, effective, in input, ref result);
                        }
                        else
                        {
                            var state_11_0 = effective == 599u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective < 515u ? ClipState.Enter : ClipState.Stay;
                            TResult.Forward(1, 2, 3f, state_11_0, effective, in input, ref result);
                        }
                    }
                }
            }
            stateTick = tick;
            previousEffective = effective;
            previousQuotient = quotient;
        }
        var flags = PlaybackFlags.Started;
        if (previousEffective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        return Mint(stateTick, stateCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Backward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, uint tick)
        where TInput : unmanaged
        where TResult : unmanaged, IWorkOperation<TInput, TResult>
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        var previousQuotient = from.Tick / Duration;
        var previousEffective = from.Tick - previousQuotient * Duration;
        var quotient = tick / Duration;
        var effective = tick - quotient * Duration;
        uint cycles;
        if (tick <= from.Tick)
            cycles = previousQuotient - quotient;
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
                    var state_0_0 = effective == 0u
                        ? ClipState.Exit
                        : cycles != 0 || previousEffective >= 7u ? ClipState.Enter : ClipState.Stay;
                    TResult.Backward(0, 1, 1f, state_0_0, effective, in input, ref result);
                }
                else
                {
                    if (effective < 7u)
                    {
                        var state_1_0 = cycles != 0 || previousEffective >= 7u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(0, 1, 1f, state_1_0, effective, in input, ref result);
                        var state_1_1 = effective == 3u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 11u ? ClipState.Enter : ClipState.Stay;
                        var factor_1_1 = (effective - 3u) / 3f;
                        var difference_1_1 = 3f - 2f;
                        var amount_1_1 = 2f + difference_1_1 * factor_1_1;
                        TResult.Backward(1, 2, amount_1_1, state_1_1, effective, in input, ref result);
                        TResult.Backward(3, 4, 5f, state_1_1, effective, in input, ref result);
                    }
                    else
                    {
                        var state_2_0 = cycles != 0 || previousEffective >= 11u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(1, 2, 3f, state_2_0, effective, in input, ref result);
                        TResult.Backward(3, 4, 5f, state_2_0, effective, in input, ref result);
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
                        var state_4_0 = effective == 18u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 29u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(1, 2, 8f, state_4_0, effective, in input, ref result);
                        TResult.Backward(3, 4, 5f, state_4_0, effective, in input, ref result);
                    }
                    else
                    {
                        var state_5_0 = effective == 29u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 47u ? ClipState.Enter : ClipState.Stay;
                        var factor_5_0 = (effective - 29u) / 17f;
                        var difference_5_0 = 21f - 13f;
                        var amount_5_0 = 13f + difference_5_0 * factor_5_0;
                        TResult.Backward(0, 1, amount_5_0, state_5_0, effective, in input, ref result);
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
                    var state_6_0 = effective == 47u
                        ? ClipState.Exit
                        : cycles != 0 || previousEffective >= 76u ? ClipState.Enter : ClipState.Stay;
                    TResult.Backward(0, 1, 13f, state_6_0, effective, in input, ref result);
                    TResult.Backward(2, 3, 2f, state_6_0, effective, in input, ref result);
                }
                else
                {
                    if (effective < 123u)
                    {
                        var state_7_0 = effective == 76u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 123u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(1, 2, 8f, state_7_0, effective, in input, ref result);
                        var factor_7_1 = (effective - 76u) / 46f;
                        var difference_7_1 = 55f - 34f;
                        var amount_7_1 = 34f + difference_7_1 * factor_7_1;
                        TResult.Backward(3, 4, amount_7_1, state_7_0, effective, in input, ref result);
                    }
                    else
                    {
                        var state_8_0 = effective == 123u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 321u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(2, 3, 2f, state_8_0, effective, in input, ref result);
                    }
                }
            }
            else
            {
                if (effective < 321u)
                {
                    var state_9_0 = cycles != 0 || previousEffective >= 321u ? ClipState.Enter : ClipState.Stay;
                    TResult.Backward(2, 3, 2f, state_9_0, effective, in input, ref result);
                    var state_9_1 = effective == 200u
                        ? ClipState.Exit
                        : cycles != 0 || previousEffective >= 321u ? ClipState.Enter : ClipState.Stay;
                    TResult.Backward(3, 4, 5f, state_9_1, effective, in input, ref result);
                }
                else
                {
                    if (effective < 515u)
                    {
                        var state_10_0 = effective == 321u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 515u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(0, 1, 1f, state_10_0, effective, in input, ref result);
                        TResult.Backward(2, 3, 8f, state_10_0, effective, in input, ref result);
                        TResult.Backward(3, 4, 5f, state_10_0, effective, in input, ref result);
                    }
                    else
                    {
                        var state_11_0 = effective == 515u
                            ? ClipState.Exit
                            : cycles != 0 ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(1, 2, 3f, state_11_0, effective, in input, ref result);
                    }
                }
            }
        }
        return Mint(tick, newCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Playback Backward<TInput, TResult>(in Playback from, in TInput input, ref TResult result, ReadOnlySpan<uint> ticks)
        where TInput : unmanaged
        where TResult : unmanaged, IWorkOperation<TInput, TResult>
    {
        if (!from.Has(PlaybackFlags.Started))
            throw new InvalidOperationException("Playback was never started; mint one with FusedPulse.Start.");
        if (from.Has(PlaybackFlags.Stopped))
            throw new InvalidOperationException("Playback is stopped.");
        if (ticks.IsEmpty)
            return from;
        var stateTick = from.Tick;
        var stateCycles = from.Cycles;
        var previousQuotient = stateTick / Duration;
        var previousEffective = stateTick - previousQuotient * Duration;
        foreach (var tick in ticks)
        {
            var quotient = tick / Duration;
            var effective = tick - quotient * Duration;
            uint cycles;
            if (tick <= stateTick)
                cycles = previousQuotient - quotient;
            else
                cycles = effective > previousEffective ? 1u : 0u;
            stateCycles = (ushort)(stateCycles - Math.Min(stateCycles, cycles));
            if (effective < 47u)
            {
                if (effective < 11u)
                {
                    if (effective < 3u)
                    {
                        var state_0_0 = effective == 0u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 7u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(0, 1, 1f, state_0_0, effective, in input, ref result);
                    }
                    else
                    {
                        if (effective < 7u)
                        {
                            var state_1_0 = cycles != 0 || previousEffective >= 7u ? ClipState.Enter : ClipState.Stay;
                            TResult.Backward(0, 1, 1f, state_1_0, effective, in input, ref result);
                            var state_1_1 = effective == 3u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective >= 11u ? ClipState.Enter : ClipState.Stay;
                            var factor_1_1 = (effective - 3u) / 3f;
                            var difference_1_1 = 3f - 2f;
                            var amount_1_1 = 2f + difference_1_1 * factor_1_1;
                            TResult.Backward(1, 2, amount_1_1, state_1_1, effective, in input, ref result);
                            TResult.Backward(3, 4, 5f, state_1_1, effective, in input, ref result);
                        }
                        else
                        {
                            var state_2_0 = cycles != 0 || previousEffective >= 11u ? ClipState.Enter : ClipState.Stay;
                            TResult.Backward(1, 2, 3f, state_2_0, effective, in input, ref result);
                            TResult.Backward(3, 4, 5f, state_2_0, effective, in input, ref result);
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
                            var state_4_0 = effective == 18u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective >= 29u ? ClipState.Enter : ClipState.Stay;
                            TResult.Backward(1, 2, 8f, state_4_0, effective, in input, ref result);
                            TResult.Backward(3, 4, 5f, state_4_0, effective, in input, ref result);
                        }
                        else
                        {
                            var state_5_0 = effective == 29u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective >= 47u ? ClipState.Enter : ClipState.Stay;
                            var factor_5_0 = (effective - 29u) / 17f;
                            var difference_5_0 = 21f - 13f;
                            var amount_5_0 = 13f + difference_5_0 * factor_5_0;
                            TResult.Backward(0, 1, amount_5_0, state_5_0, effective, in input, ref result);
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
                        var state_6_0 = effective == 47u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 76u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(0, 1, 13f, state_6_0, effective, in input, ref result);
                        TResult.Backward(2, 3, 2f, state_6_0, effective, in input, ref result);
                    }
                    else
                    {
                        if (effective < 123u)
                        {
                            var state_7_0 = effective == 76u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective >= 123u ? ClipState.Enter : ClipState.Stay;
                            TResult.Backward(1, 2, 8f, state_7_0, effective, in input, ref result);
                            var factor_7_1 = (effective - 76u) / 46f;
                            var difference_7_1 = 55f - 34f;
                            var amount_7_1 = 34f + difference_7_1 * factor_7_1;
                            TResult.Backward(3, 4, amount_7_1, state_7_0, effective, in input, ref result);
                        }
                        else
                        {
                            var state_8_0 = effective == 123u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective >= 321u ? ClipState.Enter : ClipState.Stay;
                            TResult.Backward(2, 3, 2f, state_8_0, effective, in input, ref result);
                        }
                    }
                }
                else
                {
                    if (effective < 321u)
                    {
                        var state_9_0 = cycles != 0 || previousEffective >= 321u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(2, 3, 2f, state_9_0, effective, in input, ref result);
                        var state_9_1 = effective == 200u
                            ? ClipState.Exit
                            : cycles != 0 || previousEffective >= 321u ? ClipState.Enter : ClipState.Stay;
                        TResult.Backward(3, 4, 5f, state_9_1, effective, in input, ref result);
                    }
                    else
                    {
                        if (effective < 515u)
                        {
                            var state_10_0 = effective == 321u
                                ? ClipState.Exit
                                : cycles != 0 || previousEffective >= 515u ? ClipState.Enter : ClipState.Stay;
                            TResult.Backward(0, 1, 1f, state_10_0, effective, in input, ref result);
                            TResult.Backward(2, 3, 8f, state_10_0, effective, in input, ref result);
                            TResult.Backward(3, 4, 5f, state_10_0, effective, in input, ref result);
                        }
                        else
                        {
                            var state_11_0 = effective == 515u
                                ? ClipState.Exit
                                : cycles != 0 ? ClipState.Enter : ClipState.Stay;
                            TResult.Backward(1, 2, 3f, state_11_0, effective, in input, ref result);
                        }
                    }
                }
            }
            stateTick = tick;
            previousEffective = effective;
            previousQuotient = quotient;
        }
        var flags = PlaybackFlags.Started;
        if (previousEffective == Duration - 1u)
            flags |= PlaybackFlags.LastLoopFrame;
        return Mint(stateTick, stateCycles, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Playback Mint(uint tick, ushort cycles, PlaybackFlags flags)
        => Unsafe.BitCast<ulong, Playback>(tick | (ulong)cycles << 32 | (ulong)(ushort)flags << 48);
}
