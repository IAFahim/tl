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
    public void DiscoveredBakesInstallDispatchEntriesIntoTheRuntimeTable()
    {
        var result = Driver("Bakes");

        Assert.Equal("4;1|Entity;2|World+Entity;2|World+Entity;3|World+Entity+GuardToken#1;1|World#0", result);
    }

    [Fact]
    public void ManualInstallsAppendInCallOrderAndBoundsAreLoud()
    {
        var result = Driver("ManualBake");

        Assert.Equal("2;1|World;1|Entity#ArgumentOutOfRangeException", result);
    }

    [Fact]
    public void TimelineBakeAttachesMarkersSubsetsAndGatesAdvancementEndToEnd()
    {
        var result = Driver("BakeWalk");

        Assert.Equal("narrow:7,damage:7#7#silent#936|936#1000#2,2", result);
    }

    [Fact]
    public void TimelineBakeWalksPairsInPairKeyOrder()
    {
        var result = Driver("BakePairOrder");

        Assert.Equal("heal,narrow:3,damage:3", result);
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

        namespace Domain;

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
            public static void Execute(in Frame<DamageTrack, DamageClip> frame, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                health += frame.IsBackward ? amount : -amount;
            }
        }

        public readonly struct ApplyDamageBake : IBake<ApplyDamage, World, Entity>
        {
            public static void Bake(ApplyDamage consumer, World world, Entity entity) { world.Tags.Add(new DamageTag(entity.Id)); }
        }

        public readonly struct ApplyDamageNarrowBake : IBake<ApplyDamage, Entity>
        {
            public static void Bake(ApplyDamage consumer, Entity entity) { entity.Owner.Marks.Add("narrow:" + entity.Id.ToString(CultureInfo.InvariantCulture)); }
        }

        public readonly struct ApplyDamageWideBake : IBake<ApplyDamage, World, Entity>
        {
            public static void Bake(ApplyDamage consumer, World world, Entity entity) { world.Marks.Add("damage:" + entity.Id.ToString(CultureInfo.InvariantCulture)); }
        }

        public readonly struct ApplyDamageGuardBake : IBake<ApplyDamage, World, Entity, GuardToken>
        {
            public static void Bake(ApplyDamage consumer, World world, Entity entity, GuardToken token) { world.Tags.Add(new DamageTag(-entity.Id)); }
        }

        public readonly struct ApplyHeal : ITrack<HealTrack, HealClip>, IBake<ApplyHeal, World>
        {
            public static void Execute(in Frame<HealTrack, HealClip> frame, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                health += frame.IsBackward ? -amount : amount;
            }

            public static void Bake(ApplyHeal consumer, World world) { world.Marks.Add("heal"); }
        }

        public readonly record struct BuffClip(float Amount);

        public readonly record struct BuffTrack(float Multiplier) : IBlend<BuffClip>
        {
            public void Blend(in BuffClip first, in BuffClip second, float factor, out BuffClip result)
                => result = new BuffClip(first.Amount + (second.Amount - first.Amount) * factor);
        }

        public readonly struct ApplyBuff : ITrack<BuffTrack, BuffClip>
        {
            public static void Execute(in Frame<BuffTrack, BuffClip> frame, ref float armor)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier;
                armor += frame.IsBackward ? -amount : amount;
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
            public static void Execute(in Frame<GuardTrack, GuardClip> frame, in Resistance resistance, ref float health)
            {
                var amount = frame.Clip.Amount * frame.Track.Multiplier * resistance.Scale;
                health += frame.IsBackward ? amount : -amount;
            }
        }


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
                    Timeline<DamageTrack, DamageClip>.Advance(asset, positions, true, health);
                    forward.Add(F(health[0]));
                }
                var backward = new List<string>();
                for (var tick = 0; tick < 8; tick++)
                {
                    Timeline<DamageTrack, DamageClip>.Advance(asset, positions, false, health);
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
                Timeline<DamageTrack, DamageClip>.Advance(damage, damagePositions, true, damageHealth);
                Timeline<HealTrack, HealClip>.Advance(heal, healPositions, true, healHealth);
                var first = Snapshot();
                Timeline<DamageTrack, DamageClip>.Advance(damage, damagePositions, true, damageHealth);
                Timeline<HealTrack, HealClip>.Advance(heal, healPositions, true, healHealth);
                var second = Snapshot();
                Timeline<DamageTrack, DamageClip>.Advance(damage, damagePositions, false, damageHealth);
                Timeline<HealTrack, HealClip>.Advance(heal, healPositions, false, healHealth);
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
                    Timeline<GuardTrack, GuardClip>.Advance(guard, positions, true, health);
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
                Timeline<BuffTrack, BuffClip>.Advance(buff, positions, true, armor);
                var first = F(armor[0]);
                Timeline<BuffTrack, BuffClip>.Advance(buff, positions, true, armor);
                var second = F(armor[0]);
                Timeline<BuffTrack, BuffClip>.Advance(buff, positions, false, armor);
                var third = F(armor[0]);
                return first + "#" + second + "#" + third;
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
                var tags = string.Join(",", world.Tags.Select(static tag => tag.Entity.ToString(CultureInfo.InvariantCulture)));

                var bare = new World();
                Timeline.Bake(timeline, bare);
                var silent = bare.Marks.Count == 0 && bare.Tags.Count == 0 ? "silent" : "loud";

                var markedPositions = new ushort[] { 0 };
                var markedHealth = new float[] { 1000f };
                var oraclePositions = new ushort[] { 0 };
                var oracleHealth = new float[] { 1000f };
                var idlePositions = new ushort[] { 0 };
                var idleHealth = new float[] { 1000f };
                var idleWorld = new World();
                Timeline.Bake(timeline, idleWorld);
                for (var frame = 0; frame < 4; frame++)
                {
                    foreach (var tag in world.Tags)
                        Timeline<DamageTrack, DamageClip>.Advance(timeline, markedPositions, true, markedHealth);
                    foreach (var tag in idleWorld.Tags)
                        Timeline<DamageTrack, DamageClip>.Advance(timeline, idlePositions, true, idleHealth);
                    Timeline<DamageTrack, DamageClip>.Advance(view, oraclePositions, true, oracleHealth);
                }
                return marks + "#" + tags + "#" + silent + "#"
                    + F(markedHealth[0]) + "|" + F(oracleHealth[0]) + "#"
                    + F(idleHealth[0]) + "#" + Positions(markedPositions[0], oraclePositions[0]);
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
                    _ = BakeRuntime<GuardTrack, GuardClip>.BakeContextCount(99);
                    loud = "silent";
                }
                catch (ArgumentOutOfRangeException)
                {
                    loud = "ArgumentOutOfRangeException";
                }
                return order + "#" + loud;
            }

            static void ManualGuardBakeA(object[] __tlArgs) { }

            static void ManualGuardBakeB(object[] __tlArgs) { }

            static string Dump<TTrack, TClip>()
                where TTrack : unmanaged, IBlend<TClip>
                where TClip : unmanaged
            {
                var parts = new List<string> { BakeRuntime<TTrack, TClip>.BakeCount.ToString(CultureInfo.InvariantCulture) };
                for (var index = 0; index < BakeRuntime<TTrack, TClip>.BakeCount; index++)
                {
                    var contexts = BakeRuntime<TTrack, TClip>.BakeContextCount(index);
                    var names = new List<string>();
                    for (var context = 0; context < contexts; context++)
                        names.Add(Key(BakeRuntime<TTrack, TClip>.BakeContextKey(index, context)));
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
        """;
}
