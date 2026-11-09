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

// Which recorded Unity footage plays while this clip is on the timeline (0 means none).
[TlClip("Film/Footage")]
public struct FootageClip { public int Footage; }

// Which UI elements of the current scene are on screen while this clip plays: one bit per element.
// Clips follow each other, so each one carries the whole set (a new element means a new clip).
[TlClip("Film/Reveal")]
public struct RevealClip { public uint Mask; }

// A number that glides from From to To over the clip: a playhead, a step counter, a footage frame.
[TlClip("Film/Sweep")]
public struct SweepClip { public float From; public float To; }

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

public struct FootageTrack : ITlTrack<FootageClip>
{
    [Feeds(typeof(ShowFootage))]
    public static int WhichFootage(in TlFrame<FootageClip> frame) => frame.Clip.Footage;

    [Feeds(typeof(ShowFootageTick))]
    public static int TickInTheClip(in TlFrame<FootageClip> frame) => frame.WithinClip;
}

public struct RevealTrack : ITlTrack<RevealClip>
{
    [Feeds(typeof(ShowElements))]
    public static uint WhichElements(in TlFrame<RevealClip> frame) => frame.Clip.Mask;
}

public struct SweepTrack : ITlTrack<SweepClip>
{
    [Feeds(typeof(MoveSweep))]
    public static float Glide(in TlFrame<SweepClip> frame)
    {
        var p = frame.Progress;
        return frame.Clip.From + (frame.Clip.To - frame.Clip.From) * (p * p * (3f - 2f * p));
    }
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

[TlKind]
public struct ShowFootage
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int footage, ref OnScreen screen) => screen.Footage = footage;
}

[TlKind]
public struct ShowFootageTick
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int tick, ref OnScreen screen) => screen.FootageTick = tick;
}

[TlKind]
public struct ShowElements
{
    public static void Combine(ref uint total, in uint next) => total = next;
    public static void Apply(in uint mask, ref OnScreen screen) => screen.Reveal = mask;
}

[TlKind]
public struct MoveSweep
{
    public static void Combine(ref float total, in float next) => total = next;
    public static void Apply(in float value, ref OnScreen screen) => screen.Sweep = value;
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
    public int Footage;
    public int FootageTick;
    public uint Reveal;
    public float Sweep;
    public int Flashes;
}
