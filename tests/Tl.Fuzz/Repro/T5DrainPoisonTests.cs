using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public sealed class T5DrainPoisonTests
{
    static byte[] BuildMinimalLibrary()
    {
        var names = new[] { "tl_1" };
        var channels = new[] { "ch_1" };
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var typed = new TypedTimeline
        {
            Name = "tl_1",
            Duration = 10,
            Channel = "ch_1",
        };
        var track = new TypedTrack
        {
            Name = "t0",
            Type = typeof(FuzzTrack),
            Data = BitConverter.GetBytes(1f),
        };
        track.Clips.Add(new TypedClip
        {
            Type = typeof(FuzzClip),
            Start = 0,
            End = 10,
            Data = BitConverter.GetBytes(1f),
        });
        typed.Tracks.Add(track);
        var tlb = LibraryBake.BakeTyped(typed, manifest);
        return LibraryAssembler.Assemble(
            [new LibraryPart { Name = "tl_1", Tlb = tlb, Next = 0 }],
            60,
            LibrarySettings.Default);
    }

    [Fact]
    public void CaughtInvalidRequest_LeavesPendingQueueUnclearedAndWedgesFutureApplyRequests()
    {
        var bytes = BuildMinimalLibrary();
        using var world = new TimelineWorld(bytes);
        using var players = new PlaybackTable(2, 4);

        players.Start(0, 999);
        Assert.Throws<ArgumentException>(() => world.ApplyRequests(players));

        players.Start(0, 1);
        var nextException = Record.Exception(() => world.ApplyRequests(players));
        Assert.Null(nextException);
        Assert.Equal(1, players.CountOf(0));
    }
}
