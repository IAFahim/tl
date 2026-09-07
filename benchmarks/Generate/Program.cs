using System.Globalization;
using Tl.Algorithms;
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
