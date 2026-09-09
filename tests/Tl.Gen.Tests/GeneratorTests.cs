using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl;
using Tl.Gen;
using Tl.Gen.Analysis;
using Tl.Gen.CSharp;
using Tl.Gen.Model;
using Tl.Generation;
using Xunit;

namespace Tl.Gen.Tests;

public interface ITestInvoker
{
    uint[] GetRegionStarts();
    void Run(uint[] ticks, out float sum, out int enter, out int stay, out int exit, out List<string> log);
}

// Test-local fixture: the empty input marker for consumers without read-only
// context. Not part of the public API.
public readonly struct NoInput;

public class GeneratorTests
{
    [Fact]
    public void GeneratedKernelCompilesWithSourceAliases()
    {
        const string source = """
            using Tl;
            using Domain = Game.Data;

            namespace Game.Data
            {
                public readonly record struct Clip(float Value);
                public readonly struct Track : IBlend<Clip>
                {
                    public void Blend(in Clip first, in Clip second, float t, out Clip result) => result = first;
                }
            }

            namespace Game.Playback
            {
                public readonly partial struct Pulse : ITimeline<Domain.Track, Domain.Clip>
                {
                    public static void Define(scoped TimelineBuilder<Domain.Track, Domain.Clip> timeline)
                    {
                        var track = timeline.Track(new Domain.Track());
                        timeline.Clip(in track, new Domain.Clip(3f), 0u, 4u);
                    }
                }
            }
            """;

        var (declarations, diagnostics) = DeclarationReader.Read([("Pulse.cs", source)]);
        Assert.Empty(diagnostics);
        var declaration = Assert.Single(declarations);
        var plan = RegionAnalyzer.Analyze(declaration.Definition);
        var generated = KernelEmitter.EmitKernel(
            plan,
            WorkSlotMaterializer.ForRegions(plan),
            declaration.KernelName,
            declaration.File,
            declaration.Line,
            declaration.Kind);

        Assert.NotNull(CompileCode(source, generated));
    }

    [Fact]
    public void Validator_RejectsInvalidClipBounds()
    {
        var def = new TimelineDefinition
        {
            Name = "BadClipTimeline",
            TrackTypeName = "TestTrack",
            ClipTypeName = "TestClip",
            Tracks = [new TrackDefinition { Index = 0, TrackExpression = "new TestTrack()" }],
            Clips = [new ClipDefinition { TrackIndex = 0, Start = 10, End = 10, PayloadExpression = "new TestClip(1)" }]
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => TimelineValidator.Validate(def));
    }

    [Fact]
    public void Validator_RejectsThreeOverlappingClips()
    {
        var def = new TimelineDefinition
        {
            Name = "ThreeOverlapTimeline",
            TrackTypeName = "TestTrack",
            ClipTypeName = "TestClip",
            Tracks = [new TrackDefinition { Index = 0, TrackExpression = "new TestTrack()" }],
            Clips =
            [
                new ClipDefinition { TrackIndex = 0, Start = 0, End = 10, PayloadExpression = "new TestClip(1)" },
                new ClipDefinition { TrackIndex = 0, Start = 2, End = 12, PayloadExpression = "new TestClip(2)" },
                new ClipDefinition { TrackIndex = 0, Start = 4, End = 14, PayloadExpression = "new TestClip(3)" },
            ]
        };

        Assert.Throws<NotSupportedException>(() => TimelineValidator.Validate(def));
    }

    [Fact]
    public void RegionAnalyzer_ComputesCorrectRegionsAndBlends()
    {
        var def = new TimelineDefinition
        {
            Name = "TestTimeline",
            TrackTypeName = "TestTrack",
            ClipTypeName = "TestClip",
            Tracks =
            [
                new TrackDefinition { Index = 0, TrackExpression = "new TestTrack()" },
                new TrackDefinition { Index = 1, TrackExpression = "new TestTrack()" }
            ],
            Clips =
            [
                new ClipDefinition { TrackIndex = 0, Start = 0, End = 10, PayloadExpression = "new TestClip(1)" },
                new ClipDefinition { TrackIndex = 0, Start = 5, End = 15, PayloadExpression = "new TestClip(2)" },
                new ClipDefinition { TrackIndex = 1, Start = 2, End = 8, PayloadExpression = "new TestClip(3)" },
            ]
        };

        var plan = RegionAnalyzer.Analyze(def);

        Assert.Equal(2, plan.MaxActiveTracks);
        Assert.Equal(15u, plan.Duration);
        Assert.Equal([0u, 2u, 5u, 8u, 10u, 15u], plan.RegionStarts);
    }

    [Fact]
    public void LargeKernelSharesEachDirectionalRegionProgram()
    {
        var def = new TimelineDefinition
        {
            Name = "LargeTimeline",
            Namespace = "Tl.Gen.Tests.Generated",
            TrackTypeName = "global::Tl.Gen.Tests.GeneratorTests.SampleTrack",
            ClipTypeName = "global::Tl.Gen.Tests.GeneratorTests.SampleClip",
            Tracks = Enumerable.Range(0, 80)
                .Select(index => new TrackDefinition
                {
                    Index = (ushort)index,
                    TrackExpression = $"new global::Tl.Gen.Tests.GeneratorTests.SampleTrack({index})"
                })
                .ToList(),
            Clips = Enumerable.Range(0, 80)
                .Select(index => new ClipDefinition
                {
                    TrackIndex = (ushort)index,
                    Start = (uint)index,
                    End = 160u - (uint)index,
                    PayloadExpression = $"new global::Tl.Gen.Tests.GeneratorTests.SampleClip({index}f)"
                })
                .ToList()
        };

        var plan = RegionAnalyzer.Analyze(def);
        var slots = WorkSlotMaterializer.ForRegions(plan);
        var kernel = KernelEmitter.EmitKernel(plan, slots, "LargeTimeline", "Large.cs", 1);

        Assert.Contains("private static void ApplyForward", kernel);
        Assert.Contains("private static void ApplyBackward", kernel);
        Assert.Equal(80, kernel.Split("TResult.Forward(", StringSplitOptions.None).Length - 1);
        Assert.Equal(80, kernel.Split("TResult.Backward(", StringSplitOptions.None).Length - 1);
        Assert.Empty(CSharpSyntaxTree.ParseText(kernel).GetDiagnostics());

        var assembly = CompileCode(kernel + """
            public struct LargeResult : global::Tl.ITrack<
                global::Tl.Gen.Tests.GeneratorTests.SampleTrack,
                global::Tl.Gen.Tests.GeneratorTests.SampleClip,
                global::Tl.Gen.Tests.NoInput,
                LargeResult>
            {
                public float Sum;
                public int Count;

                public static void Forward(int ordinal, int count, ushort index,
                    in global::Tl.Gen.Tests.GeneratorTests.SampleTrack track,
                    in global::Tl.Gen.Tests.GeneratorTests.SampleClip clip,
                    global::Tl.ClipState state,
                    in uint tick,
                    in global::Tl.Gen.Tests.NoInput input,
                    ref LargeResult result)
                {
                    result.Sum += clip.Value;
                    result.Count++;
                }

                public static void Backward(int ordinal, int count, ushort index,
                    in global::Tl.Gen.Tests.GeneratorTests.SampleTrack track,
                    in global::Tl.Gen.Tests.GeneratorTests.SampleClip clip,
                    global::Tl.ClipState state,
                    in uint tick,
                    in global::Tl.Gen.Tests.NoInput input,
                    ref LargeResult result)
                {
                    result.Sum -= clip.Value;
                    result.Count++;
                }
            }

            public static class LargeRunner
            {
                public static int Run()
                {
                    var ticks = new uint[160];
                    var backwardTicks = new uint[160];
                    for (var index = 0; index < ticks.Length; index++)
                    {
                        ticks[index] = (uint)index;
                        backwardTicks[index] = (uint)(ticks.Length - index - 1);
                    }
                    var input = default(global::Tl.Gen.Tests.NoInput);
                    var forwardBatch = new LargeResult();
                    var forwardBatchPlayback = LargeTimeline.Start();
                    forwardBatchPlayback = LargeTimeline.Forward(
                        in forwardBatchPlayback, in input, ref forwardBatch, ticks);
                    var forwardScalar = new LargeResult();
                    var forwardScalarPlayback = LargeTimeline.Start();
                    foreach (var tick in ticks)
                        forwardScalarPlayback = LargeTimeline.Forward(
                            in forwardScalarPlayback, in input, ref forwardScalar, tick);
                    var backwardBatch = new LargeResult();
                    var backwardBatchPlayback = LargeTimeline.Start(159u);
                    backwardBatchPlayback = LargeTimeline.Backward(
                        in backwardBatchPlayback, in input, ref backwardBatch, backwardTicks);
                    var backwardScalar = new LargeResult();
                    var backwardScalarPlayback = LargeTimeline.Start(159u);
                    foreach (var tick in backwardTicks)
                        backwardScalarPlayback = LargeTimeline.Backward(
                            in backwardScalarPlayback, in input, ref backwardScalar, tick);
                    if (forwardBatch.Sum != 170640f
                        || forwardBatch.Sum != forwardScalar.Sum
                        || forwardBatch.Count != 6480
                        || forwardBatch.Count != forwardScalar.Count
                        || backwardBatch.Sum != -170640f
                        || backwardBatch.Sum != backwardScalar.Sum
                        || backwardBatch.Count != 6480
                        || backwardBatch.Count != backwardScalar.Count
                        || forwardBatchPlayback != forwardScalarPlayback
                        || backwardBatchPlayback != backwardScalarPlayback)
                        throw new global::System.InvalidOperationException();
                    return forwardBatch.Count + backwardBatch.Count;
                }
            }
            """);
        var runner = assembly.GetType("Tl.Gen.Tests.Generated.LargeRunner")!;
        Assert.Equal(12960, runner.GetMethod("Run")!.Invoke(null, null));
    }

    public readonly record struct SampleClip(float Value);
    public readonly struct SampleTrack : IBlend<SampleClip>
    {
        public readonly int Id;
        public SampleTrack(int id = 0) => Id = id;

        public void Blend(in SampleClip first, in SampleClip second, float t, out SampleClip result)
            => result = new SampleClip(first.Value * (1f - t) + second.Value * t);
    }

    public struct ComplexResult :
        ITrack<SampleTrack, SampleClip, NoInput, ComplexResult>
    {
        public float Sum;
        public int EnterCount;
        public int StayCount;
        public int ExitCount;
        public List<string> Log;

        public ComplexResult()
        {
            Log = [];
        }

        public static void Forward(int ordinal, int count, ushort index,
            in SampleTrack track, in SampleClip clip, ClipState state,
            in uint tick, in NoInput input, ref ComplexResult result)
        {
            switch (state)
            {
                case ClipState.Enter:
                    result.EnterCount++;
                    result.Log.Add($"Fwd:Enter:t{tick}:tr{index}:val{clip.Value}");
                    break;
                case ClipState.Stay:
                    result.StayCount++;
                    result.Sum += clip.Value;
                    result.Log.Add($"Fwd:Stay:t{tick}:tr{index}:val{clip.Value}");
                    break;
                case ClipState.Exit:
                    result.ExitCount++;
                    result.Log.Add($"Fwd:Exit:t{tick}:tr{index}:val{clip.Value}");
                    break;
            }
        }

        public static void Backward(int ordinal, int count, ushort index,
            in SampleTrack track, in SampleClip clip, ClipState state,
            in uint tick, in NoInput input, ref ComplexResult result)
        {
            switch (state)
            {
                case ClipState.Enter:
                    result.EnterCount++;
                    result.Log.Add($"Bwd:Enter:t{tick}:tr{index}:val{clip.Value}");
                    break;
                case ClipState.Stay:
                    result.StayCount++;
                    result.Sum -= clip.Value;
                    result.Log.Add($"Bwd:Stay:t{tick}:tr{index}:val{clip.Value}");
                    break;
                case ClipState.Exit:
                    result.ExitCount++;
                    result.Log.Add($"Bwd:Exit:t{tick}:tr{index}:val{clip.Value}");
                    break;
            }
        }
    }

    private class SimpleOracle
    {
        private readonly List<(ushort Track, float Value, uint Start, uint End)> _clips;
        private readonly uint _duration;

        public SimpleOracle(List<(ushort Track, float Value, uint Start, uint End)> clips, uint duration)
        {
            _clips = clips;
            _duration = duration;
        }

        public void StepForward(uint prevTick, uint tick, ref ComplexResult consumer)
        {
            if (_duration == 0) return;

            var tracks = _clips.Select(c => c.Track).Distinct().OrderBy(t => t).ToList();

            foreach (var t in tracks)
            {
                var active = _clips.Where(c => c.Track == t && tick >= c.Start && tick < c.End).ToList();
                if (active.Count == 0) continue;

                float clipVal;
                uint winStart, winEnd;

                if (active.Count == 1)
                {
                    clipVal = active[0].Value;
                    winStart = active[0].Start;
                    winEnd = active[0].End;
                }
                else
                {
                    var a = active[0];
                    var b = active[1];
                    if (a.Start > b.Start) (a, b) = (b, a);
                    var factorStart = Math.Max(a.Start, b.Start);
                    var factorEnd = Math.Min(a.End, b.End);
                    var factorLen = factorEnd - factorStart;
                    float factor = factorLen <= 1 ? 0.5f : (tick - factorStart) / (float)(factorLen - 1);
                    clipVal = a.Value * (1f - factor) + b.Value * factor;
                    winStart = Math.Min(a.Start, b.Start);
                    winEnd = Math.Max(a.End, b.End);
                }

                ClipState state;
                if (tick == winEnd - 1)
                    state = ClipState.Exit;
                else if (prevTick < winStart && tick >= winStart)
                    state = ClipState.Enter;
                else
                    state = ClipState.Stay;

                switch (state)
                {
                    case ClipState.Enter:
                        consumer.EnterCount++;
                        consumer.Log.Add($"Fwd:Enter:t{tick}:tr{t}:val{clipVal}");
                        break;
                    case ClipState.Stay:
                        consumer.StayCount++;
                        consumer.Sum += clipVal;
                        consumer.Log.Add($"Fwd:Stay:t{tick}:tr{t}:val{clipVal}");
                        break;
                    case ClipState.Exit:
                        consumer.ExitCount++;
                        consumer.Log.Add($"Fwd:Exit:t{tick}:tr{t}:val{clipVal}");
                        break;
                }
            }
        }
    }

    [Fact]
    public void Generator_CompiledTablesMatchRuntimeAndSimpleOracle()
    {
        var clips = new List<(ushort Track, float Value, uint Start, uint End)>
        {
            (0, 10f, 0, 8),
            (0, 20f, 4, 12),
            (1, 5f, 2, 6),
            (1, 15f, 10, 16),
        };
        uint duration = 16;

        // 1. Independent Simple Oracle
        var oracle = new SimpleOracle(clips, duration);
        var oracleConsumer = new ComplexResult();
        uint cur = 0;
        uint[] walk = [0u, 1u, 3u, 5u, 6u, 7u, 10u, 11u, 14u, 15u];
        foreach (var next in walk)
        {
            oracle.StepForward(cur, next, ref oracleConsumer);
            cur = next;
        }

        // 2. Runtime Compilation
        var runtimeId = Timeline<SampleTrack, SampleClip>.Build(b =>
        {
            var t0 = b.Track(new SampleTrack(0));
            var t1 = b.Track(new SampleTrack(1));
            foreach (var c in clips)
            {
                var tr = c.Track == 0 ? t0 : t1;
                b.Clip(in tr, new SampleClip(c.Value), c.Start, c.End);
            }
        }).InMemory();

        var runtimeConsumer = new ComplexResult();
        var runtimeInput = default(NoInput);
        var pb = Timeline.Start(runtimeId, 0u);
        pb = Timeline.Forward(runtimeId, in pb, in runtimeInput, ref runtimeConsumer, walk);

        // Compare Runtime with Oracle
        Assert.Equal(oracleConsumer.Sum, runtimeConsumer.Sum, precision: 3);
        Assert.Equal(oracleConsumer.EnterCount, runtimeConsumer.EnterCount);
        Assert.Equal(oracleConsumer.StayCount, runtimeConsumer.StayCount);
        Assert.Equal(oracleConsumer.ExitCount, runtimeConsumer.ExitCount);
        Assert.Equal(oracleConsumer.Log, runtimeConsumer.Log);

        // 3. Generated Tables via CSharpAdapter
        var def = new TimelineDefinition
        {
            Name = "CompiledTestTimeline",
            Namespace = "Tl.Gen.Tests.Generated",
            TrackTypeName = "CompiledTestTimeline",
            ClipTypeName = "global::Tl.Gen.Tests.GeneratorTests.SampleClip",
            BlendMethodBody = "result = new global::Tl.Gen.Tests.GeneratorTests.SampleClip(first.Value * (1f - t) + second.Value * t);",
            Tracks =
            [
                new TrackDefinition { Index = 0, TrackExpression = "new CompiledTestTimeline()" },
                new TrackDefinition { Index = 1, TrackExpression = "new CompiledTestTimeline()" }
            ],
            Clips = clips.Select(c => new ClipDefinition
            {
                TrackIndex = c.Track,
                Start = c.Start,
                End = c.End,
                PayloadExpression = $"new global::Tl.Gen.Tests.GeneratorTests.SampleClip({c.Value}f)"
            }).ToList()
        };

        var plan = RegionAnalyzer.Analyze(def);
        var files = CSharpAdapter.Default.Emit(plan);
        Assert.NotEmpty(files);
        var generatedCode = files[0].Content;

        var runnerCode = """
            using System.Collections.Generic;
            """ + "\n" + generatedCode + "\n" + """
            public class TestInvoker : global::Tl.Gen.Tests.ITestInvoker
            {
                public uint[] GetRegionStarts() => CompiledTestTimeline.RegionStarts.ToArray();

                public void Run(uint[] ticks, out float sum, out int enter, out int stay, out int exit, out List<string> log)
                {
                    var consumer = new GenConsumer();
                    var input = default(GenInput);
                    var pb = GeneratedTimeline<CompiledTestTimeline, global::Tl.Gen.Tests.GeneratorTests.SampleClip>.Start(0u);
                    pb = GeneratedTimeline<CompiledTestTimeline, global::Tl.Gen.Tests.GeneratorTests.SampleClip>.Forward(in pb, in input, ref consumer, ticks);
                    sum = consumer.Sum;
                    enter = consumer.EnterCount;
                    stay = consumer.StayCount;
                    exit = consumer.ExitCount;
                    log = consumer.Log;
                }
            }

            public readonly struct GenInput;

            public struct GenConsumer :
                ITrack<CompiledTestTimeline, global::Tl.Gen.Tests.GeneratorTests.SampleClip, GenInput, GenConsumer>
            {
                public float Sum;
                public int EnterCount;
                public int StayCount;
                public int ExitCount;
                public List<string> Log;

                public GenConsumer() { Log = []; }

                public static void Forward(int ordinal, int count, ushort index,
                    in CompiledTestTimeline track, in global::Tl.Gen.Tests.GeneratorTests.SampleClip clip, ClipState state,
                    in uint tick, in GenInput input, ref GenConsumer result)
                {
                    switch (state)
                    {
                        case ClipState.Enter:
                            result.EnterCount++;
                            result.Log.Add($"Fwd:Enter:t{tick}:tr{index}:val{clip.Value}");
                            break;
                        case ClipState.Stay:
                            result.StayCount++;
                            result.Sum += clip.Value;
                            result.Log.Add($"Fwd:Stay:t{tick}:tr{index}:val{clip.Value}");
                            break;
                        case ClipState.Exit:
                            result.ExitCount++;
                            result.Log.Add($"Fwd:Exit:t{tick}:tr{index}:val{clip.Value}");
                            break;
                    }
                }

                public static void Backward(int ordinal, int count, ushort index,
                    in CompiledTestTimeline track, in global::Tl.Gen.Tests.GeneratorTests.SampleClip clip, ClipState state,
                    in uint tick, in GenInput input, ref GenConsumer result)
                {
                    switch (state)
                    {
                        case ClipState.Enter:
                            result.EnterCount++;
                            result.Log.Add($"Bwd:Enter:t{tick}:tr{index}:val{clip.Value}");
                            break;
                        case ClipState.Stay:
                            result.StayCount++;
                            result.Sum -= clip.Value;
                            result.Log.Add($"Bwd:Stay:t{tick}:tr{index}:val{clip.Value}");
                            break;
                        case ClipState.Exit:
                            result.ExitCount++;
                            result.Log.Add($"Bwd:Exit:t{tick}:tr{index}:val{clip.Value}");
                            break;
                    }
                }
            }
            """;

        var generatedAssembly = CompileCode(runnerCode);
        Assert.NotNull(generatedAssembly);

        var invokerType = generatedAssembly.GetType("Tl.Gen.Tests.Generated.TestInvoker")!;
        var invoker = (ITestInvoker)Activator.CreateInstance(invokerType)!;

        // Verify region starts
        Assert.Equal(plan.RegionStarts, invoker.GetRegionStarts());

        // Run generated tables execution
        invoker.Run(walk, out var genSum, out var genEnter, out var genStay, out var genExit, out var genLog);

        // 4. Parity verification: Generated == Runtime == Oracle
        Assert.Equal(runtimeConsumer.Sum, genSum, precision: 3);
        Assert.Equal(runtimeConsumer.EnterCount, genEnter);
        Assert.Equal(runtimeConsumer.StayCount, genStay);
        Assert.Equal(runtimeConsumer.ExitCount, genExit);
        Assert.Equal(runtimeConsumer.Log, genLog);

        Timeline.Destroy(runtimeId);
    }

    private static Assembly CompileCode(params string[] sources)
    {
        var syntaxTrees = sources.Select(static source => CSharpSyntaxTree.ParseText(source)).ToArray();
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Span<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Timeline).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(GeneratorTests).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
        };

        var compilation = CSharpCompilation.Create(
            $"DynamicGen_{Guid.NewGuid():N}",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()));
            throw new InvalidOperationException($"Compilation failed:\n{errors}\nSource:\n{string.Join("\n", sources)}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }
}
