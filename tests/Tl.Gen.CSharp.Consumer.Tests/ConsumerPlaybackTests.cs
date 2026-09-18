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

    [Fact]
    public void DiscoveredBakesInstallDispatchEntriesIntoTheRuntimeTable()
    {
        var result = Driver("Bakes");

        Assert.Equal("4;1|Entity;2|World+Entity;2|World+Entity;3|World+Entity+GuardToken#1;1|World#0", result);
    }

    [Fact]
    public void ManualInstallsAppendInCallOrderAndBoundsAreLoud()
    {
        var result = Driver("ManualBake");

        Assert.Equal("2;1|World;1|Entity#ArgumentOutOfRangeException", result);
    }

    [Fact]
    public void TimelineBakeAttachesMarkersSubsetsAndGatesAdvancementEndToEnd()
    {
        var result = Driver("BakeWalk");

        Assert.Equal("narrow:7,damage:7#7#silent#936|936#1000#2,2", result);
    }

    [Fact]
    public void TimelineBakeWalksPairsInPairKeyOrder()
    {
        var result = Driver("BakePairOrder");

        Assert.StartsWith("ordered:", result);
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
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct(StringComparer.Ordinal)
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

        public sealed class World { public readonly List<string> Marks = []; public readonly List<DamageTag> Tags = []; }

        public readonly record struct Entity(World Owner, int Id);

        public readonly record struct DamageTag(int Entity);

        public readonly record struct GuardToken(int Level);

        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void Execute(in Frame<DamageTrack, DamageClip> frame, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                health += frame.IsBackward ? amount : -amount;
            }
        }

        public readonly struct ApplyDamageBake : IBake<ApplyDamage, World, Entity>
        {
            public static void Bake(ApplyDamage consumer, World world, Entity entity) { world.Tags.Add(new DamageTag(entity.Id)); }
        }

        public readonly struct ApplyDamageNarrowBake : IBake<ApplyDamage, Entity>
        {
            public static void Bake(ApplyDamage consumer, Entity entity) { entity.Owner.Marks.Add("narrow:" + entity.Id.ToString(CultureInfo.InvariantCulture)); }
        }

        public readonly struct ApplyDamageWideBake : IBake<ApplyDamage, World, Entity>
        {
            public static void Bake(ApplyDamage consumer, World world, Entity entity) { world.Marks.Add("damage:" + entity.Id.ToString(CultureInfo.InvariantCulture)); }
        }

        public readonly struct ApplyDamageGuardBake : IBake<ApplyDamage, World, Entity, GuardToken>
        {
            public static void Bake(ApplyDamage consumer, World world, Entity entity, GuardToken token) { world.Tags.Add(new DamageTag(-entity.Id)); }
        }

        public readonly struct ApplyHeal : ITrack<HealTrack, HealClip>, IBake<ApplyHeal, World>
        {
            public static void Execute(in Frame<HealTrack, HealClip> frame, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                health += frame.IsBackward ? -amount : amount;
            }

            public static void Bake(ApplyHeal consumer, World world) { world.Marks.Add("heal"); }
        }

        public readonly record struct BuffClip(float Amount);

        public readonly record struct BuffTrack(float Multiplier) : IBlend<BuffClip>
        {
            public void Blend(in BuffClip first, in BuffClip second, float factor, out BuffClip result)
                => result = new BuffClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly struct ApplyBuff : ITrack<BuffTrack, BuffClip>
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

        public readonly struct ApplyGuarded : ITrack<GuardTrack, GuardClip>
        {
            public static void Execute(in Frame<GuardTrack, GuardClip> frame, in Resistance resistance, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
                health += frame.IsBackward ? amount : -amount;
            }
        }

        internal sealed class Baker
        {
            internal sealed class BakedClip
        {
            public required uint Start { get; init; }
            public required uint End { get; init; }
            public required object Value { get; init; }
            public ushort PoolIndex { get; set; }
        }

        internal abstract class BakedTrack
        {
            public ulong Key;
            public byte Index;
            public ushort TrackValueIndex;
            public readonly List<BakedClip> Clips = [];

            public abstract int TrackValueBytes { get; }
            public abstract int ClipValueBytes { get; }
            public abstract byte[] TrackImage();
            public abstract byte[] ClipImage(BakedClip clip);
        }

        internal sealed class BakedTrack<TTrack, TClip> : BakedTrack
            where TTrack : unmanaged, IBlend<TClip>
            where TClip : unmanaged
        {
            public required TTrack TrackValue { get; init; }

            public override int TrackValueBytes => Unsafe.SizeOf<TTrack>();
            public override int ClipValueBytes => Unsafe.SizeOf<TClip>();

            public override byte[] TrackImage()
            {
                var value = TrackValue;
                var image = new byte[TrackValueBytes];
                MemoryMarshal.Write(image, in value);
                return image;
            }

            public override byte[] ClipImage(BakedClip clip)
            {
                var clipValue = (TClip)clip.Value!;
                var image = new byte[ClipValueBytes];
                MemoryMarshal.Write(image, in clipValue);
                return image;
            }
        }

        internal sealed class ImageBytesComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[]? left, byte[]? right)
            {
                if (ReferenceEquals(left, right)) return true;
                if (left is null || right is null || left.Length != right.Length) return false;
                for (var i = 0; i < left.Length; i++)
                    if (left[i] != right[i]) return false;
                return true;
            }

            public int GetHashCode(byte[] image)
            {
                var hash = new HashCode();
                hash.AddBytes(image);
                return hash.ToHashCode();
            }
        }

            private readonly List<BakedTrack> _tracks = [];
            private bool _loops;

            public Baker Track<TTrack, TClip>(TTrack value) where TTrack : unmanaged, IBlend<TClip> where TClip : unmanaged
            {
                _tracks.Add(new BakedTrack<TTrack, TClip>
                {
                    TrackValue = value,
                    Key = PairRuntime<TTrack, TClip>.Key,
                    Index = (byte)_tracks.Count,
                });
                return this;
            }

            public Baker Clip<TClip>(int track, uint start, uint end, TClip clip) where TClip : unmanaged
            {
                _tracks[track].Clips.Add(new BakedClip { Start = start, End = end, Value = clip });
                return this;
            }


            static int CompareImages(byte[] left, byte[] right)
            {
                var byLength = left.Length.CompareTo(right.Length);
                if (byLength != 0) return byLength;
                for (var i = 0; i < left.Length; i++)
                    if (left[i] != right[i])
                        return left[i].CompareTo(right[i]);
                return 0;
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

                var pairKeys = _tracks.Select(track => track.Key).Distinct().OrderBy(key => key).ToArray();
                var pairIndex = new Dictionary<ulong, int>();
                for (var index = 0; index < pairKeys.Length; index++)
                    pairIndex[pairKeys[index]] = index;

                var trackPools = new List<byte[]>[pairKeys.Length];
                var trackIndices = new Dictionary<byte[], ushort>[pairKeys.Length];
                var clipPools = new List<byte[]>[pairKeys.Length];
                var clipIndices = new Dictionary<byte[], ushort>[pairKeys.Length];
                var trackValueBytes = new int[pairKeys.Length];
                var clipValueBytes = new int[pairKeys.Length];
                for (var index = 0; index < pairKeys.Length; index++)
                {
                    var members = _tracks.Where(track => pairIndex[track.Key] == index).ToList();
                    var trackUnique = members.Select(track => track.TrackImage()).Distinct(new ImageBytesComparer()).ToList();
                    var clipUnique = members.SelectMany(track => track.Clips.Select(track.ClipImage)).Distinct(new ImageBytesComparer()).ToList();
                    trackUnique.Sort(CompareImages);
                    clipUnique.Sort(CompareImages);
                    if (trackUnique.Count > 65535 || clipUnique.Count > 65535)
                        throw new InvalidOperationException("Value pool overflow: ushort slot indices hold at most 65,535 unique values per pool.");
                    trackValueBytes[index] = members[0].TrackValueBytes;
                    clipValueBytes[index] = members[0].ClipValueBytes;
                    trackPools[index] = trackUnique;
                    clipPools[index] = clipUnique;
                    trackIndices[index] = new Dictionary<byte[], ushort>(trackUnique.Count, new ImageBytesComparer());
                    clipIndices[index] = new Dictionary<byte[], ushort>(clipUnique.Count, new ImageBytesComparer());
                    for (var i = 0; i < trackUnique.Count; i++) trackIndices[index][(byte[])trackUnique[i]] = (ushort)i;
                    for (var i = 0; i < clipUnique.Count; i++) clipIndices[index][(byte[])clipUnique[i]] = (ushort)i;
                }

                foreach (var track in _tracks)
                {
                    var index = pairIndex[track.Key];
                    track.TrackValueIndex = trackIndices[index][track.TrackImage()];
                    foreach (var clip in track.Clips)
                        clip.PoolIndex = clipIndices[index][track.ClipImage(clip)];
                }

                var pairOffset = 64u;
                var stageOffset = pairOffset + 48u * (uint)pairKeys.Length;
                var programBase = stageOffset + 16u * (uint)stageList.Count;
                var stepCount = stageList.Sum(active => active.Count);
                var poolOffset = (programBase + 8u * (uint)stepCount + 15u) & ~15u;

                var trackPoolOffsets = new uint[pairKeys.Length];
                var clipPoolOffsets = new uint[pairKeys.Length];
                var poolCursor = poolOffset;
                for (var index = 0; index < pairKeys.Length; index++)
                {
                    var pairAddress = pairOffset + 48u * (uint)index;
                    trackPoolOffsets[index] = poolCursor - pairAddress;
                    poolCursor = (poolCursor + (uint)trackPools[index].Count * (uint)trackValueBytes[index] + 15u) & ~15u;
                    clipPoolOffsets[index] = poolCursor - pairAddress;
                    poolCursor = (poolCursor + (uint)clipPools[index].Count * (uint)clipValueBytes[index] + 15u) & ~15u;
                }
                var frameOffset = (poolCursor + 15u) & ~15u;

                var occurrences = new List<(uint Offset, int Pair, BakedTrack Track, int Stage)>();
                var cursor = frameOffset;
                for (var stage = 0; stage < stageList.Count; stage++)
                    foreach (var track in stageList[stage])
                    {
                        occurrences.Add((cursor, pairIndex[track.Key], track, stage));
                        cursor += 24u;
                    }

                var bytes = new byte[cursor];
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x31424C54u);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 3u);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), _loops ? 1u : 0u);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), duration);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)_tracks.Count);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), (uint)stageList.Count);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), (uint)pairKeys.Length);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), pairOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), stageOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), poolOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(40), frameOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(44), (uint)bytes.Length);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), (uint)bytes.Length);

                for (var index = 0; index < pairKeys.Length; index++)
                {
                    var at = (int)pairOffset + 48 * index;
                    BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(at), pairKeys[index]);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 8), 24u);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 12), trackPoolOffsets[index]);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 16), (uint)trackPools[index].Count);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 20), (uint)trackValueBytes[index]);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 24), clipPoolOffsets[index]);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 28), (uint)clipPools[index].Count);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 32), (uint)clipValueBytes[index]);
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

                for (var index = 0; index < pairKeys.Length; index++)
                {
                    var at = (int)(pairOffset + 48u * (uint)index + trackPoolOffsets[index]);
                    foreach (var image in trackPools[index])
                    {
                        image.CopyTo(bytes, at);
                        at += image.Length;
                    }
                    at = (int)(pairOffset + 48u * (uint)index + clipPoolOffsets[index]);
                    foreach (var image in clipPools[index])
                    {
                        image.CopyTo(bytes, at);
                        at += image.Length;
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

                    var at = (int)occurrence.Offset;
                    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at), occurrence.Track.TrackValueIndex);
                    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at + 2), clips[0].PoolIndex);
                    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at + 4), clips.Length == 2 ? clips[1].PoolIndex : (ushort)0xFFFF);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 8), windowStart);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 12), windowEnd);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 16), factorStart);
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 20), factorSpan);
                    bytes[at + 6] = occurrence.Track.Index;
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
                using var asset = TimelineAsset.Of(TimelineAsset.Load(new Baker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(4f))
                    .Clip(0, 0u, 2u, new DamageClip(8f))
                    .Clip(0, 2u, 4u, new DamageClip(4f))
                    .Clip(0, 6u, 8u, new DamageClip(8f))
                    .Clip(0, 7u, 8u, new DamageClip(0f))
                    .Bake()));
                var positions = new ushort[] { 0 };
                var health = new float[] { 1000f };
                var forward = new List<string>();
                for (var tick = 0; tick < 8; tick++)
                {
                    Timeline<DamageTrack, DamageClip>.Advance(asset, positions, true, health);
                    forward.Add(F(health[0]));
                }
                var backward = new List<string>();
                for (var tick = 0; tick < 8; tick++)
                {
                    Timeline<DamageTrack, DamageClip>.Advance(asset, positions, false, health);
                    backward.Add(F(health[0]));
                }
                return string.Join("|", forward) + "#" + string.Join("|", backward) + "#" + F(health[0]) + "#" + Positions(positions[0]);
            }

            public static string Rows()
            {
                using var damage = TimelineAsset.Of(TimelineAsset.Load(new Baker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(4f))
                    .Clip(0, 0u, 2u, new DamageClip(8f))
                    .Bake()));
                using var heal = TimelineAsset.Of(TimelineAsset.Load(new Baker()
                    .Track<HealTrack, HealClip>(new HealTrack(1f))
                    .Clip(0, 0u, 2u, new HealClip(8f))
                    .Bake()));
                var damagePositions = new ushort[] { 0, 0 };
                var damageHealth = new float[] { 1000f, 250f };
                var healPositions = new ushort[] { 0 };
                var healHealth = new float[] { 500f };
                string Snapshot()
                    => F(damageHealth[0]) + "," + F(healHealth[0]) + "," + F(damageHealth[1]) + ","
                    + Positions(damagePositions[0], healPositions[0], damagePositions[1]);
                Timeline<DamageTrack, DamageClip>.Advance(damage, damagePositions, true, damageHealth);
                Timeline<HealTrack, HealClip>.Advance(heal, healPositions, true, healHealth);
                var first = Snapshot();
                Timeline<DamageTrack, DamageClip>.Advance(damage, damagePositions, true, damageHealth);
                Timeline<HealTrack, HealClip>.Advance(heal, healPositions, true, healHealth);
                var second = Snapshot();
                Timeline<DamageTrack, DamageClip>.Advance(damage, damagePositions, false, damageHealth);
                Timeline<HealTrack, HealClip>.Advance(heal, healPositions, false, healHealth);
                var third = Snapshot();
                return first + "#" + second + "#" + third;
            }

            public static string Missing()
            {
                using var guard = TimelineAsset.Of(TimelineAsset.Load(new Baker()
                    .Track<GuardTrack, GuardClip>(new GuardTrack(2f))
                    .Clip(0, 0u, 2u, new GuardClip(8f))
                    .Bake()));
                var positions = new ushort[] { 0 };
                var health = new float[] { 400f };
                try
                {
                    Timeline<GuardTrack, GuardClip>.Advance(guard, positions, true, health);
                }
                catch (ArgumentException exception)
                {
                    return "THROWN|" + exception.Message + "|" + F(health[0]) + "|" + Positions(positions[0]);
                }
                return "NOTHROWN|" + F(health[0]) + "|" + Positions(positions[0]);
            }

            public static string Standalone()
            {
                using var buff = TimelineAsset.Of(TimelineAsset.Load(new Baker()
                    .Track<BuffTrack, BuffClip>(new BuffTrack(3f))
                    .Clip(0, 0u, 10u, new BuffClip(5f))
                    .Bake()));
                var positions = new ushort[] { 0 };
                var armor = new float[] { 10f };
                Timeline<BuffTrack, BuffClip>.Advance(buff, positions, true, armor);
                var first = F(armor[0]);
                Timeline<BuffTrack, BuffClip>.Advance(buff, positions, true, armor);
                var second = F(armor[0]);
                Timeline<BuffTrack, BuffClip>.Advance(buff, positions, false, armor);
                var third = F(armor[0]);
                return first + "#" + second + "#" + third;
            }

            public static string Bakes()
                => Dump<DamageTrack, DamageClip>() + "#" + Dump<HealTrack, HealClip>() + "#" + Dump<BuffTrack, BuffClip>();

            public static string BakeWalk()
            {
                var world = new World();
                var entity = new Entity(world, 7);
                using var view = TimelineAsset.Of(TimelineAsset.Load(new Baker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(4f))
                    .Clip(0, 0u, 2u, new DamageClip(8f))
                    .Bake()));
                var timeline = view.Index;

                Timeline.Bake(timeline, world, entity);

                var marks = string.Join(",", world.Marks);
                var tags = string.Join(",", world.Tags.Select(static tag => tag.Entity.ToString(CultureInfo.InvariantCulture)));

                var bare = new World();
                Timeline.Bake(timeline, bare);
                var silent = bare.Marks.Count == 0 && bare.Tags.Count == 0 ? "silent" : "loud";

                var markedPositions = new ushort[] { 0 };
                var markedHealth = new float[] { 1000f };
                var oraclePositions = new ushort[] { 0 };
                var oracleHealth = new float[] { 1000f };
                var idlePositions = new ushort[] { 0 };
                var idleHealth = new float[] { 1000f };
                var idleWorld = new World();
                Timeline.Bake(timeline, idleWorld);
                for (var frame = 0; frame < 4; frame++)
                {
                    foreach (var tag in world.Tags)
                        Timeline<DamageTrack, DamageClip>.Advance(timeline, markedPositions, true, markedHealth);
                    foreach (var tag in idleWorld.Tags)
                        Timeline<DamageTrack, DamageClip>.Advance(timeline, idlePositions, true, idleHealth);
                    Timeline<DamageTrack, DamageClip>.Advance(view, oraclePositions, true, oracleHealth);
                }
                return marks + "#" + tags + "#" + silent + "#"
                    + F(markedHealth[0]) + "|" + F(oracleHealth[0]) + "#"
                    + F(idleHealth[0]) + "#" + Positions(markedPositions[0], oraclePositions[0]);
            }

            public static string BakePairOrder()
            {
                var world = new World();
                var entity = new Entity(world, 3);
                var timeline = TimelineAsset.Load(new Baker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
                    .Clip(0, 0u, 1u, new DamageClip(1f))
                    .Track<HealTrack, HealClip>(new HealTrack(1f))
                    .Clip(1, 0u, 1u, new HealClip(1f))
                    .Bake());
                Timeline.Bake(timeline, world, entity);
                var expected = PairRuntime<DamageTrack, DamageClip>.Key < PairRuntime<HealTrack, HealClip>.Key
                    ? new List<string> { "narrow:3", "damage:3", "heal" }
                    : new List<string> { "heal", "narrow:3", "damage:3" };
                return (world.Marks.SequenceEqual(expected) ? "ordered:" : "scrambled:") + string.Join(",", world.Marks);
            }

            public static unsafe string ManualBake()
            {
                BakeRuntime<GuardTrack, GuardClip>.Bake(&ManualGuardBakeA, TypeKey<World>.Value);
                BakeRuntime<GuardTrack, GuardClip>.Bake(&ManualGuardBakeB, TypeKey<Entity>.Value);
                var order = Dump<GuardTrack, GuardClip>();
                string loud;
                try
                {
                    _ = BakeRuntime<GuardTrack, GuardClip>.BakeContextCount(99);
                    loud = "silent";
                }
                catch (ArgumentOutOfRangeException)
                {
                    loud = "ArgumentOutOfRangeException";
                }
                return order + "#" + loud;
            }

            static void ManualGuardBakeA(object[] __tlArgs) { }

            static void ManualGuardBakeB(object[] __tlArgs) { }

            static string Dump<TTrack, TClip>()
                where TTrack : unmanaged, IBlend<TClip>
                where TClip : unmanaged
            {
                var parts = new List<string> { BakeRuntime<TTrack, TClip>.BakeCount.ToString(CultureInfo.InvariantCulture) };
                for (var index = 0; index < BakeRuntime<TTrack, TClip>.BakeCount; index++)
                {
                    var contexts = BakeRuntime<TTrack, TClip>.BakeContextCount(index);
                    var names = new List<string>();
                    for (var context = 0; context < contexts; context++)
                        names.Add(Key(BakeRuntime<TTrack, TClip>.BakeContextKey(index, context)));
                    parts.Add(contexts.ToString(CultureInfo.InvariantCulture) + "|" + string.Join("+", names));
                }
                return string.Join(";", parts);
            }

            static string Key(ulong value)
                => value == TypeKey<World>.Value ? "World"
                : value == TypeKey<Entity>.Value ? "Entity"
                : value == TypeKey<GuardToken>.Value ? "GuardToken"
                : value.ToString("X16", CultureInfo.InvariantCulture);
        }
        """;
}
