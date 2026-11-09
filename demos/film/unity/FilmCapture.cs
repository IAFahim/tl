using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Tl.Unity;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// Records a demo as PNG frames. Time.captureFramerate = 60 makes every rendered frame exactly 1/60 s, so one frame is one Tl tick.
// ticks.csv: frame, the tick of the first playing timeline, and the position of the first entity that has one.
public class FilmCapture
{
    const int Width = 1280, Height = 720;
    const string Force = "ForceDemoBuilder, Assembly-CSharp-Editor";

    [UnityTest] public IEnumerator Hello() => Record("hello", "HelloTlBuilder, Assembly-CSharp-Editor", "Build", 150);
    [UnityTest] public IEnumerator ForceLap() => Record("force", Force, "Build", 360);
    [UnityTest] public IEnumerator Control() => Record("control", Force, "BuildControl", 240);
    [UnityTest] public IEnumerator Events() => Record("events", "EventsDemoBuilder, Tl.Demo.Events.Editor", "Build", 300);
    [UnityTest] public IEnumerator Motion() => Record("motion", "MotionDemoBuilder, Tl.Demo.Motion.Editor", "Build", 360);
    [UnityTest] public IEnumerator Stats() => Record("stats", "StatsDemoBuilder, Tl.Demo.Stats.Editor", "Build", 300);
    [UnityTest] public IEnumerator Inventory() => Record("inventory", "InventoryDemoBuilder, Tl.Demo.Inventory.Editor", "Build", 300);
    [UnityTest] public IEnumerator Crowd() => Record("crowd", "CrowdDemoBuilder, Tl.Demo.Crowd.Editor", "Build", 240);

    static IEnumerator Record(string name, string builder, string method, int frames)
    {
        var type = Type.GetType(builder);
        Assert.IsNotNull(type, builder + " is compiled");
        var scene = (string)type.GetMethod(method).Invoke(null, null);
        EditorSceneManager.OpenScene(scene);
        var folder = Path.GetFullPath(Path.Combine("Logs", "film", name));
        if (Directory.Exists(folder)) foreach (var old in Directory.GetFiles(folder)) File.Delete(old);
        Directory.CreateDirectory(folder);

        yield return new EnterPlayMode();
        Time.captureFramerate = 60;
        var entities = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = entities.CreateEntityQuery(ComponentType.ReadOnly<Playback>());
        var camera = Camera.main;
        Assert.IsNotNull(camera, "the scene has a main camera");
        var target = new RenderTexture(Width, Height, 24);
        var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        var csv = new StringBuilder("frame,tick,x,y,z\n");
        var frame = 0;
        var deadline = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < deadline && frame < frames)
        {
            yield return new WaitForEndOfFrame();
            if (query.IsEmpty) continue;
            var found = query.ToEntityArray(Allocator.Temp);
            var entity = Entity.Null;
            var tick = 0;
            for (var i = 0; i < found.Length && entity == Entity.Null; i++)
            {
                var playbacks = entities.GetBuffer<Playback>(found[i]);
                if (playbacks.Length > 0 && playbacks[0].Timeline != 0 && (playbacks[0].Flags & global::Tl.PlaybackFlags.StartPending) == 0)
                {
                    entity = found[i];
                    tick = (int)playbacks[0].Tick;
                }
            }
            found.Dispose();
            if (entity == Entity.Null) continue;
            var position = entities.HasComponent<LocalTransform>(entity) ? entities.GetComponentData<LocalTransform>(entity).Position : default;
            if (frame % 2 == 0)
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                camera.targetTexture = null;
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(folder, "f" + (frame / 2).ToString("D4") + ".png"), texture.EncodeToPNG());
                csv.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:0.0000},{3:0.0000},{4:0.0000}", frame / 2, tick, position.x, position.y, position.z));
            }
            frame++;
        }
        Time.captureFramerate = 0;
        yield return new ExitPlayMode();
        File.WriteAllText(Path.Combine(folder, "ticks.csv"), csv.ToString());
        Debug.Log("FilmCapture " + name + ": " + frame + " frames");
        Assert.AreEqual(frames, frame, "recorded all frames for " + name);
    }
}
