using System.Globalization;
using Tl.Algorithms;
using Tl.Hooks;
using static Waffle.WaffleSyntax;

if (args.Length != 1)
    throw new ArgumentException("Supply the repository root directory.");

var algorithmsDir = Path.Combine(args[0], "benchmarks", "Algorithms", "Generated");
var dispatchDir = Path.Combine(args[0], "benchmarks", "Dispatch", "Generated");
var dataDir = Path.Combine(args[0], "benchmarks", "Generate", "Data");

Directory.CreateDirectory(algorithmsDir);
Directory.CreateDirectory(dispatchDir);

ushort[] ReadIndices(string fixtureName)
{
    var path = Path.Combine(dataDir, fixtureName + ".indices");
    var values = File.ReadAllText(path)
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(token => ushort.Parse(token[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture))
        .ToArray();

    for (var i = 1; i < values.Length; i++)
        if (values[i] <= values[i - 1])
            throw new InvalidDataException($"{path}: indices must be strictly ascending.");

    return values;
}

string Hex(ushort value) => "0x" + value.ToString("X4", CultureInfo.InvariantCulture);

string Hex2(int value) => "0x" + value.ToString("X2", CultureInfo.InvariantCulture);

string Nibble(int value) => "0x" + value.ToString("X", CultureInfo.InvariantCulture);

foreach (var (name, clips, duration) in new[] { ("Small", 8, 256), ("Medium", 64, 4096), ("Large", 512, 65536) })
{
    var fixture = Fixture.Build(clips, duration);

    string Emit(Region r)
    {
        if (r.A < 0)
            return "return;";

        if (r.B < 0)
            return Render($$"""
                sink.Sample(in payloads[{{r.A}}], 1f);
                return;
                """);

        var factor = r.BlendLength <= 1 ? "0.5f" : $"(float)(tick - {r.BlendStart}) / {(r.BlendLength - 1).ToString(CultureInfo.InvariantCulture)}f";
        return Render($$"""
            var factor = {{factor}};
            sink.Sample(in payloads[{{r.A}}], 1f - factor);
            sink.Sample(in payloads[{{r.B}}], factor);
            return;
            """);
    }

    string Tree(int lo, int hi)
    {
        if (hi - lo == 1)
            return Emit(fixture.Regions[lo]);

        var mid = (lo + hi) / 2;
        var left = Tree(lo, mid);
        var right = Tree(mid, hi);

        return Render($$"""
            if (tick < {{fixture.Regions[mid].Start}})
            {
                {{left}}
            }
            else
            {
                {{right}}
            }
            """);
    }

    var cases = new List<string>();

    for (var i = 0; i < fixture.Regions.Length; i++)
    {
        var transition = i + 1 == fixture.Regions.Length ? string.Empty : Render($$"""
            if (tick >= {{fixture.Regions[i].End}})
            {
                state = {{i + 1}};
                goto case {{i + 1}};
            }
            """);
        var emit = Emit(fixture.Regions[i]);
        cases.Add(Render($$"""
            case {{i}}:
            {
                {{transition}}
                {{emit}}
            }
            """));
    }

    var tree = Tree(0, fixture.Regions.Length);
    var source = Render($$"""
        namespace Tl.Algorithms;

        public readonly struct {{name}}Code : IGenerated
        {
            public static Fixture Create() => Fixture.Build({{clips}}, {{duration}});

            public static void Tree<TSink>(int tick, Payload[] payloads, ref TSink sink)
                where TSink : struct, ISink
            {
                {{tree}}
            }

            public static void State<TSink>(int tick, ref int state, Payload[] payloads, ref TSink sink)
                where TSink : struct, ISink
            {
                switch (state)
                {
                {{ForEach(cases, out var body)}}
                    {{body}}
                {{End}}
                }
            }
        }
        """);

    var path = Path.Combine(algorithmsDir, $"{name}.g.cs");
    File.WriteAllText(path, source);
    Console.WriteLine($"{name}: {clips} clips, {fixture.Regions.Length} regions, {source.Length} source characters.");
}

foreach (var name in new[] { "Fused256", "Fused4096" })
{
    var indices = ReadIndices(name);
    var sparseCases = new List<string>();
    var denseCases = new List<string>();
    var tableRows = new List<string>();
    var tlMethods = new List<string>();

    for (var i = 0; i < indices.Length; i++)
    {
        var body = Render($$"""
            if (tick >= 29u)
                s.Visit({{4 * i + 3}});
            else if (tick >= 11u)
                s.Visit({{4 * i + 2}});
            else if (tick >= 3u)
                s.Visit({{4 * i + 1}});
            else
                s.Visit({{4 * i}});
            """);

        sparseCases.Add(Render($$"""
            case {{Hex(indices[i])}}:
                {{body}}
                break;
            """));

        denseCases.Add(Render($$"""
            case {{i}}:
                {{body}}
                break;
            """));

        tableRows.Add($"t[{Hex(indices[i])}] = &TL{i};");

        tlMethods.Add(Render($$"""
            private static void TL{{i}}(uint tick, ref DispatchSink s)
            {
                {{body}}
            }
        """));
    }

    var fusedSparse = Render($$"""
        public static void FusedSparse(ushort index, uint tick, ref DispatchSink s)
        {
            switch (index)
            {
            {{ForEach(sparseCases, out var sparseCase)}}
                {{sparseCase}}
            {{End}}
            }
        }
    """);

    var fusedDense = Render($$"""
        public static void FusedDense(int id, uint tick, ref DispatchSink s)
        {
            switch (id)
            {
            {{ForEach(denseCases, out var denseCase)}}
                {{denseCase}}
            {{End}}
            }
        }
    """);

    var twoLevelFp = Render($$"""
        public static void TwoLevelFp(ushort index, uint tick, ref DispatchSink s)
            => s_table[index](tick, ref s);
    """);

    var fusedTable = Render($$"""
        private static readonly delegate*<uint, ref DispatchSink, void>[] s_table = BuildTable();
    """);

    var buildTable = Render($$"""
        private static delegate*<uint, ref DispatchSink, void>[] BuildTable()
        {
            var t = new delegate*<uint, ref DispatchSink, void>[65536];
        {{ForEach(tableRows, out var tableRow)}}
            {{tableRow}}
        {{End}}
            return t;
        }
    """);

    var source = Render($$"""
    // <auto-generated>
    // Fused dispatch fixture with {{indices.Length}} timelines: one huge switch over sparse
    // ushort indices, one huge switch over dense ids (jump table + inline
    // bodies), and a function-pointer table calling per-timeline methods.
    // See docs/benchmarks.md.
    namespace Tl.Hooks;

    public unsafe struct {{name}} : IFusedFixture
    {
        public static ReadOnlySpan<ushort> Indices => s_indices;

        private static readonly ushort[] s_indices = [{{string.Join(",", indices.Select(Hex))}}];

    {{string.Join("\n\n", new[] { fusedSparse, fusedDense, twoLevelFp, fusedTable, buildTable, string.Join("\n", tlMethods) })}}
    }
    """) + "\n";

    File.WriteAllText(Path.Combine(dispatchDir, name + ".g.cs"), source);
    Console.WriteLine($"{name}: {indices.Length} timelines, {source.Length} source characters.");
}

foreach (var name in new[] { "Sparse256", "Sparse4096" })
{
    var indices = ReadIndices(name);
    var ranked = indices.Select((value, rank) => (Value: value, Rank: rank)).ToList();
    var members = new List<string>();

    var linearLines = ranked.Select(t => $"if (index == {Hex(t.Value)}) {{ s.Visit({t.Rank}); return; }}").ToList();

    members.Add(Render($$"""
        public static void Linear(ushort index, ref DispatchSink s)
        {
        {{ForEach(linearLines, out var linearLine)}}
            {{linearLine}}
        {{End}}
        }
    """));

    members.Add(Render($$"""
        public static void Binary(ushort index, ref DispatchSink s)
        {
            var lo = 0;
            var hi = s_indices.Length;

            while (lo < hi)
            {
                var mid = (lo + hi) >>> 1;
                if (s_indices[mid] <= index)
                    lo = mid + 1;
                else
                    hi = mid;
            }

            s.Visit(lo - 1);
        }
    """));

    var flatCases = ranked.Select(t => $"case {Hex(t.Value)}: s.Visit({t.Rank}); break;").ToList();

    members.Add(Render($$"""
        public static void FlatSwitch(ushort index, ref DispatchSink s)
        {
            switch (index)
            {
        {{ForEach(flatCases, out var flatCase)}}
                {{flatCase}}
        {{End}}
            }
        }
    """));

    var radix8Lines = ranked.GroupBy(t => t.Value >> 8)
        .Select(g => $"case {Hex2(g.Key)}: R8_{g.Key.ToString("X2")}(index, ref s); break;")
        .ToList();

    members.Add(Render($$"""
        public static void Radix8(ushort index, ref DispatchSink s)
        {
            switch (index >> 8)
            {
        {{ForEach(radix8Lines, out var radix8Line)}}
                {{radix8Line}}
        {{End}}
            }
        }
    """));

    foreach (var bucket in ranked.GroupBy(t => t.Value >> 8))
    {
        var bucketCases = bucket.Select(t => $"case {Hex2(t.Value & 0xFF)}: s.Visit({t.Rank}); break;").ToList();
        var bucketName = bucket.Key.ToString("X2");

        members.Add(Render($$"""
            private static void R8_{{bucketName}}(ushort index, ref DispatchSink s)
            {
                switch (index & 0xFF)
                {
            {{ForEach(bucketCases, out var bucketCase)}}
                    {{bucketCase}}
            {{End}}
                }
            }
        """));
    }

    var radix4Lines = Enumerable.Range(0, 16)
        .Select(n => $"case {Nibble(n)}: R4_{n.ToString("X")}(index, ref s); break;")
        .ToList();

    members.Add(Render($$"""
        public static void Radix4(ushort index, ref DispatchSink s)
        {
            switch (index >> 12)
            {
        {{ForEach(radix4Lines, out var radix4Line)}}
                {{radix4Line}}
        {{End}}
            }
        }
    """));

    void EmitRadix4Bucket(int depth, string prefix, List<(ushort Value, int Rank)> items)
    {
        var caseLines = new List<string>();
        var childKeys = new List<int>();
        var childItems = new List<List<(ushort Value, int Rank)>>();

        if (depth == 2)
        {
            foreach (var t in items)
                caseLines.Add($"case {Nibble(t.Value & 0xF)}: s.Visit({t.Rank}); break;");
        }
        else
        {
            foreach (var g in items.GroupBy(t => (t.Value >> (12 - 4 * (depth + 1))) & 0xF))
            {
                childKeys.Add(g.Key);
                childItems.Add(g.ToList());
                caseLines.Add($"case {Nibble(g.Key)}: R4_{prefix}{g.Key.ToString("X")}(index, ref s); break;");
            }
        }

        var switchExpression = depth switch
        {
            0 => "(index >> 8) & 0xF",
            1 => "(index >> 4) & 0xF",
            _ => "index & 0xF",
        };

        members.Add(Render($$"""
            private static void R4_{{prefix}}(ushort index, ref DispatchSink s)
            {
                switch ({{switchExpression}})
                {
            {{ForEach(caseLines, out var caseLine)}}
                    {{caseLine}}
            {{End}}
                }
            }
        """));

        for (var c = 0; c < childItems.Count; c++)
            EmitRadix4Bucket(depth + 1, prefix + childKeys[c].ToString("X"), childItems[c]);
    }

    for (var nibble = 0; nibble < 16; nibble++)
        EmitRadix4Bucket(0, nibble.ToString("X"), ranked.Where(t => t.Value >> 12 == nibble).ToList());

    members.Add(Render($$"""
        public static unsafe void DenseFp(ushort index, ref DispatchSink s) => s_table[index](ref s);
    """));

    members.Add(Render($$"""
        private static readonly delegate*<ref DispatchSink, void>[] s_table = BuildTable();
    """));

    var tableRows = ranked.Select(t => $"t[{Hex(t.Value)}] = &L{t.Rank};").ToList();

    members.Add(Render($$"""
        private static unsafe delegate*<ref DispatchSink, void>[] BuildTable()
        {
            var t = new delegate*<ref DispatchSink, void>[65536];
        {{ForEach(tableRows, out var tableRow)}}
            {{tableRow}}
        {{End}}
            return t;
        }
    """));

    members.Add(string.Join("\n", ranked.Select(t => $"    private static void L{t.Rank}(ref DispatchSink s) => s.Visit({t.Rank});")));

    var source = Render($$"""
    // <auto-generated>
    // Sparse ushort dispatch fixture with {{indices.Length}} timeline indices: linear chain,
    // binary search, flat sparse switch, radix 8+8, radix 4x4x4x4, and a dense
    // function-pointer jump table. See docs/benchmarks.md.
    namespace Tl.Hooks;

    public unsafe struct {{name}} : IRadixFixture
    {
        public static ReadOnlySpan<ushort> Indices => s_indices;

        private static readonly ushort[] s_indices = [{{string.Join(",", indices.Select(Hex))}}];

    {{string.Join("\n\n", members)}}
    }
    """) + "\n";

    File.WriteAllText(Path.Combine(dispatchDir, name + ".g.cs"), source);
    Console.WriteLine($"{name}: {indices.Length} timelines, {source.Length} source characters.");
}

// ---- Frozen per-timeline playback (Dispatch fixtures) ----
// Per-timeline code specialized at emission time against tables the generator
// can read, under the redesigned consumer API: the Playback flags word
// carries only lifecycle and completion facts (Started minted at Start,
// Completed positional per direction), and movement facts are per-work
// ClipState codes (Enter=0, Stay=1, Exit=2) accumulated by the sink. The
// full-bake dense LUT form bakes the whole per-tick leaf into ONE
// interleaved ulong word per tick per direction - the per-slot Exit mask,
// the direction-dependent Completed bit, the any-active bit, the active-slot
// count, and (single-slot fixtures) the blend-resolved slot value's float
// bits - all computed with the identical float expressions the runtime
// evaluates. Each slot's entry reference (the window Start forward, the
// window End backward) rides beside the words - in a uint table for the
// single-slot shape, interleaved with the slot's value bits in one pair
// word per slot for the multi-slot shape - so Enter is one compare against
// prev at ONE load per slot. Bake v3 keeps the hot loop free of
// data-dependent branches: every tick does its slot work UNCONDITIONALLY -
// the any-active bit adds itself to Count, each slot's ClipState code is
// pure integer arithmetic on the exit bit and one setcc enter compare, and
// inactive slots are baked to contribute exactly nothing (reference at the
// direction's always-Enter-false sentinel, value bits zero) - so empty
// ticks do no observable sink work with no `if (n != 0)` guard at all; the
// out-of-range clamp target is the empty sentinel word at the duration.
// The frozen form is bound to one exact non-looping timeline: no wraps, no
// cycle arithmetic (Cycles passes through unchanged), no duration-0/empty
// handling.

string U(uint value) => value.ToString(CultureInfo.InvariantCulture) + "u";

string EmitFrozen(
    string name, string[] notes, string tail,
    uint[] starts, RegionRow[] regionRows, byte[] regionFlags,
    TrackRow[] trackRows, ClipRow[] clipRows, ClipEdge[] edges,
    float[] amounts)
{
    var duration = starts[^1];

    // The cut facts are off the redesigned engine's hot path (the aggregate
    // movement bits are gone), but the tables stay: this emission-time
    // cross-check is what keeps them honest. Bit 1 = the region starts on a
    // clip start, bit 2 = on a clip end, bit 4 = the region ends on a clip
    // end; the last region has no successor, so its bit 4 stays clear.
    for (var r = 0; r < starts.Length; r++)
    {
        byte expected = 0;
        foreach (var edge in edges)
        {
            if (edge.Start == starts[r])
                expected |= 1;
            if (edge.End == starts[r])
                expected |= 2;
            if (r + 1 < starts.Length && edge.End == starts[r + 1])
                expected |= 4;
        }

        if (regionFlags[r] != expected)
            throw new InvalidOperationException($"{name}: region {r} cut flags {regionFlags[r]} diverge from the clip edges ({expected}).");
    }

    // The redesigned frozen path emits only the full-bake dense LUT form;
    // a longer timeline rejects here rather than emitting an unverified
    // lookup strategy.
    const int denseLimit = 1024;
    if (duration > denseLimit)
        throw new InvalidOperationException($"{name}: the frozen full-bake dense LUT covers durations <= {denseLimit}; duration {duration} exceeds it.");

    var slotCount = 0;
    foreach (var row in regionRows)
        if (row.TrackCount > slotCount)
            slotCount = row.TrackCount;

    // The clamped out-of-range table entry reproduces the empty sentinel
    // region at the duration: it must carry no tracks, so ticks past the
    // duration do no sink work. (A clip starting exactly at the duration
    // cannot exist - its end would move the duration - so the cut
    // cross-check above already pins that shape.)
    if (regionRows[^1].TrackCount != 0)
        throw new InvalidOperationException($"{name}: dense full-bake requires an empty sentinel region at the duration (no tracks).");

    // The word's per-slot exit mask is one byte and the active-slot count a
    // nibble, so the packing covers at most eight concurrently active
    // tracks.
    if (slotCount > 8)
        throw new InvalidOperationException($"{name}: dense full-bake packs the exit mask into one byte and the active count into a nibble; {slotCount} active tracks exceed 8.");

    // Single-slot fixtures (exactly one active-track position at any tick)
    // get the fully packed word: the slot value's float bits ride in bits
    // 0..31, so ONE load serves the value, the Exit bit, the Completed bit,
    // and the any-active bit; the entry reference rides in its own uint
    // table (one more load per tick). Multi-slot fixtures keep the facts
    // word and give each slot ONE interleaved pair table instead - a ulong
    // per tick whose low half is the slot value's float bits and whose high
    // half is that slot's entry reference - so the per-slot cost stays ONE
    // load for value AND movement together (the bake v2 access pattern:
    // one word load plus one load per slot).
    var single = slotCount == 1;

    var wordsForward = new ulong[(int)duration + 1];
    var wordsBackward = new ulong[(int)duration + 1];
    var pairsForward = new ulong[slotCount][];
    var pairsBackward = new ulong[slotCount][];
    for (var k = 0; k < slotCount; k++)
    {
        pairsForward[k] = new ulong[(int)duration + 1];
        pairsBackward[k] = new ulong[(int)duration + 1];
    }
    var enterForward = new uint[slotCount][];
    var enterBackward = new uint[slotCount][];
    for (var k = 0; k < slotCount; k++)
    {
        enterForward[k] = new uint[(int)duration + 1];
        enterBackward[k] = new uint[(int)duration + 1];
    }

    // The value baked into a slot: the byte-for-byte expression
    // Tracks.MoveNext plus Blend evaluate at run time, folded here in
    // generator float arithmetic - standalone clips contribute the payload
    // constant, pairs the factor (tick - factorStart) / (float)
    // (factorLength - 1) (0.5f when the window is one tick) run through
    // first * (1f - f) + second * f. All float, never double: the parity
    // receipts demand bit-exact equality with the table path.
    float SlotValue(uint tick, TrackRow track)
    {
        if (track.ClipCount == 1)
            return amounts[clipRows[track.ClipStart].ClipIndex];

        var first = clipRows[track.ClipStart];
        var second = clipRows[track.ClipStart + 1];
        var factor = first.FactorLength <= 1 ? 0.5f : (tick - first.FactorStart) / (float)(first.FactorLength - 1);
        return amounts[first.ClipIndex] * (1f - factor) + amounts[second.ClipIndex] * factor;
    }

    // A work's true window: ClipEdges[ClipIndex] for an original, the OUTER
    // window (min Start, max End) for a blend-resolved pair - exactly the
    // convention TrackWork.State reports (pinned by the per-work oracle).
    (uint Start, uint End) Window(TrackRow track)
    {
        var first = edges[clipRows[track.ClipStart].ClipIndex];
        if (track.ClipCount == 1)
            return (first.Start, first.End);

        var second = edges[clipRows[track.ClipStart + 1].ClipIndex];
        var start = first.Start < second.Start ? first.Start : second.Start;
        var end = first.End > second.End ? first.End : second.End;
        return (start, end);
    }

    // One interleaved word per tick per direction, baked over the whole tick
    // domain 0..duration inclusive (the entry at the duration is the empty
    // sentinel the clamp targets): bits 0..31 the slot-0 float bits
    // (single-slot shape only), bits 32..39 the per-slot exit mask (bit per
    // slot: forward at window End-1, backward at window Start - Exit is
    // positional), bit 40 the direction-dependent Completed bit (forward
    // from duration-1 on, backward exactly at 0), bit 41 the any-active bit
    // (the branchless Count: one added per tick with at least one active
    // slot, zero on gaps), bits 44..47 the active slot count (kept for
    // table inspection; the hot loop reads only bit 41). Each slot's entry
    // reference - the window Start forward, the window End backward - rides
    // in the single-slot shape's uint tables and in the multi-slot shape's
    // pair high halves, so Enter is one setcc compare of prev against it;
    // every slot FIRST bakes the direction's always-Enter-false sentinel
    // (uint.MaxValue forward, where prev < uint.MaxValue always holds; 0u
    // backward, where prev >= 0u always holds) and active slots overwrite
    // it, so an inactive slot's unconditional code arithmetic yields
    // exactly 0 (and its 0f value bits make the Sum add an exact no-op).
    for (var t = 0; t <= duration; t++)
    {
        var region = 0;
        while (region + 1 < starts.Length && starts[region + 1] <= (uint)t)
            region++;

        var row = regionRows[region];
        byte exitsF = 0, exitsB = 0;
        var slot = 0;
        for (var k = 0; k < slotCount; k++)
        {
            pairsForward[k][t] = (ulong)uint.MaxValue << 32;
            pairsBackward[k][t] = 0u;
            enterForward[k][t] = uint.MaxValue;
            enterBackward[k][t] = 0u;
        }
        for (var k = 0; k < row.TrackCount; k++)
        {
            var track = trackRows[row.TrackStart + k];
            var (start, end) = Window(track);
            var bits = (uint)BitConverter.SingleToInt32Bits(SlotValue((uint)t, track));
            pairsForward[slot][t] = bits | (ulong)start << 32;
            pairsBackward[slot][t] = bits | (ulong)end << 32;
            enterForward[slot][t] = start;
            enterBackward[slot][t] = end;
            if ((uint)t == end - 1)
                exitsF |= (byte)(1 << slot);
            if ((uint)t == start)
                exitsB |= (byte)(1 << slot);
            slot++;
        }

        var completedF = (byte)(t >= duration - 1u ? 1 : 0);
        var completedB = (byte)(t == 0u ? 1 : 0);
        var anyActive = (uint)(slot != 0 ? 1 : 0);
        var slotBits = single ? (uint)pairsForward[0][t] : 0u;

        wordsForward[t] = (ulong)slotBits | (ulong)exitsF << 32 | (ulong)completedF << 40 | (ulong)anyActive << 41 | (ulong)slot << 44;
        wordsBackward[t] = (ulong)slotBits | (ulong)exitsB << 32 | (ulong)completedB << 40 | (ulong)anyActive << 41 | (ulong)slot << 44;
    }

    // The tables emit as ReadOnlySpan collection expressions (not static
    // readonly arrays): Roslyn lowers them to frozen read-only data blobs,
    // so the JIT sees constant lengths (immediate bounds guards instead of
    // a length load per access) and no GC-static base initialization check
    // in the hot loop - same values, same indexing, zero allocation.
    string WordTable(string table, ulong[] values)
    {
        var rows = new List<string>();
        for (var i = 0; i < values.Length; i += 4)
        {
            var count = Math.Min(4, values.Length - i);
            var row = string.Join(", ", Enumerable.Range(i, count).Select(j => "0x" + values[j].ToString("X16", CultureInfo.InvariantCulture) + "UL"));
            rows.Add(i + count < values.Length ? row + "," : row);
        }

        return $$"""
            private static ReadOnlySpan<ulong> {{table}} =>
            [
                {{string.Join("\n    ", rows)}}
            ];
            """;
    }

    string UintTable(string table, uint[] values)
    {
        var rows = new List<string>();
        for (var i = 0; i < values.Length; i += 8)
        {
            var count = Math.Min(8, values.Length - i);
            var row = string.Join(", ", Enumerable.Range(i, count).Select(j => U(values[j])));
            rows.Add(i + count < values.Length ? row + "," : row);
        }

        return $$"""
            private static ReadOnlySpan<uint> {{table}} =>
            [
                {{string.Join("\n    ", rows)}}
            ];
            """;
    }

    // AggressiveInlining on the frozen entry points: the full-bake bodies
    // are small enough to fold into callers, which is what keeps the hub's
    // forwarder layer (and any consumer loop) on the tight path instead of
    // paying a call per batch.
    const string Inline = "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";

    var startMethod = $$"""
        {{Inline}}
        public static Playback Start(uint at = 0) => new Playback(at, 0, PlaybackFlags.Started);
        """;

    // One Forward/Backward shape for both slot shapes. The loop carries only
    // the previous tick, the pass-through cycles, and the running flags: the
    // sink never saw the intermediate Playback, so the single construction
    // after the loop (from the last tick) is the only one needed. Frozen
    // timelines never loop - no step adds cycles - so the Playback
    // constructor's cycle-capacity throw is unreachable here: the
    // construction re-passes from.Cycles (within Playback's invariant) and a
    // flags word that only ever holds Started | Completed. Per tick the
    // flags are minted fresh (Started always, Completed from the word's bit
    // 40) and EVERY slot line runs unconditionally - bake v3's zero-data-
    // dependent-branch movement: Count adds the word's any-active bit (bit
    // 41, no `if (n != 0)`); each slot's value bits AND entry reference
    // arrive in ONE pair load (multi-slot shape; the single-slot shape reads
    // the value from the word itself and the reference from its uint
    // table); each slot's ClipState code is pure integer arithmetic,
    // exit + (exit | enter) with exit the word's exit bit for the slot and
    // enter the 0/1 setcc of the prev compare against the reference (exit
    // set -> 1 + 1 = 2, exit clear -> 0 + enter - Exit precedence exact,
    // no branch), while inactive slots were baked to contribute nothing
    // (reference at the always-Enter-false sentinel, value bits 0), so
    // their unconditional adds are exact no-ops; the per-slot codes are
    // summed and folded into Flags once per tick (integer addition is
    // associative even under wraparound, so the collapsed accumulation is
    // bit-exact); Backward subtracts both in the same order (the exact
    // inverse). The only conditional branches in the loop are the constant
    // clamp (tick < duration, never taken in range), the never-taken table
    // bounds guards, and the span iteration itself - none depends on the
    // tick VALUES, so no stream shape can mispredict.
    string Loop(string method, bool backward)
    {
        var op = backward ? "-" : "+";
        var wordTable = single ? (backward ? "s_tickB" : "s_tickF") : (backward ? "s_wordB" : "s_wordF");
        var enterPrefix = backward ? "s_enterB" : "s_enterF";
        var enterCompare = backward ? ">=" : "<";
        var pairPrefix = backward ? "s_pairB" : "s_pairF";
        var codes = Enumerable.Range(0, slotCount).ToList();

        var slotLines = single
            ? $$"""
                var exit0 = exits & 1;
                var enter0 = prev {{enterCompare}} {{enterPrefix}}0[i] ? 0 : 1;
                sink.Flags {{op}}= exit0 + (exit0 | enter0);
                sink.Sum {{op}}= BitConverter.Int32BitsToSingle((int)word);
                """
            : string.Join("\n", new[]
            {
                string.Join("\n", codes.Select(k => $"var pair{k} = {pairPrefix}{k}[i];")),
                string.Join("\n", codes.Select(k => $"sink.Sum {op}= BitConverter.Int32BitsToSingle((int)pair{k});")),
                string.Join("\n", codes.Select(k => $"var exit{k} = (exits >> {k}) & 1;")),
                string.Join("\n", codes.Select(k => $"var enter{k} = prev {enterCompare} (uint)(pair{k} >> 32) ? 0 : 1;")),
                $"sink.Flags {op}= " + string.Join(" + ", codes.Select(k => $"exit{k} + (exit{k} | enter{k})")) + ";",
            });

        return $$"""
            {{Inline}}
            public static Playback {{method}}(in Playback from, ref FrozenSink sink, params ReadOnlySpan<uint> ticks)
            {
                var cycles = from.Cycles;
                var prev = from.Tick;
                var flags = from.Flags;
                foreach (var tick in ticks)
                {
                    var i = (int)(tick < {{U(duration)}} ? tick : {{U(duration)}});
                    var word = {{wordTable}}[i];
                    var exits = (int)(word >> 32);
                    flags = (PlaybackFlags)((uint)PlaybackFlags.Started
                        | (((word >> 40) & 1u) * (uint)PlaybackFlags.Completed));
                    sink.Count {{op}}= (int)((word >> 41) & 1u);
                    {{slotLines}}
                    prev = tick;
                }
                return new Playback(prev, cycles, flags);
            }
            """;
    }

    var forward = Loop("Forward", false);
    var backward = Loop("Backward", true);

    var members = new List<string> { startMethod, forward, backward };

    if (single)
    {
        members.Add(WordTable("s_tickF", wordsForward));
        members.Add(WordTable("s_tickB", wordsBackward));
        for (var k = 0; k < slotCount; k++)
        {
            members.Add(UintTable($"s_enterF{k}", enterForward[k]));
            members.Add(UintTable($"s_enterB{k}", enterBackward[k]));
        }
    }
    else
    {
        members.Add(WordTable("s_wordF", wordsForward));
        members.Add(WordTable("s_wordB", wordsBackward));
        for (var k = 0; k < slotCount; k++)
        {
            members.Add(WordTable($"s_pairF{k}", pairsForward[k]));
            members.Add(WordTable($"s_pairB{k}", pairsBackward[k]));
        }
    }

    var header = string.Join("\n", notes.Select(note => "// " + note));
    var body = string.Join("\n\n", members);

    return $$"""
        // <auto-generated>
        {{header}}
        namespace Tl.Hooks;

        public unsafe struct {{name}} : IFrozen
        {
        {{body}}
        }
        {{tail}}
        """ + "\n";
}

// (a) The real fixture: tables read straight from Hooks.cs - a single source
// of truth shared with the oracle the parity receipts run through.
var vitalsStarts = VitalsTrack.RegionStarts.ToArray();
var vitalsSource = EmitFrozen(
    "VitalsFrozen",
    [
        "Frozen playback for the VitalsTrack fixture (duration 600, 13 region",
        "starts = 12 regions plus the empty sentinel at 600, 4 tracks, blends",
        "and gaps), specialized at emission time against the redesigned API:",
        "the Playback flags word carries only lifecycle and completion facts",
        "(Started minted at Start, Completed positional per direction), and",
        "movement facts are per-work ClipState codes accumulated by the sink.",
        "Accumulation contract, mirrored bit-for-bit by the oracle in",
        "Dispatch's Program.cs: per tick WITH at least one active track, in",
        "track-row order — Sum += one blend-resolved clip value per active",
        "slot, Flags += the slot's ClipState code (Enter=0, Stay=1, Exit=2:",
        "Exit positional at the window's last active frame — End-1 forward,",
        "Start backward; Enter when the step crossed the entry edge — Start",
        "forward, End backward; both from per-slot enter-reference tables),",
        "then Count++; empty ticks do no sink work; Backward subtracts in the",
        "same order (the exact inverse).",
        "Slot values are baked floats - folded at emission time with the same",
        "blend-factor arithmetic the table path evaluates at run time",
        "((tick - factorStart) / (float)(factorLength - 1), 0.5f for a",
        "one-tick window, then first * (1f - f) + second * f, all in float),",
        "so the parity receipts hold bit-for-bit. Specialized from",
        "PlaybackCore for this exact non-looping timeline: no wraps, no cycle",
        "arithmetic (Cycles passes through unchanged), effective positions",
        "are the raw ticks, and duration-0/empty handling does not apply.",
        "Lookup strategy: dense per-tick word tables, one per direction,",
        "plus one interleaved pair table per slot per direction.",
        "Word layout (little-endian, bake v3): bits 0..31 zero (this is the",
        "multi-slot shape), bits 32..39 the per-slot Exit mask, bit 40 the",
        "direction-dependent Completed bit, bit 41 the any-active bit (the",
        "branchless Count: Count += that bit — no `if (n != 0)` guard),",
        "bits 44..47 the active slot count (inspection only; the loop reads",
        "bit 41). Each s_pairFk/s_pairBk ulong table row carries the slot's",
        "value float bits in its low half and its entry reference — window",
        "Start forward, window End backward — in its high half, so ONE load",
        "per slot serves value AND movement; INACTIVE slots bake the",
        "direction's always-Enter-false sentinel (uint.MaxValue forward, 0u",
        "backward) as the reference and zero value bits, so each slot's code",
        "= exitBit + (exitBit | enterBit) (enterBit the setcc of the prev",
        "compare; exit set -> 1 + 1 = 2, exit clear -> enterBit: Exit",
        "precedence exact) yields 0 and the Sum add is an exact no-op —",
        "zero data-dependent branches per tick, with the per-slot codes",
        "summed into one Flags accumulation (integer-exact). The",
        "out-of-range clamp target is the empty sentinel word at the",
        "duration.",
        "See docs/benchmarks.md.",
    ],
    """
    // The frozen call contract: one static face per generated timeline so
    // the parity receipts can be generic over fixtures.
    public interface IFrozen
    {
        static abstract Playback Start(uint at = 0);

        static abstract Playback Forward(in Playback from, ref FrozenSink sink, params ReadOnlySpan<uint> ticks);

        static abstract Playback Backward(in Playback from, ref FrozenSink sink, params ReadOnlySpan<uint> ticks);
    }

    // What the frozen code accumulates directly: same fields, same order,
    // same arithmetic as the oracle consumers in Dispatch's Program.cs.
    public struct FrozenSink
    {
        public float Sum;
        public long Flags;
        public int Count;
    }
    """,
    vitalsStarts,
    VitalsTrack.RegionRows.ToArray(),
    VitalsTrack.RegionFlags.ToArray(),
    VitalsTrack.TrackRows.ToArray(),
    VitalsTrack.ClipRows.ToArray(),
    VitalsTrack.ClipEdges.ToArray(),
    VitalsTrack.ClipData.ToArray().Select(clip => clip.Amount).ToArray());

File.WriteAllText(Path.Combine(dispatchDir, "VitalsFrozen.g.cs"), vitalsSource);
Console.WriteLine($"VitalsFrozen: {vitalsStarts.Length} region starts, {vitalsSource.Length} source characters.");

// (b) The synthesized single-track fixture in the Review/Movement shape: one
// track, 16 clips, clip i = [i*4, i*4+3) with value i+1, duration 63; the
// 4th tick of each group is the gap. Tables derive from the same event sweep
// Timeline.Build uses; Dispatch's --verify cross-checks the hand-set oracle
// literals against the raw clip list.
const int fusedClips = 16;
const uint fusedDuration = 63;
var fusedCuts = new SortedSet<uint> { 0 };
for (var i = 0; i < fusedClips; i++)
{
    fusedCuts.Add((uint)(i * 4));
    fusedCuts.Add((uint)(i * 4 + 3));
}
var fusedStarts = fusedCuts.ToArray();
if (fusedStarts[^1] != fusedDuration)
    throw new InvalidOperationException("Fused16 duration must be the last cut.");
var fusedRegionRows = new RegionRow[fusedStarts.Length];
var fusedRegionFlags = new byte[fusedStarts.Length];
var fusedTrackRows = new List<TrackRow>();
var fusedClipRows = new List<ClipRow>();
var fusedEdges = new List<ClipEdge>();

for (var r = 0; r < fusedStarts.Length; r++)
{
    var lo = fusedStarts[r];
    var rowStart = fusedTrackRows.Count;

    for (var i = 0; i < fusedClips; i++)
    {
        if ((uint)(i * 4) > lo || lo >= (uint)(i * 4 + 3))
            continue;
        fusedClipRows.Add(new ClipRow(checked((ushort)i), 0, 0));
        fusedTrackRows.Add(new TrackRow(0, checked((ushort)(fusedClipRows.Count - 1)), 1));
        fusedEdges.Add(new ClipEdge((uint)(i * 4), (uint)(i * 4 + 3)));
    }

    fusedRegionRows[r] = new RegionRow(
        checked((ushort)rowStart), checked((ushort)(fusedTrackRows.Count - rowStart)));

    var re = r + 1 < fusedStarts.Length ? fusedStarts[r + 1] : 0;
    byte flag = 0;
    for (var i = 0; i < fusedClips; i++)
    {
        if ((uint)(i * 4) == lo)
            flag |= 1;
        if ((uint)(i * 4 + 3) == lo)
            flag |= 2;
        if ((uint)(i * 4 + 3) == re)
            flag |= 4;
    }
    fusedRegionFlags[r] = flag;
}

var fusedSource = EmitFrozen(
    "Fused16Frozen",
    [
        "Frozen playback for the Movement-shaped fixture (one track, 16 clips,",
        "clip i = [i*4, i*4+3) with value i+1, duration 63; the 4th tick of",
        "each group is a gap), specialized at emission time against the",
        "redesigned API: lifecycle-only Playback flags (Started minted at",
        "Start, Completed positional per direction) and per-work ClipState",
        "accumulation. Same accumulation contract as VitalsFrozen (see",
        "there); Backward subtracts in the same order. Single-slot shape: the",
        "slot value's float bits ride in the word's low half, so ONE load per",
        "tick covers the value, the Exit bit, the Completed bit, and the",
        "any-active bit; Enter is the setcc of prev against the s_enterF0/",
        "s_enterB0 reference (uint.MaxValue forward / 0u backward baked on",
        "gaps, so gap ticks contribute exactly nothing with no guard branch),",
        "and the code arithmetic exit + (exit | enter) keeps the whole loop",
        "free of data-dependent branches.",
        "Specialized from PlaybackCore for this exact non-looping timeline:",
        "no wraps, no cycle arithmetic, no duration-0/empty handling.",
        "See docs/benchmarks.md.",
    ],
    """
    // FrozenSink and IFrozen live in VitalsFrozen.g.cs; this fixture reuses
    // them unchanged.
    """,
    fusedStarts,
    fusedRegionRows,
    fusedRegionFlags,
    [.. fusedTrackRows],
    [.. fusedClipRows],
    [.. fusedEdges],
    Enumerable.Range(1, fusedClips).Select(i => (float)i).ToArray());

File.WriteAllText(Path.Combine(dispatchDir, "Fused16Frozen.g.cs"), fusedSource);
Console.WriteLine($"Fused16Frozen: {fusedClips} clips, {fusedStarts.Length} region starts, {fusedSource.Length} source characters.");

// ---- Frozen hub (dense <= 256 dispatch over the frozen timelines) ----
// ONE call site dispatching a timeline index into the right generated
// frozen timeline. The registry below defines the index space; a third
// frozen timeline joins the list, takes the next index, and the hub's
// switch stays dense over contiguous 0..n-1 cases - the jump-table shape
// the dispatch verdicts established holds well past 256 dense indices.

// The frozen hub registry: index -> generated frozen type name, in dispatch order.
var frozenRegistry = new[] { "VitalsFrozen", "Fused16Frozen" };

string HubMethod(string name, string signature, string pass)
{
    var cases = new List<string>();

    for (var i = 0; i < frozenRegistry.Length; i++)
        cases.Add(Render($$"""
            case {{i}}:
                return {{frozenRegistry[i]}}.{{pass}};
            """));

    return Render($$"""
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Playback {{name}}({{signature}})
        {
            if (index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Frozen timeline index " + index + "; the hub dispatches 0.." + (Count - 1) + ".");
            switch (index)
            {
            {{ForEach(cases, out var hubCase)}}
                {{hubCase}}
            {{End}}
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Unreachable: the guard above already rejected this index.");
            }
        }
        """);
}

var hubHeader = new List<string>
{
    "<auto-generated>",
    "The frozen hub: ONE call site dispatching a timeline index into the",
    "right generated frozen timeline. Each method bounds-checks first",
    "(index >= Count throws ArgumentOutOfRangeException before any work),",
    "then runs one dense switch over contiguous cases 0..Count-1 - the",
    "jump-table shape the dispatch verdicts established for dense switches",
    "up to 256 indices and beyond. Both the hub methods and the frozen",
    "entry points carry AggressiveInlining so the forwarder folds away and",
    "the per-tick loop stays the only work on the hot path.",
    "Registry order, fixed by the frozen hub registry in Generate/Program.cs:",
    "    index  timeline",
};
hubHeader.AddRange(frozenRegistry.Select((timeline, index) => $"    {index,5}  {timeline}"));
hubHeader.AddRange([
    "A third frozen timeline joins that registry and this switch grows by",
    "one contiguous case. FrozenSink and IFrozen live in VitalsFrozen.g.cs.",
    "See docs/benchmarks.md.",
]);

var hubSource = Render($$"""
    // {{string.Join("\n// ", hubHeader)}}
    namespace Tl.Hooks;

    public static unsafe class FrozenHub
    {
    public const int Count = {{frozenRegistry.Length}};

    {{string.Join("\n\n", new[]
    {
        HubMethod("Start", "ushort index, uint at = 0", "Start(at)"),
        HubMethod("Forward", "ushort index, in Playback from, ref FrozenSink sink, params ReadOnlySpan<uint> ticks", "Forward(in from, ref sink, ticks)"),
        HubMethod("Backward", "ushort index, in Playback from, ref FrozenSink sink, params ReadOnlySpan<uint> ticks", "Backward(in from, ref sink, ticks)"),
    })}}
    }
    """) + "\n";

File.WriteAllText(Path.Combine(dispatchDir, "FrozenHub.g.cs"), hubSource);
Console.WriteLine($"FrozenHub: {frozenRegistry.Length} frozen timelines registered ({string.Join(", ", frozenRegistry.Select((t, i) => $"{t} = {i}"))}), {hubSource.Length} source characters.");
