using System;
using Film;
using Unity.Entities;

// What make_film.py writes to out/unity/<film>/film.json (read with JsonUtility, so plain arrays and public fields only).
[Serializable] public class FilmLine { public int at; public string text; }

[Serializable]
public class FilmScene
{
    public string show, title, sub, text, footage, unity, statBig, statSub;
    public int step;
    public string[] groups, readout;
    public float[] playheadAt, stepAt;
    public FilmLine[] lines;
}

[Serializable] public class FilmSceneTime { public int start, end, firstLine, lastLine; public int[] starts; }
[Serializable] public class FilmLane { public string name; public int[] spans; }
[Serializable] public class FilmCue { public int tick, voice; }
[Serializable] public class FilmStats { public int step, health, max, attack; }
[Serializable] public class FilmBag { public int step, Wood, Stone, Plank, Potion, Sword, Coin, Key, Gem; }
[Serializable] public class FilmFootage { public string name; public int frames; public int[] tick; public float[] x, y, z; }

[Serializable]
public class FilmData
{
    public string film, title, helloCode;
    public int ticks;
    public string[] captions, footageNames;
    public FilmScene[] scenes;
    public FilmSceneTime[] sceneTable;
    public FilmLane[] lanes;
    public FilmCue[] cues;
    public FilmStats[] stats;
    public FilmBag[] inventory;
    public int[] helloSteps;
    public float[] helloValues;
    public FilmFootage[] footage;

    public FilmFootage FootageOf(string name)
    {
        foreach (var f in footage)
            if (f.name == name) return f;
        return null;
    }
}

// The one place the UI looks at Tl: a system copies the OnScreen column into a plain struct each frame.
// Reading a column from managed code outside a system throws when Tl's job is still writing it; a system does not.
public static class FilmBridge
{
    public static OnScreen Current;
    public static bool Has;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class FilmBridgeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var screen in SystemAPI.Query<RefRO<OnScreen>>())
        {
            FilmBridge.Current = screen.ValueRO;
            FilmBridge.Has = true;
        }
    }
}
