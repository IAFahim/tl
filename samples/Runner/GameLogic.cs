using System;
using Tl;

namespace Game
{
    public enum Stat { Health, JumpCounter }
    public enum Button { None, Attack, Jump }
    public enum Sound { None, Step, Land, Boom }
    public enum Foot { Left, Right }

    [TimelineSlotKeys]
    public enum Skill { Jump, Slide, Hurt }

    public struct Motion { public float X; public float Y; public Motion(float x, float y) { X = x; Y = y; } }
    public struct Position { public float X; public float Y; public Position(float x, float y) { X = x; Y = y; } }
    public readonly struct JumpPower { public readonly float Value; public JumpPower(float value) { Value = value; } }
    public struct Stats { public int Health; public int JumpCounter; }
    public readonly struct InputState { public readonly Button Held; public InputState(Button held) { Held = held; } }
    public readonly struct HeadIndex { public readonly int Value; public HeadIndex(int value) { Value = value; } }
    public readonly struct Cue { public readonly Sound Voice; public Cue(Sound voice) { Voice = voice; } }
    public struct Trigger { public ushort Timeline; }
    public struct FootstepCue { public Sound Voice; }

    public readonly struct MoveTrack : IBlend<MoveClip>
    {
        public readonly float Mult;
        public MoveTrack(float mult) { Mult = mult; }
        public void Blend(in MoveClip first, in MoveClip second, float factor, out MoveClip result)
            => result = new MoveClip(first.Dx + ((second.Dx - first.Dx) * factor), first.Dy + ((second.Dy - first.Dy) * factor));
    }
    public readonly struct MoveClip { public readonly float Dx; public readonly float Dy; public MoveClip(float dx, float dy) { Dx = dx; Dy = dy; } }
    public readonly struct ArcTrack : IBlend<ArcClip>
    {
        public readonly float Scale;
        public ArcTrack(float scale) { Scale = scale; }
        public void Blend(in ArcClip first, in ArcClip second, float factor, out ArcClip result)
            => result = new ArcClip(first.Velocity + ((second.Velocity - first.Velocity) * factor));
    }
    public readonly struct ArcClip { public readonly float Velocity; public ArcClip(float velocity) { Velocity = velocity; } }
    public readonly struct StatTrack : IBlend<StatClip>
    {
        public readonly Stat Which;
        public StatTrack(Stat which) { Which = which; }
        public void Blend(in StatClip first, in StatClip second, float factor, out StatClip result)
            => result = new StatClip(first.Which, first.Delta + (int)((second.Delta - first.Delta) * factor));
    }
    public readonly struct StatClip { public readonly Stat Which; public readonly int Delta; public StatClip(Stat which, int delta) { Which = which; Delta = delta; } }
    public readonly struct InputTrack : IBlend<InputWindowClip>
    {
        public void Blend(in InputWindowClip first, in InputWindowClip second, float factor, out InputWindowClip result)
            => result = first;
    }
    public readonly struct InputWindowClip { public readonly Button Button; public readonly TimelineId OnPress; public InputWindowClip(Button button, TimelineId onPress) { Button = button; OnPress = onPress; } }
    public readonly struct StompTrack : IBlend<StompClip>
    {
        public void Blend(in StompClip first, in StompClip second, float factor, out StompClip result)
            => result = new StompClip(first.Radius + ((second.Radius - first.Radius) * factor));
    }
    public readonly struct StompClip { public readonly float Radius; public StompClip(float radius) { Radius = radius; } }
    public readonly struct CueTrack : IBlend<CueClip>
    {
        public void Blend(in CueClip first, in CueClip second, float factor, out CueClip result)
            => result = first;
    }
    public readonly struct CueClip { public readonly Sound Voice; public CueClip(Sound voice) { Voice = voice; } }
    public readonly struct FootstepMarker { public readonly Sound Step; public readonly Foot Foot; public FootstepMarker(Sound step, Foot foot) { Step = step; Foot = foot; } }
    [TimelineSystemMember(typeof(Movement))]
    public readonly struct Move : ITrack<MoveTrack, MoveClip>
    {
        [Feeds(typeof(Stride))]
        public static Motion Evaluate(in Frame<MoveTrack, MoveClip> frame)
            => new(frame.Direction * frame.Clip.Dx * frame.Track.Mult, frame.Direction * frame.Clip.Dy * frame.Track.Mult);
    }

    [TimelineKind(Mode = KindMode.Each)]
    public struct Stride
    {
        public static void Apply(in Motion delta, ref Motion motion)
        {
            motion.X += delta.X;
            motion.Y += delta.Y;
        }
    }

    public readonly struct Jump : ITrack<ArcTrack, ArcClip>
    {
        [Feeds(typeof(Lift))]
        public static float Evaluate(in Frame<ArcTrack, ArcClip> frame, in JumpPower power)
            => frame.Direction * frame.Clip.Velocity * frame.Track.Scale * power.Value;
    }

    [TimelineKind(Mode = KindMode.Each)]
    public struct Lift
    {
        public static void Apply(in float lift, ref Motion motion) => motion.Y += lift;
    }

    [TimelineSystemMember(typeof(Movement))]
    public readonly struct ChangeStat : ITrack<StatTrack, StatClip>
    {
        [Pulse(typeof(StatChange), On = ClipEvent.Start)]
        public static StatClip Evaluate(in Frame<StatTrack, StatClip> frame)
            => new(frame.Clip.Which, frame.Direction * frame.Clip.Delta);
    }

    [TimelineKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
    public struct StatChange
    {
        public static void Apply(in StatClip change, ref Stats stats)
        {
            if (change.Which == Stat.JumpCounter)
                stats.JumpCounter += change.Delta;
            else
                stats.Health += change.Delta;
        }
    }

    public readonly struct InputWindow : ITrack<InputTrack, InputWindowClip>
    {
        [Feeds(typeof(Starts))]
        public static ushort Evaluate(in Frame<InputTrack, InputWindowClip> frame, in InputState input)
            => frame.Direction > 0 && input.Held == frame.Clip.Button ? frame.Clip.OnPress.Value : (ushort)0;
    }

    [TimelineKind(Mode = KindMode.Each)]
    public struct Starts
    {
        public static void Apply(in ushort timeline, ref Trigger trigger)
        {
            if (timeline != 0) trigger.Timeline = timeline;
        }
    }

    public readonly struct Stomp : ITrack<StompTrack, StompClip>
    {
        public static void ExecuteActive(in Frame<StompTrack, StompClip> frame, ref Events<float> hits)
            => hits.Raise(frame.Clip.Radius);
    }

    public readonly struct Cues : ITrack<CueTrack, CueClip>
    {
        public static void OnClipStart(in Frame<CueTrack, CueClip> frame, ref Events<Cue> cues)
            => cues.Raise(new Cue(frame.Clip.Voice));
    }

    [TimelineSystemMember(typeof(Movement))]
    public readonly struct Footsteps : IMarker<FootstepMarker>
    {
        [Pulse(typeof(Footstep), On = ClipEvent.Marker)]
        public static Sound Evaluate(in MarkerFrame<FootstepMarker> marker) => marker.Marker.Step;
    }

    [TimelineKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
    public struct Footstep
    {
        public static void Apply(in Sound voice, ref FootstepCue cue) => cue.Voice = voice;
    }

    [TimelineSystem]
    public ref partial struct Movement
    {
        public Span<Motion> Motion;
        public ReadOnlySpan<JumpPower> JumpPower;
        public Span<Stats> Stats;
        public ReadOnlySpan<InputState> Input;
        public Span<Trigger> Triggers;
        public Span<FootstepCue> Footsteps;
    }

    [TimelineSystem]
    public ref partial struct Signals
    {
        public Events<Cue> Cues;
        public Events<float> Hits;
    }
}
