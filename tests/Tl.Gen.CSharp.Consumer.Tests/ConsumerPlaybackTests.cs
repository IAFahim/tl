using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Tl.Gen.CSharp.Consumer.Tests;

public sealed class ConsumerPlaybackTests
{
    private static readonly Lazy<Assembly> Fixture = new(Compile);

    [Fact]
    public void TickForwardAndBackwardMatchesTheHandWrittenOracleIncludingTheBlendWindow()
    {
        var result = Driver("Blend");

        Assert.Equal(
            "968|936|920|904|904|904|872|856" +
            "#872|904|904|904|920|936|968|1000" +
            "#1000#0",
            result);
    }

    [Fact]
    public void AbaAuthoredRowsDeliverEachTypedFrameToItsOwnColumns()
    {
        var result = Driver("Rows");

        Assert.Equal("968,508,218,1,1,1#936,516,186,2,2,2#968,508,218,1,1,1", result);
    }

    [Fact]
    public void MissingColumnBindThrowsAtFacadeTickBeforeAnyEffects()
    {
        var result = Driver("Missing");

        Assert.StartsWith("THROWN|", result);
        Assert.Contains("ApplyGuarded", result);
        Assert.Contains("Resistance", result);
        Assert.Contains("required column missing for registered consumer", result);
        Assert.EndsWith("|400|0", result);
    }

    [Fact]
    public void StandaloneJobDispatchesOnDataAuthoredTimelineWithoutAuthoredTimelineOrUse()
    {
        var result = Driver("Standalone");

        Assert.Equal("25#40#25", result);
    }

    private static string Driver(string method)
    {
        var value = Fixture.Value.GetType("Domain.Playback")!.GetMethod(method)!.Invoke(null, null);
        return Assert.IsType<string>(value);
    }

    private static Assembly Compile()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("ConsumerPlaybackFixture" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(Domain, options, "Domain.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics);
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics) + "\n" + string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(static tree => tree.ToString())));
        return Assembly.Load(stream.ToArray());
    }

    private static IEnumerable<MetadataReference> References()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITimelineJob<,>).Assembly.Location).Distinct(StringComparer.Ordinal)
            .Select(static path => MetadataReference.CreateFromFile(path));

    private const string Domain = """
        using System;
        using System.Buffers.Binary;
        using System.Collections.Generic;
        using System.Globalization;
        using System.Linq;
        using System.Runtime.CompilerServices;
        using System.Runtime.InteropServices;
        using Tl;

        namespace Domain;

        public readonly record struct DamageClip(float Amount);

        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
                => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly record struct HealClip(float Amount);

        public readonly record struct HealTrack(float Multiplier) : IBlend<HealClip>
        {
            public void Blend(in HealClip first, in HealClip second, float factor, out HealClip result)
                => result = new HealClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public struct Resistance { public float Scale; }

        public readonly struct ApplyDamage : ITimelineJob<DamageTrack, DamageClip>
        {
            public static void Execute(in Frame<DamageTrack, DamageClip> frame, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                health += frame.IsBackward ? amount : -amount;
            }
        }

        public readonly struct ApplyHeal : ITimelineJob<HealTrack, HealClip>
        {
            public static void Execute(in Frame<HealTrack, HealClip> frame, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                health += frame.IsBackward ? -amount : amount;
            }
        }

        public readonly record struct BuffClip(float Amount);

        public readonly record struct BuffTrack(float Multiplier) : IBlend<BuffClip>
        {
            public void Blend(in BuffClip first, in BuffClip second, float factor, out BuffClip result)
                => result = new BuffClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly struct ApplyBuff : ITimelineJob<BuffTrack, BuffClip>
        {
            public static void Execute(in Frame<BuffTrack, BuffClip> frame, ref float armor)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                armor += frame.IsBackward ? -amount : amount;
            }
        }

        public readonly record struct GuardClip(float Amount);

        public readonly record struct GuardTrack(float Multiplier) : IBlend<GuardClip>
        {
            public void Blend(in GuardClip first, in GuardClip second, float factor, out GuardClip result)
                => result = new GuardClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly struct ApplyGuarded : ITimelineJob<GuardTrack, GuardClip>
        {
            public static void Execute(in Frame<GuardTrack, GuardClip> frame, in Resistance resistance, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
                health += frame.IsBackward ? amount : -amount;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Slot<TTrack, TClip> where TTrack : unmanaged where TClip : unmanaged
        {
            public TTrack Track;
            public TClip First;
            public TClip Second;
            public uint WindowStart;
            public uint WindowEnd;
            public uint FactorStart;
            public uint FactorSpan;
            public byte TrackIndex;
        }

        internal sealed class Baker
        {
            internal sealed class BakedClip
            {
                public required uint Start { get; init; }
                public required uint End { get; init; }
                public required object Value { get; init; }
            }

            internal abstract class BakedTrack
            {
                public ulong Key;
                public uint Stride;
                public byte Index;
                public readonly List<BakedClip> Clips = [];

                public abstract void Write(byte[] bytes, int offset, BakedClip? first, BakedClip? second, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan);
            }

            internal sealed class BakedTrack<TTrack, TClip> : BakedTrack
                where TTrack : unmanaged, IBlend<TClip>
                where TClip : unmanaged
            {
                public required TTrack TrackValue { get; init; }

                public override void Write(byte[] bytes, int offset, BakedClip? first, BakedClip? second, uint windowStart, uint windowEnd, uint factorStart, uint factorSpan)
                {
                    var slot = new Slot<TTrack, TClip>
                    {
                        Track = TrackValue,
                        First = (TClip)first!.Value,
                        Second = second is null ? default : (TClip)second.Value,
                        WindowStart = windowStart,
                        WindowEnd = windowEnd,
                        FactorStart = factorStart,
                        FactorSpan = factorSpan,
                        TrackIndex = Index,
                    };
                    MemoryMarshal.Write(bytes.AsSpan(offset), in slot);
                }
            }

            private readonly List<BakedTrack> _tracks = [];

            public Baker Track<TTrack, TClip>(TTrack value) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
            {
                _tracks.Add(new BakedTrack<TTrack, TClip>
                {
                    TrackValue = value,
                    Key = PairRuntime<TTrack, TClip>.Key,
                    Stride = (uint)((Unsafe.SizeOf<Slot<TTrack, TClip>>() + 15) & ~15),
                    Index = (byte)_tracks.Count,
                });
                return this;
            }

            public Baker Clip<TClip>(int track, uint start, uint end, TClip clip) where TClip : unmanaged
            {
                _tracks[track].Clips.Add(new BakedClip { Start = start, End = end, Value = clip });
                return this;
            }

            public byte[] Bake()
            {
                var duration = 0u;
                foreach (var track in _tracks)
                    foreach (var clip in track.Clips)
                        duration = Math.Max(duration, clip.End);

                var cuts = new SortedSet<uint>();
                if (duration != 0)
                {
                    cuts.Add(0u);
                    cuts.Add(duration);
                    foreach (var track in _tracks)
                        foreach (var clip in track.Clips)
                        {
                            cuts.Add(clip.Start);
                            cuts.Add(clip.End);
                        }
                }

                var boundaries = cuts.ToArray();
                var stageList = new List<List<BakedTrack>>();
                var stageEdges = new List<(uint Start, uint End)>();
                for (var region = 0; region + 1 < boundaries.Length; region++)
                {
                    var edge = boundaries[region];
                    var active = new List<BakedTrack>();
                    foreach (var track in _tracks)
                        if (track.Clips.Any(clip => clip.Start <= edge && edge < clip.End))
                            active.Add(track);
                    stageList.Add(active);
                    stageEdges.Add((edge, boundaries[region + 1]));
                }

                var pairs = _tracks.Select(track => (track.Key, track.Stride)).Distinct().OrderBy(pair => pair.Key).ToArray();
                var pairIndex = new Dictionary<ulong, int>();
                for (var index = 0; index < pairs.Length; index++)
                    pairIndex[pairs[index].Key] = index;

                var pairOffset = 48u;
                var stageOffset = pairOffset + 16u * (uint)pairs.Length;
                var programBase = stageOffset + 16u * (uint)stageList.Count;
                var stepCount = stageList.Sum(active => active.Count);
                var frameOffset = (programBase + 8u * (uint)stepCount + 15u) & ~15u;

                var occurrences = new List<(uint Offset, int Pair, BakedTrack Track, int Stage)>();
                var cursor = frameOffset;
                for (var stage = 0; stage < stageList.Count; stage++)
                    foreach (var track in stageList[stage])
                    {
                        occurrences.Add((cursor, pairIndex[track.Key], track, stage));
                        cursor += pairs[pairIndex[track.Key]].Stride;
                    }

                var bytes = new byte[cursor];
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x31424C54u);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 1u);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0u);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), duration);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)_tracks.Count);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)stageList.Count);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), (uint)pairs.Length);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), pairOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), stageOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), frameOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), (uint)bytes.Length);

                for (var index = 0; index < pairs.Length; index++)
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan((int)pairOffset + 16 * index), pairs[index].Key);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)pairOffset + 16 * index + 8), pairs[index].Stride);
                }

                var programOffset = programBase;
                for (var stage = 0; stage < stageList.Count; stage++)
                {
                    var at = stageOffset + 16u * (uint)stage;
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at), stageEdges[stage].Start);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 4), stageEdges[stage].End);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 8), programOffset);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)at + 12), (uint)stageList[stage].Count);
                    foreach (var occurrence in occurrences.Where(item => item.Stage == stage))
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)programOffset), occurrence.Offset);
                        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan((int)programOffset + 4), (uint)occurrence.Pair);
                        programOffset += 8u;
                    }
                }

                foreach (var occurrence in occurrences)
                {
                    var edge = stageEdges[occurrence.Stage].Start;
                    var clips = occurrence.Track.Clips
                        .Select((clip, index) => (clip, index))
                        .Where(item => item.clip.Start <= edge && edge < item.clip.End)
                        .OrderBy(item => item.clip.Start).ThenBy(item => item.index)
                        .Select(item => item.clip)
                        .ToArray();
                    var windowStart = clips.Min(clip => clip.Start);
                    var windowEnd = clips.Max(clip => clip.End);
                    var factorStart = 0u;
                    var factorSpan = 0u;
                    if (clips.Length == 2)
                    {
                        factorStart = Math.Max(clips[0].Start, clips[1].Start);
                        factorSpan = Math.Min(clips[0].End, clips[1].End) - factorStart;
                    }

                    occurrence.Track.Write(bytes, (int)occurrence.Offset, clips[0], clips.Length == 2 ? clips[1] : null, windowStart, windowEnd, factorStart, factorSpan);
                }

                return bytes;
            }
        }

        public static class Playback
        {
            private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

            private static string Positions(params uint[] positions)
                => string.Join(",", positions.Select(static position => position.ToString(CultureInfo.InvariantCulture)));

            public static string Blend()
            {
                using var asset = TimelineAsset.Load(new Baker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(4f))
                    .Clip(0, 0u, 2u, new DamageClip(8f))
                    .Clip(0, 2u, 4u, new DamageClip(4f))
                    .Clip(0, 6u, 8u, new DamageClip(8f))
                    .Clip(0, 7u, 8u, new DamageClip(0f))
                    .Bake());
                BakedLane<DamageTrack, DamageClip>.Bind(asset);
                var positions = new uint[] { 0 };
                var health = new float[] { 1000f };
                var cycles = new long[] { 0 };
                var forward = new List<string>();
                for (var tick = 0; tick < 8; tick++)
                {
                    Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, true).Apply(health, cycles);
                    forward.Add(F(health[0]));
                }
                var backward = new List<string>();
                for (var tick = 0; tick < 8; tick++)
                {
                    Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(positions, false).Apply(health, cycles);
                    backward.Add(F(health[0]));
                }
                return string.Join("|", forward) + "#" + string.Join("|", backward) + "#" + F(health[0]) + "#" + Positions(positions[0]);
            }

            public static string Rows()
            {
                using var damage = TimelineAsset.Load(new Baker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(4f))
                    .Clip(0, 0u, 2u, new DamageClip(8f))
                    .Bake());
                using var heal = TimelineAsset.Load(new Baker()
                    .Track<HealTrack, HealClip>(new HealTrack(1f))
                    .Clip(0, 0u, 2u, new HealClip(8f))
                    .Bake());
                BakedLane<DamageTrack, DamageClip>.Bind(damage);
                BakedLane<HealTrack, HealClip>.Bind(heal);
                var damagePositions = new uint[] { 0, 0 };
                var damageHealth = new float[] { 1000f, 250f };
                var healPositions = new uint[] { 0 };
                var healHealth = new float[] { 500f };
                var cycles = new long[3];
                string Snapshot()
                    => F(damageHealth[0]) + "," + F(healHealth[0]) + "," + F(damageHealth[1]) + ","
                    + Positions(damagePositions[0], healPositions[0], damagePositions[1]);
                Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(damagePositions, true).Apply(damageHealth, cycles.AsSpan(0, 2));
                Timeline<BakedLane<HealTrack, HealClip>>.Seek(healPositions, true).Apply(healHealth, cycles.AsSpan(2, 1));
                var first = Snapshot();
                Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(damagePositions, true).Apply(damageHealth, cycles.AsSpan(0, 2));
                Timeline<BakedLane<HealTrack, HealClip>>.Seek(healPositions, true).Apply(healHealth, cycles.AsSpan(2, 1));
                var second = Snapshot();
                Timeline<BakedLane<DamageTrack, DamageClip>>.Seek(damagePositions, false).Apply(damageHealth, cycles.AsSpan(0, 2));
                Timeline<BakedLane<HealTrack, HealClip>>.Seek(healPositions, false).Apply(healHealth, cycles.AsSpan(2, 1));
                var third = Snapshot();
                return first + "#" + second + "#" + third;
            }

            public static string Missing()
            {
                using var guard = TimelineAsset.Load(new Baker()
                    .Track<GuardTrack, GuardClip>(new GuardTrack(2f))
                    .Clip(0, 0u, 2u, new GuardClip(8f))
                    .Bake());
                var positions = new uint[] { 0 };
                var health = new float[] { 400f };
                var cycles = new long[] { 0 };
                try
                {
                    BakedLane<GuardTrack, GuardClip>.Bind(guard);
                    Timeline<BakedLane<GuardTrack, GuardClip>>.Seek(positions, true).Apply(health, cycles);
                }
                catch (ArgumentException exception)
                {
                    return "THROWN|" + exception.Message + "|" + F(health[0]) + "|" + Positions(positions[0]);
                }
                return "NOTHROWN|" + F(health[0]) + "|" + Positions(positions[0]);
            }

            public static string Standalone()
            {
                using var buff = TimelineAsset.Load(new Baker()
                    .Track<BuffTrack, BuffClip>(new BuffTrack(3f))
                    .Clip(0, 0u, 10u, new BuffClip(5f))
                    .Bake());
                BakedLane<BuffTrack, BuffClip>.Bind(buff);
                var positions = new uint[] { 0 };
                var armor = new float[] { 10f };
                var cycles = new long[] { 0 };
                Timeline<BakedLane<BuffTrack, BuffClip>>.Seek(positions, true).Apply(armor, cycles);
                var first = F(armor[0]);
                Timeline<BakedLane<BuffTrack, BuffClip>>.Seek(positions, true).Apply(armor, cycles);
                var second = F(armor[0]);
                Timeline<BakedLane<BuffTrack, BuffClip>>.Seek(positions, false).Apply(armor, cycles);
                var third = F(armor[0]);
                return first + "#" + second + "#" + third;
            }
        }
        """;
}
