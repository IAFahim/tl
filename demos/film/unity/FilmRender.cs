using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// Plays the film scene in Unity at a fixed 30 frames per second (so one frame is one film tick) and saves the UI RenderTexture
// as jpg frames, plus cues.csv: the frame where each voice starts, read from Tl's OnScreen column in Unity.
// Environment: FILM_UNITY_DIR (out/unity/<film>), FILM_FOOTAGE_DIR (out/unity/footage), FILM_FRAMES_DIR (output), FILM_TICKS (frames to save).
public class FilmRender
{
    [UnityTest, Timeout(7200000)]
    public IEnumerator Render()
    {
        var builder = Type.GetType("FilmBuilder, Assembly-CSharp-Editor");
        Assert.IsNotNull(builder, "FilmBuilder is compiled");
        EditorSceneManager.OpenScene((string)builder.GetMethod("Build").Invoke(null, null));
        var frames = int.Parse(Environment.GetEnvironmentVariable("FILM_TICKS") ?? "300");
        var folder = Path.GetFullPath(Environment.GetEnvironmentVariable("FILM_FRAMES_DIR") ?? Path.Combine("Logs", "film-frames"));
        if (Directory.Exists(folder)) foreach (var old in Directory.GetFiles(folder)) File.Delete(old);
        Directory.CreateDirectory(folder);
        var target = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Film/Film.renderTexture");
        Assert.IsNotNull(target, "the film render texture exists");

        yield return new EnterPlayMode();
        Time.captureFramerate = 30;
        var bridge = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch (Exception) { return Type.EmptyTypes; } }).FirstOrDefault(t => t.Name == "FilmBridge");
        var current = bridge.GetField("Current");
        var has = bridge.GetField("Has");
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        var cues = new StringBuilder("frame,voice\n");
        var saved = 0;
        var lastVoice = 0;
        var deadline = Time.realtimeSinceStartup + 3600f;
        while (saved < frames && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            if (!(bool)has.GetValue(null)) continue;
            var state = current.GetValue(null);
            var type = state.GetType();
            if ((int)type.GetField("Scene").GetValue(state) <= 0) continue;
            var voice = (int)type.GetField("Voice").GetValue(state);
            if (voice != 0 && voice != lastVoice) cues.AppendLine(saved + "," + voice);
            lastVoice = voice;
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(folder, "f" + saved.ToString("D5") + ".jpg"), texture.EncodeToJPG(95));
            saved++;
        }
        Time.captureFramerate = 0;
        yield return new ExitPlayMode();
        File.WriteAllText(Path.Combine(folder, "cues.csv"), cues.ToString());
        Debug.Log("FilmRender saved " + saved + " frames to " + folder);
        Assert.AreEqual(frames, saved, "saved every frame");
    }
}
