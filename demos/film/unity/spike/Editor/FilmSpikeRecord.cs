using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

// Spike: the film is a Tl timeline in Unity, drawn by UI Toolkit into a RenderTexture, recorded to MP4 by Unity Recorder.
public class FilmSpikeRecord
{
    [UnityTest]
    public IEnumerator FilmSpike()
    {
        var builder = Type.GetType("FilmSpikeBuilder, Assembly-CSharp-Editor");
        Assert.IsNotNull(builder, "FilmSpikeBuilder is compiled");
        var scene = (string)builder.GetMethod("Build").Invoke(null, null);
        EditorSceneManager.OpenScene(scene);
        var seconds = double.Parse(Environment.GetEnvironmentVariable("FILM_SECONDS") ?? "24");
        var output = Path.GetFullPath(Path.Combine("Logs", "film-spike"));
        Directory.CreateDirectory(output);
        var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/FilmSpike/Film.renderTexture");
        Assert.IsNotNull(texture, "the film render texture exists");

        yield return new EnterPlayMode();

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Film";
        movie.Enabled = true;
        movie.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movie.VideoBitRateMode = VideoBitrateMode.High;
        movie.ImageInputSettings = new RenderTextureInputSettings { RenderTexture = texture };
        movie.AudioInputSettings.PreserveAudio = false;
        movie.OutputFile = Path.Combine(output, "film");
        controllerSettings.AddRecorderSettings(movie);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 30f;
        controllerSettings.CapFrameRate = true;
        var controller = new RecorderController(controllerSettings);
        var noRecorder = Environment.GetEnvironmentVariable("FILM_NORECORD") == "1";
        if (!noRecorder)
        {
            controller.PrepareRecording();
            Assert.IsTrue(controller.StartRecording(), "the recorder started");
        }
        else Time.captureFramerate = 30;

        var frames = (int)(seconds * 30);
        var start = Time.frameCount;
        var deadline = Time.realtimeSinceStartup + 600f;
        var snapshotAt = start + frames / 2;
        var frameFolder = Path.Combine(output, "frames");
        Directory.CreateDirectory(frameFolder);
        var seen = 0;
        while (Time.frameCount - start < frames && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            if (noRecorder)
            {
                var shot = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                RenderTexture.active = texture;
                shot.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                shot.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(frameFolder, "f" + (Time.frameCount - start).ToString("D5") + ".png"), shot.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(shot);
            }
            if (Time.frameCount >= snapshotAt && seen++ == 0)
            {
                var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                RenderTexture.active = texture;
                copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                copy.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(output, "mid.png"), copy.EncodeToPNG());
            }
        }
        if (!noRecorder) controller.StopRecording();
        var wait = Time.realtimeSinceStartup + 60f;
        while (controller.IsRecording() && Time.realtimeSinceStartup < wait)
            yield return null;
        for (var i = 0; i < 30; i++) yield return null;
        yield return new ExitPlayMode();

        var files = Directory.GetFiles(output, "film*.mp4");
        Debug.Log("FilmSpike recorded " + (Time.frameCount - start) + " frames, files: " + string.Join(", ", files));
        if (!noRecorder) Assert.IsNotEmpty(files, "the recorder wrote an mp4");
    }
}
