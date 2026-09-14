using System;
using System.Security.Cryptography;
using Tl;
using Tl.Core.Tests;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public unsafe class MetadataTests
{
    private static byte[] BakeUniqueAsset()
    {
        var json = """
        {
          "duration": 4,
          "loop": false,
          "tracks": [
            {
              "namespace": "Tlb",
              "type": "AlphaTrack",
              "data": { "Code": 77 },
              "clips": [
                { "namespace": "Tlb", "type": "AlphaClip", "start": 0, "end": 2, "data": { "Value": 11 } },
                { "namespace": "Tlb", "type": "AlphaClip", "start": 2, "end": 4, "data": { "Value": 22 } }
              ]
            }
          ]
        }
        """;
        return TimelineBaker.BakeJson(json);
    }

    [Fact]
    public void MetadataTailAndStrippedCopyBothBindTheirKernel()
    {
        var full = BakeUniqueAsset();
        var stripped = TlbMetadata.Strip(full);
        var hotHash = SHA256.HashData(stripped);
        Assert.True(TlbMetadata.HasMetadata(full));
        Assert.False(TlbMetadata.HasMetadata(stripped));

        SpyKernel.SpyHit = false;
        TimelineKernels.Register(
            BinaryWord(hotHash, 0),
            BinaryWord(hotHash, 1),
            BinaryWord(hotHash, 2),
            BinaryWord(hotHash, 3),
            &SpyKernel.Tick);

        using (var fullAsset = TimelineAsset.Load(full))
        {
            var rows = new[] { new TimelineComponent(fullAsset.Reference) };
            var query = Timeline.Rows(rows);
            query.Tick(0u, 1);
            query.Tick(1u, 1);
        }
        Assert.True(SpyKernel.SpyHit);

        SpyKernel.SpyHit = false;
        using (var strippedAsset = TimelineAsset.Load(TlbMetadata.Strip(full)))
        {
            var rows = new[] { new TimelineComponent(strippedAsset.Reference) };
            var query = Timeline.Rows(rows);
            query.Tick(0u, 1);
            query.Tick(1u, 1);
        }
        Assert.True(SpyKernel.SpyHit);
    }

    [Fact]
    public void MetadataAssetBindsItsGeneratedKernelWithInterpreterIdenticalEffects()
    {
        var bytes = TimelineBaker.BakeJson(Recording.OracleJson);
        Assert.True(TlbMetadata.HasMetadata(bytes));
        using var kernelAsset = TimelineAsset.Load(bytes);
        using var interpreterAsset = TimelineAsset.Load(InterpreterCopy(bytes));
        var kernelRows = new[] { new TimelineComponent(kernelAsset.Reference) };
        var interpreterRows = new[] { new TimelineComponent(interpreterAsset.Reference) };

        var bound = TimelineKernels.Bound;
        Recording.Records.Clear();
        Timeline.Rows(kernelRows).Tick(5u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        var kernelEffects = Recording.Records.ToArray();
        Assert.NotEmpty(kernelEffects);
        Assert.Equal(1u, kernelRows[0].Position);

        Recording.Records.Clear();
        Timeline.Rows(interpreterRows).Tick(5u, 1);
        Assert.Equal(bound + 1, TimelineKernels.Bound);
        Assert.Equal(kernelEffects, Recording.Records.ToArray());
        Assert.Equal(kernelRows[0].Position, interpreterRows[0].Position);
        Assert.Equal(kernelRows[0].Cycle, interpreterRows[0].Cycle);

        Recording.Records.Clear();
        Timeline.Rows(kernelRows).Tick(5u, -2);
        var kernelBackward = Recording.Records.ToArray();
        Recording.Records.Clear();
        Timeline.Rows(interpreterRows).Tick(5u, -2);
        Assert.Equal(kernelBackward, Recording.Records.ToArray());
        Assert.Equal(kernelRows[0].Position, interpreterRows[0].Position);
        Assert.Equal(0u, kernelRows[0].Position);

        var kernelStaggered = new[]
        {
            new TimelineComponent(kernelAsset.Reference),
            new TimelineComponent(kernelAsset.Reference) { Position = 3u },
            new TimelineComponent(kernelAsset.Reference) { Position = 3u, Cycle = 1L },
        };
        var interpreterStaggered = new[]
        {
            new TimelineComponent(interpreterAsset.Reference),
            new TimelineComponent(interpreterAsset.Reference) { Position = 3u },
            new TimelineComponent(interpreterAsset.Reference) { Position = 3u, Cycle = 1L },
        };

        Recording.Records.Clear();
        Timeline.Rows(kernelStaggered).Tick(2u, 1);
        var kernelMixed = Recording.Records.ToArray();

        Recording.Records.Clear();
        Timeline.Rows(interpreterStaggered).Tick(2u, 1);
        Assert.Equal(kernelMixed, Recording.Records.ToArray());
        for (var row = 0; row < kernelStaggered.Length; row++)
        {
            Assert.Equal(kernelStaggered[row].Position, interpreterStaggered[row].Position);
            Assert.Equal(kernelStaggered[row].Cycle, interpreterStaggered[row].Cycle);
        }
    }

    private static byte[] InterpreterCopy(byte[] baked)
    {
        var copy = (byte[])baked.Clone();
        copy[40] = 0xA5;
        return copy;
    }

    private static ulong BinaryWord(byte[] hash, int word) =>
        BitConverter.ToUInt64(hash, 8 * word);

    [Fact]
    public void KernelEmissionHashesHotRegion()
    {
        var full = BakeUniqueAsset();
        var stripped = TlbMetadata.Strip(full);

        var fullSource = KernelEmitter.Emit(full);
        var strippedSource = KernelEmitter.Emit(stripped);

        Assert.Equal(fullSource, strippedSource);
    }

    [Fact]
    public void MetadataAbsentAssetsStripToIdenticalCopy()
    {
        var codeBytes = new Baker()
            .Track<Tlb.AlphaTrack, Tlb.AlphaClip>(new Tlb.AlphaTrack(3))
            .Clip(0, 0, 4, new Tlb.AlphaClip(9))
            .Bake();

        Assert.False(TlbMetadata.HasMetadata(codeBytes));
        Assert.Equal(codeBytes, TlbMetadata.Strip(codeBytes));
        Assert.Throws<ArgumentException>(() => { TlbMetadata.Read(codeBytes); });
    }

    [Fact]
    public void ReadParsesSortedPoolAndAlignedPairTypes()
    {
        var full = BakeUniqueAsset();
        var view = TlbMetadata.Read(full);

        for (var i = 1; i < view.Strings.Count; i++)
            Assert.True(string.CompareOrdinal(view.Strings[i - 1], view.Strings[i]) < 0);

        var pair = Assert.Single(view.PairTypes);
        var track = view.Types[pair.TrackType];
        var clip = view.Types[pair.ClipType];
        Assert.Equal("AlphaTrack", track.Name);
        Assert.Equal("Tlb", track.Namespace);
        Assert.Equal("AlphaClip", clip.Name);
        Assert.Equal(typeof(Tlb.AlphaTrack).Assembly.GetName().Name, track.Assembly);

        var trackIndex = BitConverter.ToUInt32(full, 16);
        Assert.Equal(1u, trackIndex);
    }
}

public static unsafe class SpyKernel
{
    public static bool SpyHit;

    public static bool Tick(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)
    {
        SpyHit = true;
        return true;
    }
}
