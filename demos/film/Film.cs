using Tl;

namespace Film;

// A video is a timeline. These clips are what the film's editor drops on it.

// Which scene is on screen for these ticks.
[TlClip("Film/Scene")]
public struct SceneClip { public int Scene; }

// Which caption line is on screen for these ticks (0 means no caption).
[TlClip("Film/Caption")]
public struct CaptionClip { public int Line; }

// A voice line starts speaking at the first tick of this clip.
[TlClip("Film/Voice")]
public struct VoiceClip { public int Line; }

// A beat: something on screen flashes once, at the first tick of this clip.
[TlClip("Film/Beat")]
public struct BeatClip { public int Weight; }

public struct SceneTrack : ITlTrack<SceneClip>
{
    [Feeds(typeof(ShowScene))]
    public static int WhichScene(in TlFrame<SceneClip> frame) => frame.Clip.Scene;

    [Feeds(typeof(ScenePhase))]
    public static float HowFarThroughTheScene(in TlFrame<SceneClip> frame) => frame.Progress;
}

public struct CaptionTrack : ITlTrack<CaptionClip>
{
    [Feeds(typeof(ShowCaption))]
    public static int WhichLine(in TlFrame<CaptionClip> frame) => frame.Clip.Line;
}

public struct VoiceTrack : ITlTrack<VoiceClip>
{
    [Pulse(typeof(StartVoice), On = ClipEvent.Start)]
    public static int LineThatStartsNow(in TlFrame<VoiceClip> frame) => frame.Clip.Line;
}

public struct BeatTrack : ITlTrack<BeatClip>
{
    [Pulse(typeof(Flash), On = ClipEvent.Start)]
    public static int FlashWeight(in TlFrame<BeatClip> frame) => frame.Clip.Weight;
}

[TlKind]
public struct ShowScene
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int scene, ref OnScreen screen) => screen.Scene = scene;
}

[TlKind]
public struct ScenePhase
{
    public static void Combine(ref float total, in float next) => total = next;
    public static void Apply(in float phase, ref OnScreen screen) => screen.ScenePhase = phase;
}

[TlKind]
public struct ShowCaption
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int line, ref OnScreen screen) => screen.Caption = line;
}

[TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
public struct StartVoice
{
    public static void Apply(in int line, ref OnScreen screen) => screen.Voice = line;
}

[TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
public struct Flash
{
    public static void Apply(in int weight, ref OnScreen screen) => screen.Flashes += weight;
}

// What the film shows at one tick. The page reads this column for every frame it renders.
[TlState]
public partial struct OnScreen
{
    public int Scene;
    public float ScenePhase;
    public int Caption;
    public int Voice;
    public int Flashes;
}
