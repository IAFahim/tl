using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;

namespace Tl.Pack.Inventory.Tests
{
    public class InventoryPackTests
    {
        const ulong Pinned = 5645869165897381019UL;

        static ulong Mix(ulong h, int v) => (h ^ (uint)v) * 1099511628211UL;

        static ulong Mix(ulong h, in Bag bag)
            => Mix(Mix(Mix(Mix(Mix(Mix(Mix(Mix(h, bag.Wood), bag.Stone), bag.Plank), bag.Potion), bag.Sword), bag.Coin), bag.Key), bag.Gem);

        static Bag Play(int forward, int back, ref ulong h, ref long warm)
        {
            using var library = global::Tl.Unity.TlLibrary.FromBytes(InventoryLibraryBytes.Bytes());
            using var world = new World("TlInventoryPack");
            var entities = world.EntityManager;
            library.Publish(entities);
            var tick = world.CreateSystem<global::Tl.Unity.TimelineKindTickSystem>();
            var apply = world.CreateSystem(typeof(global::Tl.Unity.TlKindSystem<>).MakeGenericType(typeof(Bag).Assembly.GetType("Tl.Generated.TlAuto_Tl_Pack_Inventory_Jobs", true)));
            var types = new List<ComponentType> { typeof(global::Tl.Unity.Playback), typeof(global::Tl.Unity.TimelineRequest), typeof(global::Tl.Unity.TimelinePlaying), typeof(Bag) };
            types.AddRange(InventoryItemsKind.Types);
            var owner = entities.CreateEntity(types.ToArray());
            entities.SetComponentEnabled<global::Tl.Unity.TimelinePlaying>(owner, false);
            InventoryItemsKind.Init(entities, owner);
            entities.GetBuffer<global::Tl.Unity.TimelineRequest>(owner).Add(global::Tl.Unity.TimelineRequest.Start(0, 1, 0));
            var step = entities.CreateEntityQuery(typeof(global::Tl.Unity.TimelineStep));
            var forwardNow = true;
            for (var s = 0; s < forward + back; s++)
            {
                var direction = s < forward;
                if (direction != forwardNow)
                {
                    step.SetSingleton(new global::Tl.Unity.TimelineStep { Forward = direction });
                    forwardNow = direction;
                }
                var before = GC.GetAllocatedBytesForCurrentThread();
                tick.Update(world.Unmanaged);
                apply.Update(world.Unmanaged);
                entities.CompleteAllTrackedJobs();
                var delta = GC.GetAllocatedBytesForCurrentThread() - before;
                if (s >= 10) warm += delta;
                h = Mix(h, entities.GetComponentData<Bag>(owner));
            }
            return entities.GetComponentData<Bag>(owner);
        }

        [Test]
        public void RewindEmptiesTheBagAndTheFinalBagAndHashMatchDotNet()
        {
            var h = 14695981039346656037UL;
            long warm = 0;
            var rewound = Play(450, 450, ref h, ref warm);
            var final = Play(600, 0, ref h, ref warm);
            Assert.AreEqual(default(Bag), rewound);
            Assert.AreEqual(new Bag { Wood = 1, Stone = 3, Plank = 2, Potion = 1, Sword = 1, Coin = 10, Key = 0, Gem = 1 }, final);
            Assert.AreEqual(0L, warm);
            Assert.AreEqual(Pinned, h);
        }
    }
}
