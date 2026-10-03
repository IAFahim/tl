using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl;
using Tl.TestSupport;
using Tl.Tlb;
using Xunit;

namespace TlbTests;

public readonly struct WaveClip(float amount, int steps)
{
    public readonly float Amount = amount;
    public readonly int Steps = steps;
}

public readonly struct WaveTrack(float multiplier) : IBlend<WaveClip>
{
    public readonly float Multiplier = multiplier;

    public void Blend(in WaveClip first, in WaveClip second, float factor, out WaveClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor, first.Steps);
}

public readonly struct PulseClip(float amount)
{
    public readonly float Amount = amount;
}

public readonly struct PulseTrack(float scale) : IBlend<PulseClip>
{
    public readonly float Scale = scale;

    public void Blend(in PulseClip first, in PulseClip second, float factor, out PulseClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}

public sealed class ContainerRoundTrip
{
    static byte[] Image<T>(T value) where T : unmanaged
    {
        var image = new byte[Unsafe.SizeOf<T>()];
        MemoryMarshal.Write(image, in value);
        return image;
    }

    static byte[] BakedBlended() => new DomainBaker()
        .Track<WaveTrack, WaveClip>(new WaveTrack(2f))
        .Clip(0, 0u, 12u, new WaveClip(3f, 4))
        .Clip(0, 8u, 20u, new WaveClip(7f, 4))
        .Bake();

    static byte[] BakedTwoPairs() => new DomainBaker()
        .Track<WaveTrack, WaveClip>(new WaveTrack(2f))
        .Track<PulseTrack, PulseClip>(new PulseTrack(5f))
        .Clip(0, 0u, 4u, new WaveClip(3f, 4))
        .Clip(1, 2u, 6u, new PulseClip(9f))
        .Bake();

    [Fact]
    public void HeaderStagesAndRowsMirrorTheAuthoredTimeline()
    {
        var c = TlbContainer.Parse(BakedBlended());
        Assert.Equal(4u, TlbContainer.HotVersion);
        Assert.Equal(0u, c.Loops);
        Assert.Equal(20u, c.Duration);
        Assert.Equal(1u, c.TrackCount);
        Assert.Single(c.Pairs);
        Assert.Equal(24u, c.Pairs[0].SlotStride);
        Assert.Equal(0ul, c.Pairs[0].Layout);
        Assert.Equal(3, c.Stages.Length);
        Assert.Equal(0u, c.Stages[0].Start);
        Assert.Equal(8u, c.Stages[0].End);
        Assert.Equal(8u, c.Stages[1].Start);
        Assert.Equal(12u, c.Stages[1].End);
        Assert.Equal(12u, c.Stages[2].Start);
        Assert.Equal(20u, c.Stages[2].End);
        Assert.Equal(3, c.Rows.Length);
        Assert.Equal(0u, c.Rows[0].TrackIndex);
        Assert.All(c.Rows, r => Assert.Equal(0u, r.TrackValueIndex));
        Assert.Equal(0u, c.Rows[0].WindowStart);
        Assert.Equal(12u, c.Rows[0].WindowEnd);
        Assert.Equal(TlbRow.NoClipValue, c.Rows[0].SecondValueIndex);
        Assert.Equal(0u, c.Rows[0].FactorSpan);
        Assert.Equal(0u, c.Rows[1].WindowStart);
        Assert.Equal(20u, c.Rows[1].WindowEnd);
        Assert.Equal(1u, c.Rows[1].SecondValueIndex);
        Assert.Equal(8u, c.Rows[1].FactorStart);
        Assert.Equal(4u, c.Rows[1].FactorSpan);
        Assert.Equal(8u, c.Rows[2].WindowStart);
        Assert.Equal(20u, c.Rows[2].WindowEnd);
        Assert.Equal(TlbRow.NoClipValue, c.Rows[2].SecondValueIndex);
    }

    [Fact]
    public void PoolValuesAreTheAuthoredStructBytes()
    {
        var c = TlbContainer.Parse(BakedBlended());
        var track = c.Pairs[0].TrackPool;
        var clips = c.Pairs[0].ClipPool;
        Assert.Equal(4u, track.ValueBytes);
        Assert.Single(track.Values);
        Assert.Equal(Image(new WaveTrack(2f)), track.Values[0]);
        Assert.Equal(8u, clips.ValueBytes);
        Assert.Equal(2u, clips.Count);
        Assert.Equal(Image(new WaveClip(3f, 4)), clips.Values[0]);
        Assert.Equal(Image(new WaveClip(7f, 4)), clips.Values[1]);
        Assert.All(c.Rows, r => Assert.True(r.FirstValueIndex < clips.Count));
    }

    [Fact]
    public void LayoutOffsetsSatisfyThePinnedPlacement()
    {
        var c = TlbContainer.Parse(BakedBlended());
        Assert.Equal(64u, c.PairOffset);
        Assert.Equal(64u + 48u * 1, c.StageOffset);
        var steps = c.Stages.Sum(s => s.Steps.Length);
        Assert.Equal(c.StageOffset + 16u * 3, c.Stages[0].ProgramOffset);
        Assert.Equal(c.FrameOffset + 24u * steps, c.HotLength);
        Assert.Equal((uint)BakedBlended().Length, c.Bytes);
        Assert.Equal(c.Bytes, c.HotLength);
        Assert.Null(c.Tail);
    }

    [Fact]
    public void TwoTypePairsAscendAndCarryDistinctPools()
    {
        var c = TlbContainer.Parse(BakedTwoPairs());
        Assert.Equal(2, c.Pairs.Length);
        Assert.True(c.Pairs[0].Key < c.Pairs[1].Key);
        Assert.Equal(4u, c.Pairs[0].TrackPool.ValueBytes);
        Assert.Equal(4u, c.Pairs[1].TrackPool.ValueBytes);
        Assert.Equal(8u, c.Pairs[0].ClipPool.ValueBytes);
        Assert.Equal(4u, c.Pairs[1].ClipPool.ValueBytes);
        Assert.Equal(Image(new PulseClip(9f)), c.Pairs[1].ClipPool.Values[0]);
        Assert.Null(c.Tail);
    }

    [Fact]
    public void ContainerJsonRoundTripsEverySection()
    {
        var c = TlbContainer.Parse(BakedBlended());
        var json = TlbJson.Write(c);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("container", root.GetProperty("plane").GetString());
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(20u, root.GetProperty("header").GetProperty("duration").GetUInt32());
        var pairs = root.GetProperty("pairs");
        Assert.Single(pairs.EnumerateArray());
        Assert.Equal(2u, pairs[0].GetProperty("clipPool").GetProperty("count").GetUInt32());
        Assert.Equal(2, pairs[0].GetProperty("clipPool").GetProperty("values").GetArrayLength());
        Assert.Equal(3, root.GetProperty("stages").GetArrayLength());
        Assert.Equal(3, root.GetProperty("rows").GetArrayLength());
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => root.GetProperty("tail"));
    }
}

public sealed class ContainerRejection
{
    static byte[] CopyOf(byte[] source) => [.. source];

    [Fact]
    public void TruncatedFileIsRejected()
    {
        var ex = Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(new byte[63]));
        Assert.Equal("TLB truncated.", ex.Message);
    }

    [Fact]
    public void UnknownMagicOrVersionIsRejected()
    {
        var blob = CopyOf(ContainerRoundTripBaked());
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(0), 0x32424C54);
        Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob));
        blob = CopyOf(ContainerRoundTripBaked());
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(4), 5);
        var ex = Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob));
        Assert.Equal("TLB magic or version invalid; rebake the asset with the current toolchain.", ex.Message);
    }

    [Fact]
    public void HeaderByteFieldsAreCrossChecked()
    {
        var blob = CopyOf(ContainerRoundTripBaked());
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(48), (uint)blob.Length + 1);
        Assert.Contains("does not match the file length", Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob)).Message);
        blob = CopyOf(ContainerRoundTripBaked());
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(44), (uint)blob.Length + 4);
        Assert.Contains("HotLength", Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob)).Message);
        blob = CopyOf(ContainerRoundTripBaked());
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan(28), 65);
        Assert.Contains("PairOffset", Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob)).Message);
    }

    [Fact]
    public void StageContiguityBreakIsRejected()
    {
        var blob = CopyOf(ContainerRoundTripBaked());
        var c = TlbContainer.Parse(blob);
        BinaryPrimitives.WriteUInt32LittleEndian(blob.AsSpan((int)c.StageOffset + 4), 7);
        Assert.Contains("contiguity", Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob)).Message);
    }

    [Fact]
    public void RowIndexBeyondPoolIsRejected()
    {
        var blob = CopyOf(ContainerRoundTripBaked());
        var c = TlbContainer.Parse(blob);
        var slot = c.Stages[0].Steps[0].Slot;
        BinaryPrimitives.WriteUInt16LittleEndian(blob.AsSpan((int)slot + 2), 9);
        Assert.Contains("FirstValueIndex", Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob)).Message);
    }

    [Fact]
    public void SecondIndexWithoutBlendWindowIsRejected()
    {
        var blob = CopyOf(ContainerRoundTripBaked());
        var c = TlbContainer.Parse(blob);
        var slot = c.Stages[0].Steps[0].Slot;
        BinaryPrimitives.WriteUInt16LittleEndian(blob.AsSpan((int)slot + 4), 1);
        Assert.Contains("blend window", Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob)).Message);
    }

    [Fact]
    public void PairKeyOrderIsRejectedWhenNotAscending()
    {
        var blob = CopyOf(TwoPairBaked());
        var c = TlbContainer.Parse(blob);
        Assert.Equal(2, c.Pairs.Length);
        var second = BinaryPrimitives.ReadUInt64LittleEndian(blob.AsSpan((int)c.PairOffset));
        BinaryPrimitives.WriteUInt64LittleEndian(blob.AsSpan((int)c.PairOffset + 48), second - 1);
        Assert.Contains("strictly ascend", Assert.Throws<TlbFormatException>(() => TlbContainer.Parse(blob)).Message);
    }

    static byte[] ContainerRoundTripBaked() => new DomainBaker()
        .Track<WaveTrack, WaveClip>(new WaveTrack(2f))
        .Clip(0, 0u, 12u, new WaveClip(3f, 4))
        .Clip(0, 8u, 20u, new WaveClip(7f, 4))
        .Bake();

    static byte[] TwoPairBaked() => new DomainBaker()
        .Track<WaveTrack, WaveClip>(new WaveTrack(2f))
        .Track<PulseTrack, PulseClip>(new PulseTrack(5f))
        .Clip(0, 0u, 4u, new WaveClip(3f, 4))
        .Clip(1, 2u, 6u, new PulseClip(9f))
        .Bake();
}

public sealed class FieldMapAndTrace
{
    static byte[] BakedBlended() => new DomainBaker()
        .Track<WaveTrack, WaveClip>(new WaveTrack(2f))
        .Clip(0, 0u, 12u, new WaveClip(3f, 4))
        .Clip(0, 8u, 20u, new WaveClip(7f, 4))
        .Bake();

    static byte[] BakedSameStage() => new DomainBaker()
        .Track<WaveTrack, WaveClip>(new WaveTrack(2f))
        .Track<PulseTrack, PulseClip>(new PulseTrack(5f))
        .Clip(0, 0u, 8u, new WaveClip(3f, 4))
        .Clip(1, 0u, 8u, new PulseClip(9f))
        .Bake();

    static IEnumerable<(byte[] Blob, TlbContainer Container)> Corpus()
    {
        yield return (BakedBlended(), TlbContainer.Parse(BakedBlended()));
        yield return (BakedSameStage(), TlbContainer.Parse(BakedSameStage()));
        foreach (var name in new[] { "minimal", "blended" })
        {
            var path = Path.Combine(AppContext.BaseDirectory, name + ".tlb");
            if (!File.Exists(path)) continue;
            var blob = File.ReadAllBytes(path);
            yield return (blob, TlbContainer.Parse(blob));
        }
    }

    [Fact]
    public void FieldsTileEveryByteExactlyOnce()
    {
        foreach (var (blob, c) in Corpus())
        {
            var fields = TlbMap.Fields(c, blob);
            Assert.NotEmpty(fields);
            Assert.Equal(0u, fields[0].Start);
            for (var i = 1; i < fields.Length; i++)
            {
                Assert.True(fields[i].Length > 0, $"{fields[i].Path} is empty");
                Assert.Equal(fields[i - 1].End, fields[i].Start);
            }
            Assert.Equal(c.Bytes, fields[^1].End);
        }
    }

    [Fact]
    public void FieldsDecodeTheAuthoredValues()
    {
        var blob = BakedBlended();
        var c = TlbContainer.Parse(blob);
        var fields = TlbMap.Fields(c, blob).ToDictionary(f => f.Path, f => f.Value);
        Assert.Equal("\"TLB1\"", fields["header.magic"]);
        Assert.Equal("4", fields["header.version"]);
        Assert.Equal("20", fields["header.duration"]);
        Assert.Equal("0", fields["header.loops"]);
        Assert.Equal("00000040 · f32 2", fields["pair 0 track value 0"]);
        Assert.Equal("65535 (none)", fields["row @0xE0.secondValueIndex"]);
        Assert.Equal("1", fields["row @0xF8.secondValueIndex"]);
        Assert.Equal("8", fields["row @0xF8.factorStart"]);
    }

    [Fact]
    public void LoadTraceCoversEveryByte()
    {
        foreach (var (blob, c) in Corpus())
        {
            var covered = new bool[blob.Length];
            foreach (var access in TlbTrace.Load(c))
                for (var i = (int)access.Start; i < (int)access.End; i++)
                    covered[i] = true;
            Assert.All(covered, Assert.True);
        }
    }

    [Fact]
    public void TickTraceReadsExactlyTheExecutingStructure()
    {
        var c = TlbContainer.Parse(BakedBlended());
        Assert.Equal(224u, c.FrameOffset);

        var early = TlbTrace.Tick(c, 5, false);
        DoesNotContain(early, 0xF8 + 4, 0xF8 + 6);
        DoesNotContain(early, 0xE0 + 16, 0xE0 + 20);
        Contains(early, 192, 196);
        Contains(early, 208, 216);
        DoesNotContain(early, 216, 224);
        Assert.Contains(early, a => a.What == "stage 0 entry" && a.Why.Contains("hit"));
        Assert.DoesNotContain(early, a => a.What == "stage 1 entry");

        var blend = TlbTrace.Tick(c, 10, false);
        Contains(blend, 0xF8 + 4, 0xF8 + 6);
        Contains(blend, 0xF8 + 16, 0xF8 + 20);
        Contains(blend, 208, 216);
        Contains(blend, 216, 224);
        Assert.Contains(blend, a => a.What == "stage 0 entry" && a.Why.Contains("scan"));
        Assert.Contains(blend, a => a.What == "stage 1 entry" && a.Why.Contains("hit"));

        var late = TlbTrace.Tick(c, 19, false);
        DoesNotContain(late, 0x110 + 4, 0x110 + 6);
        Contains(late, 216, 224);
        DoesNotContain(late, 208, 216);

        var past = TlbTrace.Tick(c, 20, false);
        Assert.DoesNotContain(past, a => a.What.Contains("step"));
        Assert.DoesNotContain(past, a => a.What.Contains("value"));
        Assert.Equal(3, past.Count(a => a.Why.Contains("scan")));

        var reverse = TlbTrace.Tick(TlbContainer.Parse(BakedSameStage()), 3, true);
        var order = string.Join("|", reverse.Select(a => a.What));
        var firstStep = order.IndexOf("step 1", StringComparison.Ordinal);
        var secondStep = order.IndexOf("step 0", StringComparison.Ordinal);
        Assert.True(firstStep >= 0 && secondStep >= 0 && firstStep < secondStep);
    }

    static void Contains(TlbAccess[] accesses, uint start, uint end) =>
        Assert.Contains(accesses, a => a.Start == start && a.End == end);

    static void DoesNotContain(TlbAccess[] accesses, uint start, uint end) =>
        Assert.DoesNotContain(accesses, a => a.Start == start && a.End == end);
}

public sealed class GoldenFixtureCorpus
{
    [Fact]
    public void GoldenFixturesParseWithTheWorkedExampleFacts()
    {
        var found = 0;
        foreach (var name in new[] { "minimal", "blended" })
        {
            var path = Path.Combine(AppContext.BaseDirectory, name + ".tlb");
            if (!File.Exists(path)) continue;
            found++;
            var blob = File.ReadAllBytes(path);
            var c = TlbContainer.Parse(blob);
            Assert.Equal((uint)blob.Length, c.Bytes);
            Assert.True(c.HotLength < c.Bytes, "fixture containers carry the cold tail");
            Assert.Equal(c.Pairs.Length, c.Tail!.PairTypes.Length);
            foreach (var (track, clip) in c.Tail.PairTypes)
            {
                Assert.EndsWith(", Tl.Bake.Tests", c.Tail.TypeName(track), StringComparison.Ordinal);
                Assert.EndsWith(", Tl.Bake.Tests", c.Tail.TypeName(clip), StringComparison.Ordinal);
            }
            if (name == "minimal")
            {
                Assert.Equal(200u, c.HotLength);
                Assert.Equal(327u, c.Bytes);
                Assert.Equal(4u, c.Duration);
                Assert.Single(c.Stages);
                Assert.Equal("Tlb.JobTrack, Tl.Bake.Tests", c.Tail.TypeName(c.Tail.PairTypes[0].TrackType));
                Assert.Equal("Tlb.JobClip, Tl.Bake.Tests", c.Tail.TypeName(c.Tail.PairTypes[0].ClipType));
            }
            if (name == "blended")
            {
                Assert.Equal(680u, c.HotLength);
                Assert.Equal(1021u, c.Bytes);
                Assert.Equal(6, c.Stages.Length);
            }
        }
        Assert.True(found <= 2, "unexpected fixture count");
    }
}
