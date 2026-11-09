using System;
using System.IO;
using System.Linq;
using Tl.Unity;
using Tl.Unity.Editor;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

// Builds the film as a Unity Timeline from what make_film.py wrote to out/unity/<film>/ (set FILM_UNITY_DIR to that folder).
// Every Tl clip becomes a clip on a Tl track, the voices become clips on an audio track, so the designer can edit the film
// in the Timeline window. Scene: Assets/Film/Film.unity (a SubScene with the director, a UIDocument that renders into a RenderTexture).
public static class FilmBuilder
{
    const string Folder = "Assets/Film";
    const double TicksPerSecond = 30.0;

    [Serializable] class JsonClip { public int start, end; public string data; }
    [Serializable] class JsonTrack { public string type; public JsonClip[] clips; }
    [Serializable] class JsonTimeline { public int duration; public JsonTrack[] tracks; }
    [Serializable] class JsonCues { public FilmCue[] cues; }

    [MenuItem("Tl/Film/Build Scene")]
    public static void BuildAndOpen() => EditorSceneManager.OpenScene(Build());

    public static string Build()
    {
        var dir = Environment.GetEnvironmentVariable("FILM_UNITY_DIR") ?? throw new InvalidOperationException("set FILM_UNITY_DIR to demos/film/out/unity/<film>");
        var timeline = JsonUtility.FromJson<JsonTimeline>(File.ReadAllText(Path.Combine(dir, "timeline.json")));
        var cues = JsonUtility.FromJson<JsonCues>(File.ReadAllText(Path.Combine(dir, "film.json"))).cues;

        AssetDatabase.DeleteAsset(Folder + "/Data");
        AssetDatabase.CreateFolder(Folder, "Data");
        File.Copy(Path.Combine(dir, "film.json"), Folder + "/Data/film.json", true);
        foreach (var mp3 in Directory.GetFiles(Path.Combine(dir, "audio"), "*.mp3"))
            File.Copy(mp3, Folder + "/Data/" + Path.GetFileName(mp3), true);
        AssetDatabase.Refresh();

        var asset = CreateTimeline(timeline, cues);
        var objects = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var director = new GameObject("Film").AddComponent<PlayableDirector>();
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
        filmUi.DataJson = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/Data/film.json");
        var sub = new GameObject("Film Objects").AddComponent<SubScene>();
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
        settings.targetTexture = target;
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        settings.clearColor = true;
        settings.colorClearValue = new Color(0.04f, 0.06f, 0.12f, 1f);
        AssetDatabase.DeleteAsset(Folder + "/FilmPanel.asset");
        AssetDatabase.CreateAsset(settings, Folder + "/FilmPanel.asset");
        return settings;
    }

    static Type TlDataType(string clip) =>
        AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch (Exception) { return Type.EmptyTypes; } })
            .First(t => t.Name == "Film_" + clip + "_TlData");

    static TimelineAsset CreateTimeline(JsonTimeline json, FilmCue[] cues)
    {
        var path = Folder + "/Film.playable";
        AssetDatabase.DeleteAsset(path);
        var timeline = TlTimelines.CreateAsset();
        AssetDatabase.CreateAsset(timeline, path);
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = json.duration / TicksPerSecond;      // a clip past the timeline's end makes the bake empty
        foreach (var source in json.tracks)
        {
            var track = timeline.CreateTrack<TlTrack>(null, source.type.Replace("Clip", ""));
            track.Template = (TlClipData)Activator.CreateInstance(TlDataType(source.type));
            foreach (var c in source.clips)
            {
                var clip = track.CreateClip<TlClip>();
                clip.start = c.start / TicksPerSecond;
                clip.duration = (c.end - c.start) / TicksPerSecond;
                JsonUtility.FromJsonOverwrite(c.data, ((TlClip)clip.asset).Data);
                EditorUtility.SetDirty(clip.asset);
            }
            EditorUtility.SetDirty(track);
        }
        var audio = timeline.CreateTrack<AudioTrack>(null, "Narration");
        foreach (var cue in cues)
        {
            var mp3 = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/Data/voice-" + cue.voice + ".mp3");
            if (mp3 == null) continue;
            var clip = audio.CreateClip(mp3);
            clip.start = cue.tick / TicksPerSecond;
            clip.displayName = "voice " + cue.voice;
        }
        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        return timeline;
    }
}
