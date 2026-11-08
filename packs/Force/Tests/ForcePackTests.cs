using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tl.Unity;
using Unity.Entities;
using ForceState = Tl.Pack.Force.Force;

namespace Tl.Pack.Force.Tests
{
    public class ForcePackTests
    {
        const ulong Pinned = 7683521458411619355UL;
        const int Rows = 8;
        const int Steps = 600;

        static uint Next(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        static ulong Mix(ulong h, float v) { var s = new global::Tl.TlStateHash { Value = h }; s.FloatLane(v); return s.Value; }

        [Test]
        public void ThePackStateIsAComponentAndItsKindRunsAsJobs()
        {
            var pack = typeof(ForceKind).Assembly;
            Assert.IsTrue(typeof(IComponentData).IsAssignableFrom(typeof(ForceState)));
            Assert.IsNotNull(pack.GetType("Tl.Generated.TlAuto_Tl_Pack_Force_Jobs"));
        }

        [Test]
        public void SixHundredMixedStepsGiveTheDotNetHashAndAllocateNothingWarm()
        {
            using var library = TlLibrary.FromBytes(ForceLibraryBytes.Bytes());
            using var world = new World("TlForcePack");
            var entities = world.EntityManager;
            library.Publish(entities);
            var pack = typeof(ForceKind).Assembly;
            var tick = world.CreateSystem<TimelineKindTickSystem>();
            var apply = world.CreateSystem(typeof(TlKindSystem<>).MakeGenericType(pack.GetType("Tl.Generated.TlAuto_Tl_Pack_Force_Jobs", true)));
            var rows = new Entity[Rows];
            for (var e = 0; e < Rows; e++)
            {
                var types = new List<ComponentType> { typeof(global::Tl.Unity.Playback), typeof(global::Tl.Unity.TimelineRequest), typeof(global::Tl.Unity.TimelinePlaying), typeof(ForceState) };
                types.AddRange(ForceKindKind.Types);
                rows[e] = entities.CreateEntity(types.ToArray());
                entities.SetComponentEnabled<global::Tl.Unity.TimelinePlaying>(rows[e], false);
                ForceKindKind.Init(entities, rows[e]);
            }
            var step = entities.CreateEntityQuery(typeof(global::Tl.Unity.TimelineStep));
            var forwardNow = true;
            var rng = 0x6D2B79F5u;
            var h = global::Tl.TlStateHash.Seed;
            var moved = false;
            long warm = 0;
            for (var s = 0; s < Steps; s++)
            {
                var forward = !(s >= 300 && s < 340);
                if (forward != forwardNow)
                {
                    step.SetSingleton(new global::Tl.Unity.TimelineStep { Forward = forward });
                    forwardNow = forward;
                }
                for (var e = 0; e < Rows; e++)
                {
                    var r = Next(ref rng);
                    switch (r & 7)
                    {
                        case 0:
                            entities.GetBuffer<global::Tl.Unity.TimelineRequest>(rows[e]).Add(global::Tl.Unity.TimelineRequest.Start(0, (ushort)(1 + (r >> 8) % 3), 0));
                            break;
                        case 1:
                            if (((r >> 8) & 3) == 0) entities.GetBuffer<global::Tl.Unity.TimelineRequest>(rows[e]).Add(global::Tl.Unity.TimelineRequest.Stop(0, (ushort)(1 + (r >> 10) % 3)));
                            break;
                        case 2:
                            if (((r >> 8) & 31) == 0) entities.GetBuffer<global::Tl.Unity.TimelineRequest>(rows[e]).Add(global::Tl.Unity.TimelineRequest.StopAll(0));
                            break;
                    }
                }
                var before = GC.GetAllocatedBytesForCurrentThread();
                tick.Update(world.Unmanaged);
                apply.Update(world.Unmanaged);
                entities.CompleteAllTrackedJobs();
                var delta = GC.GetAllocatedBytesForCurrentThread() - before;
                if (s >= 100) warm += delta;
                for (var e = 0; e < Rows; e++)
                {
                    var force = entities.GetComponentData<ForceState>(rows[e]);
                    moved |= force.Value.x != 0f;
                    h = Mix(h, force.Value.x);
                    h = Mix(h, force.Value.y);
                    h = Mix(h, force.Value.z);
                }
            }
            Assert.IsTrue(moved);
            Assert.AreEqual(0L, warm);
            Assert.AreEqual(Pinned, h);
        }
    }
}
