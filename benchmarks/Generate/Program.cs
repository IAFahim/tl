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
// can read. Two lookup strategies, picked by duration: the full-bake dense
// LUT mode (<= 1024 ticks) bakes the WHOLE per-tick leaf - positional flags
// as one byte per tick per direction, every blend-resolved track value as a
// baked float per active-track slot, both computed with the identical
// expressions the runtime evaluates - so the hot loop is table loads, the
// movement-fact rank lookups, and sink accumulation, plus one Playback
// construction after the loop. Longer timelines keep binary branch trees
// over the known starts (the Tree pattern from the Algorithms fixtures),
// rank-compare movement facts, and immediate-payload leaves. The frozen form
// is bound to one exact non-looping timeline either way.

string Lit(float value) => value.ToString("R", CultureInfo.InvariantCulture) + "f";

string U(uint value) => value.ToString(CultureInfo.InvariantCulture) + "u";

string EmitFrozen(
    string name, string[] notes, string tail,
    uint[] starts, RegionRow[] regionRows, byte[] regionFlags,
    TrackRow[] trackRows, ClipRow[] clipRows, float[] amounts)
{
    var duration = starts[^1];

    // One leaf per region (branch-tree mode, durations past the dense
    // threshold): positional flags from the region's constants, sampling
    // as immediate adds, Complete folded to where it can still fire.
    string Leaf(int r, bool backward)
    {
        var rs = starts[r];
        var re = r + 1 < starts.Length ? starts[r + 1] : duration;
        var cut = regionFlags[r];
        var row = regionRows[r];
        var lines = new List<string>
        {
            row.TrackCount > 0 ? "flags = PlaybackFlags.Active;" : "flags = PlaybackFlags.None;",
        };

        if ((cut & 1) != 0)
            lines.Add($"if (tick == {U(rs)}) flags |= PlaybackFlags.First;");
        if ((cut & 4) != 0)
            lines.Add($"if (tick == {U(re - 1)}) flags |= PlaybackFlags.Last;");

        // Non-looping completion: forward fires from duration-1 on, backward
        // only exactly at tick 0.
        if (backward)
        {
            if (rs == 0)
                lines.Add("if (tick == 0u) flags |= PlaybackFlags.Complete;");
        }
        else if (re >= duration)
        {
            if (rs >= duration - 1)
                lines.Add("flags |= PlaybackFlags.Complete;");
            else
                lines.Add($"if (tick >= {U(duration - 1)}) flags |= PlaybackFlags.Complete;");
        }

        for (var t = 0; t < row.TrackCount; t++)
        {
            var track = trackRows[row.TrackStart + t];
            var op = backward ? "-=" : "+=";
            if (track.ClipCount == 1)
            {
                lines.Add($"sink.Sum {op} {Lit(amounts[clipRows[track.ClipStart].ClipIndex])};");
                continue;
            }

            // Blend-resolved track: the factor expression is byte-for-byte
            // the one Tracks.MoveNext evaluates, with constant windows, so
            // the float arithmetic matches the table path exactly.
            var first = clipRows[track.ClipStart];
            var second = clipRows[track.ClipStart + 1];
            var factor = $"factor{t}";
            lines.Add(first.FactorLength <= 1
                ? $"var {factor} = 0.5f;"
                : $"var {factor} = (tick - {U(first.FactorStart)}) / {Lit(first.FactorLength - 1)};");
            lines.Add($"sink.Sum {op} {Lit(amounts[first.ClipIndex])} * (1f - {factor}) + {Lit(amounts[second.ClipIndex])} * {factor};");
        }

        return string.Join("\n", lines);
    }

    string Tree(int lo, int hi, bool backward)
    {
        if (hi - lo == 1)
            return Leaf(lo, backward);

        var mid = (lo + hi) / 2;
        var left = Tree(lo, mid, backward);
        var right = Tree(mid, hi, backward);
        return Render($$"""
            if (tick < {{U(starts[mid])}})
            {
                {{left}}
            }
            else
            {
                {{right}}
            }
            """);
    }

    // Movement facts without the boundary walk: Enter/Exit fire when a
    // clip-start or clip-end cut boundary (exactly the region boundaries
    // carrying that cut bit) falls inside the crossed span, which is a rank
    // comparison over the known constants.
    var startCuts = Enumerable.Range(0, starts.Length).Where(i => (regionFlags[i] & 1) != 0).Select(i => starts[i]).ToArray();
    var endCuts = Enumerable.Range(0, starts.Length).Where(i => (regionFlags[i] & 2) != 0).Select(i => starts[i]).ToArray();

    // Full-bake dense LUT mode for short timelines (duration <= 1024): the
    // whole per-tick leaf is precomputed. Positional flags become one byte
    // per tick per direction, every blend-resolved track value a baked
    // float per active-track slot, and the hot loop is left with guarded
    // table loads, the movement-fact rank lookups, and sink accumulation.
    // Longer timelines keep the branch trees this emitter also produces;
    // both fixtures today are far below the threshold.
    const int denseLimit = 1024;
    var dense = duration <= denseLimit;
    int[] startRankTable = [], endRankTable = [];

    // Positional flag packing for the byte tables: bit 0 = First,
    // bit 1 = Active, bit 2 = Last, bit 3 = Complete, so
    // (PlaybackFlags)((uint)entry << 27) reproduces the PlaybackFlags word
    // (First = 1<<27 .. Complete = 1<<30). The movement bits Enter/Exit sit
    // outside that contiguous range on purpose and stay rank logic.
    const byte FirstBit = 1, ActiveBit = 2, LastBit = 4, CompleteBit = 8;

    // One slot table per active-track position: slot k holds the k-th
    // active track's blend-resolved value in track-row order (the exact
    // order the leaf adds them in), 0f wherever fewer tracks are active -
    // an exact no-op in the addition sequence the oracle performs.
    var slotCount = 0;
    foreach (var row in regionRows)
        if (row.TrackCount > slotCount)
            slotCount = row.TrackCount;

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

    byte[] baseFlagsForward = [], baseFlagsBackward = [];
    float[][] slotTables = [];

    if (dense)
    {
        // The clamped out-of-range table entry must reproduce the empty
        // sentinel region exactly, both at tick == duration and beyond:
        // no tracks, and no clip starting at the duration (a StartCut
        // there would make tick == duration carry First while larger
        // out-of-range ticks would not - one clamp target cannot express
        // both, so that shape falls back to asserting here).
        if (regionRows[^1].TrackCount != 0 || (regionFlags[^1] & 1) != 0)
            throw new InvalidOperationException($"{name}: dense full-bake requires an empty sentinel region (no tracks, no clip starting at the duration).");

        startRankTable = new int[(int)duration];
        endRankTable = new int[(int)duration];
        baseFlagsForward = new byte[(int)duration + 1];
        baseFlagsBackward = new byte[(int)duration + 1];
        slotTables = new float[slotCount][];
        for (var k = 0; k < slotCount; k++)
            slotTables[k] = new float[(int)duration + 1];

        var startRank = 0;
        var endRank = 0;

        for (var t = 0; t <= duration; t++)
        {
            var region = 0;
            while (region + 1 < starts.Length && starts[region + 1] <= (uint)t)
                region++;
            while (startRank < startCuts.Length && startCuts[startRank] <= (uint)t)
                startRank++;
            while (endRank < endCuts.Length && endCuts[endRank] <= (uint)t)
                endRank++;

            if (t < duration)
            {
                startRankTable[t] = startRank;
                endRankTable[t] = endRank;
            }

            // The positional word the leaf assembles, evaluated per tick:
            // Active for any track, First/Last on the region's cut
            // boundaries, Complete folded per direction (forward from
            // duration-1 on and past the duration, backward exactly at 0).
            var row = regionRows[region];
            var rs = starts[region];
            var re = region + 1 < starts.Length ? starts[region + 1] : duration;
            var cut = regionFlags[region];
            byte positional = 0;

            if (row.TrackCount > 0)
                positional |= ActiveBit;
            if (t == rs && (cut & 1) != 0)
                positional |= FirstBit;
            if (t == re - 1 && (cut & 4) != 0)
                positional |= LastBit;

            baseFlagsForward[t] = (byte)(positional | (t >= duration - 1u ? CompleteBit : 0));
            baseFlagsBackward[t] = (byte)(positional | (t == 0u ? CompleteBit : 0));

            var slot = 0;
            for (var k = 0; k < row.TrackCount; k++)
            {
                if (slot >= slotCount)
                    throw new InvalidOperationException($"{name}: tick {t} has more active tracks than the {slotCount} slot tables cover.");

                slotTables[slot++][t] = SlotValue((uint)t, trackRows[row.TrackStart + k]);
            }
        }
    }

    // The dense tables: one entry per tick below the duration, values the
    // tree lookups would compute (cut counts at or below the tick). byte
    // covers the cut ranks these sizes produce; a table with a value above
    // 255 widens to ushort instead.
    string Table(string name, int[] values)
    {
        var max = 0;
        foreach (var value in values)
            if (value > max)
                max = value;

        var bytes = max <= byte.MaxValue;
        var elementType = bytes ? "byte" : "ushort";
        string Lit(int value) => bytes ? Hex2(value) : Hex((ushort)value);

        var rows = new List<string>();
        for (var i = 0; i < values.Length; i += 16)
        {
            var count = Math.Min(16, values.Length - i);
            var row = string.Join(", ", Enumerable.Range(i, count).Select(j => Lit(values[j])));
            rows.Add(i + count < values.Length ? row + "," : row);
        }

        return Render($$"""
            private static readonly {{elementType}}[] {{name}} =
            [
            {{ForEach(rows, out var tableRow)}}
                {{tableRow}}
            {{End}}
            ];
            """);
    }

    // The baked flag bytes: one entry per tick plus the out-of-range clamp
    // target at the duration (the empty sentinel), packing described by
    // the FirstBit/ActiveBit/LastBit/CompleteBit constants above.
    string ByteTable(string name, byte[] values)
    {
        var rows = new List<string>();
        for (var i = 0; i < values.Length; i += 16)
        {
            var count = Math.Min(16, values.Length - i);
            var row = string.Join(", ", Enumerable.Range(i, count).Select(j => Hex2(values[j])));
            rows.Add(i + count < values.Length ? row + "," : row);
        }

        return Render($$"""
            private static readonly byte[] {{name}} =
            [
            {{ForEach(rows, out var byteRow)}}
                {{byteRow}}
            {{End}}
            ];
            """);
    }

    // The baked slot values: "R"-round-tripped float literals, one table
    // per active-track slot, 0f wherever that slot is inactive at the tick.
    string FloatTable(string name, float[] values)
    {
        var rows = new List<string>();
        for (var i = 0; i < values.Length; i += 8)
        {
            var count = Math.Min(8, values.Length - i);
            var row = string.Join(", ", Enumerable.Range(i, count).Select(j => Lit(values[j])));
            rows.Add(i + count < values.Length ? row + "," : row);
        }

        return Render($$"""
            private static readonly float[] {{name}} =
            [
            {{ForEach(rows, out var floatRow)}}
                {{floatRow}}
            {{End}}
            ];
            """);
    }

    string Rank(string method, uint[] cuts)
    {
        string Body(int lo, int hi)
        {
            if (lo == hi)
                return $"return {lo};";
            if (hi - lo == 1)
                return cuts[lo] == 0 ? $"return {lo + 1};" : $"return tick < {U(cuts[lo])} ? {lo} : {lo + 1};";

            var mid = (lo + hi) / 2;
            var left = Body(lo, mid);
            var right = Body(mid + 1, hi);
            return Render($$"""
                if (tick < {{U(cuts[mid])}})
                {
                    {{left}}
                }
                else
                {
                    {{right}}
                }
                """);
        }

        if (cuts.Length == 0)
            return Render($$"""
                private static int {{method}}(uint tick) => 0;
                """);

        return Render($$"""
            private static int {{method}}(uint tick)
            {
                {{Body(0, cuts.Length)}}
            }
            """);
    }

    // The movement-fact block: same comparisons and direction rules in
    // both modes, reading the previous tick from the hoisted `prev` local.
    // Dense mode swaps the rank-tree calls for guarded table loads (the
    // fallback past the duration is the total cut count, the rank a tick at
    // or beyond the duration yields); the tree mode keeps the
    // StartRank/EndRank helper calls.
    string Movement(bool backward)
    {
        string RankLoad(string table, int top, string tickExpr)
            => $"{tickExpr} < {U(duration)} ? {table}[{tickExpr}] : {top}";

        if (!dense)
        {
            return backward ? Render($$"""
                if (tick < prev)
                {
                    if (EndRank(prev) > EndRank(tick))
                        flags |= PlaybackFlags.Enter;
                    if (StartRank(prev) > StartRank(tick))
                        flags |= PlaybackFlags.Exit;
                }
                """) : Render($$"""
                if (tick > prev)
                {
                    if (StartRank(tick) > StartRank(prev))
                        flags |= PlaybackFlags.Enter;
                    if (EndRank(tick) > EndRank(prev))
                        flags |= PlaybackFlags.Exit;
                }
                """);
        }

        return backward ? Render($$"""
            if (tick < prev)
            {
                var endRank = {{RankLoad("s_endRank", endCuts.Length, "prev")}};
                var prevEndRank = {{RankLoad("s_endRank", endCuts.Length, "tick")}};
                var startRank = {{RankLoad("s_startRank", startCuts.Length, "prev")}};
                var prevStartRank = {{RankLoad("s_startRank", startCuts.Length, "tick")}};
                if (endRank > prevEndRank)
                    flags |= PlaybackFlags.Enter;
                if (startRank > prevStartRank)
                    flags |= PlaybackFlags.Exit;
            }
            """) : Render($$"""
            if (tick > prev)
            {
                var startRank = {{RankLoad("s_startRank", startCuts.Length, "tick")}};
                var prevStartRank = {{RankLoad("s_startRank", startCuts.Length, "prev")}};
                var endRank = {{RankLoad("s_endRank", endCuts.Length, "tick")}};
                var prevEndRank = {{RankLoad("s_endRank", endCuts.Length, "prev")}};
                if (startRank > prevStartRank)
                    flags |= PlaybackFlags.Enter;
                if (endRank > prevEndRank)
                    flags |= PlaybackFlags.Exit;
            }
            """);
    }

    // AggressiveInlining on the frozen entry points: the full-bake bodies
    // are small enough to fold into callers, which is what keeps the hub's
    // forwarder layer (and any consumer loop) on the tight path instead of
    // paying a call per batch.
    const string Inline = "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";

    var startMethod = Render($$"""
        {{Inline}}
        public static Playback Start(uint at = 0) => Playback.Start(at);
        """);

    // One Forward/Backward shape for both modes. The loop carries only the
    // previous tick, the pass-through cycles, and the running flags as
    // locals: the sink never saw the intermediate Playback, so the single
    // construction after the loop (from the last tick) is the only one
    // needed. Frozen timelines never loop - no step adds cycles - so the
    // Playback constructor's cycle-capacity throw is unreachable here: the
    // construction re-passes from.Cycles (within Playback.MaxCycles by the
    // type's invariant) and a flags word that only ever holds
    // PlaybackFlags bits.
    string Loop(string method, bool backward)
    {
        var op = backward ? "-" : "+";
        var countOp = backward ? "--" : "++";

        var perTick = dense
            ? Render($$"""
                var i = tick < {{U(duration)}} ? tick : {{U(duration)}};
                flags = (PlaybackFlags)((uint){{(backward ? "s_baseFlagsB" : "s_baseFlagsF")}}[i] << 27);
                {{Movement(backward)}}
                sink.Flags {{op}}= (uint)flags;
                {{string.Join("\n", Enumerable.Range(0, slotCount).Select(k => $"sink.Sum {op}= s_slot{k}[i];"))}}
                if ((flags & PlaybackFlags.Active) != 0)
                    sink.Count{{countOp}};
                prev = tick;
                """)
            : Render($$"""
                {{Tree(0, starts.Length, backward)}}
                {{Movement(backward)}}
                sink.Flags {{op}}= (uint)flags;
                if ((flags & PlaybackFlags.Active) != 0)
                    sink.Count{{countOp}};
                prev = tick;
                """);

        return Render($$"""
            {{Inline}}
            public static Playback {{method}}(in Playback from, ref FrozenSink sink, params ReadOnlySpan<uint> ticks)
            {
                var cycles = from.Cycles;
                var prev = from.Tick;
                var flags = from.Flags;
                foreach (var tick in ticks)
                {
                    {{perTick}}
                }
                return new Playback(prev, cycles, flags);
            }
            """);
    }

    var forward = Loop("Forward", false);
    var backward = Loop("Backward", true);

    var members = new List<string> { startMethod, forward, backward };

    if (dense)
    {
        members.Add(ByteTable("s_baseFlagsF", baseFlagsForward));
        members.Add(ByteTable("s_baseFlagsB", baseFlagsBackward));
        for (var k = 0; k < slotCount; k++)
            members.Add(FloatTable($"s_slot{k}", slotTables[k]));
        members.Add(Table("s_startRank", startRankTable));
        members.Add(Table("s_endRank", endRankTable));
    }
    else
    {
        members.Add(Rank("StartRank", startCuts));
        members.Add(Rank("EndRank", endCuts));
    }

    // The lookup strategy belongs to the emitter (it picks the mode), so
    // its header lines are spliced in here just before the trailing "See
    // docs" note rather than written into the per-fixture notes.
    var slotSpan = slotCount == 1 ? "s_slot0" : $"s_slot0..s_slot{slotCount - 1}";

    string[] strategy = dense
        ? [
            $"Lookup strategy: full-bake dense LUT mode (this timeline's duration {duration} is",
            $"within the <= {denseLimit} threshold). The whole per-tick leaf is baked: positional",
            "flags (Active/First/Last/Complete) are one byte per tick per direction",
            "(s_baseFlagsF/s_baseFlagsB, shifted << 27 into the PlaybackFlags word) and",
            $"every blend-resolved track value is a baked float ({slotSpan}, one table per",
            "active-track slot in track-row order, 0f where the slot is inactive) - both",
            "computed with the identical expressions the table path evaluates. The hot loop",
            "is guarded table loads, the movement-fact rank lookups (s_startRank/s_endRank,",
            "load-vs-load diffs whose past-the-duration fallback is the total cut count), and",
            "sink accumulation: no per-tick blend math, no flag-assembly branches, and the",
            "only Playback construction is the one after the loop. The out-of-range clamp",
            "target is the tables' sentinel entry at the duration (the empty region,",
            "asserted at emission). Timelines longer than the threshold emit binary branch",
            "trees over the region starts and cut boundaries with per-region leaves instead.",
        ]
        : [
            $"Lookup strategy: binary branch trees (this timeline's duration {duration} exceeds",
            $"the {denseLimit} threshold of the full-bake dense LUT mode). Region lookup is a",
            "branch tree over the region starts with per-region leaf bodies (immediate",
            "payloads, constant-compare flags), and movement facts are rank trees over the",
            $"cut boundaries; durations <= {denseLimit} get fully baked per-tick flag and slot",
            "tables instead.",
        ];
    var header = string.Join("\n", notes[..^1].Concat(strategy).Append(notes[^1]).Select(note => "// " + note));
    var body = string.Join("\n\n", members);

    return Render($$"""
        // <auto-generated>
        {{header}}
        namespace Tl.Hooks;

        public unsafe struct {{name}} : IFrozen
        {
        {{body}}
        }
        {{tail}}
        """) + "\n";
}

// (a) The real fixture: tables read straight from Hooks.cs - a single source
// of truth shared with the oracle the parity receipts run through.
var vitalsStarts = VitalsTrack.RegionStarts.ToArray();
var vitalsSource = EmitFrozen(
    "VitalsFrozen",
    [
        "Frozen playback for the VitalsTrack fixture (duration 600, 13 region",
        "starts = 12 regions plus the empty sentinel at 600, 4 tracks, blends",
        "and gaps). Region starts, cut bits, clip windows, and payloads are",
        "compile-time constants here: movement facts compare ranks over the",
        "known clip-start/clip-end cut boundaries, and the per-tick slot",
        "values are baked floats - folded at emission time with the same",
        "blend-factor arithmetic the table path evaluates at run time",
        "((tick - factorStart) / (float)(factorLength - 1), 0.5f for a",
        "one-tick window, then first * (1f - f) + second * f, all in float),",
        "so the parity receipts hold bit-for-bit.",
        "Specialized from PlaybackCore for this exact timeline: non-looping,",
        "so no wraps, no cycle arithmetic (Cycles passes through unchanged),",
        "effective positions are the raw ticks, and duration-0/empty-timeline",
        "handling does not apply.",
        "Accumulation contract, mirrored bit-for-bit by the oracle in",
        "Dispatch's Program.cs: per tick, in order - Sum += one blend-resolved",
        "clip value per active track in track-row order, Flags += (uint)status,",
        "Count++ for ticks with at least one active track; Backward subtracts",
        "in the same order (the exact inverse).",
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
        "each group is a gap); every clip's value is one baked slot float per",
        "tick and movement facts compare cut-boundary ranks.",
        "Specialized from PlaybackCore for this exact timeline: non-looping,",
        "no wraps, no cycle arithmetic, no duration-0/empty handling.",
        "Accumulation contract: identical to VitalsFrozen (see there);",
        "Backward subtracts in the same order.",
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
