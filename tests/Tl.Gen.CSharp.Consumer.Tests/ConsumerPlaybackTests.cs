using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Tl.Gen.CSharp.Consumer.Tests;

public sealed class ConsumerPlaybackTests
{
    private static readonly Lazy<Assembly> Fixture = new(Compile);

    [Fact]
    public void TickForwardAndBackwardMatchesTheHandWrittenOracleIncludingTheBlendWindow()
    {
        var result = Driver("Blend");

        Assert.Equal(
            "968|936|920|904|904|904|872|856" +
            "#872|904|904|904|920|936|968|1000" +
            "#1000#0",
            result);
    }

    [Fact]
    public void AbaAuthoredRowsDeliverEachTypedFrameToItsOwnColumns()
    {
        var result = Driver("Rows");

        Assert.Equal("968,508,218,1,1,1#936,516,186,2,2,2#968,508,218,1,1,1", result);
    }

    [Fact]
    public void MissingColumnBindThrowsAtFacadeTickBeforeAnyEffects()
    {
        var result = Driver("Missing");

        Assert.StartsWith("THROWN|", result);
        Assert.Contains("ApplyGuarded", result);
        Assert.Contains("Resistance", result);
        Assert.Contains("required column missing for registered consumer", result);
        Assert.EndsWith("|400|0", result);
    }

    [Fact]
    public void StandaloneJobDispatchesOnDataAuthoredTimelineWithoutAuthoredTimelineOrUse()
    {
        var result = Driver("Standalone");

        Assert.Equal("25#40#25", result);
    }

    [Fact]
    public void OnMemoSpellingFoldsIdenticallyToTheRefFloatMeasuredShape()
    {
        var result = Driver("Memo");

        Assert.Equal("25#40#25", result);
    }

    [Fact]
    public void MultiOutputMemoFoldsIntoTypedLanesAndApplyReadsEachColumn()
    {
        var result = Driver("DualMemo");

        Assert.Equal("10,10,10#3,3,3#-10,-10#-3,-3", result);
    }

    [Fact]
    public void DiscoveredBakesInstallDispatchEntriesIntoTheRuntimeTable()
    {
        var result = Driver("Bakes");

        Assert.Equal("2;2|World+Entity;2|World+Entity#1;1|World#1;1|Entity", result);
    }

    [Fact]
    public void ManualInstallsAppendInCallOrderAndBoundsAreLoud()
    {
        var result = Driver("ManualBake");

        Assert.Equal("3;3|World+Entity+GuardToken;1|World;1|Entity#ArgumentOutOfRangeException", result);
    }

    [Fact]
    public void TimelineBakeAttachesMarkersSubsetsAndGatesAdvancementEndToEnd()
    {
        var result = Driver("BakeWalk");

        Assert.Equal("damage:7,narrow:7#silent#936#2", result);
    }

    [Fact]
    public void TimelineBakeWalksPairsInPairKeyOrder()
    {
        var result = Driver("BakePairOrder");

        Assert.Equal("heal,damage:3,narrow:3", result);
    }

    [Fact]
    public void OwnerTlJumpShapeBakesWithInAndRefParametersAndAttachesComponents()
    {
        var result = Driver("OwnerShape");

        Assert.Equal("e7=JumpY,e7=Sfx", result);
    }

    [Fact]
    public void RefContextMutatesTheCallerVariableEndToEnd()
    {
        var result = Driver("RefFlow");

        Assert.Equal("42", result);
    }

    [Fact]
    public void MixedModifierPairsDispatchInOneBakeCall()
    {
        var result = Driver("MixedAsset");

        Assert.Equal("damage:5,narrow:5#-7", result);
    }

    [Fact]
    public void SpanBatchBakeAttachesEveryEntityInOneCall()
    {
        var result = Driver("BatchSpan");

        Assert.Equal("spawn:1,spawn:2,spawn:3", result);
    }

    [Fact]
    public void ConsumerTypedBakeParameterIsBoundToDefaultBecauseConsumersAreStateless()
    {
        var result = Driver("ConsumerParameter");

        Assert.Equal("probe:0", result);
    }

    private static string Driver(string method)
    {
        var value = Fixture.Value.GetType("Domain.Playback")!.GetMethod(method)!.Invoke(null, null);
        return Assert.IsType<string>(value);
    }

    private static Assembly Compile()
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("ConsumerPlaybackFixture",
            [CSharpSyntaxTree.ParseText(Domain, options, "Domain.cs"),
             CSharpSyntaxTree.ParseText(BakerSource, options, "DomainBaker.cs")], References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TimelineIncrementalGenerator().AsSourceGenerator()], parseOptions: options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics);
        using var stream = new MemoryStream();
        var result = output.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics) + "\n" + string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(static tree => tree.ToString())));
        return Assembly.Load(stream.ToArray());
    }

    private static IEnumerable<MetadataReference> References()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ITrack<,>).Assembly.Location).Distinct(StringComparer.Ordinal)
            .Select(static path => MetadataReference.CreateFromFile(path));

    private static string BakerSource
        => "using System;\nusing System.Collections.Generic;\nusing System.Linq;\n"
         + File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Shared", "DomainBaker.cs"));

    private const string Domain = """
        using System;
        using System.Buffers.Binary;
        using System.Collections.Generic;
        using System.Globalization;
        using System.Linq;
        using System.Runtime.CompilerServices;
        using System.Runtime.InteropServices;
        using Tl;
        using Tl.TestSupport;

        namespace Domain
        {

        public readonly record struct DamageClip(float Amount);

        public readonly record struct DamageTrack(float Multiplier) : IBlend<DamageClip>
        {
            public void Blend(in DamageClip first, in DamageClip second, float factor, out DamageClip result)
                => result = new DamageClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly record struct HealClip(float Amount);

        public readonly record struct HealTrack(float Multiplier) : IBlend<HealClip>
        {
            public void Blend(in HealClip first, in HealClip second, float factor, out HealClip result)
                => result = new HealClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public struct Resistance { public float Scale; }

        public sealed class World { public readonly List<string> Marks = []; public readonly List<DamageTag> Tags = []; }

        public readonly record struct Entity(World Owner, int Id);

        public readonly record struct DamageTag(int Entity);

        public readonly record struct GuardToken(int Level);

        public readonly struct ApplyDamage : ITrack<DamageTrack, DamageClip>
        {
            public static void OnActive(in Frame<DamageTrack, DamageClip> frame, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                health += frame.IsBackward ? amount : -amount;
            }
        }

        public readonly struct ApplyDamageBake : IBake<ApplyDamage>
        {
            public static void Bake(in World world, ref Entity entity) { world.Marks.Add("damage:" + entity.Id.ToString(CultureInfo.InvariantCulture)); }
        }

        public readonly struct ApplyDamageNarrowBake : IBake<ApplyDamage>
        {
            public static void Bake(in World world, ref Entity entity) { world.Marks.Add("narrow:" + entity.Id.ToString(CultureInfo.InvariantCulture)); }
        }

        public readonly struct ApplyHeal : ITrack<HealTrack, HealClip>, IBake<ApplyHeal>
        {
            public static void OnActive(in Frame<HealTrack, HealClip> frame, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                health += frame.IsBackward ? -amount : amount;
            }

            public static void Bake(in World world) { world.Marks.Add("heal"); }
        }

        public readonly record struct BuffClip(float Amount);

        public readonly record struct BuffTrack(float Multiplier) : IBlend<BuffClip>
        {
            public void Blend(in BuffClip first, in BuffClip second, float factor, out BuffClip result)
                => result = new BuffClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly struct ApplyBuff : ITrack<BuffTrack, BuffClip>
        {
            public static void OnActive(in Frame<BuffTrack, BuffClip> frame, ref float armor)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                armor += frame.IsBackward ? -amount : amount;
            }
        }

        public readonly record struct MemoClip(float Amount);

        public readonly record struct MemoTrack(float Multiplier) : IBlend<MemoClip>
        {
            public void Blend(in MemoClip first, in MemoClip second, float factor, out MemoClip result)
                => result = new MemoClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly struct MemoBuff : ITrack<MemoTrack, MemoClip>
        {
            public static void OnMemo(in Frame<MemoTrack, MemoClip> frame, out float armor)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                armor = frame.IsBackward ? -amount : amount;
            }
        }

        public readonly record struct DualClip(float Amount, int Ticks);

        public readonly record struct DualTrack(float Multiplier) : IBlend<DualClip>
        {
            public void Blend(in DualClip first, in DualClip second, float factor, out DualClip result)
                => result = new DualClip(first.Amount + (second.Amount - first.Amount) * factor, second.Ticks);
        }

        public readonly struct DualMemo : ITrack<DualTrack, DualClip>
        {
            public static void OnMemo(in Frame<DualTrack, DualClip> frame, out float armor, out int ticks)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                armor = frame.IsBackward ? -amount : amount;
                ticks = frame.IsBackward ? -frame.Clip.Ticks : frame.Clip.Ticks;
            }
        }

        public readonly record struct GuardClip(float Amount);

        public readonly record struct GuardTrack(float Multiplier) : IBlend<GuardClip>
        {
            public void Blend(in GuardClip first, in GuardClip second, float factor, out GuardClip result)
                => result = new GuardClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly struct ApplyGuarded : ITrack<GuardTrack, GuardClip>
        {
            public static void OnActive(in Frame<GuardTrack, GuardClip> frame, in Resistance resistance, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
                health += frame.IsBackward ? amount : -amount;
            }
        }

        public readonly struct GuardWardBake : IBake<ApplyGuarded>
        {
            public static void Bake(in World world, ref Entity entity, GuardToken token) { world.Tags.Add(new DamageTag(-entity.Id - token.Level)); }
        }

        public readonly record struct SpawnClip(byte Stream);

        public readonly record struct SpawnTrack(byte Gain) : IBlend<SpawnClip>
        {
            public void Blend(in SpawnClip first, in SpawnClip second, float factor, out SpawnClip result) => result = first;
        }

        public readonly struct SpawnJob : ITrack<SpawnTrack, SpawnClip>
        {
            public static void OnActive(in Frame<SpawnTrack, SpawnClip> frame) { }
        }

        public readonly struct SpawnWaveBake : IBake<SpawnJob>
        {
            public static void Bake(Span<Entity> entities)
            {
                foreach (var entity in entities)
                    entity.Owner.Marks.Add("spawn:" + entity.Id.ToString(CultureInfo.InvariantCulture));
            }
        }

        public readonly struct EntityMutateBake : IBake<ApplyBuff>
        {
            public static void Bake(ref Entity entity) => entity = new Entity(entity.Owner, entity.Id + 1);
        }

        public readonly record struct ProbeClip(byte Amount);

        public readonly record struct ProbeTrack(byte Gain) : IBlend<ProbeClip>
        {
            public void Blend(in ProbeClip first, in ProbeClip second, float factor, out ProbeClip result) => result = first;
        }

        public readonly struct ProbeJob : ITrack<ProbeTrack, ProbeClip>
        {
            public readonly int Observed;

            public static void OnActive(in Frame<ProbeTrack, ProbeClip> frame) { }
        }

        public readonly struct ProbeStateBake : IBake<ProbeJob>
        {
            public static void Bake(ProbeJob consumer, in World world)
                => world.Marks.Add("probe:" + consumer.Observed.ToString(CultureInfo.InvariantCulture));
        }

        }

        namespace TlJumpShape
        {
        public readonly record struct JumpClip(int Height);

        public readonly record struct JumpTrack(float Scale) : IBlend<JumpClip>
        {
            public void Blend(in JumpClip first, in JumpClip second, float factor, out JumpClip result) => result = first;
        }

        public readonly record struct SoundClip(int Code);

        public readonly record struct SoundTrack(float Gain) : IBlend<SoundClip>
        {
            public void Blend(in SoundClip first, in SoundClip second, float factor, out SoundClip result) => result = first;
        }

        public sealed class World
        {
            public readonly List<string> Attached = [];
        }

        public struct JumpY { public float Value; }

        public struct Sfx { public float Value; }

        public readonly struct Entity
        {
        public readonly World Owner;
        public readonly int Id;

        public Entity(World owner, int id) { Owner = owner; Id = id; }

        public void Add<T>(T component) => Owner.Attached.Add("e" + Id.ToString(CultureInfo.InvariantCulture) + "=" + typeof(T).Name);
        }

        public readonly struct MoveY : ITrack<JumpTrack, JumpClip>
        {
        public static void OnActive(in Frame<JumpTrack, JumpClip> frame, ref float y)
            => y += frame.Direction * frame.Clip.Height * frame.Track.Scale;
        }

        public readonly struct PlaySound : ITrack<SoundTrack, SoundClip>
        {
        public static void OnActive(in Frame<SoundTrack, SoundClip> frame, ref float channel)
            => channel += frame.Direction * frame.Clip.Code * frame.Track.Gain;
        }

        public readonly struct AttachJump : IBake<MoveY>
        {
        public static void Bake(in World world, ref Entity entity)
            => entity.Add(new JumpY());
        }

        public readonly struct AttachSound : IBake<PlaySound>
        {
        public static void Bake(in World world, ref Entity entity)
            => entity.Add(new Sfx());
        }
        }

        namespace Domain
        {


        public static class Playback
        {
            private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

            private static string Positions(params uint[] positions)
                => string.Join(",", positions.Select(static position => position.ToString(CultureInfo.InvariantCulture)));

            public static string Blend()
            {
                using var asset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(4f))
                    .Clip(0, 0u, 2u, new DamageClip(8f))
                    .Clip(0, 2u, 4u, new DamageClip(4f))
                    .Clip(0, 6u, 8u, new DamageClip(8f))
                    .Clip(0, 7u, 8u, new DamageClip(0f))
                    .Bake()));
                var positions = new ushort[] { 0 };
                var health = new float[] { 1000f };
                var forward = new List<string>();
                for (var tick = 0; tick < 8; tick++)
                {
                    Timeline<DamageTrack, DamageClip>.Apply(asset, positions, true, health); Timeline.Advance(asset, positions, true);
                    forward.Add(F(health[0]));
                }
                var backward = new List<string>();
                for (var tick = 0; tick < 8; tick++)
                {
                    Timeline<DamageTrack, DamageClip>.Apply(asset, positions, false, health); Timeline.Advance(asset, positions, false);
                    backward.Add(F(health[0]));
                }
                return string.Join("|", forward) + "#" + string.Join("|", backward) + "#" + F(health[0]) + "#" + Positions(positions[0]);
            }

            public static string Rows()
            {
                using var damage = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(4f))
                    .Clip(0, 0u, 2u, new DamageClip(8f))
                    .Bake()));
                using var heal = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<HealTrack, HealClip>(new HealTrack(1f))
                    .Clip(0, 0u, 2u, new HealClip(8f))
                    .Bake()));
                var damagePositions = new ushort[] { 0, 0 };
                var damageHealth = new float[] { 1000f, 250f };
                var healPositions = new ushort[] { 0 };
                var healHealth = new float[] { 500f };
                string Snapshot()
                    => F(damageHealth[0]) + "," + F(healHealth[0]) + "," + F(damageHealth[1]) + ","
                    + Positions(damagePositions[0], healPositions[0], damagePositions[1]);
                Timeline<DamageTrack, DamageClip>.Apply(damage, damagePositions, true, damageHealth); Timeline.Advance(damage, damagePositions, true);
                Timeline<HealTrack, HealClip>.Apply(heal, healPositions, true, healHealth); Timeline.Advance(heal, healPositions, true);
                var first = Snapshot();
                Timeline<DamageTrack, DamageClip>.Apply(damage, damagePositions, true, damageHealth); Timeline.Advance(damage, damagePositions, true);
                Timeline<HealTrack, HealClip>.Apply(heal, healPositions, true, healHealth); Timeline.Advance(heal, healPositions, true);
                var second = Snapshot();
                Timeline<DamageTrack, DamageClip>.Apply(damage, damagePositions, false, damageHealth); Timeline.Advance(damage, damagePositions, false);
                Timeline<HealTrack, HealClip>.Apply(heal, healPositions, false, healHealth); Timeline.Advance(heal, healPositions, false);
                var third = Snapshot();
                return first + "#" + second + "#" + third;
            }

            public static string Missing()
            {
                using var guard = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<GuardTrack, GuardClip>(new GuardTrack(2f))
                    .Clip(0, 0u, 2u, new GuardClip(8f))
                    .Bake()));
                var positions = new ushort[] { 0 };
                var health = new float[] { 400f };
                try
                {
                    Timeline<GuardTrack, GuardClip>.Apply(guard, positions, true, health); Timeline.Advance(guard, positions, true);
                }
                catch (ArgumentException exception)
                {
                    return "THROWN|" + exception.Message + "|" + F(health[0]) + "|" + Positions(positions[0]);
                }
                return "NOTHROWN|" + F(health[0]) + "|" + Positions(positions[0]);
            }

            public static string Standalone()
            {
                using var buff = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<BuffTrack, BuffClip>(new BuffTrack(3f))
                    .Clip(0, 0u, 10u, new BuffClip(5f))
                    .Bake()));
                var positions = new ushort[] { 0 };
                var armor = new float[] { 10f };
                Timeline<BuffTrack, BuffClip>.Apply(buff, positions, true, armor); Timeline.Advance(buff, positions, true);
                var first = F(armor[0]);
                Timeline<BuffTrack, BuffClip>.Apply(buff, positions, true, armor); Timeline.Advance(buff, positions, true);
                var second = F(armor[0]);
                Timeline<BuffTrack, BuffClip>.Apply(buff, positions, false, armor); Timeline.Advance(buff, positions, false);
                var third = F(armor[0]);
                return first + "#" + second + "#" + third;
            }

            public static string Memo()
            {
                using var buff = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<MemoTrack, MemoClip>(new MemoTrack(3f))
                    .Clip(0, 0u, 10u, new MemoClip(5f))
                    .Bake()));
                var positions = new ushort[] { 0 };
                var armor = new float[] { 10f };
                Timeline<MemoTrack, MemoClip>.Apply(buff, positions, true, armor); Timeline.Advance(buff, positions, true);
                var first = F(armor[0]);
                Timeline<MemoTrack, MemoClip>.Apply(buff, positions, true, armor); Timeline.Advance(buff, positions, true);
                var second = F(armor[0]);
                Timeline<MemoTrack, MemoClip>.Apply(buff, positions, false, armor); Timeline.Advance(buff, positions, false);
                var third = F(armor[0]);
                return first + "#" + second + "#" + third;
            }

            public static string DualMemo()
            {
                using var dual = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<DualTrack, DualClip>(new DualTrack(2f))
                    .Clip(0, 0u, 4u, new DualClip(5f, 3))
                    .Bake()));
                var indices = new ushort[] { dual.Index, dual.Index, dual.Index };
                var positions = new ushort[] { 0, 1, 2 };
                var armor = new float[3];
                var ticks = new int[3];
                Timeline<DualTrack, DualClip>.Apply(indices, positions, true, armor, ticks);
                var backPositions = new ushort[] { 1, 2 };
                var backArmor = new float[2];
                var backTicks = new int[2];
                Timeline<DualTrack, DualClip>.Apply(new ushort[] { dual.Index, dual.Index }, backPositions, false, backArmor, backTicks);
                return string.Join(",", armor.Select(F)) + "#" + string.Join(",", ticks.Select(x => x.ToString(CultureInfo.InvariantCulture)))
                    + "#" + string.Join(",", backArmor.Select(F)) + "#" + string.Join(",", backTicks.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            }

            public static string Bakes()
                => Dump<DamageTrack, DamageClip>() + "#" + Dump<HealTrack, HealClip>() + "#" + Dump<BuffTrack, BuffClip>();

            public static string BakeWalk()
            {
                var world = new World();
                var entity = new Entity(world, 7);
                using var view = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(4f))
                    .Clip(0, 0u, 2u, new DamageClip(8f))
                    .Bake()));
                var timeline = view.Index;

                Timeline.Bake(timeline, world, entity);

                var marks = string.Join(",", world.Marks);

                var bare = new World();
                Timeline.Bake(timeline, bare);
                var silent = bare.Marks.Count == 0 ? "silent" : "loud";

                var positions = new ushort[] { 0 };
                var health = new float[] { 1000f };
                for (var frame = 0; frame < 2; frame++)
                {
                    Timeline<DamageTrack, DamageClip>.Apply(timeline, positions, true, health);
                    Timeline.Advance(timeline, positions, true);
                }
                return marks + "#" + silent + "#" + F(health[0]) + "#" + Positions(positions[0]);
            }

            public static string OwnerShape()
            {
                var world = new TlJumpShape.World();
                var entity = new TlJumpShape.Entity(world, 7);
                using var view = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<TlJumpShape.JumpTrack, TlJumpShape.JumpClip>(new TlJumpShape.JumpTrack(1f))
                    .Clip(0, 0u, 2u, new TlJumpShape.JumpClip(3))
                    .Track<TlJumpShape.SoundTrack, TlJumpShape.SoundClip>(new TlJumpShape.SoundTrack(1f))
                    .Clip(1, 0u, 2u, new TlJumpShape.SoundClip(2))
                    .Bake()));

                Timeline.Bake(view.Index, world, entity);

                return string.Join(",", world.Attached.OrderBy(static item => item, StringComparer.Ordinal));
            }

            public static string RefFlow()
            {
                var world = new World();
                var entity = new Entity(world, 41);
                using var view = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<BuffTrack, BuffClip>(new BuffTrack(3f))
                    .Clip(0, 0u, 4u, new BuffClip(5f))
                    .Bake()));

                Timeline.Bake(view.Index, entity);

                return entity.Id.ToString(CultureInfo.InvariantCulture);
            }

            public static string MixedAsset()
            {
                var world = new World();
                var entity = new Entity(world, 5);
                using var view = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
                    .Clip(0, 0u, 1u, new DamageClip(1f))
                    .Track<GuardTrack, GuardClip>(new GuardTrack(1f))
                    .Clip(1, 0u, 1u, new GuardClip(1f))
                    .Bake()));

                Timeline.Bake(view.Index, world, entity, new GuardToken(2));

                return string.Join(",", world.Marks.OrderBy(static item => item, StringComparer.Ordinal)) + "#"
                    + string.Join(",", world.Tags.Select(static tag => tag.Entity.ToString(CultureInfo.InvariantCulture)));
            }

            public static string BatchSpan()
            {
                var world = new World();
                Span<Entity> entities = [new Entity(world, 1), new Entity(world, 2), new Entity(world, 3)];
                using var view = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<SpawnTrack, SpawnClip>(new SpawnTrack(1))
                    .Clip(0, 0u, 2u, new SpawnClip(1))
                    .Bake()));

                Timeline.Bake(view.Index, world, entities);

                return string.Join(",", world.Marks.OrderBy(static item => item, StringComparer.Ordinal));
            }

            public static string ConsumerParameter()
            {
                var world = new World();
                using var view = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                    .Track<ProbeTrack, ProbeClip>(new ProbeTrack(1))
                    .Clip(0, 0u, 2u, new ProbeClip(1))
                    .Bake()));

                Timeline.Bake(view.Index, world);

                return string.Join(",", world.Marks);
            }

            public static string BakePairOrder()
            {
                var world = new World();
                var entity = new Entity(world, 3);
                var timeline = TimelineAsset.Load(new DomainBaker()
                    .Track<DamageTrack, DamageClip>(new DamageTrack(1f))
                    .Clip(0, 0u, 1u, new DamageClip(1f))
                    .Track<HealTrack, HealClip>(new HealTrack(1f))
                    .Clip(1, 0u, 1u, new HealClip(1f))
                    .Bake());
                Timeline.Bake(timeline, world, entity);
                return string.Join(",", world.Marks);
            }

            public static unsafe string ManualBake()
            {
                BakeRuntime<GuardTrack, GuardClip>.Bake(&ManualGuardBakeA, TypeKey<World>.Value);
                BakeRuntime<GuardTrack, GuardClip>.Bake(&ManualGuardBakeB, TypeKey<Entity>.Value);
                var order = Dump<GuardTrack, GuardClip>();
                string loud;
                try
                {
                    _ = BakeRuntime<GuardTrack, GuardClip>.BakeParameterCount(99);
                    loud = "silent";
                }
                catch (ArgumentOutOfRangeException)
                {
                    loud = "ArgumentOutOfRangeException";
                }
                return order + "#" + loud;
            }

            static unsafe void ManualGuardBakeA(byte** __tlArgs) { }

            static unsafe void ManualGuardBakeB(byte** __tlArgs) { }

            static string Dump<TTrack, TClip>()
                where TTrack : unmanaged, IBlend<TClip>
                where TClip : unmanaged
            {
                var parts = new List<string> { BakeRuntime<TTrack, TClip>.BakeCount.ToString(CultureInfo.InvariantCulture) };
                for (var index = 0; index < BakeRuntime<TTrack, TClip>.BakeCount; index++)
                {
                    var contexts = BakeRuntime<TTrack, TClip>.BakeParameterCount(index);
                    var names = new List<string>();
                    for (var context = 0; context < contexts; context++)
                        names.Add(Key(BakeRuntime<TTrack, TClip>.BakeParameterKey(index, context)));
                    parts.Add(contexts.ToString(CultureInfo.InvariantCulture) + "|" + string.Join("+", names));
                }
                return string.Join(";", parts);
            }

            static string Key(ulong value)
                => value == TypeKey<World>.Value ? "World"
                : value == TypeKey<Entity>.Value ? "Entity"
                : value == TypeKey<GuardToken>.Value ? "GuardToken"
                : value.ToString("X16", CultureInfo.InvariantCulture);
        }
        }
        """;
}
