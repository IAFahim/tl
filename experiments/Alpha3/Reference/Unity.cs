using Unity.Burst;
using Unity.Entities;

namespace UnifiedTimelineProof
{
    public struct TimelineComponent : IComponentData { public TimelineState Value; }
    public struct ResistanceComponent : IComponentData { public Resistance Value; }
    public struct HealthComponent : IComponentData { public Health Value; }
    public struct PoseComponent : IComponentData { public Pose Value; }
    public struct DamageActive : IComponentData, IEnableableComponent { }
    public struct AnimationActive : IComponentData, IEnableableComponent { }
    public struct GameClock : IComponentData { public uint Tick; public int Delta; }

    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    [WithAll(typeof(ResistanceComponent), typeof(HealthComponent), typeof(PoseComponent))]
    public partial struct SelectTimelineJob : IJobEntity
    {
        public uint GameTick;
        public int Direction;

        private void Execute(ref TimelineComponent timeline, EnabledRefRW<DamageActive> damage, EnabledRefRW<AnimationActive> animation)
        {
            timeline.Value = Timeline.Select(in timeline.Value, GameTick, Direction);
            damage.ValueRW = timeline.Value.Selection.Damage;
            animation.ValueRW = timeline.Value.Selection.Animation;
        }
    }

    [BurstCompile]
    [WithAll(typeof(DamageActive))]
    public partial struct DamageEntityJob : IJobEntity
    {
        private void Execute(in TimelineComponent timeline, in ResistanceComponent resistance, ref HealthComponent health)
            => DamageJob.Execute(in timeline.Value, in resistance.Value, ref health.Value);
    }

    [BurstCompile]
    [WithAll(typeof(AnimationActive))]
    public partial struct AnimationEntityJob : IJobEntity
    {
        private void Execute(in TimelineComponent timeline, in HealthComponent health, ref PoseComponent pose)
            => AnimationJob.Execute(in timeline.Value, in health.Value, ref pose.Value);
    }

    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    [WithAll(typeof(ResistanceComponent), typeof(HealthComponent), typeof(PoseComponent))]
    public partial struct CompleteTimelineJob : IJobEntity
    {
        private void Execute(ref TimelineComponent timeline, EnabledRefRW<DamageActive> damage, EnabledRefRW<AnimationActive> animation)
        {
            timeline.Value = Timeline.Complete(in timeline.Value);
            damage.ValueRW = false;
            animation.ValueRW = false;
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CombatSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<GameClock>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var clock = SystemAPI.GetSingletonRW<GameClock>();
            var delta = clock.ValueRO.Delta;
            var gameTick = clock.ValueRO.Tick;
            var direction = delta < 0 ? -1 : 1;
            var dependency = state.Dependency;

            for (var remaining = delta; remaining != 0; remaining -= direction)
            {
                if (direction < 0)
                    gameTick = unchecked(gameTick - 1);
                dependency = new SelectTimelineJob { GameTick = gameTick, Direction = direction }.ScheduleParallel(dependency);
                if (direction > 0)
                {
                    dependency = new DamageEntityJob().ScheduleParallel(dependency);
                    dependency = new AnimationEntityJob().ScheduleParallel(dependency);
                }
                else
                {
                    dependency = new AnimationEntityJob().ScheduleParallel(dependency);
                    dependency = new DamageEntityJob().ScheduleParallel(dependency);
                }
                dependency = new CompleteTimelineJob().ScheduleParallel(dependency);
                if (direction > 0)
                    gameTick = unchecked(gameTick + 1);
            }

            clock.ValueRW.Tick = gameTick;
            state.Dependency = dependency;
        }
    }

    public static class CombatEntities
    {
        public static Entity Create(EntityManager manager, TimelineAsset asset, in Resistance resistance, in Health health, in Pose pose)
        {
            var entity = manager.CreateEntity(typeof(TimelineComponent), typeof(ResistanceComponent), typeof(HealthComponent),
                typeof(PoseComponent), typeof(DamageActive), typeof(AnimationActive));
            manager.SetComponentData(entity, new TimelineComponent { Value = new TimelineState(asset) });
            manager.SetComponentData(entity, new ResistanceComponent { Value = resistance });
            manager.SetComponentData(entity, new HealthComponent { Value = health });
            manager.SetComponentData(entity, new PoseComponent { Value = pose });
            manager.SetComponentEnabled<DamageActive>(entity, false);
            manager.SetComponentEnabled<AnimationActive>(entity, false);
            return entity;
        }
    }
}
