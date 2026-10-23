using System.Text;
using Tl;
using Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class ConformanceVectorTests
{
    public static IEnumerable<object[]> VectorNames()
        => ConformanceVectors.Names.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(VectorNames))]
    public void Vector_RegeneratesBitForBit(string name)
    {
        var rendered = ConformanceVectors.Render(ConformanceVectors.Build(name));
        Assert.Equal(rendered, File.ReadAllText(ConformanceVectors.VectorPath(name)));
    }

    [Theory]
    [MemberData(nameof(VectorNames))]
    public void Vector_ReplaysThroughTheRuntime(string name)
    {
        var vector = ConformanceVectors.Parse(name, File.ReadAllText(ConformanceVectors.VectorPath(name)));
        var bytes = ConformanceVectors.SourceBytes(name);
        Assert.Equal(vector.TlbSha256, ConformanceVectors.Sha256Hex(bytes));
        var index = TimelineAsset.Load(bytes);
        Assert.Equal(vector.Pairs.Select(p => p.PairKey).OrderBy(k => k), vector.Pairs.Select(p => p.PairKey));
        foreach (var section in vector.Pairs)
        {
            if (section.Fold == ConformanceConsumers.JobFold) ReplayPair<JobTrack, JobClip>(index, vector, section);
            else ReplayPair<FoldTrack, FoldClip>(index, vector, section);
        }
    }

    static void ReplayPair<TTrack, TClip>(ushort index, ConformanceVector vector, ConformancePair section)
        where TTrack : unmanaged, IBlend<TClip>
        where TClip : unmanaged
    {
        _ = Timeline<TTrack, TClip>.View(index);
        var copy = Inspection.CopyLanes<TTrack, TClip>(index);
        Assert.NotNull(copy);
        Assert.Equal(vector.Duration, copy!.Duration);
        Assert.Equal(vector.Looping, copy.Looping);
        Assert.Equal(section.Lanes.Length, copy.Lanes.Count);
        for (var l = 0; l < section.Lanes.Length; l++)
        {
            Assert.Equal(section.Lanes[l].Key, copy.Lanes[l].Key);
            Assert.Equal(section.Lanes[l].Forward, copy.Lanes[l].Forward);
            Assert.Equal(section.Lanes[l].Backward, copy.Lanes[l].Backward);
        }
        foreach (var entry in section.Clock)
        {
            var effects = new float[1];
            Timeline<TTrack, TClip>.Apply(index, entry.Position, entry.Forward, effects);
            Assert.Equal(entry.Effect, ConformanceVectors.Bits(effects[0]));
            var position = entry.Position;
            Timeline<TTrack, TClip>.Advance(index, ref position, entry.Forward);
            Assert.Equal(entry.Next, position);
        }
    }

    [Fact]
    public void Vector_FilesEmitOnRequest()
    {
        if (Environment.GetEnvironmentVariable("TL_EMIT_VECTORS") != "1") return;
        Directory.CreateDirectory(Path.Combine(ConformanceVectors.GoldenRoot(), "vectors"));
        foreach (var name in ConformanceVectors.Names)
            File.WriteAllText(ConformanceVectors.VectorPath(name), ConformanceVectors.Render(ConformanceVectors.Build(name)), new UTF8Encoding(false));
    }
}
