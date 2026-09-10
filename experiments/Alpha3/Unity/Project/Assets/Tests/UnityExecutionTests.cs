using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Burst;
using Unity.Entities;

namespace UnifiedTimelineProof.Tests
{
    public sealed class UnityExecutionTests
    {
        [Test]
        public void EnabledStagesPreserveForwardAndReverseOrderAcrossEntities()
        {
            Assert.IsTrue(BurstCompiler.IsEnabled);
            using (var world = new World("alpha3 Unity execution"))
            {
                var manager = world.EntityManager;
                var clock = manager.CreateEntity(typeof(GameClock));
                manager.SetComponentData(clock, new GameClock { Tick = 200000u, Delta = 4 });
                var resistance = new Resistance { Scale = 0.5f };
                var health = new Health { Value = 100f };
                var pose = default(Pose);
                var attack = CombatEntities.Create(manager, Attack.Asset, in resistance, in health, in pose);
                var heavy = CombatEntities.Create(manager, HeavyAttack.Asset, in resistance, in health, in pose);
                var incomplete = manager.CreateEntity(typeof(TimelineComponent), typeof(ResistanceComponent),
                    typeof(PoseComponent), typeof(StageReceiptComponent), typeof(DamageActive), typeof(AnimationActive));
                manager.SetComponentData(incomplete, new TimelineComponent { Value = new TimelineState(Attack.Asset) });
                manager.SetComponentData(incomplete, new ResistanceComponent { Value = resistance });
                manager.SetComponentData(incomplete, new PoseComponent());
                manager.SetComponentEnabled<DamageActive>(incomplete, false);
                manager.SetComponentEnabled<AnimationActive>(incomplete, false);

                Assert.IsFalse(manager.IsComponentEnabled<DamageActive>(attack));
                Assert.IsFalse(manager.IsComponentEnabled<AnimationActive>(attack));
                var system = world.CreateSystem<CombatSystem>();
                system.Update(world.Unmanaged);
                manager.CompleteAllTrackedJobs();

                AssertEntity(manager, attack, 4u, 90f, 380f, 1, 4);
                AssertEntity(manager, heavy, 4u, 80f, 360f, 1, 4);
                Assert.AreEqual(0u, manager.GetComponentData<TimelineComponent>(incomplete).Value.Playback.Position);
                Assert.AreEqual(0, manager.GetComponentData<StageReceiptComponent>(incomplete).DamageCalls);
                Assert.AreEqual(0, manager.GetComponentData<StageReceiptComponent>(incomplete).AnimationCalls);
                Assert.IsFalse(manager.IsComponentEnabled<DamageActive>(incomplete));
                Assert.IsFalse(manager.IsComponentEnabled<AnimationActive>(incomplete));
                Assert.AreEqual(200004u, manager.GetComponentData<GameClock>(clock).Tick);

                manager.SetComponentData(clock, new GameClock { Tick = 200004u, Delta = -4 });
                system.Update(world.Unmanaged);
                manager.CompleteAllTrackedJobs();

                AssertEntity(manager, attack, 0u, 100f, 0f, 2, 8);
                AssertEntity(manager, heavy, 0u, 100f, 0f, 2, 8);
                Assert.AreEqual(0u, manager.GetComponentData<TimelineComponent>(incomplete).Value.Playback.Position);
                Assert.AreEqual(200000u, manager.GetComponentData<GameClock>(clock).Tick);
            }
        }

        [Test]
        public void SchemaGateRunsDamageOnlyAndRejectsIncompleteMixedAsset()
        {
            using (var world = new World("alpha3 schema gate"))
            {
                var manager = world.EntityManager;
                var clock = manager.CreateEntity(typeof(GameClock));
                manager.SetComponentData(clock, new GameClock { Tick = 50u, Delta = 1 });
                var resistance = new Resistance { Scale = 0.5f };
                var valid = manager.CreateEntity(typeof(TimelineComponent), typeof(ResistanceComponent), typeof(HealthComponent),
                    typeof(StageReceiptComponent), typeof(DamageActive), typeof(AnimationActive));
                manager.SetComponentData(valid, new TimelineComponent { Value = new TimelineState(DamageOnlyAttack.Asset) });
                manager.SetComponentData(valid, new ResistanceComponent { Value = resistance });
                manager.SetComponentData(valid, new HealthComponent { Value = new Health { Value = 100f } });
                manager.SetComponentEnabled<DamageActive>(valid, false);
                manager.SetComponentEnabled<AnimationActive>(valid, false);

                var invalid = manager.CreateEntity(typeof(TimelineComponent), typeof(ResistanceComponent), typeof(HealthComponent),
                    typeof(StageReceiptComponent), typeof(DamageActive), typeof(AnimationActive));
                manager.SetComponentData(invalid, new TimelineComponent { Value = new TimelineState(Attack.Asset) });
                manager.SetComponentData(invalid, new ResistanceComponent { Value = resistance });
                manager.SetComponentData(invalid, new HealthComponent { Value = new Health { Value = 100f } });
                manager.SetComponentEnabled<DamageActive>(invalid, true);
                manager.SetComponentEnabled<AnimationActive>(invalid, false);

                world.CreateSystem<CombatSystem>().Update(world.Unmanaged);
                manager.CompleteAllTrackedJobs();

                Assert.IsFalse(manager.HasComponent<PoseComponent>(valid));
                Assert.AreEqual(1u, manager.GetComponentData<TimelineComponent>(valid).Value.Playback.Position);
                Assert.AreEqual(90f, manager.GetComponentData<HealthComponent>(valid).Value.Value);
                Assert.AreEqual(1, manager.GetComponentData<StageReceiptComponent>(valid).DamageCalls);
                Assert.AreEqual(0, manager.GetComponentData<StageReceiptComponent>(valid).AnimationCalls);
                Assert.AreEqual(0u, manager.GetComponentData<TimelineComponent>(invalid).Value.Playback.Position);
                Assert.AreEqual(100f, manager.GetComponentData<HealthComponent>(invalid).Value.Value);
                Assert.AreEqual(0, manager.GetComponentData<StageReceiptComponent>(invalid).DamageCalls);
                Assert.IsFalse(manager.IsComponentEnabled<DamageActive>(invalid));
            }
        }

        [Test]
        public void CompletedEntityDoesNotBlockLiveEntity()
        {
            using (var world = new World("alpha3 mixed completion"))
            {
                var manager = world.EntityManager;
                var clock = manager.CreateEntity(typeof(GameClock));
                manager.SetComponentData(clock, new GameClock { Tick = 90u, Delta = 1 });
                var resistance = new Resistance { Scale = 0.5f };
                var health = new Health { Value = 100f };
                var pose = default(Pose);
                var completed = CombatEntities.Create(manager, Attack.Asset, in resistance, in health, in pose);
                var live = CombatEntities.Create(manager, Attack.Asset, in resistance, in health, in pose);
                var state = manager.GetComponentData<TimelineComponent>(completed).Value;
                for (var tick = 0u; tick < 4u; tick++)
                {
                    var selected = Select(state, tick);
                    state = Timeline.Complete(in selected);
                }
                manager.SetComponentData(completed, new TimelineComponent { Value = state });

                world.CreateSystem<CombatSystem>().Update(world.Unmanaged);
                manager.CompleteAllTrackedJobs();

                Assert.AreEqual(4u, manager.GetComponentData<TimelineComponent>(completed).Value.Playback.Position);
                Assert.AreEqual(1u, manager.GetComponentData<TimelineComponent>(live).Value.Playback.Position);
                Assert.AreEqual(100f, manager.GetComponentData<PoseComponent>(live).Value.X);
                Assert.AreEqual(0, manager.GetComponentData<StageReceiptComponent>(completed).AnimationCalls);
                Assert.AreEqual(1, manager.GetComponentData<StageReceiptComponent>(live).AnimationCalls);
            }
        }

        [Test]
        public void ScheduledJobsRetainNoBorrowedFrame()
        {
            Assert.IsTrue(typeof(Frame<DamageTrack, DamageClip>).IsByRefLike);
            Assert.IsTrue(typeof(Frame<AnimationTrack, AnimationClip>).IsByRefLike);
            AssertNoByRefLikeField(typeof(SelectTimelineJob));
            AssertNoByRefLikeField(typeof(SelectDamageOnlyTimelineJob));
            AssertNoByRefLikeField(typeof(ClearTimelineStagesJob));
            AssertNoByRefLikeField(typeof(DamageEntityJob));
            AssertNoByRefLikeField(typeof(AnimationEntityJob));
            AssertNoByRefLikeField(typeof(CompleteTimelineJob));
            AssertNoByRefLikeField(typeof(CombatSystem));
        }

        private static TimelineState Select(TimelineState state, uint tick)
            => Timeline.Select(in state, tick, 1);

        private static void AssertEntity(EntityManager manager, Entity entity, uint position, float health, float pose, int damageCalls, int animationCalls)
        {
            Assert.AreEqual(position, manager.GetComponentData<TimelineComponent>(entity).Value.Playback.Position);
            Assert.AreEqual(health, manager.GetComponentData<HealthComponent>(entity).Value.Value);
            Assert.AreEqual(pose, manager.GetComponentData<PoseComponent>(entity).Value.X);
            Assert.AreEqual(damageCalls, manager.GetComponentData<StageReceiptComponent>(entity).DamageCalls);
            Assert.AreEqual(animationCalls, manager.GetComponentData<StageReceiptComponent>(entity).AnimationCalls);
            Assert.IsFalse(manager.IsComponentEnabled<DamageActive>(entity));
            Assert.IsFalse(manager.IsComponentEnabled<AnimationActive>(entity));
        }

        private static void AssertNoByRefLikeField(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                Assert.IsFalse(field.FieldType.IsByRefLike, type.FullName + "." + field.Name);
        }
    }
}
