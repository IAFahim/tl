using System.Reflection;
using Xunit;

namespace Tl.Core.Tests;

public class WarmPathTierTests
{
    enum WarmMode
    {
        Inline,
        Optimize
    }

    readonly record struct WarmMethod(Type Type, string Name, int Arity, string? FirstParameter, WarmMode Mode);

    static readonly WarmMethod[] Allowlist =
    [
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.IsFolded), 1, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.Gather), 1, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.Apply), 4, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.Apply), 5, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.ApplySlot), 4, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.ApplySlot), 5, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.FoldedView), 1, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.ApplyRecords), 4, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.ApplyRecords), 5, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), "ApplyRecordsForward", 4, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), "ApplyRecordsBackward", 4, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.Advance), 3, null, WarmMode.Inline),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), nameof(TimelineSet<LaneTrack, LaneClip>.Advance), 4, null, WarmMode.Optimize),
        new(typeof(TimelineSet<LaneTrack, LaneClip>), "AdvanceRow", 8, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), ".ctor", 4, null, WarmMode.Inline),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), nameof(TimelineSetLane<LaneTrack, LaneClip>.Seek), 2, null, WarmMode.Inline),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), nameof(TimelineSetLane<LaneTrack, LaneClip>.Apply), 1, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), nameof(TimelineSetLane<LaneTrack, LaneClip>.Apply), 2, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), nameof(TimelineSetLane<LaneTrack, LaneClip>.ApplySlot), 2, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), nameof(TimelineSetLane<LaneTrack, LaneClip>.ApplySlot), 3, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ApplyUniformSegment", 9, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ArenaRecords", 6, null, WarmMode.Inline),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ArenaUniformWalk", 8, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ArenaMixedForward", 9, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ArenaMixedBackward", 9, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ApplyUniformForward", 6, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ApplyUniformBackward", 6, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ApplyMixedForward", 7, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ApplyMixedBackward", 7, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "FastMixedForward", 7, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "FastMixedBackward", 7, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ShortRuns", 3, null, WarmMode.Inline),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "UniformChunk", 4, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ValidateChunk", 5, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "FastMixedChunk", 6, null, WarmMode.Optimize),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "ResolveOrThrow", 3, null, WarmMode.Inline),
        new(typeof(TimelineSetLane<LaneTrack, LaneClip>), "RunEndTwo", 4, null, WarmMode.Optimize),
        new(typeof(Timeline<LaneTrack, LaneClip>), nameof(Timeline<LaneTrack, LaneClip>.Apply), 4, null, WarmMode.Inline),
        new(typeof(Timeline<LaneTrack, LaneClip>), nameof(Timeline<LaneTrack, LaneClip>.Apply), 5, null, WarmMode.Inline),
        new(typeof(Timeline<LaneTrack, LaneClip>), nameof(Timeline<LaneTrack, LaneClip>.Advance), 3, "UInt16", WarmMode.Inline),
        new(typeof(Timeline<LaneTrack, LaneClip>), "ApplySharedClock", 4, null, WarmMode.Optimize),
        new(typeof(Timeline<LaneTrack, LaneClip>), "Bank", 0, null, WarmMode.Inline),
        new(typeof(Timeline<LaneTrack, LaneClip>), nameof(Timeline<LaneTrack, LaneClip>.ResolveChunk), 4, null, WarmMode.Optimize),
        new(typeof(Timeline<LawLane>), nameof(Timeline<LawLane>.Apply), 3, null, WarmMode.Inline),
        new(typeof(Timeline<LawLane>), nameof(Timeline<LawLane>.Apply), 4, null, WarmMode.Inline),
        new(typeof(Timeline<LawLane>), nameof(Timeline<LawLane>.Advance), 2, null, WarmMode.Inline),
        new(typeof(Timeline<LawLane>), nameof(Timeline<LawLane>.Advance), 3, null, WarmMode.Inline),
        new(typeof(TimelineLane<LawLane>), ".ctor", 2, null, WarmMode.Inline),
        new(typeof(TimelineLane<LawLane>), nameof(TimelineLane<LawLane>.Advance), 3, null, WarmMode.Optimize),
        new(typeof(TimelineLane<LawLane>), nameof(TimelineLane<LawLane>.Apply), 4, null, WarmMode.Optimize),
        new(typeof(TimelineLane<LawLane>), nameof(TimelineLane<LawLane>.Apply), 1, null, WarmMode.Optimize),
        new(typeof(TimelineLane<LawLane>), "ApplyRuns", 1, null, WarmMode.Optimize),
        new(typeof(TimelineLane<LawLane>), "ApplyAccelerated", 1, null, WarmMode.Optimize),
        new(typeof(TimelineLane<LawLane>), "RunShaped", 1, null, WarmMode.Inline),
        new(typeof(Timeline), nameof(Timeline.Advance), 3, "ReadOnlySpan`1", WarmMode.Inline),
        new(typeof(Timeline), nameof(Timeline.Advance), 4, "ReadOnlySpan`1", WarmMode.Optimize),
        new(typeof(Timeline), nameof(Timeline.Advance), 3, "TimelineAsset", WarmMode.Inline),
        new(typeof(Timeline), nameof(Timeline.Advance), 4, "TimelineAsset", WarmMode.Inline),
        new(typeof(Timeline), nameof(Timeline.Advance), 3, "UInt16", WarmMode.Inline),
        new(typeof(Timeline), nameof(Timeline.Advance), 4, "UInt16", WarmMode.Optimize),
        new(typeof(Timeline), "AdvanceRecords", 4, "UInt32", WarmMode.Inline),
        new(typeof(Timeline), "AdvanceRecordsForward", 4, "UInt16", WarmMode.Inline),
        new(typeof(Timeline), "AdvanceRecordsBackward", 4, "UInt16", WarmMode.Inline),
        new(typeof(LaneOps), nameof(LaneOps.RunEnd), 3, null, WarmMode.Optimize),
        new(typeof(LaneOps), nameof(LaneOps.SingletonChunk), 3, null, WarmMode.Inline),
        new(typeof(LaneOps), "StaggeredEnds", 3, null, WarmMode.Inline),
        new(typeof(LaneOps), nameof(LaneOps.Add), 4, null, WarmMode.Inline),
        new(typeof(LaneOps), nameof(LaneOps.Fill), 4, null, WarmMode.Inline),
        new(typeof(LaneOps), nameof(LaneOps.EffPermuteForward), 8, null, WarmMode.Optimize),
        new(typeof(LaneOps), nameof(LaneOps.EffPermuteBackward), 8, null, WarmMode.Optimize),
        new(typeof(LaneOps), nameof(LaneOps.EffectForward), 8, null, WarmMode.Optimize),
        new(typeof(LaneOps), nameof(LaneOps.EffectBackward), 8, null, WarmMode.Optimize),
        new(typeof(LaneOps), nameof(LaneOps.AdvanceForward), 6, null, WarmMode.Optimize),
        new(typeof(LaneOps), nameof(LaneOps.AdvanceBackward), 6, null, WarmMode.Optimize),
        new(typeof(LaneOps), nameof(LaneOps.AdvanceRows), 7, null, WarmMode.Optimize),
        new(typeof(LaneOps), "AdvanceForwardWide", 5, null, WarmMode.Inline),
        new(typeof(LaneOps), "AdvanceBackwardWide", 5, null, WarmMode.Inline),
        new(typeof(Timeline<LaneTrack, LaneClip>), nameof(Timeline<LaneTrack, LaneClip>.Apply), 4, null, WarmMode.Optimize),
        new(typeof(Timeline<LaneTrack, LaneClip>), nameof(Timeline<LaneTrack, LaneClip>.Advance), 3, null, WarmMode.Optimize),
        new(typeof(Timeline<LaneTrack, LaneClip>), "CheckSizes", 0, null, WarmMode.Inline),
        new(typeof(Checked), nameof(Checked.Live), 1, null, WarmMode.Inline),
        new(typeof(Checked), nameof(Checked.Columns), 2, null, WarmMode.Inline),
        new(typeof(Checked), nameof(Checked.Columns), 3, null, WarmMode.Inline),
        new(typeof(Checked), nameof(Checked.Columns), 4, null, WarmMode.Inline),
        new(typeof(Checked), nameof(Checked.Length), 2, null, WarmMode.Inline),
    ];

    [Fact]
    public void WarmPathMethodsCarryTier0EscapeAttribute()
    {
        Assert.NotEmpty(Allowlist);
        foreach (var entry in Allowlist)
        {
            var expected = entry.Mode == WarmMode.Inline
                ? MethodImplAttributes.AggressiveInlining
                : MethodImplAttributes.AggressiveOptimization;
            var found = MembersOf(entry).ToArray();
            Assert.True(found.Length > 0, $"{entry.Type.Name}.{entry.Name}/{entry.Arity} is missing from the warm path");
            foreach (var method in found)
                Assert.True(
                    (method.MethodImplementationFlags & expected) != 0,
                    $"{entry.Type.Name}.{entry.Name} ({Signature(method)}) must carry {expected} to stay out of tier-0");
        }
    }

    static IEnumerable<MethodBase> MembersOf(WarmMethod entry)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        if (entry.Name == ".ctor")
            return entry.Type.GetConstructors(all).Where(m => m.GetParameters().Length == entry.Arity);
        return entry.Type
            .GetMethods(all)
            .Where(m => m.Name == entry.Name && m.GetParameters().Length == entry.Arity)
            .Where(m => entry.FirstParameter is null || m.GetParameters()[0].ParameterType.Name == entry.FirstParameter);
    }

    static string Signature(MethodBase method)
        => string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
}
