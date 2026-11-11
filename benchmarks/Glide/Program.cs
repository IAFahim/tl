using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using Tl;
using Tl.TestSupport;

namespace GlideBench;

static unsafe class Program
{
    const int TimelineCount = 1000;
    const int StartBatchEntities = 256;
    const ulong HashOffset = 14695981039346656037ul;
    const ulong HashPrime = 1099511628211ul;

    struct Clip
    {
        public int Value;
    }

    struct Track : IBlend<Clip>
    {
        public void Blend(in Clip first, in Clip second, float factor, out Clip result) => result = first;
    }

    static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            if (args.Length == 0 || args[0] is "--help" or "-h")
            {
                Console.WriteLine("fixture: Glide --fixture <duration> <output.tlib>");
                Console.WriteLine("run: Glide <fixture.tlib> <entities> <active> <capacity> <limit> [warmup=256] [steps=32] [rounds=6] [auto|scalar]");
                Console.WriteLine("shapes: 4000000 4 4; 4000000 1 1; 1000000 4 4; 10000 150 4");
                Console.WriteLine("retained median and allocation sum exclude round 0; all raw rounds are retained");
                return 0;
            }
            if (args.Length == 3 && args[0] == "--fixture")
            {
                WriteFixture(Parse(args[1], 1, ushort.MaxValue, "duration"), args[2]);
                return 0;
            }
            if (args.Length < 5 || args.Length > 9) throw new ArgumentException("Use --help for arguments.");
            var entities = Parse(args[1], 1, int.MaxValue - 63, "entities");
            var active = Parse(args[2], 1, 256, "active");
            var capacity = Parse(args[3], 1, 256, "capacity");
            var limit = Parse(args[4], 0, 255, "limit");
            var warmup = args.Length > 5 ? Parse(args[5], 1, int.MaxValue, "warmup") : 256;
            var steps = args.Length > 6 ? Parse(args[6], 1, int.MaxValue, "steps") : 32;
            var rounds = args.Length > 7 ? Parse(args[7], 2, 10000, "rounds") : 6;
            var isa = args.Length > 8 ? args[8] : "auto";
            if (isa is not ("auto" or "scalar")) throw new ArgumentException("ISA must be auto or scalar.");
            Run(args[0], entities, active, capacity, limit, warmup, steps, rounds, isa);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    static int Parse(string value, int minimum, int maximum, string name)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < minimum || parsed > maximum)
            throw new ArgumentOutOfRangeException(name, value, $"Expected {minimum}..{maximum}.");
        return parsed;
    }

    static void WriteFixture(int duration, string path)
    {
        var tlb = new DomainBaker().Track<Track, Clip>(default).Clip(0, 0, (uint)duration, new Clip { Value = 1 }).Looping().Bake();
        var parts = new LibraryPart[TimelineCount];
        for (var i = 0; i < parts.Length; i++) parts[i] = new LibraryPart { Name = "Glide" + i.ToString("D4", CultureInfo.InvariantCulture), Tlb = tlb };
        var bytes = LibraryAssembler.Assemble(parts, 60, LibrarySettings.Default);
        if (FixtureDuration(bytes) != duration) throw new InvalidOperationException("Fixture duration mismatch.");
        File.WriteAllBytes(path, bytes);
        Console.WriteLine($"fixture={Path.GetFullPath(path)} duration={duration} timelines={TimelineCount} bytes={bytes.Length} sha256={Convert.ToHexString(SHA256.HashData(bytes))}");
    }

    static int FixtureDuration(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 64 || Read32(bytes, 0) != 0x42494C54u || Read32(bytes, 8) != TimelineCount)
            throw new InvalidOperationException("Fixture must be a TLIB with exactly 1000 timelines.");
        var manifest = Read32(bytes, 12);
        if ((ulong)manifest + TimelineCount * 32ul > (ulong)bytes.Length) throw new InvalidOperationException("Fixture manifest bounds.");
        var duration = 0u;
        for (var i = 0; i < TimelineCount; i++)
        {
            var row = checked((int)manifest + i * 32);
            var offset = Read32(bytes, row + 16);
            var length = Read32(bytes, row + 20);
            if (Read32(bytes, row) != (uint)i + 1 || length < 80 || (ulong)offset + length > (ulong)bytes.Length)
                throw new InvalidOperationException($"Fixture timeline bounds/id {i}.");
            var tlb = bytes.Slice(checked((int)offset), checked((int)length));
            var current = Read32(tlb, 12);
            if (Read32(tlb, 0) != 0x31424C54u || Read32(tlb, 4) != 6 || Read32(tlb, 8) != 1 || current == 0 || current > ushort.MaxValue || Read32(tlb, 52) != 0 || Read32(tlb, 56) != 0 || Read32(tlb, 60) != 0)
                throw new InvalidOperationException($"Fixture timeline must be a plain channel-zero loop: {i}.");
            if (i == 0) duration = current;
            else if (current != duration) throw new InvalidOperationException("Fixture timelines have different durations.");
        }
        return (int)duration;
    }

    static uint Read32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, 4));

    static void Run(string fixture, int entities, int active, int capacity, int limit, int warmup, int steps, int rounds, string isa)
    {
        var vector128 = Vector128.IsHardwareAccelerated;
        var vector256 = Vector256.IsHardwareAccelerated;
        var vector512 = Vector512.IsHardwareAccelerated;
        var scalarRequested = isa == "scalar" || Environment.GetEnvironmentVariable("DOTNET_EnableHWIntrinsic") == "0" || Environment.GetEnvironmentVariable("COMPlus_EnableHWIntrinsic") == "0";
        if (scalarRequested && (vector128 || vector256 || vector512 || Sse2.IsSupported || Avx2.IsSupported || AdvSimd.IsSupported))
            throw new InvalidOperationException("Scalar run requested but hardware intrinsics remain enabled.");
        var bytes = File.ReadAllBytes(fixture);
        var duration = FixtureDuration(bytes);
        var fixtureHash = Convert.ToHexString(SHA256.HashData(bytes));
        var coreTfm = typeof(PlaybackTable).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "unknown";
        var coreHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(PlaybackTable).Assembly.Location)));
        var samples = new double[rounds];
        var allocations = new long[rounds];
        using var process = Process.GetCurrentProcess();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        process.Refresh();
        var privateBefore = process.PrivateMemorySize64;
        var setupStart = Stopwatch.GetTimestamp();
        using var world = new TimelineWorld(bytes);
        using var players = CreatePlayers(entities, capacity, limit);
        for (var entity = 0; entity < entities; entity++)
        {
            for (var slot = 0; slot < active; slot++) players.Start(entity, Timeline(entity, slot), Phase(entity, slot, duration));
            if ((entity + 1) % StartBatchEntities == 0) world.ApplyRequests(players);
        }
        world.ApplyRequests(players);
        var setupMs = Milliseconds(Stopwatch.GetTimestamp() - setupStart);
        var firstStart = Stopwatch.GetTimestamp();
        world.Advance(players);
        var firstMs = Milliseconds(Stopwatch.GetTimestamp() - firstStart);
        var warmStart = Stopwatch.GetTimestamp();
        for (var tick = 1; tick < warmup; tick++) world.Advance(players);
        var warmupMs = firstMs + Milliseconds(Stopwatch.GetTimestamp() - warmStart);
        var timedBefore = GC.GetTotalAllocatedBytes(true);
        for (var round = 0; round < rounds; round++)
        {
            var before = GC.GetTotalAllocatedBytes(true);
            var start = Stopwatch.GetTimestamp();
            for (var tick = 0; tick < steps; tick++) world.Advance(players);
            samples[round] = Milliseconds(Stopwatch.GetTimestamp() - start) / steps;
            allocations[round] = GC.GetTotalAllocatedBytes(true) - before;
        }
        var allocatedTimed = GC.GetTotalAllocatedBytes(true) - timedBefore;
        process.Refresh();
        var privateAfter = process.PrivateMemorySize64;
        var actualLimit = ActualLimit(players);
        if (actualLimit != limit) throw new InvalidOperationException($"Cache was unexpectedly disabled: requested={limit}, actual={actualLimit}.");
        var positiveHorizons = PositiveHorizons(players, entities);
        var ticks = checked((long)warmup + (long)rounds * steps);
        var view = players.View;
        var hash = VerifyAndHash(view, players.Playing, entities, active, capacity, duration, ticks);
        var retained = samples.AsSpan(1).ToArray();
        Array.Sort(retained);
        var median = retained.Length % 2 == 0 ? (retained[retained.Length / 2 - 1] + retained[retained.Length / 2]) / 2 : retained[retained.Length / 2];
        long allocatedRetained = 0;
        for (var i = 1; i < allocations.Length; i++) allocatedRetained += allocations[i];
        var slotVisits = (long)entities * active;
#if GLIDE_BUILD
        const string build = "glide";
#else
        const string build = "baseline";
#endif
        Console.WriteLine($"host=dotnet build={build} coreTfm={coreTfm} coreSha256={coreHash} runtime={Environment.Version} entities={entities} active={active} capacity={capacity} duration={duration} requestedLimit={limit} actualLimit={actualLimit} positiveHorizons={positiveHorizons} participants=1 setupMs={setupMs:F6} firstMs={firstMs:F6} readySetupMs={setupMs + firstMs:F6} warmupMs={warmupMs:F6} privateBefore={privateBefore} privateAfter={privateAfter} privateDelta={privateAfter - privateBefore} allocTimed={allocatedTimed} allocRetained={allocatedRetained} allocZero={(allocatedRetained == 0 ? 1 : 0)} roundAlloc={string.Join(',', allocations)} warmup={warmup} rounds={rounds} steps={steps} ticks={ticks} medianMs={median:F6} nsPerActive={median * 1000000.0 / slotVisits:F6} samplesMs={string.Join(',', samples.Select(value => value.ToString("F6", CultureInfo.InvariantCulture)))} oracle=1 hash={hash:X16} fixtureSha256={fixtureHash} scalarRequested={(scalarRequested ? 1 : 0)} vector128={(vector128 ? 1 : 0)} vector256={(vector256 ? 1 : 0)} vector512={(vector512 ? 1 : 0)} sse2={(Sse2.IsSupported ? 1 : 0)} avx2={(Avx2.IsSupported ? 1 : 0)} advSimd={(AdvSimd.IsSupported ? 1 : 0)}");
    }

    static PlaybackTable CreatePlayers(int entities, int capacity, int limit)
    {
        if (limit == 0) return new PlaybackTable(entities, capacity);
#if GLIDE_BUILD
        return new PlaybackTable(entities, capacity, limit);
#else
        throw new ArgumentException("Baseline build only accepts horizon limit 0.");
#endif
    }

    static int ActualLimit(PlaybackTable players)
    {
#if GLIDE_BUILD
        return players.HorizonLimit;
#else
        return 0;
#endif
    }

    static int PositiveHorizons(PlaybackTable players, int entities)
    {
#if GLIDE_BUILD
        var horizons = players.Horizons;
        if (horizons == null) return 0;
        var positive = 0;
        for (var entity = 0; entity < entities; entity++)
            if (horizons[entity] != 0) positive++;
        return positive;
#else
        return 0;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ushort Timeline(int entity, int slot) => (ushort)(1 + ((entity * 17L + slot * 7L) % TimelineCount));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ushort Phase(int entity, int slot, int duration) => (ushort)((entity * 37L + slot * 11L) % duration);

    static double Milliseconds(long elapsed) => elapsed * 1000.0 / Stopwatch.Frequency;

    static ulong VerifyAndHash(PlaybackView view, ulong* playing, int entities, int active, int capacity, int duration, long ticks)
    {
        var hash = HashOffset;
        for (var entity = 0; entity < entities; entity++)
        {
            var count = view.CountOf(entity);
            if (count != active) throw new InvalidOperationException($"Count oracle row={entity}: expected={active}, actual={count}.");
            if ((playing[entity >> 6] & (1ul << (entity & 63))) == 0) throw new InvalidOperationException($"Playing oracle row={entity}.");
            hash = Mix32(hash, (uint)entity);
            hash = Mix32(hash, (uint)count);
            var run = view.RunOf(entity);
            for (var slot = 0; slot < active; slot++)
            {
                var playback = run + slot;
                var expectedTimeline = (ushort)(1 + ((entity * 17L + slot * 7L) % TimelineCount));
                var expectedTick = (ushort)(((entity * 37L + slot * 11L) % duration + ticks) % duration);
                if (playback->Timeline != expectedTimeline || playback->Tick != expectedTick || playback->Channel != 0 || playback->Flags != PlaybackFlags.None || playback->Rate != 0 || playback->Phase != 0)
                    throw new InvalidOperationException($"Playback oracle row={entity} slot={slot}: expected={expectedTimeline}:{expectedTick}:0:0:0:0, actual={playback->Timeline}:{playback->Tick}:{playback->Channel}:{(byte)playback->Flags}:{playback->Rate}:{playback->Phase}.");
                for (var b = 0; b < 8; b++) hash = unchecked((hash ^ ((byte*)playback)[b]) * HashPrime);
            }
            if (active <= capacity)
                for (var slot = active; slot < capacity; slot++)
                    if (((ulong*)run)[slot] != 0) throw new InvalidOperationException($"Unused inline slot oracle row={entity} slot={slot}.");
        }
        var words = (entities + 63) >> 6;
        var tail = entities & 63;
        if (tail != 0 && (playing[words - 1] >> tail) != 0) throw new InvalidOperationException("Playing tail oracle.");
        return hash;
    }

    static ulong Mix32(ulong hash, uint value)
    {
        for (var b = 0; b < 4; b++) hash = unchecked((hash ^ (byte)(value >> (b * 8))) * HashPrime);
        return hash;
    }
}
