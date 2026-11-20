using System.Diagnostics;
using System.Globalization;
using KindGame;
using Tl;
using Tl.Gen.Tlb;

unsafe
{
    const int timelineCount = 500;
    const int tracksPerTimeline = 16;
    const int shortest = 30;
    const int spread = 31;
    const int blendOverlap = 5;

    var rowsList = Arg("--rows", "10000,50000");
    var activeList = Arg("--active", "40,100");
    var liveList = Arg("--live", "8");
    var warmup = int.Parse(Arg("--warmup", "40"), CultureInfo.InvariantCulture);
    var measure = int.Parse(Arg("--measure", "16"), CultureInfo.InvariantCulture);
    var runs = int.Parse(Arg("--runs", "5"), CultureInfo.InvariantCulture);

    Console.WriteLine($"env dotnet={Environment.Version} cpus={Environment.ProcessorCount} timelines={timelineCount} tracks={tracksPerTimeline} durations={shortest}-{shortest + spread - 1} warmup={warmup} measure={measure} runs={runs}");
    foreach (var liveText in liveList.Split(','))
    {
        var live = int.Parse(liveText, CultureInfo.InvariantCulture);
        var library = Library(live);
        foreach (var rowsText in rowsList.Split(','))
        foreach (var activeText in activeList.Split(','))
            Cell(library, live, int.Parse(rowsText, CultureInfo.InvariantCulture), int.Parse(activeText, CultureInfo.InvariantCulture));
    }

    string Arg(string name, string fallback)
    {
        var at = Array.IndexOf(args, name);
        return at >= 0 && at + 1 < args.Length ? args[at + 1] : fallback;
    }

    static int DurationOf(int timeline) => shortest + timeline * 7 % spread;

    static bool Blends(int track) => track < 4 || track == 8 || track == 9;

    static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    static byte[] Raw<T>(T value) where T : unmanaged
    {
        var bytes = new byte[sizeof(T)];
        fixed (byte* at = bytes) *(T*)at = value;
        return bytes;
    }

    static byte[] Library(int liveTracks)
    {
        var names = new List<string>();
        for (var i = 0; i < timelineCount; i++) names.Add("shape" + (i + 1).ToString("D4", CultureInfo.InvariantCulture));
        var manifest = LibraryBake.Manifest(names, new List<string>(), 60);
        var kinds = LibraryBake.Kinds(new[] { typeof(WalkTrack).Assembly });
        var parts = new List<LibraryPart>();
        var state = 0x9E3779B9u;
        for (var i = 0; i < timelineCount; i++)
        {
            var duration = DurationOf(i);
            var timeline = new TypedTimeline { Name = names[i], Duration = (uint)duration, Loops = true };
            for (var track = 0; track < tracksPerTimeline; track++)
            {
                var covered = Math.Max(1, duration * liveTracks / tracksPerTimeline);
                var start = Next(ref state) % (uint)(duration - covered + 1);
                var end = start + (uint)covered;
                var split = Blends(track) && covered >= 2 * blendOverlap - 1;
                var firstEnd = split ? start + (uint)((covered + blendOverlap) / 2) : end;
                var secondStart = firstEnd - blendOverlap;
                var seed = i + track;
                var typed = new TypedTrack { Name = "t" + track };
                if (track < 8)
                {
                    typed.Type = typeof(ArcTrack);
                    typed.Data = Raw(new ArcTrack { Scale = 1f });
                    typed.Clips.Add(new TypedClip { Type = typeof(ArcClip), Start = start, End = firstEnd, Data = Raw(new ArcClip { Velocity = 1 + seed % 5 }) });
                    if (split) typed.Clips.Add(new TypedClip { Type = typeof(ArcClip), Start = secondStart, End = end, Data = Raw(new ArcClip { Velocity = 1 + (seed + 2) % 5 }) });
                }
                else if (track < 12)
                {
                    var priority = (byte)(seed % 3);
                    typed.Type = typeof(WalkTrack);
                    typed.Data = Raw(new WalkTrack { Scale = 1f });
                    typed.Clips.Add(new TypedClip { Type = typeof(WalkClip), Start = start, End = firstEnd, Data = Raw(new WalkClip { Speed = 1 + priority, Priority = priority }) });
                    if (split) typed.Clips.Add(new TypedClip { Type = typeof(WalkClip), Start = secondStart, End = end, Data = Raw(new WalkClip { Speed = 1 + priority, Priority = priority }) });
                }
                else if (track < 14)
                {
                    typed.Type = typeof(HitTrack);
                    typed.Data = Raw(new HitTrack { Scale = 1f });
                    typed.Clips.Add(new TypedClip { Type = typeof(HitClip), Start = start, End = end, Data = Raw(new HitClip { Amount = 1 + seed % 5 }) });
                }
                else
                {
                    typed.Type = typeof(StatTrack);
                    typed.Data = Raw(new StatTrack { Scale = 1f });
                    typed.Clips.Add(new TypedClip { Type = typeof(StatClip), Start = start, End = end, Data = Raw(new StatClip { Delta = 1 + seed % 3 }) });
                }
                timeline.Tracks.Add(typed);
            }
            var tlb = LibraryBake.BakeTyped(timeline, manifest);
            parts.Add(new LibraryPart { Name = names[i], Tlb = tlb, Kinds = LibraryBake.KindPayload(names[i], tlb, kinds) });
        }
        return LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
    }

    void Cell(byte[] library, int live, int rows, int active)
    {
        var medians = new List<double>();
        ulong hash = 0;
        long kindBytes = 0, playerBytes = 0, scoreBytes = 0, allocated = 0, residentBytes = -1;
        double setupMs = 0;
        for (var run = 0; run < runs; run++)
        {
            var residentBefore = run == 0 ? ResidentAnon() : -1;
            var setup = Stopwatch.StartNew();
            using var world = new TimelineWorld(library);
            using var players = new PlaybackTable(rows, active);
            var store = new KindStore(world, players);
            var motion = new Motion[rows];
            var legacy = new LegacyMotion[rows];
            var stat = new WalkStat[rows];
            var power = new JumpPower[rows];
            var stamina = new Stamina[rows];
            var heat = new Heat[rows];
            var health = new Health[rows];
            for (var e = 0; e < rows; e++)
            {
                stat[e].Value = 1f;
                power[e].Value = 1f;
                health[e].Value = 1_000_000;
                for (var k = 0; k < active; k++)
                    players.Start(e, (ushort)(1 + (e * 7 + k * 13) % timelineCount), (ushort)((e * 31 + k * 17) % shortest));
            }
            void Step()
            {
                world.Tick(players, true);
                new Movement { Motion = motion, Legacy = legacy, WalkStat = stat, JumpPower = power, Stamina = stamina, Heat = heat }.Run(world, players, true);
                new Vitals { Health = health }.Run(world, players, true);
            }
            Step();
            setupMs = setup.Elapsed.TotalMilliseconds;
            for (var i = 0; i < warmup; i++) Step();
            if (residentBefore >= 0) residentBytes = ResidentAnon() - residentBefore;
            var samples = new double[measure];
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < measure; i++)
            {
                var start = Stopwatch.GetTimestamp();
                Step();
                samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            }
            var spent = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated = Math.Max(allocated, spent);
            Array.Sort(samples);
            medians.Add(samples[measure / 2]);
            var h = TlStateHash.Begin();
            for (var e = 0; e < rows; e++)
            {
                h.Float(motion[e].X);
                h.Float(motion[e].Y);
                h.Int(stamina[e].Value);
                h.Int(health[e].Value);
                h.Slots(players.View.RunOf(e), players.View.CountOf(e));
            }
            hash = h.Value;
            kindBytes = 0;
            for (var k = 0; k < store.KindCount; k++) kindBytes += store.GetStats(k).BytesPerRow;
            playerBytes = players.View.SlotsPerEntity * 8L + 4;
            scoreBytes = ScoreBytes(store);
        }
        medians.Sort();
        var ms = medians[medians.Count / 2];
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
            "cell rows={0} active={1} live={2} stepMs={3:F3} minMs={4:F3} maxMs={5:F3} nsPerActive={6:F2} kindBytesPerRow={7} playbackBytesPerRow={8} sharedTableBytes={9} residentBytesPerRow={10} setupMs={11:F0} alloc={12} hash={13:X16}",
            rows, active, live, ms, medians[0], medians[^1], ms * 1e6 / ((double)rows * active), kindBytes, playerBytes, scoreBytes, residentBytes < 0 ? -1 : residentBytes / rows, setupMs, allocated, hash));
    }

    static long ResidentAnon()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        if (!File.Exists("/proc/self/status")) return -1;
        foreach (var line in File.ReadLines("/proc/self/status"))
            if (line.StartsWith("RssAnon:", StringComparison.Ordinal))
                return long.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture) * 1024;
        return -1;
    }

    static long ScoreBytes(KindStore store)
    {
        var property = typeof(KindStore).GetProperty("Score");
        if (property == null) return 0;
        var value = property.GetValue(store)!;
        var cells = (int)value.GetType().GetField("Cells")!.GetValue(value)!;
        var start = (int*)System.Reflection.Pointer.Unbox(value.GetType().GetField("Start")!.GetValue(value)!);
        return (long)(cells + 1) * 4 + (long)start[cells] * 16;
    }
}
