using Tl.TestSupport;
using Xunit;

namespace Tl.Core.Tests;

[Collection("GlideWorld")]
public sealed unsafe class PlaybackImageTests
{
    struct Clip { public int Value; }
    struct Track : IBlend<Clip>
    {
        public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
    }

    static byte[] Library()
    {
        var parts = new LibraryPart[6];
        for (var i = 0; i < parts.Length; i++)
        {
            var baker = new DomainBaker().Track<Track, Clip>(default).Clip(0, 0, (uint)(9 + 7 * i), new Clip { Value = i });
            parts[i] = new() { Name = "T" + i, Tlb = (i % 2 == 0 ? baker.Looping() : baker).Bake() };
        }
        return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
    }

    static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    static void Controls(PlaybackTable players, ref uint rng)
    {
        for (var e = 0; e < players.EntityCapacity; e++)
        {
            var roll = Next(ref rng);
            if (roll % 5 != 0) continue;
            var timeline = (ushort)(1 + (roll >> 8) % 6);
            var at = (ushort)((roll >> 16) % 30);
            switch ((roll >> 3) % 12)
            {
                case 0: players.Pause(e, timeline); break;
                case 1: players.Resume(e, timeline); break;
                case 2: players.Reverse(e, timeline, (at & 1) != 0); break;
                case 3: players.SetSpeed(e, timeline, (ushort)(8 + at)); break;
                case 4: players.Seek(e, timeline, at); break;
                case 5: players.Stop(e, timeline); break;
                case 6: players.Reset(e); break;
                default: players.Start(e, timeline, at); break;
            }
        }
    }

    static ulong Digest(PlaybackTable players)
    {
        var hash = TlStateHash.Begin();
        for (var e = 0; e < players.EntityCapacity; e++)
        {
            var slots = players.SlotsOf(e);
            hash.Int(players.CountOf(e));
            for (var i = 0; i < players.CountOf(e); i++)
            {
                var playback = slots[i];
                if (players.Frozen) LazyTicks.Shift(&playback, 1, players.Clock, players.Forward);
                hash.Lane(*(ulong*)&playback);
            }
        }
        for (var w = 0; w < players.PlayingWords; w++) hash.Lane(players.PlayingBits[w]);
        hash.Lane((ulong)players.DroppedStarts);
        return hash.Value;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(255)]
    public void RestoreReplaysEveryStepExactlyThroughControlsSpillsAndTheLazyHorizon(int limit)
    {
        const int Steps = 400, At = 150;
        using var world = new TimelineWorld(Library());
        using var players = new PlaybackTable(130, 2, limit);
        using var twin = new PlaybackTable(130, 2, limit);
        using var image = new PlaybackImage();
        var digests = new ulong[Steps];
        uint rng = 0xC0FFEE12, twinRng = rng, savedRng = 0;
        var forward = true;
        var savedForward = true;
        var spilled = 0;
        var frozen = 0;
        for (var step = 0; step < Steps; step++)
        {
            if (step % 53 == 31) forward = !forward;
            Controls(players, ref rng);
            Controls(twin, ref twinRng);
            if (step == At)
            {
                players.Save(image);
                savedRng = rng;
                savedForward = forward;
            }
            foreach (var table in new[] { players, twin })
            {
                world.ApplyRequests(table);
                world.Advance(table, forward);
            }
            digests[step] = Digest(players);
            Assert.Equal(Digest(twin), digests[step]);
            for (var e = 0; e < players.EntityCapacity; e++) if (players.IsSpilled(e)) spilled++;
            if (players.Frozen) frozen++;
        }
        Assert.True(spilled > 0);
        Assert.True(limit == 0 || frozen > Steps / 4);
        Assert.True(image.Length > 0);
        for (var pass = 0; pass < 2; pass++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            players.Restore(image);
            if (pass == 1) Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
            rng = savedRng;
            forward = savedForward;
            for (var step = At; step < Steps; step++)
            {
                if (step != At)
                {
                    if (step % 53 == 31) forward = !forward;
                    Controls(players, ref rng);
                }
                world.ApplyRequests(players);
                world.Advance(players, forward);
                Assert.Equal(digests[step], Digest(players));
            }
        }
        var length = image.Length;
        var start = GC.GetAllocatedBytesForCurrentThread();
        players.Save(image);
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - start);
        Assert.True(image.Length >= length / 2);
    }

    [Fact]
    public void RestoreRefusesAnEmptyImageAndATableOfAnotherShape()
    {
        using var world = new TimelineWorld(Library());
        using var players = new PlaybackTable(8, 2);
        using var other = new PlaybackTable(9, 2);
        using var image = new PlaybackImage();
        Assert.Throws<InvalidOperationException>(() => players.Restore(image));
        players.Start(0, 1);
        world.ApplyRequests(players);
        world.Advance(players);
        players.Save(image);
        var saved = Digest(players);
        Assert.Throws<InvalidOperationException>(() => other.Restore(image));
        world.Advance(players);
        Assert.NotEqual(saved, Digest(players));
        players.Restore(image);
        Assert.Equal(saved, Digest(players));
    }
}
