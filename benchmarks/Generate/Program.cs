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
// can read: region lookup becomes a binary branch tree over the known starts
// (the Tree pattern from the Algorithms fixtures), movement facts become rank
// comparisons over the known cut-bit boundaries, and clip payloads become
// immediates. The frozen form is bound to one exact non-looping timeline.

string Lit(float value) => value.ToString("R", CultureInfo.InvariantCulture) + "f";

string U(uint value) => value.ToString(CultureInfo.InvariantCulture) + "u";

string EmitFrozen(
    string name, string[] notes, string tail,
    uint[] starts, RegionRow[] regionRows, byte[] regionFlags,
    TrackRow[] trackRows, ClipRow[] clipRows, float[] amounts)
{
    var duration = starts[^1];

    // One leaf per region: positional flags from the region's constants,
    // sampling as immediate adds, Complete folded to where it can still fire.
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

    // Dense LUT mode for short timelines: one guarded table load replaces
    // the region tree, two more replace the rank trees, and the per-region
    // leaves stay byte-identical behind a jump-table switch over region
    // indices. Longer timelines keep the branch trees this emitter also
    // produces; both fixtures today are far below the threshold.
    const int denseLimit = 1024;
    var dense = duration <= denseLimit;
    int[] regionTable = [], startRankTable = [], endRankTable = [];

    if (dense)
    {
        regionTable = new int[(int)duration];
        startRankTable = new int[(int)duration];
        endRankTable = new int[(int)duration];
        var region = 0;
        var startRank = 0;
        var endRank = 0;

        for (var t = 0; t < duration; t++)
        {
            while (region + 1 < starts.Length && starts[region + 1] <= (uint)t)
                region++;
            while (startRank < startCuts.Length && startCuts[startRank] <= (uint)t)
                startRank++;
            while (endRank < endCuts.Length && endCuts[endRank] <= (uint)t)
                endRank++;
            regionTable[t] = region;
            startRankTable[t] = startRank;
            endRankTable[t] = endRank;
        }
    }

    // Region lookup in dense mode: s_region[tick] is the region containing
    // the tick (the last region start <= tick, exactly what the tree
    // computes); ticks at or beyond the duration take the sentinel region
    // the tree lands them in, so the receipts' out-of-range jumps keep
    // their parity.
    string DenseSwitch(bool backward)
    {
        var sentinel = starts.Length - 1;
        var regionCases = new List<string>();

        for (var r = 0; r < starts.Length; r++)
            regionCases.Add(Render($$"""
                case {{r}}:
                {
                    {{Leaf(r, backward)}}
                    break;
                }
                """));

        return Render($$"""
            var region = tick < {{U(duration)}} ? s_region[tick] : {{sentinel}};
            switch (region)
            {
            {{ForEach(regionCases, out var regionCase)}}
                {{regionCase}}
            {{End}}
                default:
                    goto case {{sentinel}};
            }
            """);
    }

    // The dense tables: one entry per tick below the duration, values the
    // tree lookups would compute (region index, cut counts at or below the
    // tick). byte covers the region indices and cut ranks these sizes
    // produce; a table with a value above 255 widens to ushort instead.
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
    // both modes. Dense mode swaps the rank-tree calls for guarded table
    // loads (the fallback past the duration is the total cut count, the
    // rank a tick at or beyond the duration yields); the tree mode keeps
    // the StartRank/EndRank helper calls.
    string Movement(bool backward)
    {
        string RankLoad(string table, int top, string tickExpr)
            => $"{tickExpr} < {U(duration)} ? {table}[{tickExpr}] : {top}";

        if (!dense)
        {
            return backward ? Render($$"""
                if (tick < state.Tick)
                {
                    if (EndRank(state.Tick) > EndRank(tick))
                        flags |= PlaybackFlags.Enter;
                    if (StartRank(state.Tick) > StartRank(tick))
                        flags |= PlaybackFlags.Exit;
                }
                """) : Render($$"""
                if (tick > state.Tick)
                {
                    if (StartRank(tick) > StartRank(state.Tick))
                        flags |= PlaybackFlags.Enter;
                    if (EndRank(tick) > EndRank(state.Tick))
                        flags |= PlaybackFlags.Exit;
                }
                """);
        }

        return backward ? Render($$"""
            if (tick < state.Tick)
            {
                var endRank = {{RankLoad("s_endRank", endCuts.Length, "state.Tick")}};
                var prevEndRank = {{RankLoad("s_endRank", endCuts.Length, "tick")}};
                var startRank = {{RankLoad("s_startRank", startCuts.Length, "state.Tick")}};
                var prevStartRank = {{RankLoad("s_startRank", startCuts.Length, "tick")}};
                if (endRank > prevEndRank)
                    flags |= PlaybackFlags.Enter;
                if (startRank > prevStartRank)
                    flags |= PlaybackFlags.Exit;
            }
            """) : Render($$"""
            if (tick > state.Tick)
            {
                var startRank = {{RankLoad("s_startRank", startCuts.Length, "tick")}};
                var prevStartRank = {{RankLoad("s_startRank", startCuts.Length, "state.Tick")}};
                var endRank = {{RankLoad("s_endRank", endCuts.Length, "tick")}};
                var prevEndRank = {{RankLoad("s_endRank", endCuts.Length, "state.Tick")}};
                if (startRank > prevStartRank)
                    flags |= PlaybackFlags.Enter;
                if (endRank > prevEndRank)
                    flags |= PlaybackFlags.Exit;
            }
            """);
    }

    var startMethod = Render($$"""
        public static Playback Start(uint at = 0) => Playback.Start(at);
        """);

    var forwardRegion = dense ? DenseSwitch(false) : Tree(0, starts.Length, false);
    var backwardRegion = dense ? DenseSwitch(true) : Tree(0, starts.Length, true);

    var forward = Render($$"""
        public static Playback Forward(in Playback from, ref FrozenSink sink, params ReadOnlySpan<uint> ticks)
        {
            var state = from;
            foreach (var tick in ticks)
            {
                PlaybackFlags flags;
                {{forwardRegion}}
                {{Movement(false)}}
                sink.Flags += (uint)flags;
                if ((flags & PlaybackFlags.Active) != 0)
                    sink.Count++;
                state = new Playback(tick, state.Cycles, flags);
            }
            return state;
        }
        """);

    var backward = Render($$"""
        public static Playback Backward(in Playback from, ref FrozenSink sink, params ReadOnlySpan<uint> ticks)
        {
            var state = from;
            foreach (var tick in ticks)
            {
                PlaybackFlags flags;
                {{backwardRegion}}
                {{Movement(true)}}
                sink.Flags -= (uint)flags;
                if ((flags & PlaybackFlags.Active) != 0)
                    sink.Count--;
                state = new Playback(tick, state.Cycles, flags);
            }
            return state;
        }
        """);

    var members = new List<string> { startMethod, forward, backward };

    if (dense)
    {
        members.Add(Table("s_region", regionTable));
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
    string[] strategy = dense
        ? [
            $"Lookup strategy: dense LUT mode (this timeline's duration {duration} is within",
            $"the <= {denseLimit} threshold). Region lookup is one guarded table load plus a",
            "jump-table switch over region indices - the leaf bodies are identical to the",
            "tree mode's - and the movement-fact ranks are guarded loads whose",
            "past-the-duration fallback is the total cut count. Timelines longer than",
            $"{denseLimit} ticks emit binary branch trees over the region starts and cut",
            "boundaries instead.",
        ]
        : [
            $"Lookup strategy: binary branch trees (this timeline's duration {duration} exceeds",
            $"the {denseLimit} threshold of the dense LUT mode). Region lookup is a branch tree",
            "over the region starts and movement facts are rank trees over the cut",
            $"boundaries; durations <= {denseLimit} get dense region and rank tables with a",
            "jump-table switch over region indices instead.",
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
        "known clip-start/clip-end cut boundaries, and sampling adds immediates",
        "with the same blend-factor arithmetic as the table path",
        "((tick - factorStart) / (float)(factorLength - 1)).",
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
        "each group is a gap); every clip region folds to one immediate add",
        "and movement facts compare cut-boundary ranks.",
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
