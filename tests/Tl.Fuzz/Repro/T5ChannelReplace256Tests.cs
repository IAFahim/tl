using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Fuzz.Repro;

public struct T5ReproClip
{
    public int Value;
}

public struct T5ReproTrack : IBlend<T5ReproClip>
{
    public int Scale;

    public void Blend(in T5ReproClip first, in T5ReproClip second, float factor, out T5ReproClip result)
        => result = first;
}

[TimelineKind(Accepts = KindEntries.Both, Mode = KindMode.Combine)]
public struct T5ReproCombineKind
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref int col) => col += total;
}

public sealed unsafe class T5ChannelReplace256Tests
{
    static byte[] Build258TimelineLibrary()
    {
        const int count = 258;
        var names = new string[count];
        for (var i = 0; i < count; i++) names[i] = $"tl_{i + 1:D3}";
        var channels = new[] { "ch_1" };
        var manifest = LibraryBake.Manifest(names, channels, 60);
        var overrides = new Dictionary<string, KindOverride>
        {
            [typeof(T5ReproCombineKind).FullName!] = new KindOverride(inline: 260, gamePulses: 8, maxInlineBytes: 64),
        };
        var kinds = LibraryBake.Kinds([typeof(T5ReproCombineKind).Assembly], overrides);
        var parts = new List<LibraryPart>(count);
        for (var i = 0; i < count; i++)
        {
            var typed = new TypedTimeline
            {
                Name = names[i],
                Duration = 20,
                Channel = i < 2 ? "ch_1" : null,
            };
            var track = new TypedTrack
            {
                Name = "tk",
                Type = typeof(T5ReproTrack),
                Data = BitConverter.GetBytes(1),
            };
            track.Clips.Add(new TypedClip
            {
                Type = typeof(T5ReproClip),
                Start = 0,
                End = 20,
                Data = BitConverter.GetBytes(i + 1),
            });
            typed.Tracks.Add(track);
            var tlb = LibraryBake.BakeTyped(typed, manifest);
            parts.Add(new LibraryPart
            {
                Name = names[i],
                Tlb = tlb,
                Kinds = LibraryBake.KindPayload(names[i], tlb, kinds),
                Next = 0,
            });
        }
        var settings = LibrarySettings.Default;
        settings.ChannelCount = 1;
        settings.Kinds = overrides;
        return LibraryAssembler.Assemble(parts, 60, settings);
    }

    [Fact]
    public void KindStore_ReplacingChannelOn256SlotEntity_KillsOldPlaybackWithoutStartingReplacement()
    {
        var bytes = Build258TimelineLibrary();
        using var world = new TimelineWorld(bytes);
        using var players = new PlaybackTable(1, 256);
        using var store = new KindStore(world, players);

        players.Start(0, 1);
        for (ushort tl = 3; tl <= 257; tl++) players.Start(0, tl);
        world.Tick(players, true);
        Assert.Equal(256, players.CountOf(0));

        players.Start(0, 2);
        world.Tick(players, true);

        var hasTl2 = false;
        for (var i = 0; i < players.CountOf(0); i++)
            if (players.SlotsOf(0)[i].Timeline == 2)
                hasTl2 = true;

        Assert.True(hasTl2);
    }
}
