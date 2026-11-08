using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;

namespace Tl.Pack.Motion.Tests
{
    public class MotionPackTests
    {
        const ulong Pinned = 14881653450773647005UL;
        const int Steps = 600;

        static ulong Mix(ulong h, float v) { var s = new global::Tl.TlStateHash { Value = h }; s.Float(v); return s.Value; }

        static ulong Mix(ulong h, in Pose pose, in Tint tint)
        {
            h = Mix(Mix(Mix(h, pose.Position.x), pose.Position.y), pose.Position.z);
            h = Mix(Mix(Mix(h, pose.Scale.x), pose.Scale.y), pose.Scale.z);
            h = Mix(Mix(h, pose.Yaw), pose.Has);
            return Mix(Mix(Mix(Mix(Mix(h, tint.Color.R), tint.Color.G), tint.Color.B), tint.Color.A), tint.Has);
        }

        [Test]
        public void ThreeLoopingObjectsGiveTheDotNetHashAndAllocateNothingWarm()
        {
            using var library = global::Tl.Unity.TlLibrary.FromBytes(MotionLibraryBytes.Bytes());
            using var world = new World("TlMotionPack");
            var entities = world.EntityManager;
            library.Publish(entities);
            var tick = world.CreateSystem<global::Tl.Unity.TimelineKindTickSystem>();
            var apply = world.CreateSystem(typeof(global::Tl.Unity.TlKindSystem<>).MakeGenericType(typeof(Pose).Assembly.GetType("Tl.Generated.TlAuto_Tl_Pack_Motion_Jobs", true)));
            var timelines = new ushort[] { 2, 1, 3 };
            var rows = new Entity[3];
            for (var e = 0; e < 3; e++)
            {
                var types = new List<ComponentType> { typeof(global::Tl.Unity.Playback), typeof(global::Tl.Unity.TimelineRequest), typeof(global::Tl.Unity.TimelinePlaying), typeof(Pose), typeof(Tint) };
                types.AddRange(MotionPositionKind.Types);
                types.AddRange(MotionScaleKind.Types);
                types.AddRange(MotionYawKind.Types);
                types.AddRange(MotionTintKind.Types);
                rows[e] = entities.CreateEntity(types.ToArray());
                entities.SetComponentEnabled<global::Tl.Unity.TimelinePlaying>(rows[e], false);
                MotionPositionKind.Init(entities, rows[e]);
                MotionScaleKind.Init(entities, rows[e]);
                MotionYawKind.Init(entities, rows[e]);
                MotionTintKind.Init(entities, rows[e]);
                entities.GetBuffer<global::Tl.Unity.TimelineRequest>(rows[e]).Add(global::Tl.Unity.TimelineRequest.Start(0, timelines[e], 0));
            }
            var h = global::Tl.TlStateHash.Seed;
            long warm = 0;
            for (var s = 0; s < Steps; s++)
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                tick.Update(world.Unmanaged);
                apply.Update(world.Unmanaged);
                entities.CompleteAllTrackedJobs();
                var delta = GC.GetAllocatedBytesForCurrentThread() - before;
                if (s >= 10) warm += delta;
                for (var e = 0; e < 3; e++)
                    h = Mix(h, entities.GetComponentData<Pose>(rows[e]), entities.GetComponentData<Tint>(rows[e]));
            }
            Assert.AreEqual(5, entities.GetComponentData<Pose>(rows[2]).Has);
            Assert.AreEqual(0L, warm);
            Assert.AreEqual(Pinned, h);
        }
    }
}
