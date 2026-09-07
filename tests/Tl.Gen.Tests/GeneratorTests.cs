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
        Assert.Equal(1, plan.MaxActiveBlends);
        Assert.Equal(15u, plan.Duration);
        Assert.Equal([0u, 2u, 5u, 8u, 10u, 15u], plan.RegionStarts);
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
        IForward<SampleTrack, SampleClip, NoInput, ComplexResult>,
        IBackward<SampleTrack, SampleClip, NoInput, ComplexResult>
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

        public void Forward(in Tracks<SampleTrack, SampleClip> tracks, in NoInput input, in uint tick, ref ComplexResult result)
        {
            foreach (var work in tracks)
            {
                switch (work.State)
                {
                    case ClipState.Enter:
                        result.EnterCount++;
                        result.Log.Add($"Fwd:Enter:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                        break;
                    case ClipState.Stay:
                        result.StayCount++;
                        result.Sum += work.Clip.Value;
                        result.Log.Add($"Fwd:Stay:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                        break;
                    case ClipState.Exit:
                        result.ExitCount++;
                        result.Log.Add($"Fwd:Exit:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                        break;
                }
            }
        }

        public void Backward(in Tracks<SampleTrack, SampleClip> tracks, in NoInput input, in uint tick, ref ComplexResult result)
        {
            foreach (var work in tracks)
            {
                switch (work.State)
                {
                    case ClipState.Enter:
                        result.EnterCount++;
                        result.Log.Add($"Bwd:Enter:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                        break;
                    case ClipState.Stay:
                        result.StayCount++;
                        result.Sum -= work.Clip.Value;
                        result.Log.Add($"Bwd:Stay:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                        break;
                    case ClipState.Exit:
                        result.ExitCount++;
                        result.Log.Add($"Bwd:Exit:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                        break;
                }
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
        });

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
                IForward<CompiledTestTimeline, global::Tl.Gen.Tests.GeneratorTests.SampleClip, GenInput, GenConsumer>,
                IBackward<CompiledTestTimeline, global::Tl.Gen.Tests.GeneratorTests.SampleClip, GenInput, GenConsumer>
            {
                public float Sum;
                public int EnterCount;
                public int StayCount;
                public int ExitCount;
                public List<string> Log;

                public GenConsumer() { Log = []; }

                public void Forward(in Tracks<CompiledTestTimeline, global::Tl.Gen.Tests.GeneratorTests.SampleClip> tracks, in GenInput input, in uint tick, ref GenConsumer result)
                {
                    foreach (var work in tracks)
                    {
                        switch (work.State)
                        {
                            case ClipState.Enter:
                                result.EnterCount++;
                                result.Log.Add($"Fwd:Enter:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                                break;
                            case ClipState.Stay:
                                result.StayCount++;
                                result.Sum += work.Clip.Value;
                                result.Log.Add($"Fwd:Stay:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                                break;
                            case ClipState.Exit:
                                result.ExitCount++;
                                result.Log.Add($"Fwd:Exit:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                                break;
                        }
                    }
                }

                public void Backward(in Tracks<CompiledTestTimeline, global::Tl.Gen.Tests.GeneratorTests.SampleClip> tracks, in GenInput input, in uint tick, ref GenConsumer result)
                {
                    foreach (var work in tracks)
                    {
                        switch (work.State)
                        {
                            case ClipState.Enter:
                                result.EnterCount++;
                                result.Log.Add($"Bwd:Enter:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                                break;
                            case ClipState.Stay:
                                result.StayCount++;
                                result.Sum -= work.Clip.Value;
                                result.Log.Add($"Bwd:Stay:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                                break;
                            case ClipState.Exit:
                                result.ExitCount++;
                                result.Log.Add($"Bwd:Exit:t{tick}:tr{work.Index}:val{work.Clip.Value}");
                                break;
                        }
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

    private static Assembly CompileCode(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
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
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()));
            throw new InvalidOperationException($"Compilation failed:\n{errors}\nSource:\n{source}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }
}
