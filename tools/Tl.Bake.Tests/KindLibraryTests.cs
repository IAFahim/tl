using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace KindFixture
{
public struct Motion { public float X, Y; }
public struct Health { public int Value; }
public struct Stamina { public int Value; }
public struct Speed { public float Value; public byte Priority; }

[TimelineKind]
public struct Locomotion
{
    public static void Combine(ref Speed total, in Speed next) { if (next.Priority >= total.Priority) total = next; }
    public static void Apply(in Speed total, ref Motion motion) => motion.X += total.Value;
}

[TimelineKind]
public struct Lift
{
    public static void Combine(ref float total, in float next) => total += next;
    public static void Apply(in float total, ref Motion motion) => motion.Y += total;
}

[TimelineKind(Accepts = KindEntries.Pulses, FoldsPulses = true)]
public struct Damage
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref Health health) => health.Value -= total;
}

[TimelineKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
public struct StatChange
{
    public static void Apply(in int delta, ref Stamina stamina) => stamina.Value += delta;
}

[TimelineKind(Group = "Effects")]
public struct Burn
{
    public static void Combine(ref float total, in float next) => total += next;
    public static void Apply(in float total, ref Motion motion) => motion.X += total;
}

[TimelineKind(Accepts = KindEntries.Pulses, Group = "Effects")]
public struct Chill
{
    public static void Combine(ref int total, in int next) => total += next;
    public static void Apply(in int total, ref Stamina stamina) => stamina.Value -= total;
}

public readonly struct WalkClip(float speed, byte priority) { public readonly float Speed = speed; public readonly byte Priority = priority; }
public readonly struct WalkTrack(float scale) : IBlend<WalkClip>
{
    public readonly float Scale = scale;
    public void Blend(in WalkClip first, in WalkClip second, float factor, out WalkClip result) => result = first;
}
public readonly struct ArcClip(float velocity) { public readonly float Velocity = velocity; }
public readonly struct ArcTrack(float scale) : IBlend<ArcClip>
{
    public readonly float Scale = scale;
    public void Blend(in ArcClip first, in ArcClip second, float factor, out ArcClip result) => result = first;
}
public readonly struct HitClip(int amount) { public readonly int Amount = amount; }
public readonly struct HitTrack(float scale) : IBlend<HitClip>
{
    public readonly float Scale = scale;
    public void Blend(in HitClip first, in HitClip second, float factor, out HitClip result) => result = first;
}
public readonly struct FlameClip(float power) { public readonly float Power = power; }
public readonly struct FlameTrack(float scale) : IBlend<FlameClip>
{
    public readonly float Scale = scale;
    public void Blend(in FlameClip first, in FlameClip second, float factor, out FlameClip result) => result = first;
}
public readonly struct ChillClip(int amount) { public readonly int Amount = amount; }
public readonly struct ChillTrack(float scale) : IBlend<ChillClip>
{
    public readonly float Scale = scale;
    public void Blend(in ChillClip first, in ChillClip second, float factor, out ChillClip result) => result = first;
}
public readonly struct StepMarker(int delta) { public readonly int Delta = delta; }

public struct Walk : ITrack<WalkTrack, WalkClip>
{
    [Feeds(typeof(Locomotion))]
    public static Speed Evaluate(in Frame<WalkTrack, WalkClip> f) => new() { Value = f.Direction * f.Clip.Speed, Priority = f.Clip.Priority };
}

public struct Jump : ITrack<ArcTrack, ArcClip>
{
    [Feeds(typeof(Lift))]
    public static float Evaluate(in Frame<ArcTrack, ArcClip> f) => f.Direction * f.Clip.Velocity * f.Track.Scale;
}

public struct Hit : ITrack<HitTrack, HitClip>
{
    [Pulse(typeof(Damage), On = ClipEvent.Start)]
    public static int Start(in Frame<HitTrack, HitClip> f) => f.Direction > 0 ? f.Clip.Amount : 0;

    [Pulse(typeof(StatChange), On = ClipEvent.End)]
    public static int End(in Frame<HitTrack, HitClip> f) => -1;
}

public struct Flame : ITrack<FlameTrack, FlameClip>
{
    [Feeds(typeof(Burn))]
    public static float Evaluate(in Frame<FlameTrack, FlameClip> f) => f.Direction * f.Clip.Power * f.Track.Scale;
}

public struct Frost : ITrack<ChillTrack, ChillClip>
{
    [Pulse(typeof(Chill), On = ClipEvent.Start)]
    public static int Start(in Frame<ChillTrack, ChillClip> f) => f.Direction > 0 ? f.Clip.Amount : 0;

    [Pulse(typeof(Chill), On = ClipEvent.End)]
    public static int End(in Frame<ChillTrack, ChillClip> f) => 1;
}

public struct Steps : IMarker<StepMarker>
{
    [Pulse(typeof(StatChange), On = ClipEvent.Marker)]
    public static int OnStep(in MarkerFrame<StepMarker> m) => m.Marker.Delta;
}
}

namespace Tl.Bake.Tests
{
public class KindLibraryTests
{
    static readonly string[] Files = ["library.json", "run.json", "jump.json", "potion_hop.json", "stomp.json"];

    static string Golden(params string[] parts)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine([dir, "golden", .. parts]);
    }

    static string Stage(Action<string>? edit = null)
    {
        var temp = Path.Combine(Path.GetTempPath(), "tl-kinds-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        foreach (var name in Files) File.Copy(Golden("kinds", name), Path.Combine(temp, name));
        edit?.Invoke(temp);
        return temp;
    }

    static int Pack(string dir, params string[] extra)
        => LibraryPack.Run([dir, "--assembly", typeof(KindLibraryTests).Assembly.Location, "--namespace", "KindFixture", .. extra]);

    static Dictionary<string, int> Capacities(string kindsFile)
    {
        var result = new Dictionary<string, int>();
        foreach (var line in File.ReadAllLines(kindsFile))
        {
            var t = line.Trim();
            if (!t.StartsWith("public const int ", StringComparison.Ordinal)) continue;
            var parts = t["public const int ".Length..].TrimEnd(';').Split(" = ");
            result[parts[0]] = int.Parse(parts[1]);
        }
        return result;
    }

    [Fact]
    public void CatalogFindsKindsFeedersAndPulses()
    {
        var catalog = KindCatalog.Discover([typeof(KindLibraryTests).Assembly]);
        Assert.Equal(["Burn", "Chill", "Damage", "Lift", "Locomotion", "StatChange"], catalog.Kinds.Select(k => k.Name));
        Assert.Equal(["Effects", "Effects"], catalog.Kinds.Take(2).Select(k => k.Group));
        var damage = catalog.Kinds[catalog.IndexOf("Damage")];
        Assert.Equal(12, damage.EntryBytes);
        Assert.Equal(8, catalog.Kinds[catalog.IndexOf("Lift")].EntryBytes);
        var feeders = catalog.Feeders.Select(f => $"{catalog.Kinds[f.Kind].Name}:{f.Trigger}:{f.Consumer}.{f.Method}:{f.Ordinal}").ToArray();
        Assert.Equal([
            "Burn:Span:KindFixture.Flame.Evaluate:0",
            "Chill:End:KindFixture.Frost.End:0",
            "Chill:Start:KindFixture.Frost.Start:1",
            "Damage:Start:KindFixture.Hit.Start:0",
            "Lift:Span:KindFixture.Jump.Evaluate:0",
            "Locomotion:Span:KindFixture.Walk.Evaluate:0",
            "StatChange:End:KindFixture.Hit.End:0",
            "StatChange:Marker:KindFixture.Steps.OnStep:1",
        ], feeders);
        Assert.Equal(KindCatalog.PairKey(typeof(KindFixture.WalkTrack), typeof(KindFixture.WalkClip)), catalog.Feeders.Single(f => f.Method == "Evaluate" && f.Consumer.EndsWith("Walk")).SourceKey);
    }

    [Fact]
    public void CapacitiesMatchHandComputedFixture()
    {
        var dir = Stage();
        try
        {
            Assert.Equal(0, Pack(dir));
            var caps = Capacities(Path.Combine(dir, "Kinds.g.cs"));
            Assert.Equal(1, caps["Locomotion"]);
            Assert.Equal(2, caps["Lift"]);
            Assert.Equal(5, caps["Damage"]);
            Assert.Equal(3, caps["StatChange"]);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void StackDepthChannelsCapsAndOverridesApply()
    {
        var dir = Stage(d => File.WriteAllText(Path.Combine(d, "library.json"), """
            {
              "tickRate": 60,
              "channels": ["locomotion", "action"],
              "timelines": ["run.json", "jump.json", "potion_hop.json", "stomp.json"],
              "kinds": {
                "default": { "maxInlineBytes": 16, "stackDepth": 3 },
                "Damage": { "gamePulses": 4, "maxInlineBytes": 120 },
                "Locomotion": { "inline": 0 }
              }
            }
            """));
        try
        {
            Assert.Equal(0, Pack(dir));
            var caps = Capacities(Path.Combine(dir, "Kinds.g.cs"));
            Assert.Equal(0, caps["Locomotion"]);
            Assert.Equal(2, caps["Lift"]);
            Assert.Equal(9, caps["Damage"]);
            Assert.Equal(1, caps["StatChange"]);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void UnknownKindSettingFailsWithName()
    {
        var dir = Stage(d => File.WriteAllText(Path.Combine(d, "library.json"), """
            { "channels": ["locomotion", "action"], "timelines": ["run.json"], "kinds": { "Poison": { "inline": 0 } } }
            """));
        try
        {
            var error = Console.Error;
            var captured = new StringWriter();
            Console.SetError(captured);
            try { Assert.Equal(1, Pack(dir)); }
            finally { Console.SetError(error); }
            Assert.Contains("'Poison' is not a kind", captured.ToString());
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void PackIsDeterministicAndMatchesGolden()
    {
        var a = Stage();
        var b = Stage();
        try
        {
            Assert.Equal(0, Pack(a));
            Assert.Equal(0, Pack(b));
            var first = File.ReadAllBytes(Path.Combine(a, "game.tlib"));
            Assert.Equal(first, File.ReadAllBytes(Path.Combine(b, "game.tlib")));
            Assert.Equal(File.ReadAllText(Path.Combine(a, "Kinds.g.cs")), File.ReadAllText(Path.Combine(b, "Kinds.g.cs")));
            Assert.Equal(File.ReadAllBytes(Golden("kinds", "game.tlib")), first);
            Assert.Equal(File.ReadAllText(Golden("kinds", "Kinds.g.cs.txt")), File.ReadAllText(Path.Combine(a, "Kinds.g.cs")));
        }
        finally
        {
            Directory.Delete(a, true);
            Directory.Delete(b, true);
        }
    }

    [Fact]
    public void UnchangedCapacitiesDoNotRewriteKindsFile()
    {
        var dir = Stage();
        try
        {
            Assert.Equal(0, Pack(dir));
            var kinds = Path.Combine(dir, "Kinds.g.cs");
            var stamp = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(kinds, stamp);
            var run = Path.Combine(dir, "run.json");
            File.WriteAllText(run, File.ReadAllText(run).Replace("\"Speed\": 2", "\"Speed\": 3"));
            Assert.Equal(0, Pack(dir));
            Assert.Equal(stamp, File.GetLastWriteTimeUtc(kinds));
            File.WriteAllText(Path.Combine(dir, "library.json"), File.ReadAllText(Path.Combine(dir, "library.json")).Replace("\"gamePulses\": 4", "\"gamePulses\": 0"));
            Assert.Equal(0, Pack(dir));
            Assert.NotEqual(stamp, File.GetLastWriteTimeUtc(kinds));
            Assert.Equal(3, Capacities(kinds)["Damage"]);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void LibraryWithoutKindsKeepsVersionOneBytes()
    {
        var temp = Path.Combine(Path.GetTempPath(), "tl-lib-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            foreach (var name in new[] { "library.json", "jump.json", "slide.json" })
                File.Copy(Golden("library", name), Path.Combine(temp, name));
            Assert.Equal(0, Pack(temp));
            var bytes = File.ReadAllBytes(Path.Combine(temp, "game.tlib"));
            Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
            Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(28)));
            Assert.False(File.Exists(Path.Combine(temp, "Kinds.g.cs")));
            using var world = new TimelineWorld(bytes);
            Assert.Equal(2, world.View.TimelineCount);
        }
        finally { Directory.Delete(temp, true); }
    }

    [Fact]
    public void EventListsEqualClipWindowsInBothDirections()
    {
        var dir = Stage();
        try
        {
            Assert.Equal(0, Pack(dir));
            var lib = File.ReadAllBytes(Path.Combine(dir, "game.tlib"));
            Assert.Equal(2u, U32(lib, 4));
            var section = lib.AsSpan((int)U32(lib, 28), (int)U32(lib, 32)).ToArray();
            Assert.Equal(0x444E494Bu, U32(section, 0));
            var kindCount = (int)U32(section, 8);
            var feederCount = (int)U32(section, 12);
            var timelineCount = (int)U32(section, 16);
            var strings = (int)U32(section, 32);
            string Str(int off, int len) => Encoding.UTF8.GetString(section, strings + off, len);
            var kinds = Enumerable.Range(0, kindCount).Select(k => Str((int)U32(section, 64 + 40 * k), (int)U32(section, 68 + 40 * k))).ToArray();
            var feederTable = (int)U32(section, 24);
            var feederKind = Enumerable.Range(0, feederCount).Select(f => kinds[BinaryPrimitives.ReadUInt16LittleEndian(section.AsSpan(feederTable + 32 * f))]).ToArray();
            var feederTrigger = Enumerable.Range(0, feederCount).Select(f => section[feederTable + 32 * f + 2]).ToArray();
            var names = new[] { "jump", "potion_hop", "run", "stomp" };
            Assert.Equal(names.Length, timelineCount);
            var timelineTable = (int)U32(section, 28);
            for (var i = 0; i < timelineCount; i++)
            {
                var json = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(dir, names[i] + ".json"))).RootElement;
                var duration = json.GetProperty("duration").GetInt32();
                var expected = Expected(json, duration);
                var row = timelineTable + 48 * i;
                var windows = (int)U32(section, row + 8);
                var index = (int)U32(section, row + 20);
                var events = (int)U32(section, row + 24);
                var bits = (int)U32(section, row + 16);
                for (var t = 0; t < duration; t++)
                    for (var dirn = 0; dirn < 2; dirn++)
                    {
                        var from = (int)U32(section, index + 4 * (2 * t + dirn));
                        var to = (int)U32(section, index + 4 * (2 * t + dirn + 1));
                        var have = new List<string>();
                        for (var e = from; e < to; e++)
                        {
                            var at = events + 8 * e;
                            var w = BinaryPrimitives.ReadUInt16LittleEndian(section.AsSpan(at));
                            var f = BinaryPrimitives.ReadUInt16LittleEndian(section.AsSpan(at + 2));
                            var op = section[at + 4];
                            var start = BinaryPrimitives.ReadUInt16LittleEndian(section.AsSpan(windows + 16 * w + 4));
                            var end = BinaryPrimitives.ReadUInt16LittleEndian(section.AsSpan(windows + 16 * w + 6));
                            have.Add($"{op}:{feederKind[f]}:{feederTrigger[f]}:{start}-{end}");
                        }
                        var want = expected(t, dirn);
                        Assert.Equal(want, have.OrderBy(x => x, StringComparer.Ordinal).ToList());
                        Assert.Equal(want.Count > 0, (section[bits + t] & (1 << dirn)) != 0);
                    }
            }
        }
        finally { Directory.Delete(dir, true); }
    }

    static Func<int, int, List<string>> Expected(JsonElement json, int duration)
    {
        var items = new List<(string Kind, int Trigger, int Start, int End)>();
        foreach (var track in json.GetProperty("tracks").EnumerateArray())
            foreach (var clip in track.GetProperty("clips").EnumerateArray())
            {
                var start = clip.GetProperty("start").GetInt32();
                var end = clip.GetProperty("end").GetInt32();
                switch (track.GetProperty("type").GetString())
                {
                    case "WalkTrack": items.Add(("Locomotion", 0, start, end)); break;
                    case "ArcTrack": items.Add(("Lift", 0, start, end)); break;
                    case "HitTrack": items.Add(("Damage", 1, start, end)); items.Add(("StatChange", 2, start, end)); break;
                }
            }
        if (json.TryGetProperty("markers", out var markers))
            foreach (var marker in markers.EnumerateArray())
            {
                var tick = marker.GetProperty("tick").GetInt32();
                items.Add(("StatChange", 3, tick, tick + 1));
            }
        return (t, dir) =>
        {
            var list = new List<string>();
            foreach (var (kind, trigger, start, end) in items)
            {
                if (trigger == 0)
                {
                    if (dir == 0 ? end == t : start == t + 1) list.Add($"0:{kind}:0:{start}-{end}");
                    if (dir == 0 ? start == t : end - 1 == t) list.Add($"1:{kind}:0:{start}-{end}");
                }
                else if (trigger == 1 && start == t) list.Add($"2:{kind}:1:{start}-{end}");
                else if (trigger == 2 && end - 1 == t) list.Add($"3:{kind}:2:{start}-{end}");
                else if (trigger == 3 && start == t) list.Add($"4:{kind}:3:{start}-{end}");
            }
            return list.OrderBy(x => x, StringComparer.Ordinal).ToList();
        };
    }

    static uint U32(byte[] bytes, int at) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at));

    static Assembly Broken(string kindName, Action<TypeBuilder, Type> kindBody, KindEntries accepts, Type? feederReturn, bool pulse, bool folds = false)
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("KindBroken" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => e.Name == asm.FullName ? asm : null;
        var module = asm.DefineDynamicModule("m");
        var kind = module.DefineType("Broken." + kindName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, typeof(ValueType));
        kind.SetCustomAttribute(new CustomAttributeBuilder(typeof(TimelineKindAttribute).GetConstructor(Type.EmptyTypes)!, [], [typeof(TimelineKindAttribute).GetProperty("Accepts")!, typeof(TimelineKindAttribute).GetProperty("FoldsPulses")!], [accepts, folds]));
        kindBody(kind, typeof(int));
        var kindType = kind.CreateType();
        if (feederReturn != null)
        {
            var consumer = module.DefineType("Broken.Consumer", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, typeof(ValueType));
            consumer.AddInterfaceImplementation(typeof(ITrack<KindFixture.HitTrack, KindFixture.HitClip>));
            var method = consumer.DefineMethod("Feed", MethodAttributes.Public | MethodAttributes.Static, feederReturn, Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_0);
            if (feederReturn == typeof(float)) il.Emit(OpCodes.Conv_R4);
            il.Emit(OpCodes.Ret);
            var attribute = pulse ? typeof(PulseAttribute) : typeof(FeedsAttribute);
            method.SetCustomAttribute(new CustomAttributeBuilder(attribute.GetConstructor([typeof(Type)])!, [kindType]));
            consumer.CreateType();
        }
        return asm;
    }

    static void Apply(TypeBuilder kind, Type value)
    {
        var apply = kind.DefineMethod("Apply", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [value.MakeByRefType()]);
        apply.GetILGenerator().Emit(OpCodes.Ret);
    }

    static void ApplyAndCombine(TypeBuilder kind, Type value)
    {
        Apply(kind, value);
        var combine = kind.DefineMethod("Combine", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [value.MakeByRefType(), value.MakeByRefType()]);
        combine.GetILGenerator().Emit(OpCodes.Ret);
    }

    [Fact]
    public void DiagnosticsNameTheTypeMethodAndRepair()
    {
        Assert.Contains("needs public static Apply", Assert.Throws<BakeDiagnosticException>(() => KindCatalog.Discover([Broken("NoApply", (_, _) => { }, KindEntries.Spans, null, false)])).Message);
        Assert.Contains("Combine mode needs public static Combine", Assert.Throws<BakeDiagnosticException>(() => KindCatalog.Discover([Broken("NoCombine", Apply, KindEntries.Spans, null, false)])).Message);
        Assert.Contains("accepts pulses only; use [Pulse]", Assert.Throws<BakeDiagnosticException>(() => KindCatalog.Discover([Broken("PulsesOnly", ApplyAndCombine, KindEntries.Pulses, typeof(int), false)])).Message);
        Assert.Contains("accepts spans only", Assert.Throws<BakeDiagnosticException>(() => KindCatalog.Discover([Broken("SpansOnly", ApplyAndCombine, KindEntries.Spans, typeof(int), true)])).Message);
        Assert.Contains("FoldsPulses needs Combine mode and pulses", Assert.Throws<BakeDiagnosticException>(() => KindCatalog.Discover([Broken("FoldSpans", ApplyAndCombine, KindEntries.Spans, null, false, true)])).Message);
        var mismatch = Assert.Throws<BakeDiagnosticException>(() => KindCatalog.Discover([Broken("Mismatch", ApplyAndCombine, KindEntries.Spans, typeof(float), false)])).Message;
        Assert.Contains("'Broken.Consumer.Feed'", mismatch);
        Assert.Contains("takes 'System.Int32'", mismatch);
    }
}
}
