using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

internal static unsafe class InspectionPairs
{
    [ModuleInitializer]
    internal static void Install()
        => PairRuntime<SnapshotTrack, SnapshotClip>.Consume(&Execute, &Bind);

    static void Bind(ulong* keys, int keyCount, byte* table)
    {
    }

    static void Execute(byte* slot, byte* pair, ushort tick, FrameFlags flags, void** columns, int row)
    {
    }
}

public class InspectionTests
{
    [Fact]
    public void BankIsNullBeforeThePairIsTouched()
    {
        Assert.Null(Inspection.Bank<DormantTrack, DormantClip>());
    }

    [Fact]
    public void BankSnapshotReconcilesWithPublicViewAndRuntimeAccounting()
    {
        var (bank, assetA, assetB, assetC) = Populate(out var indexA, out var indexB, out var indexC);
        try
        {
            Assert.Equal(indexC + 1, bank.Count);
            Assert.Equal(bank.Views.Count(view => view.State != Inspection.FoldState.Folded), bank.Holes);
            Assert.Equal(bank.HeaderBytes + bank.TableBytes + bank.DirectoryBytes + bank.ArenaBytes, bank.RetainedBytes);

            var publicA = Timeline<SnapshotTrack, SnapshotClip>.View(assetA);
            var foldedA = bank.Views.First(view => view.Index == indexA);
            Assert.Equal(Inspection.FoldState.Folded, foldedA.State);
            Assert.Equal(publicA.Duration, foldedA.Duration);
            Assert.Equal(publicA.Looping != 0, foldedA.Looping);
            Assert.Equal((int)publicA.TableTicks, foldedA.Ticks);
            Assert.Equal((int)publicA.ResultCount, foldedA.Lanes);
            Assert.Equal((int)publicA.AbiVersion, foldedA.AbiVersion);
            Assert.Equal(unchecked((long)publicA.Generation), foldedA.Generation);
            Assert.NotNull(foldedA.LaneShapes);
            Assert.Equal(foldedA.Lanes, foldedA.LaneShapes!.Count);

            Assert.Equal(Inspection.FoldState.Absent, bank.Views.First(view => view.Index == indexB).State);
            Assert.Equal(Inspection.FoldState.Folded, bank.Views.First(view => view.Index == indexC).State);
            Assert.All(bank.Views.Where(view => view.State != Inspection.FoldState.Folded), view => Assert.Null(view.LaneShapes));
        }
        finally
        {
            assetA.Dispose();
            assetB.Dispose();
            assetC.Dispose();
        }
    }

    [Fact]
    public void BankGoldenSnapshotIsByteIdenticalAcrossRuns()
    {
        var (bank, assetA, assetB, assetC) = Populate(out var indexA, out var indexB, out var indexC);
        try
        {
            Assert.Equal(Render(bank, indexA, indexB, indexC), Render(bank, indexA, indexB, indexC));
            Assert.Equal(Golden + "\n", Render(bank, indexA, indexB, indexC));
        }
        finally
        {
            assetA.Dispose();
            assetB.Dispose();
            assetC.Dispose();
        }
    }

    [Fact]
    public void TablesTrackLoadsPairsAndBakeEntries()
    {
        var before = Inspection.Tables();
        var bytes = new DomainBaker()
            .Track<SnapshotTrack, SnapshotClip>(new SnapshotTrack(1))
            .Clip(0, 0u, 4u, new SnapshotClip(1f, 0f))
            .Bake();
        using var asset = TimelineAsset.Of(TimelineAsset.Load(bytes));
        var after = Inspection.Tables();
        Assert.Equal(before.Intern.Distinct + 1, after.Intern.Distinct);
        Assert.Equal(before.Intern.Live + 1, after.Intern.Live);
        Assert.True(after.Intern.Capacity >= 1024);
        Assert.Equal(after.Pair.Pairs * 1000 / after.Pair.Slots, after.Pair.LoadFactorPermille);
        Assert.True(after.Pair.Pairs >= 1);
        Assert.True(after.Pair.Consumers >= 1);
        Assert.Equal(40, after.Pair.SlotRowBytes);
        Assert.Equal(1024, after.Bake.Slots);
        Assert.True(after.Bake.Entries >= 0);
        Assert.True(after.Intern.GraveyardBlocks >= 0);
        Assert.True(after.Intern.AllocBytes >= 0);
        Assert.True(after.Intern.PeakLiveBytes >= 0);
    }

    static (Inspection.BankSnapshot Bank, TimelineAsset A, TimelineAsset B, TimelineAsset C) Populate(
        out ushort indexA, out ushort indexB, out ushort indexC)
    {
        var assetA = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<SnapshotTrack, SnapshotClip>(new SnapshotTrack(1))
            .Clip(0, 0u, 4u, new SnapshotClip(1f, 0f))
            .Clip(0, 4u, 8u, new SnapshotClip(2f, 1f))
            .Bake()));
        var assetB = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<DormantTrack, DormantClip>(new DormantTrack(2))
            .Clip(0, 0u, 2u, new DormantClip(9f))
            .Bake()));
        var assetC = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<SnapshotTrack, SnapshotClip>(new SnapshotTrack(3))
            .Clip(0, 0u, 6u, new SnapshotClip(4f, 2f))
            .Bake()));
        indexA = assetA.Index;
        indexB = assetB.Index;
        indexC = assetC.Index;
        Timeline<SnapshotTrack, SnapshotClip>.View(assetA);
        Timeline<SnapshotTrack, SnapshotClip>.View(assetC);
        var bank = Inspection.Bank<SnapshotTrack, SnapshotClip>();
        Assert.NotNull(bank);
        return (bank!, assetA, assetB, assetC);
    }

    static string Render(Inspection.BankSnapshot bank, params ushort[] owned)
    {
        var lines = new StringBuilder();
        lines.Append("track ").Append(bank.Track).Append('\n');
        lines.Append("clip ").Append(bank.Clip).Append('\n');
        lines.Append("pairKey ").Append(bank.PairKey.ToString("x16", CultureInfo.InvariantCulture)).Append('\n');
        lines.Append("blocks ").Append(bank.Blocks.ToString(CultureInfo.InvariantCulture)).Append('\n');
        lines.Append("dedupeHits ").Append(bank.DedupeHits.ToString(CultureInfo.InvariantCulture)).Append('\n');
        lines.Append("generation ").Append(bank.Generation.ToString(CultureInfo.InvariantCulture)).Append('\n');
        lines.Append("bytes ").Append(bank.HeaderBytes.ToString(CultureInfo.InvariantCulture))
            .Append(' ').Append(bank.TableBytes.ToString(CultureInfo.InvariantCulture))
            .Append(' ').Append(bank.DirectoryBytes.ToString(CultureInfo.InvariantCulture))
            .Append(' ').Append(bank.ArenaBytes.ToString(CultureInfo.InvariantCulture))
            .Append(' ').Append(bank.RetainedBytes.ToString(CultureInfo.InvariantCulture)).Append('\n');
        var rank = 0;
        foreach (var view in bank.Views.Where(view => owned.Contains((ushort)view.Index)).OrderBy(view => view.Index))
        {
            lines.Append("view ").Append(rank.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(view.State.ToString()).Append('\n');
            if (view.State != Inspection.FoldState.Folded) { rank++; continue; }
            lines.Append("  duration ").Append(view.Duration!.Value.ToString(CultureInfo.InvariantCulture))
                .Append(" looping ").Append(view.Looping!.Value.ToString(CultureInfo.InvariantCulture))
                .Append(" ticks ").Append(view.Ticks!.Value.ToString(CultureInfo.InvariantCulture))
                .Append(" lanes ").Append(view.Lanes!.Value.ToString(CultureInfo.InvariantCulture))
                .Append(" abi ").Append(view.AbiVersion!.Value.ToString(CultureInfo.InvariantCulture))
                .Append(" generation ").Append(view.Generation!.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var shape in view.LaneShapes!)
                lines.Append("  lane ").Append(shape.Key.ToString("x16", CultureInfo.InvariantCulture))
                    .Append(' ').Append(shape.Distinct.ToString(CultureInfo.InvariantCulture))
                    .Append(' ').Append(shape.LongestRun.ToString(CultureInfo.InvariantCulture)).Append('\n');
            rank++;
        }
        return lines.ToString();
    }

    const string Golden = """
        track Tl.Core.Tests.SnapshotTrack
        clip Tl.Core.Tests.SnapshotClip
        pairKey e10497d6bc032939
        blocks 2
        dedupeHits 0
        generation 2
        bytes 144 448 25680 16416 42688
        view 0 Folded
          duration 8 looping False ticks 9 lanes 1 abi 1 generation 1
          lane ad2e313ccaf1aa75 1 9
        view 1 Absent
        view 2 Folded
          duration 6 looping False ticks 7 lanes 1 abi 1 generation 2
          lane ad2e313ccaf1aa75 1 7
        """;
}

public readonly record struct SnapshotClip(float X, float Y);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct SnapshotTrack(int Code) : IBlend<SnapshotClip>
{
    public void Blend(in SnapshotClip first, in SnapshotClip second, float factor, out SnapshotClip result)
        => result = new(
            first.X + (second.X - first.X) * factor,
            first.Y + (second.Y - first.Y) * factor);
}

public readonly record struct DormantClip(float Amount);

[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Global", Justification = "fixture domain model mirrors authored timeline data")]
public readonly record struct DormantTrack(int Code) : IBlend<DormantClip>
{
    public void Blend(in DormantClip first, in DormantClip second, float factor, out DormantClip result)
        => result = new(first.Amount + (second.Amount - first.Amount) * factor);
}
