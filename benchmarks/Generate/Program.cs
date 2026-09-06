using System.Globalization;
using Tl.Algorithms;
using static Waffle.WaffleSyntax;

if (args.Length != 1)
    throw new ArgumentException("Supply the generated-source output directory.");

Directory.CreateDirectory(args[0]);

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

    var path = Path.Combine(args[0], $"{name}.g.cs");
    File.WriteAllText(path, source);
    Console.WriteLine($"{name}: {clips} clips, {fixture.Regions.Length} regions, {source.Length} source characters.");
}
