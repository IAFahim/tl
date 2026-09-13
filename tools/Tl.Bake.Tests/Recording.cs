using System;
using System.Collections.Generic;
using Tl;

namespace Tl.Bake.Tests;

public readonly record struct TlbRecord(char Pair, uint Tick, float Value);

public static unsafe class Recording
{
    public static readonly List<TlbRecord> Records = [];

    static Recording()
    {
        PairRuntime<Tlb.DualTrack, Tlb.DualAlphaClip>.Consume(&AlphaExecute, &NoBind);
        PairRuntime<Tlb.DualTrack, Tlb.DualBetaClip>.Consume(&BetaExecute, &NoBind);
        PairRuntime<Tlb.EchoTrack, Tlb.EchoClip>.Consume(&EchoExecute, &NoBind);
    }

    public static string OracleJson => """
    {
      "name": "oracle_asset",
      "duration": 8,
      "loop": false,
      "tracks": [
        {
          "name": "combat",
          "namespace": "Tlb",
          "type": "DualTrack",
          "data": { "Code": 1 },
          "clips": [
            { "name": "a1", "namespace": "Tlb", "type": "DualAlphaClip", "start": 0, "end": 6, "data": { "Value": 10 } },
            { "name": "b1", "namespace": "Tlb", "type": "DualBetaClip",  "start": 1, "end": 7, "data": { "Amount": 1.5 } },
            { "name": "a2", "namespace": "Tlb", "type": "DualAlphaClip", "start": 3, "end": 8, "data": { "Value": 30 } }
          ]
        },
        {
          "name": "echo_lane",
          "namespace": "Tlb",
          "type": "EchoTrack",
          "data": { "Code": 5 },
          "clips": [
            { "name": "e1", "namespace": "Tlb", "type": "EchoClip", "start": 0, "end": 4, "data": { "Value": 7 } }
          ]
        }
      ]
    }
    """;

    public static List<TlbRecord> ForwardOracle(uint gameTick) =>
    [
        new('A', 0u, 10f),
        new('E', 0u, 7f),
        new('A', 1u, 10f),
        new('B', 1u, 1.5f),
        new('E', 1u, 7f),
        new('A', 2u, 10f),
        new('B', 2u, 1.5f),
        new('E', 2u, 7f),
        new('A', 3u, 10f),
        new('B', 3u, 1.5f),
        new('E', 3u, 7f),
        new('A', 4u, 20f),
        new('B', 4u, 1.5f),
        new('A', 5u, 30f),
        new('B', 5u, 1.5f),
        new('B', 6u, 1.5f),
        new('A', 6u, 30f),
        new('A', 7u, 30f),
    ];

    static void AlphaExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        Tlb.DualAlphaClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.DualTrack, Tlb.DualAlphaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Records.Add(new TlbRecord('A', tick, frame.Clip.Value));
    }

    static void BetaExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        Tlb.DualBetaClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.DualTrack, Tlb.DualBetaClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Records.Add(new TlbRecord('B', tick, frame.Clip.Amount));
    }

    static void EchoExecute(byte* slot, uint gameTick, uint tick, long cycle, FrameFlags flags, void** columns, int row)
    {
        Tlb.EchoClip scratch = default;
        var frame = TickFrame.ToFrame<Tlb.EchoTrack, Tlb.EchoClip>(slot, gameTick, tick, cycle, flags, ref scratch);
        Records.Add(new TlbRecord('E', tick, frame.Clip.Value));
    }

    static void NoBind(ulong* keys, int count, byte* asset)
    {
    }
}
