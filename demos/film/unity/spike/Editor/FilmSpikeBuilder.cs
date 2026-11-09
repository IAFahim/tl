using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Tl.Generated;
using Tl.Unity;
using Tl.Unity.Editor;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

// Builds the film as a Unity Timeline from the same timelines/release.json that the HyperFrames pipeline uses.
public static class FilmSpikeBuilder
{
    const string Folder = "Assets/FilmSpike";
    const double TicksPerSecond = 30.0;

    [Serializable] class Data { public int Scene, Line, Weight; }
    [Serializable] class JsonClip { public int start, end; public Data data; }
    [Serializable] class JsonTrack { public string type; public JsonClip[] clips; }
    [Serializable] class JsonTimeline { public int duration; public JsonTrack[] tracks; }
    [Serializable] class JsonScene { public string title, text; }
    [Serializable] class JsonScript { public JsonScene[] scenes; }

    public static string Build()
    {
        var dir = Environment.GetEnvironmentVariable("FILM_DIR") ?? throw new InvalidOperationException("set FILM_DIR to demos/film");
        var seconds = double.Parse(Environment.GetEnvironmentVariable("FILM_SECONDS") ?? "24");
        var timeline = JsonUtility.FromJson<JsonTimeline>(File.ReadAllText(Path.Combine(dir, "timelines", "release.json")));
        var script = JsonUtility.FromJson<JsonScript>(File.ReadAllText(Path.Combine(dir, "script", "release.json")));

        var asset = CreateTimeline(timeline, seconds);
        var objects = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var film = new GameObject("Film");
        var director = film.AddComponent<PlayableDirector>();
        director.playableAsset = asset;
        director.playOnAwake = true;
        EditorSceneManager.SaveScene(objects, Folder + "/FilmObjects.unity");

        var main = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var ui = new GameObject("Film UI");
        var document = ui.AddComponent<UIDocument>();
        document.panelSettings = PanelSettingsWithTexture();
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Folder + "/UI/Film.uxml");
        var filmUi = ui.AddComponent<FilmUi>();
        filmUi.Document = document;
        filmUi.SceneTitles = script.scenes.Select(s => s.title).ToArray();
        filmUi.Captions = script.scenes.SelectMany(s => Regex.Split(s.text.Trim(), @"(?<=[.!?])\s+")).ToArray();
        var host = new GameObject("Film Objects");
        var sub = host.AddComponent<SubScene>();
        sub.SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Folder + "/FilmObjects.unity");
        sub.AutoLoadScene = true;
        EditorSceneManager.SaveScene(main, Folder + "/Film.unity");
        AssetDatabase.SaveAssets();
        return Folder + "/Film.unity";
    }

    static PanelSettings PanelSettingsWithTexture()
    {
        var path = Folder + "/Film.renderTexture";
        AssetDatabase.DeleteAsset(path);
        var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { name = "Film" };
        AssetDatabase.CreateAsset(target, path);
        var settings = ScriptableObject.CreateInstance<PanelSettings>();
        settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Folder + "/UI/Film.tss");
        settings.targetTexture = target;
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        settings.clearColor = true;
        settings.colorClearValue = new Color(0.04f, 0.06f, 0.12f, 1f);
        AssetDatabase.DeleteAsset(Folder + "/FilmPanel.asset");
        AssetDatabase.CreateAsset(settings, Folder + "/FilmPanel.asset");
        return settings;
    }

    static TimelineAsset CreateTimeline(JsonTimeline json, double seconds)
    {
        var path = Folder + "/Film.playable";
        AssetDatabase.DeleteAsset(path);
        var timeline = TlTimelines.CreateAsset();
        AssetDatabase.CreateAsset(timeline, path);
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = json.duration / TicksPerSecond;   // the whole film: a clip that runs past the timeline's end makes the bake empty
        var only = Environment.GetEnvironmentVariable("FILM_TRACKS");
        foreach (var source in json.tracks)
        {
            if (!string.IsNullOrEmpty(only) && !only.Split(',').Any(t => source.type.Contains(t))) continue;
            var track = timeline.CreateTrack<TlTrack>(null, source.type.Contains("SceneClip") ? "Scene" : source.type.Contains("CaptionClip") ? "Caption" : source.type.Contains("BeatClip") ? "Beat" : null);
            if (source.type.Contains("SceneClip")) track.Template = new Film_SceneClip_TlData();
            else if (source.type.Contains("CaptionClip")) track.Template = new Film_CaptionClip_TlData();
            else if (source.type.Contains("BeatClip")) track.Template = new Film_BeatClip_TlData();
            else { timeline.DeleteTrack(track); continue; }       // Voice and Footage tracks are not part of this spike
            foreach (var c in source.clips.Where(c => c.start / TicksPerSecond < seconds))
            {
                var clip = track.CreateClip<TlClip>();
                clip.start = c.start / TicksPerSecond;
                clip.duration = (c.end - c.start) / TicksPerSecond;
                var data = ((TlClip)clip.asset).Data;
                if (data is Film_SceneClip_TlData scene) scene.Scene = c.data.Scene;
                else if (data is Film_CaptionClip_TlData caption) caption.Line = c.data.Line;
                else if (data is Film_BeatClip_TlData beat) beat.Weight = c.data.Weight;
                EditorUtility.SetDirty(clip.asset);
            }
            Debug.Log("FilmSpikeBuilder: track " + source.type + " clips " + track.GetClips().Count());
            EditorUtility.SetDirty(track);
        }
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        return timeline;
    }
}
