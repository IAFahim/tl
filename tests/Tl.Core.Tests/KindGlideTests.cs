using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public sealed unsafe class KindGlideTests
{
    const int Capacity = 512;

    sealed class KindLibrary : IDisposable
    {
        public readonly TimelineWorld World;
        public readonly PlaybackTable Players;
        public readonly KindStore Store;
        public readonly KindTables Tables;
        public readonly KindCalm Calm;
        readonly byte* _forward;
        readonly byte* _backward;
        readonly int* _start;

        public KindLibrary()
        {
            World = new TimelineWorld(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "kinds.tlib")));
            Players = new PlaybackTable(1);
            Store = new KindStore(World, Players);
            Tables = (KindTables)typeof(KindStore).GetField("_t", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Store)!;
            var length = Math.Max(1, KindCalm.Length(in Tables));
            _forward = (byte*)NativeMemory.AllocZeroed((nuint)length);
            _backward = (byte*)NativeMemory.AllocZeroed((nuint)length);
            _start = (int*)NativeMemory.AllocZeroed((nuint)Math.Max(1, Tables.TimelineCount) * 4);
            Calm = KindCalm.Build(in Tables, _forward, _backward, _start);
        }

        public int Duration(int timeline) => (int)Tables.Timelines[timeline - 1].Duration;

        public bool Event(int timeline, int tick, int bit)
            => Tables.KindCount != 0 && Tables.Sec.Timelines[timeline - 1].KindMask != 0 && (Tables.Sec.EventBitsAt(Tables.Sec.Timelines + (timeline - 1), tick) & bit) != 0;

        public void Dispose()
        {
            NativeMemory.Free(_forward);
            NativeMemory.Free(_backward);
            NativeMemory.Free(_start);
            Store.Dispose();
            Players.Dispose();
            World.Dispose();
        }
    }

    struct RecordingRows : IKindRows
    {
        public List<string> Log;
        public KindEntry* Entries;
        public int Appended;
        public Playback* Slots;

        public KindEntry* Append(int row, LibraryFeeder* feeder)
        {
            Log.Add("append " + row + " " + (nint)feeder);
            Entries[Appended] = default;
            return Entries + Appended++;
        }

        public void RemoveSpan(int row, int kind, byte slot, ushort timeline, ushort step) => Log.Add("remove " + row + " " + kind + " " + slot + " " + timeline + " " + step);

        public void Purge(int row, byte slot, ushort timeline) => Log.Add("purge " + row + " " + slot + " " + timeline);

        public Playback* Reserve(int row, int used, int length)
        {
            Log.Add("reserve " + row + " " + used + " " + length);
            return Slots;
        }

        public void Sample(int row, byte slot, byte sample) => Log.Add("sample " + row + " " + slot + " " + sample);

        public void Unsample(int row) => Log.Add("unsample " + row);

        public void Seal()
        {
            for (var i = 0; i < Appended; i++) Log.Add("entry " + *(ulong*)(Entries + i));
            Appended = 0;
        }
    }

    static RecordingRows Rows(Playback* slots, KindEntry* entries) => new() { Log = new List<string>(), Entries = entries, Slots = slots };

    static int Brute(KindLibrary library, int timeline, int tick, bool forward)
    {
        var duration = library.Duration(timeline);
        var steps = 0;
        for (var at = tick; steps < Horizon.Max; steps++)
        {
            var next = forward ? at + 1 : at - 1;
            if (next < 0 || next >= duration || library.Event(timeline, next, forward ? 1 : 2)) break;
            at = next;
        }
        return steps;
    }

    [Fact]
    public void CalmTableEqualsABruteForceScanOfKindEventsForEveryTickAndDirection()
    {
        using var library = new KindLibrary();
        Assert.True(library.Tables.KindCount > 0);
        var tabled = 0;
        var edges = 0;
        for (var timeline = 1; timeline <= library.Tables.TimelineCount; timeline++)
            for (var tick = 0; tick < library.Duration(timeline); tick++)
                foreach (var forward in new[] { false, true })
                {
                    var slot = new Playback { Timeline = (ushort)timeline, Tick = (ushort)tick, Flags = forward ? PlaybackFlags.None : PlaybackFlags.Reverse };
                    var expected = Brute(library, timeline, tick, forward);
                    Assert.Equal((byte)expected, Horizon.OfKindRun(&slot, 1, in library.Tables, in library.Calm, 0, Horizon.Max));
                    Assert.Equal((byte)Brute(library, timeline, tick, !forward), Horizon.OfKindRun(&slot, 1, in library.Tables, in library.Calm, SlotWord.ReverseBit, Horizon.Max));
                    if (library.Calm.Start[timeline - 1] >= 0) tabled++;
                    if (expected < (forward ? library.Duration(timeline) - 1 - tick : tick)) edges++;
                }
        Assert.True(tabled > 0);
        Assert.True(edges > 0);
    }

    [Fact]
    public void KindFreeTablesFallBackToTheMotionHorizon()
    {
        using var library = new KindLibrary();
        var tables = library.Tables;
        tables.KindCount = 0;
        Assert.Equal(0, KindCalm.Length(in tables));
        var start = stackalloc int[tables.TimelineCount];
        byte none = 0;
        var calm = KindCalm.Build(in tables, &none, &none, start);
        var random = new Random(77);
        var slots = stackalloc Playback[6];
        for (var round = 0; round < 20_000; round++)
        {
            var count = random.Next(1, 7);
            for (var i = 0; i < count; i++) slots[i] = RandomSlot(random, library);
            var reverse = random.Next(2) == 0 ? 0ul : SlotWord.ReverseBit;
            var limit = random.Next(-1, 300);
            Assert.Equal(Horizon.OfRun(slots, count, tables.Timelines, tables.TimelineCount, reverse, limit), Horizon.OfKindRun(slots, count, in tables, in calm, reverse, limit));
        }
        Assert.Equal((byte)0, Horizon.OfKindRun(slots, 1, in tables, default, 0, Horizon.Max));
    }

    [Fact]
    public void EveryFlagByteAtEveryKindTickGlidesExactlyLikeTickEntityWithoutTouchingKindRows()
    {
        using var library = new KindLibrary();
        var slots = (Playback*)NativeMemory.AllocZeroed(Capacity * 8);
        var expected = (Playback*)NativeMemory.AllocZeroed(Capacity * 8);
        var entries = (KindEntry*)NativeMemory.AllocZeroed(4096 * 8);
        try
        {
            var glides = 0;
            var fixedPoints = 0;
            var cases = 0;
            foreach (var stepDir in new[] { 1, -1 })
                for (var timeline = 1; timeline <= library.Tables.TimelineCount; timeline++)
                    for (var tick = 0; tick < library.Duration(timeline); tick++)
                        for (var flags = 0; flags < 256; flags++)
                            foreach (var rate in new byte[] { 0, 1, 16, 255 })
                            {
                                var start = new Playback { Timeline = (ushort)timeline, Tick = (ushort)tick, Channel = 0x5A, Flags = (PlaybackFlags)flags, Rate = rate, Phase = (byte)(rate == 0 ? 0 : 7) };
                                var word = *(ulong*)&start;
                                var reverse = SlotWord.WorldReverse(stepDir > 0);
                                slots[0] = start;
                                expected[0] = start;
                                var rows = Rows(expected, entries);
                                if (SlotWord.IsInert(word))
                                {
                                    Assert.False(KindRules.TickEntity(in library.Tables, expected, 1, ref rows, 0, stepDir));
                                    rows.Seal();
                                    Assert.Empty(rows.Log);
                                    Assert.Equal(word, *(ulong*)expected);
                                    expected[0] = start;
                                    fixedPoints++;
                                }
                                var horizon = Horizon.OfKindRun(slots, 1, in library.Tables, in library.Calm, reverse, Horizon.Max);
                                for (var step = 0; step < horizon; step++)
                                {
                                    Assert.True(KindRules.TickEntity(in library.Tables, expected, 1, ref rows, 0, stepDir));
                                    GlideKernel.RunScalar((ulong*)slots, 1, reverse);
                                    rows.Seal();
                                    Assert.Empty(rows.Log);
                                    Assert.Equal(*(ulong*)expected, *(ulong*)slots);
                                    glides++;
                                }
                                cases++;
                            }
            Assert.True(cases > 10_000);
            Assert.True(glides > 1000);
            Assert.True(fixedPoints > 1000);
        }
        finally
        {
            NativeMemory.Free(slots);
            NativeMemory.Free(expected);
            NativeMemory.Free(entries);
        }
    }

    static Playback RandomSlot(Random random, KindLibrary library)
    {
        var timeline = random.Next(8) == 0 ? 0 : random.Next(1, library.Tables.TimelineCount + 1);
        var duration = timeline == 0 ? 1 : library.Duration(timeline);
        var tick = random.Next(4) switch
        {
            0 => 0,
            1 => duration - 1,
            _ => random.Next(duration),
        };
        var flags = random.Next(10) switch
        {
            < 5 => PlaybackFlags.None,
            < 7 => PlaybackFlags.Reverse,
            7 => PlaybackFlags.Paused | (PlaybackFlags)(random.Next(2)),
            _ => (PlaybackFlags)random.Next(256),
        };
        var rate = random.Next(8) == 0 ? (byte)new[] { 1, 8, 16, 40, 255 }[random.Next(5)] : (byte)0;
        return new Playback { Timeline = (ushort)timeline, Tick = (ushort)tick, Channel = (byte)random.Next(3), Flags = flags, Rate = rate, Phase = rate == 0 ? (byte)0 : (byte)random.Next(16) };
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(255)]
    public void RandomKindRunsGlideLikeTheEdgeLawAcrossRequestsAndDirectionFlips(int limit)
    {
        using var library = new KindLibrary();
        var reference = (Playback*)NativeMemory.AllocZeroed(Capacity * 8);
        var candidate = (Playback*)NativeMemory.AllocZeroed(Capacity * 8);
        var referenceEntries = (KindEntry*)NativeMemory.AllocZeroed(4096 * 8);
        var candidateEntries = (KindEntry*)NativeMemory.AllocZeroed(4096 * 8);
        try
        {
            var random = new Random(limit * 7919);
            var glides = 0;
            var edges = 0;
            for (var run = 0; run < 400; run++)
            {
                var count = random.Next(1, 7);
                NativeMemory.Clear(reference, Capacity * 8);
                for (var i = 0; i < count; i++) reference[i] = RandomSlot(random, library);
                NativeMemory.Copy(reference, candidate, Capacity * 8);
                var forward = true;
                var playing = true;
                byte horizon = 0;
                for (var tick = 0; tick < 600; tick++)
                {
                    if (random.Next(97) == 0)
                    {
                        forward = !forward;
                        horizon = 0;
                    }
                    if (random.Next(61) == 0)
                    {
                        var at = random.Next(count);
                        reference[at] = RandomSlot(random, library);
                        candidate[at] = reference[at];
                        horizon = 0;
                        playing = true;
                    }
                    if (!playing) continue;
                    var stepDir = forward ? 1 : -1;
                    var reverse = SlotWord.WorldReverse(forward);
                    var referenceRows = Rows(reference, referenceEntries);
                    var candidateRows = Rows(candidate, candidateEntries);
                    var referenceLive = KindRules.TickEntity(in library.Tables, reference, count, ref referenceRows, 0, stepDir);
                    bool candidateLive;
                    if (horizon != 0)
                    {
                        horizon--;
                        GlideKernel.RunScalar((ulong*)candidate, count, reverse);
                        candidateLive = true;
                        glides++;
                    }
                    else
                    {
                        candidateLive = KindRules.TickEntity(in library.Tables, candidate, count, ref candidateRows, 0, stepDir);
                        horizon = Horizon.OfKindRun(candidate, count, in library.Tables, in library.Calm, reverse, limit);
                        edges++;
                    }
                    referenceRows.Seal();
                    candidateRows.Seal();
                    Assert.Equal(referenceLive, candidateLive);
                    Assert.Equal(referenceRows.Log, candidateRows.Log);
                    Assert.True(new ReadOnlySpan<byte>(reference, Capacity * 8).SequenceEqual(new ReadOnlySpan<byte>(candidate, Capacity * 8)));
                    playing = referenceLive;
                }
            }
            Assert.True(glides > 1000);
            Assert.True(edges > 1000);
        }
        finally
        {
            NativeMemory.Free(reference);
            NativeMemory.Free(candidate);
            NativeMemory.Free(referenceEntries);
            NativeMemory.Free(candidateEntries);
        }
    }
}
