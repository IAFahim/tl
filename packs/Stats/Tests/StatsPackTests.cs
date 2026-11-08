using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;

namespace Tl.Pack.Stats.Tests
{
    public class StatsPackTests
    {
        const ulong Pinned = 12183504288633381751UL;
        const ushort Duel = 1;
        const ushort Haste = 2;
        const int Steps = 600;

        static ulong Mix(ulong h, int v) => (h ^ (uint)v) * 1099511628211UL;

        [Test]
        public void TheDuelGivesTheDemoCheckpointsAndTheDotNetHash()
        {
            using var library = global::Tl.Unity.TlLibrary.FromBytes(StatsLibraryBytes.Bytes());
            using var world = new World("TlStatsPack");
            var entities = world.EntityManager;
            library.Publish(entities);
            var pack = typeof(StatsDamage).Assembly;
            var tick = world.CreateSystem<global::Tl.Unity.TimelineKindTickSystem>();
            var apply = world.CreateSystem(typeof(global::Tl.Unity.TlKindSystem<>).MakeGenericType(pack.GetType("Tl.Generated.TlAuto_Tl_Pack_Stats_Jobs", true)));
            var types = new List<ComponentType> { typeof(global::Tl.Unity.Playback), typeof(global::Tl.Unity.TimelineRequest), typeof(global::Tl.Unity.TimelinePlaying), typeof(Health), typeof(Attack) };
            types.AddRange(StatsDamageKind.Types);
            types.AddRange(StatsAttackBonusKind.Types);
            var hero = entities.CreateEntity(types.ToArray());
            entities.SetComponentEnabled<global::Tl.Unity.TimelinePlaying>(hero, false);
            StatsDamageKind.Init(entities, hero);
            StatsAttackBonusKind.Init(entities, hero);
            entities.SetComponentData(hero, new Health { Value = 100, Max = 100 });
            entities.SetComponentData(hero, new Attack { Base = 10 });
            var h = 14695981039346656037UL;
            long warm = 0;
            var seen = new Dictionary<int, (int, int)>();
            for (var s = 0; s < Steps; s++)
            {
                if (s == 0) entities.GetBuffer<global::Tl.Unity.TimelineRequest>(hero).Add(global::Tl.Unity.TimelineRequest.Start(0, Duel, 0));
                if (s == 100 || s == 250) entities.GetBuffer<global::Tl.Unity.TimelineRequest>(hero).Add(global::Tl.Unity.TimelineRequest.Start(0, Haste, 0));
                var before = GC.GetAllocatedBytesForCurrentThread();
                tick.Update(world.Unmanaged);
                apply.Update(world.Unmanaged);
                entities.CompleteAllTrackedJobs();
                var delta = GC.GetAllocatedBytesForCurrentThread() - before;
                if (s >= 10) warm += delta;
                var health = entities.GetComponentData<Health>(hero);
                var attack = entities.GetComponentData<Attack>(hero);
                h = Mix(Mix(Mix(h, health.Value), attack.Base), attack.Bonus);
                if (s + 1 == 200 || s + 1 == 450 || s + 1 == 600) seen[s + 1] = (health.Value, attack.Value);
            }
            Assert.AreEqual((64, 20), seen[200]);
            Assert.AreEqual((53, 20), seen[450]);
            Assert.AreEqual((72, 10), seen[600]);
            Assert.AreEqual(0L, warm);
            Assert.AreEqual(Pinned, h);
        }
    }
}
