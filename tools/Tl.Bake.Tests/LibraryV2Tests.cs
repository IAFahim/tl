extern alias TlbTools;
using System;
using System.IO;
using System.Reflection;
using Tl;
using Tl.Gen.Tlb;
using TlbTools::Tl.Tlb;
using Xunit;

namespace Tlb
{

public enum Stat : byte { Health = 1, Mana = 2 }

public readonly struct StatClip(Stat stat, int delta)
{
    public readonly Stat Stat = stat;
    public readonly int Delta = delta;
}

public readonly struct StatTrack(float weight) : IBlend<StatClip>
{
    public readonly float Weight = weight;

    public void Blend(in StatClip first, in StatClip second, float factor, out StatClip result)
        => result = first;
}

public readonly struct GotoClip(TimelineId target)
{
    public readonly TimelineId Target = target;
}

public readonly struct GotoTrack(float scale) : IBlend<GotoClip>
{
    public readonly float Scale = scale;

    public void Blend(in GotoClip first, in GotoClip second, float factor, out GotoClip result)
        => result = first;
}

public readonly struct FootMarker(int kind)
{
    public readonly int Kind = kind;
}
}

namespace Tl.Bake.Tests
{

public class LibraryV2Tests
{
    static string Golden(params string[] parts)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine([dir, "golden", .. parts]);
    }

    static BakerAssemblyResolver Resolver()
        => BakerAssemblyResolver.FromAssemblies([typeof(LibraryV2Tests).Assembly]);

    [Fact]
    public void NextWithoutManifestFailsWithLibraryRepair()
    {
        var ex = Assert.Throws<BakeDiagnosticException>(() =>
            TimelineBaker.BakeJson("{\"duration\":4,\"next\":\"jump\",\"tracks\":[]}", Resolver()));
        Assert.Contains("tlb library", ex.Message);
    }

    [Fact]
    public void HoldAndMarkersBakeToV6()
    {
        var bytes = TimelineBaker.BakeJson(File.ReadAllBytes(Golden("fixtures", "v2_hold_markers.json")), Resolver());
        var c = TlbContainer.Parse(bytes);
        Assert.Equal(6u, c.Version);
        Assert.True(c.Hold);
        Assert.Equal(2, c.Markers.Length);
        Assert.Equal(2u, c.Markers[0].Tick);
        Assert.Equal(6u, c.Markers[1].Tick);
        var inspect = TlbInspection.Inspect(bytes);
        Assert.Contains("\"schemaVersion\": 2", inspect);
        Assert.Contains("\"markers\"", inspect);
    }

    [Fact]
    public void EnumAndTimelineIdResolveThroughLibraryManifest()
    {
        var manifest = new LibraryManifest();
        manifest.Timelines["jump"] = 1;
        manifest.Timelines["slide"] = 2;
        manifest.Channels["locomotion"] = 1;
        var slide = TimelineBaker.BakeJson(File.ReadAllBytes(Golden("library", "slide.json")), Resolver(), false, manifest);
        var c = TlbContainer.Parse(slide);
        Assert.Equal(1u, c.NextId);
        Assert.Equal(1u, c.Channel);
        var clipPool = c.Pairs[0].ClipPool;
        Assert.Equal(2u, clipPool.ValueBytes);
        var target = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(clipPool.Values[0].AsSpan());
        Assert.Equal(1, target);
        var jump = TimelineBaker.BakeJson(File.ReadAllBytes(Golden("library", "jump.json")), Resolver(), false, manifest);
        var jc = TlbContainer.Parse(jump);
        var foundHealth = false;
        var foundMana = false;
        foreach (var v in jc.Pairs[0].ClipPool.Values)
        {
            var stat = v[0];
            var delta = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(v.AsSpan(4));
            if (stat == 1 && delta == -1) foundHealth = true;
            if (stat == 2 && delta == 2) foundMana = true;
        }
        Assert.True(foundHealth && foundMana);
    }

    [Fact]
    public void LibraryPacksAndLoadsWorldWithPerTickSteps()
    {
        var temp = Path.Combine(Path.GetTempPath(), "tl-lib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var name in new[] { "library.json", "jump.json", "slide.json" })
                File.Copy(Golden("library", name), Path.Combine(temp, name));
            var rc = LibraryPack.Run([temp, "--assembly", typeof(LibraryV2Tests).Assembly.Location]);
            Assert.Equal(0, rc);
            var gameTlib = File.ReadAllBytes(Path.Combine(temp, "game.tlib"));
            using var world = new TimelineWorld(gameTlib);
            Assert.Equal(2, world.View.TimelineCount);
            var view = world.View;
            unsafe
            {
                var jumpTl = view.Timelines + 0;
                Assert.Equal(1, jumpTl->Id);
                Assert.Equal(1, jumpTl->Channel);
                Assert.Equal(8u, jumpTl->Duration);
                var slideTl = view.Timelines + 1;
                Assert.Equal(2, slideTl->Id);
                Assert.Equal(1, slideTl->Next);
                foreach (var t in new long[] { 0, 3, 4, 7 }) Assert.True(LibraryViewReader.HasEvent(jumpTl, (uint)t));
                foreach (var t in new long[] { 1, 2, 5, 6 }) Assert.False(LibraryViewReader.HasEvent(jumpTl, (uint)t));
            }
            var id = new TimelineId(1);
            for (var tick = 0; tick < 8; tick++)
            {
                unsafe
                {
                    var wv = world.View;
                    var tl = LibraryViewReader.GetTimeline(in wv, id);
                    Assert.True((nint)tl != 0);
                    var stage = LibraryViewReader.StageAt(tl, (ushort)tick);
                    Assert.True((nint)stage != 0);
                    var steps = LibraryViewReader.StepsFor(tl, stage);
                    Assert.True(steps.Length > 0);
                    var step = LibraryViewReader.FrameAt(tl, (ushort)tick, 0);
                    Assert.True((nint)step != 0);
                    Assert.True(tick >= step->WindowStart && tick < step->WindowEnd);
                    Assert.True(step->WindowEnd > step->WindowStart);
                }
            }
        }
        finally { try { Directory.Delete(temp, true); } catch { } }
    }

    [Fact]
    public void CorruptLibrariesAreRejected()
    {
        var temp = Path.Combine(Path.GetTempPath(), "tl-lib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var name in new[] { "library.json", "jump.json", "slide.json" })
                File.Copy(Golden("library", name), Path.Combine(temp, name));
            Assert.Equal(0, LibraryPack.Run([temp, "--assembly", typeof(LibraryV2Tests).Assembly.Location]));
            var good = File.ReadAllBytes(Path.Combine(temp, "game.tlib"));
            var badMagic = (byte[])good.Clone();
            badMagic[0] ^= 0xFF;
            Assert.Throws<ArgumentException>(() => new TimelineWorld(badMagic));
            Assert.Throws<ArgumentException>(() => new TimelineWorld(good.AsSpan(0, 10)));
            var badDigest = (byte[])good.Clone();
            badDigest[^1] ^= 0x01;
            Assert.Throws<ArgumentException>(() => new TimelineWorld(badDigest));
            var badBlob = (byte[])good.Clone();
            badBlob[100] ^= 0xFF;
            var fixedDigest = (byte[])badBlob.Clone();
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(fixedDigest, 0, fixedDigest.Length - 32);
                Array.Copy(hash, 0, fixedDigest, fixedDigest.Length - 32, 32);
            }
            Assert.Throws<ArgumentException>(() => new TimelineWorld(fixedDigest));
            for (var i = 0; i < 64; i++)
            {
                var mutated = (byte[])good.Clone();
                mutated[i] ^= 0xFF;
                Assert.Throws<ArgumentException>(() => new TimelineWorld(mutated));
            }
        }
        finally { try { Directory.Delete(temp, true); } catch { } }
    }

    [Fact]
    public void TlbV6RejectionsAreLocated()
    {
        var manifest = new LibraryManifest();
        var bytes = TimelineBaker.BakeJson(File.ReadAllBytes(Golden("fixtures", "v2_hold_markers.json")), Resolver());
        var badLoopNext = (byte[])bytes.Clone();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(badLoopNext.AsSpan(8), 1u);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(badLoopNext.AsSpan(52), 7u);
        Assert.Throws<ArgumentException>(() => TimelineRef.Validate(badLoopNext));
        var badMarker = (byte[])bytes.Clone();
        var markerOff = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(badMarker.AsSpan(64));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(badMarker.AsSpan((int)markerOff), 99u);
        Assert.Throws<ArgumentException>(() => TimelineRef.Validate(badMarker));
    }
}
}
