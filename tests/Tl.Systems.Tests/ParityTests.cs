using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl;
using Tl.TestSupport;
using Xunit;
using Tl.Systems.Tests;

public sealed unsafe class ParityTests : IDisposable
{
    internal const string Sources = """
        using Tl;
        using Tl.TestSupport;
        namespace Parity {
        public readonly struct PATrack : IBlend<PAClip> {
            public readonly float Scale; public PATrack(float s) { Scale = s; }
            public void Blend(in PAClip first, in PAClip second, float factor, out PAClip result) { result = new PAClip(first.Amount + (second.Amount - first.Amount) * factor); }
        }
        public struct PAClip { public float Amount; public PAClip(float a) { Amount = a; } }
        public readonly struct PBTrack : IBlend<PBClip> {
            public readonly float Scale; public PBTrack(float s) { Scale = s; }
            public void Blend(in PBClip first, in PBClip second, float factor, out PBClip result) { result = new PBClip(first.Amount + (second.Amount - first.Amount) * factor); }
        }
        public struct PBClip { public float Amount; public PBClip(float a) { Amount = a; } }
        public readonly struct PCTrack : IBlend<PCClip> {
            public readonly float Scale; public PCTrack(float s) { Scale = s; }
            public void Blend(in PCClip first, in PCClip second, float factor, out PCClip result) { result = new PCClip(first.Amount + (second.Amount - first.Amount) * factor); }
        }
        public struct PCClip { public float Amount; public PCClip(float a) { Amount = a; } }
        public readonly struct PDTrack : IBlend<PDClip> {
            public readonly float Scale; public PDTrack(float s) { Scale = s; }
            public void Blend(in PDClip first, in PDClip second, float factor, out PDClip result) { result = new PDClip(first.Amount + (second.Amount - first.Amount) * factor); }
        }
        public struct PDClip { public float Amount; public PDClip(float a) { Amount = a; } }
        public struct FA { public float V; public FA(float v) { V = v; } }
        public struct FB { public float V; public FB(float v) { V = v; } }
        public struct FI { public int V; public FI(int v) { V = v; } }
        public struct FD { public float V; public FD(float v) { V = v; } }
        [Tl.TimelineSystem]
        public ref partial struct SAll {
            public global::System.Span<FA> A;
            public global::System.Span<FB> B;
            public global::System.Span<FI> I;
            public global::System.Span<int> G;
            public global::System.Span<float> F;
            public global::System.Span<FD> D;
        }
        [Tl.TimelineSystemMember(typeof(SAll))]
        public readonly struct OA : ITrack<PATrack, PAClip> {
            public static void Fold(in Frame<PATrack, PAClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
            public static void ExecuteActive(in Frame<PATrack, PAClip> frame, in float lift, ref float a) { a += lift; }
        }
        [Tl.TimelineSystemMember(typeof(SAll))]
        public readonly struct NA : ITrack<PATrack, PAClip> {
            public static void Fold(in Frame<PATrack, PAClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
            public static void ExecuteActive(in Frame<PATrack, PAClip> frame, in float lift, ref FA a) { a.V += lift; }
            public static void OnClipEnd(in Frame<PATrack, PAClip> frame) { }
        }
        [Tl.TimelineSystemMember(typeof(SAll))]
        public readonly struct OB : ITrack<PBTrack, PBClip> {
            public static void Fold(in Frame<PBTrack, PBClip> frame, out float lift, out int kind) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; kind = (int)(frame.Clip.Amount * 10); }
            public static void ExecuteActive(in Frame<PBTrack, PBClip> frame, in float lift, in int kind, ref float b, ref int i) { b += lift; i += kind; }
        }
        [Tl.TimelineSystemMember(typeof(SAll))]
        public readonly struct NB : ITrack<PBTrack, PBClip> {
            public static void Fold(in Frame<PBTrack, PBClip> frame, out float lift, out int kind) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; kind = (int)(frame.Clip.Amount * 10); }
            public static void ExecuteActive(in Frame<PBTrack, PBClip> frame, in float lift, in int kind, ref FB b, ref FI i) { b.V += lift; i.V += kind; }
            public static void OnClipEnd(in Frame<PBTrack, PBClip> frame) { }
        }
        [Tl.TimelineSystemMember(typeof(SAll))]
        public readonly struct OC : ITrack<PCTrack, PCClip> {
            public static void ExecuteActive(in Frame<PCTrack, PCClip> frame, in int gain, ref float c) { c += gain * frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
        }
        [Tl.TimelineSystemMember(typeof(SAll))]
        public readonly struct NC : ITrack<PCTrack, PCClip> {
            public static void ExecuteActive(in Frame<PCTrack, PCClip> frame, in int gain, ref float c) { c += gain * frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
            public static void OnClipEnd(in Frame<PCTrack, PCClip> frame) { }
        }
        [Tl.TimelineSystemMember(typeof(SAll))]
        public readonly struct OD : ITrack<PDTrack, PDClip> {
            public static void Fold(in Frame<PDTrack, PDClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
            public static void ExecuteActive(in Frame<PDTrack, PDClip> frame, in float lift, ref float d) { d += lift; }
        }
        [Tl.TimelineSystemMember(typeof(SAll))]
        public readonly struct ND : ITrack<PDTrack, PDClip> {
            public static void Fold(in Frame<PDTrack, PDClip> frame, out float lift) { lift = frame.Direction * frame.Clip.Amount * frame.Track.Scale; }
            public static void ExecuteActive(in Frame<PDTrack, PDClip> frame, in float lift, ref FD d) { d.V += lift; }
            public static void OnClipEnd(in Frame<PDTrack, PDClip> frame) { }
        }
        public static class OldRunner {
            public static byte[] BakeA() => new DomainBaker().Track<PATrack, PAClip>(new PATrack(2f)).Clip(0, 0u, 40u, new PAClip(3f)).Looping().Bake();
            public static byte[] BakeB() => new DomainBaker().Track<PBTrack, PBClip>(new PBTrack(2f)).Clip(0, 0u, 40u, new PBClip(3f)).Looping().Bake();
            public static byte[] BakeC() => new DomainBaker().Track<PCTrack, PCClip>(new PCTrack(2f)).Clip(0, 0u, 40u, new PCClip(3f)).Looping().Bake();
            public static byte[] BakeD() => new DomainBaker().Track<PDTrack, PDClip>(new PDTrack(2f)).Clip(0, 0u, 40u, new PDClip(3f)).Looping().Bake();
            public static void RunA(ushort[] ids, ushort[] positions, bool forward, float[] fx) { var set = new global::Tl.ColumnSet(); set.Add<float>(fx); global::Tl.Timeline<PATrack, PAClip>.Apply(ids, positions, forward, in set); }
            public static void RunB(ushort[] ids, ushort[] positions, bool forward, float[] fx, int[] ix) { var set = new global::Tl.ColumnSet(); set.Add<float>(fx); set.Add<int>(ix); global::Tl.Timeline<PBTrack, PBClip>.Apply(ids, positions, forward, in set); }
            public static void RunC(ushort[] ids, ushort[] positions, bool forward, int[] gain, float[] fx) { var set = new global::Tl.ColumnSet(); set.Add<int>(gain); set.Add<float>(fx); global::Tl.Timeline<PCTrack, PCClip>.Apply(ids, positions, forward, in set); }
            public static void RunD(ushort[] ids, ushort[] positions, bool forward, float[] fx) { var set = new global::Tl.ColumnSet(); set.Add<float>(fx); global::Tl.Timeline<PDTrack, PDClip>.Apply(ids, positions, forward, in set); }
            public static void Advance(ushort index, ushort[] positions, bool forward) { global::Tl.Timeline.Advance(index, positions, forward); }
        }
        }
        """;

    internal readonly struct Fixture
    {
        public readonly Assembly Assembly;
        public readonly ulong[] PairKeys;
        public readonly Action<TimelineWorld, PlaybackTable, bool, (nint Pointer, Type Type)[], int> Run;
        public Fixture(Assembly assembly, ulong[] pairKeys, Action<TimelineWorld, PlaybackTable, bool, (nint Pointer, Type Type)[], int> run)
        {
            Assembly = assembly;
            PairKeys = pairKeys;
            Run = run;
            _oldRunner = assembly.GetType("Parity.OldRunner")!;
        }

        readonly Type _oldRunner;

        public byte[] Bake(string name) => (byte[])_oldRunner.GetMethod("Bake" + name)!.Invoke(null, null)!;

        public void RunOld(string name, ushort[] ids, ushort[] positions, bool forward, params object[] columns)
        {
            var args = new object?[3 + columns.Length];
            args[0] = ids;
            args[1] = positions;
            args[2] = forward;
            columns.CopyTo(args, 3);
            _oldRunner.GetMethod("Run" + name)!.Invoke(null, args);
        }
    }

    internal sealed unsafe class PinnedFixtureArray : IDisposable
    {
        public void* Values;
        public readonly Type PointerType;
        readonly Array _data;
        readonly System.Reflection.FieldInfo _value;
        readonly GCHandle _handle;

        public PinnedFixtureArray(Assembly assembly, string typeName, int length)
        {
            var element = assembly.GetType(typeName)!;
            _value = element.GetField("V")!;
            _data = Array.CreateInstance(element, length);
            _handle = GCHandle.Alloc(_data, GCHandleType.Pinned);
            Values = (void*)_handle.AddrOfPinnedObject();
            PointerType = element.MakePointerType();
        }

        public void AddInto(float[] destination)
        {
            for (var i = 0; i < destination.Length; i++) destination[i] += Convert.ToSingle(_value.GetValue(_data.GetValue(i)));
        }

        public void AddInto(int[] destination)
        {
            for (var i = 0; i < destination.Length; i++) destination[i] += Convert.ToInt32(_value.GetValue(_data.GetValue(i)));
        }

        public void Dispose()
        {
            if (_handle.IsAllocated) _handle.Free();
            GC.SuppressFinalize(this);
        }
    }

    internal sealed unsafe class PinnedInts : IDisposable
    {
        public int* Values;
        readonly GCHandle _handle;
        public PinnedInts(int[] data) { _handle = GCHandle.Alloc(data, GCHandleType.Pinned); Values = (int*)_handle.AddrOfPinnedObject(); }
        public void Dispose() { if (_handle.IsAllocated) _handle.Free(); GC.SuppressFinalize(this); }
    }

    internal sealed unsafe class PinnedFloats : IDisposable
    {
        public float* Values;
        readonly GCHandle _handle;
        public PinnedFloats(float[] data) { _handle = GCHandle.Alloc(data, GCHandleType.Pinned); Values = (float*)_handle.AddrOfPinnedObject(); }
        public void Dispose() { if (_handle.IsAllocated) _handle.Free(); GC.SuppressFinalize(this); }
    }

    internal static Fixture LoadFixture()
    {
        var tree = CSharpSyntaxTree.ParseText(Sources, new CSharpParseOptions(LanguageVersion.Latest), "Parity.cs");
        var bakerSource = "using System;\nusing System.Collections.Generic;\nusing System.Linq;\n" + EmissionTests.ReadResource("Tl.Systems.Tests.Shared.DomainBaker.cs");
        var baker = CSharpSyntaxTree.ParseText(bakerSource, new CSharpParseOptions(LanguageVersion.Latest), "DomainBaker.cs");
        var refs = EmissionTests.References().Where(reference => reference.Display is not string path || !path.EndsWith("Tl.Systems.Tests.dll", StringComparison.OrdinalIgnoreCase));
        var compilation = CSharpCompilation.Create("ParityFixture", [tree, baker], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new Tl.Gen.CSharp.TimelineIncrementalGenerator().AsSourceGenerator()]);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var generatorDiagnostics);
        Assert.Empty(generatorDiagnostics);
        var emit = Tl.Gen.CSharp.SystemGeneration.Emit(compilation, "Parity");
        Assert.True(emit.Diagnostics.Count == 0, string.Join("\n", emit.Diagnostics));
        var artifact = Assert.Single(emit.Artifacts);
        var final = updated.AddSyntaxTrees(CSharpSyntaxTree.ParseText(artifact.Content, new CSharpParseOptions(LanguageVersion.Latest), artifact.RelativePath));
        using var stream = new MemoryStream();
        var result = final.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        var module = assembly.GetType("TlSystems.TlModule_Parity")!;
        module.GetMethod("Install", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
        var kernel = assembly.GetType("TlSystems.TlSystem_Parity_global__Parity_SAll")!;
        var pairKeys = (ulong[])kernel.GetField("PairKeys", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var run = kernel.GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Static)!;
        return new Fixture(assembly, pairKeys, (world, players, forward, columns, n) =>
        {
            var args = new object?[4 + columns.Length];
            args[0] = world;
            args[1] = players;
            args[2] = forward;
            for (var i = 0; i < columns.Length; i++) args[3 + i] = Pointer.Box((void*)columns[i].Pointer, columns[i].Type);
            args[3 + columns.Length] = n;
            run.Invoke(null, args);
        });
    }

    public void Dispose() { GC.SuppressFinalize(this); }

    [Fact]
    public void FourPairsParityForwardAndBackward()
    {
        var fixture = LoadFixture();
        const int Entities = 8;
        var ids = new ushort[Entities];
        var positions = new ushort[Entities];
        var assetA = TimelineAsset.Load(fixture.Bake("A"));
        var assetB = TimelineAsset.Load(fixture.Bake("B"));
        var assetC = TimelineAsset.Load(fixture.Bake("C"));
        var assetD = TimelineAsset.Load(fixture.Bake("D"));
        var oldA = new float[Entities];
        var oldBf = new float[Entities];
        var oldBi = new int[Entities];
        var oldC = new float[Entities];
        var oldD = new float[Entities];
        var oldG = new int[Entities];
        for (var i = 0; i < Entities; i++) oldG[i] = 1;
        RunOld(true);
        RunOld(false);
        var newA = new float[Entities];
        var newBf = new float[Entities];
        var newBi = new int[Entities];
        var newC = new float[Entities];
        var newD = new float[Entities];
        var newG = new int[Entities];
        for (var i = 0; i < Entities; i++) newG[i] = 1;
        RunNew(true);
        RunNew(false);
        Assert.Equal(oldA, newA);
        Assert.Equal(oldBf, newBf);
        Assert.Equal(oldBi, newBi);
        Assert.Equal(oldC, newC);
        Assert.Equal(oldD, newD);

        void RunOld(bool forward)
        {
            for (var i = 0; i < Entities; i++) { ids[i] = assetA; positions[i] = 0; }
            for (var f = 0; f < 97; f++)
            {
                fixture.RunOld("A", ids, positions, forward, oldA);
                AdvanceAll(assetA, forward);
            }
            for (var i = 0; i < Entities; i++) { ids[i] = assetB; positions[i] = 0; }
            for (var f = 0; f < 97; f++)
            {
                fixture.RunOld("B", ids, positions, forward, oldBf, oldBi);
                AdvanceAll(assetB, forward);
            }
            for (var i = 0; i < Entities; i++) { ids[i] = assetC; positions[i] = 0; }
            for (var f = 0; f < 97; f++)
            {
                fixture.RunOld("C", ids, positions, forward, oldG, oldC);
                AdvanceAll(assetC, forward);
            }
            for (var i = 0; i < Entities; i++) { ids[i] = assetD; positions[i] = 0; }
            for (var f = 0; f < 97; f++)
            {
                fixture.RunOld("D", ids, positions, forward, oldD);
                AdvanceAll(assetD, forward);
            }
        }

        void AdvanceAll(ushort index, bool forward)
        {
            Timeline.Advance(index, positions.AsSpan(), forward);
        }

        void RunNew(bool forward)
        {
            using var scope = new ViewScope();
            BuildParityView(scope, fixture);
            var world = new TimelineWorld();
            scope.InjectInto(world);
            GC.SuppressFinalize(world);
            using var players = new PlaybackTable(Entities);
            for (var i = 0; i < Entities; i++) players.Start(i, 1);
            world.ApplyRequests(players);
            using var colA = new PinnedFixtureArray(fixture.Assembly, "Parity.FA", Entities);
            using var colB = new PinnedFixtureArray(fixture.Assembly, "Parity.FB", Entities);
            using var colI = new PinnedFixtureArray(fixture.Assembly, "Parity.FI", Entities);
            using var colG = new PinnedInts(newG);
            using var colC = new PinnedFloats(newC);
            using var colD = new PinnedFixtureArray(fixture.Assembly, "Parity.FD", Entities);
            var columns = new (nint Pointer, Type Type)[]
            {
                ((nint)colA.Values, colA.PointerType),
                ((nint)colB.Values, colB.PointerType),
                ((nint)colI.Values, colI.PointerType),
                ((nint)colG.Values, typeof(int*)),
                ((nint)colC.Values, typeof(float*)),
                ((nint)colD.Values, colD.PointerType),
            };
            for (var f = 0; f < 97; f++)
            {
                world.ApplyRequests(players);
                fixture.Run(world, players, forward, columns, Entities);
                world.Advance(players, forward);
            }
            colA.AddInto(newA);
            colB.AddInto(newBf);
            colI.AddInto(newBi);
            colD.AddInto(newD);
        }
    }

    internal static void BuildParityView(ViewScope scope, Fixture fixture)
    {
        var keys = fixture.PairKeys;
        byte[] Track(float scale) => BitConverter.GetBytes(scale);
        byte[] Clip(float amount) => BitConverter.GetBytes(amount);
        var specs = new List<ViewScope.TimelineSpec>();
        var tl = new ViewScope.TimelineSpec { Loop = 1, Duration = 40 };
        tl.Steps.Add(new ViewScope.StepSpec { PairKey = keys[0], Track = Track(2f), First = Clip(3f), WindowStart = 0, WindowEnd = 40, Lane = LaneFor(0, 40, 3f, 2f) });
        tl.Steps.Add(new ViewScope.StepSpec { PairKey = keys[1], Track = Track(2f), First = Clip(3f), WindowStart = 0, WindowEnd = 40, Lane = LaneFor2(1, 40, 3f, 2f) });
        tl.Steps.Add(new ViewScope.StepSpec { PairKey = keys[2], Track = Track(2f), First = Clip(3f), WindowStart = 0, WindowEnd = 40 });
        tl.Steps.Add(new ViewScope.StepSpec { PairKey = keys[3], Track = Track(2f), First = Clip(3f), WindowStart = 0, WindowEnd = 40, Lane = LaneFor(3, 40, 3f, 2f) });
        specs.Add(tl);
        scope.Build(specs);

        byte[] LaneFor(int pair, uint window, float amount, float scale)
        {
            var lane = new byte[window * 2 * 4];
            for (uint tick = 0; tick < window; tick++)
                for (var dir = 0; dir < 2; dir++)
                {
                    var value = (dir == 1 ? -1f : 1f) * amount * scale;
                    MemoryMarshal.Write(lane.AsSpan((int)(tick * 2 + dir) * 4, 4), in value);
                }
            return lane;
        }

        byte[] LaneFor2(int pair, uint window, float amount, float scale)
        {
            var lane = new byte[window * 2 * 8];
            for (uint tick = 0; tick < window; tick++)
                for (var dir = 0; dir < 2; dir++)
                {
                    var lift = (dir == 1 ? -1f : 1f) * amount * scale;
                    var kind = (int)(amount * 10);
                    MemoryMarshal.Write(lane.AsSpan((int)(tick * 2 + dir) * 8, 4), in lift);
                    MemoryMarshal.Write(lane.AsSpan((int)(tick * 2 + dir) * 8 + 4, 4), in kind);
                }
            return lane;
        }
    }
}
