using Tl;

namespace Game
{

public enum Stat : byte
{
    JumpCounter = 0,
}

public enum Button : byte
{
    None = 0,
    Attack = 1,
}

[TimelineSlotKeys]
public enum Skill : byte
{
    Jump = 0,
    Slide = 1,
    Hurt = 2,
}

public struct Motion
{
    public float Y;
    public Motion(float y) { Y = y; }
}

public struct Position
{
    public float X;
    public Position(float x) { X = x; }
}

public struct JumpPower
{
    public float Value;
    public JumpPower(float value) { Value = value; }
}

public struct Stats
{
    public int JumpCounter;
    public Stats(int jumpCounter) { JumpCounter = jumpCounter; }
}

public struct InputState
{
    public Button Held;
    public InputState(Button held) { Held = held; }
}

public struct HeadIndex
{
    public int Value;
    public HeadIndex(int value) { Value = value; }
}

public struct Cue
{
    public int Code;
    public Cue(int code) { Code = code; }
}

public struct StompHit
{
    public int Head;
    public float Radius;
    public StompHit(int head, float radius) { Head = head; Radius = radius; }
}

public struct Sound
{
    public int Id;
    public Sound(int id) { Id = id; }
}

public struct Anim
{
    public int Pose;
    public Anim(int pose) { Pose = pose; }
}

public struct Foot
{
    public int Count;
    public Foot(int count) { Count = count; }
}

public struct MoveTrack
{
    public float Gain;
    public MoveTrack(float gain) { Gain = gain; }
}

public struct MoveClip
{
    public float Delta;
    public MoveClip(float delta) { Delta = delta; }
}

public struct ArcTrack : IBlend<ArcClip>
{
    public float Scale;
    public ArcTrack(float scale) { Scale = scale; }
    public void Blend(in ArcClip first, in ArcClip second, float factor, out ArcClip result)
    {
        result = new ArcClip(first.Velocity + (second.Velocity - first.Velocity) * factor);
    }
}

public struct ArcClip
{
    public float Velocity;
    public ArcClip(float velocity) { Velocity = velocity; }
}

public struct StatTrack
{
    public float Weight;
    public StatTrack(float weight) { Weight = weight; }
}

public struct StatClip
{
    public Stat Stat;
    public int Delta;
    public StatClip(Stat stat, int delta) { Stat = stat; Delta = delta; }
}

public struct InputTrack
{
    public float Window;
    public InputTrack(float window) { Window = window; }
}

public struct InputWindowClip
{
    public Button Button;
    public TimelineId OnPress;
    public InputWindowClip(Button button, TimelineId onPress) { Button = button; OnPress = onPress; }
}

public struct StompTrack
{
    public float Power;
    public StompTrack(float power) { Power = power; }
}

public struct StompClip
{
    public float Radius;
    public StompClip(float radius) { Radius = radius; }
}

public struct CueTrack
{
    public float Volume;
    public CueTrack(float volume) { Volume = volume; }
}

public struct CueClip
{
    public int Code;
    public CueClip(int code) { Code = code; }
}

public struct FootstepMarker
{
    public int Step;
    public FootstepMarker(int step) { Step = step; }
}

public readonly struct Move : ITrack<MoveTrack, MoveClip>
{
    public static void ExecuteActive(in Frame<MoveTrack, MoveClip> frame, ref Motion m)
    {
        m.Y += frame.Direction * frame.Clip.Delta * frame.Track.Gain;
    }

    public static void OnClipEnd(in Frame<MoveTrack, MoveClip> frame)
    {
    }
}

public readonly struct Jump : ITrack<ArcTrack, ArcClip>
{
    public static void Fold(in Frame<ArcTrack, ArcClip> frame, out float lift)
    {
        lift = frame.Direction * frame.Clip.Velocity * frame.Track.Scale;
    }

    public static void ExecuteActive(in Frame<ArcTrack, ArcClip> frame, in float lift, ref Motion m, in JumpPower power)
    {
        m.Y += lift * power.Value;
    }

    public static void OnClipEnd(in Frame<ArcTrack, ArcClip> frame)
    {
    }
}

public readonly struct MoveBatch : ITrack<MoveTrack, MoveClip>
{
    public static void Fold(in Frame<MoveTrack, MoveClip> frame, out float lift)
    {
        lift = frame.Direction * frame.Clip.Delta * frame.Track.Gain;
    }

    public static void ExecuteBatch(in Frame<MoveTrack, MoveClip> frame, global::System.ReadOnlySpan<BatchItem<float>> items, global::System.Span<Motion> motion)
    {
        for (var i = 0; i < items.Length; i++)
            motion[items[i].Row].Y += items[i].Fold;
    }
}

public readonly struct ChangeStat : ITrack<StatTrack, StatClip>
{
    public static void OnClipStart(in Frame<StatTrack, StatClip> frame, ref Stats stats)
    {
        if (frame.Clip.Stat == Stat.JumpCounter) stats.JumpCounter += frame.Direction * frame.Clip.Delta;
    }
}

public readonly struct InputWindow : ITrack<InputTrack, InputWindowClip>
{
    public static void ExecuteActive(in Frame<InputTrack, InputWindowClip> frame, in InputState input, ref Requests requests)
    {
        if (frame.Direction > 0 && input.Held == frame.Clip.Button) requests.Start(frame.Clip.OnPress.Value);
    }
}

public readonly struct Stomp : ITrack<StompTrack, StompClip>
{
    public static void ExecuteActive(in Frame<StompTrack, StompClip> frame, in Position position, in HeadIndex head, ref Events<StompHit> hits)
    {
        if (head.Value + frame.Clip.Radius > position.X) hits.Raise(new StompHit(head.Value, frame.Clip.Radius));
    }

    public static void OnClipEnd(in Frame<StompTrack, StompClip> frame, ref Events<StompHit> hits)
    {
        if (frame.Has(FrameFlags.Interrupted)) hits.Raise(new StompHit(-1, 0f));
    }
}

public readonly struct Cues : ITrack<CueTrack, CueClip>
{
    public static void OnClipStart(in Frame<CueTrack, CueClip> frame, ref Events<Cue> cues)
    {
        cues.Raise(new Cue(frame.Clip.Code));
    }
}

public readonly struct Footsteps : IMarker<FootstepMarker>
{
    public static void OnMarker(in MarkerFrame<FootstepMarker> frame, ref Events<Cue> cues)
    {
        cues.Raise(new Cue(1000 + frame.Marker.Step));
    }
}

[TimelineSystem]
public ref partial struct Movement
{
    public global::System.Span<Motion> Motion;
    public global::System.Span<JumpPower> JumpPower;
    public global::System.Span<Stats> Stats;
    public global::System.Span<InputState> Input;
    public global::System.Span<Anim> Anim;
    public global::System.Span<Foot> Feet;
    public Requests Requests;
    public Events<Cue> Cues;
}

[TimelineSystem]
public ref partial struct Combat
{
    public global::System.Span<Position> Position;
    public global::System.Span<HeadIndex> Heads;
    public global::System.Span<Sound> Sound;
    public Events<StompHit> Hits;
}
}
