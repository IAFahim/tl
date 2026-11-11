using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

// PlaybackRules.Advance has an inline fast path for the common step; everything else goes through Move.
// This proves the two agree for EVERY flag byte, tick, direction and timeline shape (not a sample).
public sealed unsafe class AdvanceFastPathTests
{
    [Fact]
    public void AdvanceMatchesTheGeneralMoveForEveryFlagTickDirectionAndTimeline()
    {
        const int count = 5;
        var timelines = (LibraryTimeline*)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(LibraryTimeline));
        try
        {
            timelines[0].Duration = 5;                          // id 1: plain
            timelines[1].Duration = 5; timelines[1].Loop = 1;   // id 2: loops
            timelines[2].Duration = 5; timelines[2].Hold = 1;   // id 3: holds its last frame
            timelines[3].Duration = 5; timelines[3].Next = 1;   // id 4: hands over to id 1
            timelines[4].Duration = 0;                          // id 5: empty
            var checkedCases = 0;
            var fast = 0;
            foreach (var forward in new[] { true, false })
                foreach (ushort timeline in new ushort[] { 0, 1, 2, 3, 4, 5, 9 })
                    for (ushort tick = 0; tick <= 6; tick++)
                        for (var bits = 0; bits < 256; bits++)
                        {
                            var slot = new Playback { Timeline = timeline, Tick = tick, Flags = (PlaybackFlags)bits, Channel = 2 };
                            var expected = slot;
                            var motion = PlaybackRules.Move(ref expected, timelines, count, forward);
                            var expectedResult = motion == PlaybackMotion.Removed ? AdvanceResult.Removed
                                : motion == PlaybackMotion.Unknown ? AdvanceResult.Unknown
                                : AdvanceResult.Kept;
                            var actual = slot;
                            var result = PlaybackRules.Advance(ref actual, timelines, count, forward);
                            Assert.True(result == expectedResult, $"result differs: forward={forward} timeline={timeline} tick={tick} flags={bits}");
                            Assert.True(actual.Tick == expected.Tick && actual.Flags == expected.Flags && actual.Timeline == expected.Timeline && actual.Channel == expected.Channel,
                                $"slot differs: forward={forward} timeline={timeline} tick={tick} flags={bits}");
                            checkedCases++;
                            if (motion == PlaybackMotion.Stepped && forward && (bits & 0x83) == 0 && (bits & 0x08) == 0) fast++;
                        }
            Assert.Equal(2 * 7 * 7 * 256, checkedCases);
            Assert.True(fast > 100, "the sweep must actually exercise the fast path");
        }
        finally
        {
            NativeMemory.Free(timelines);
        }
    }
}
