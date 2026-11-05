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
    public readonly struct StompHit { public readonly float Radius; public readonly Position At; public StompHit(float radius, Position at) { Radius = radius; At = at; } }

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
    public readonly struct Move : ITrack<MoveTrack, MoveClip>
    {
        public static void ExecuteActive(in Frame<MoveTrack, MoveClip> frame, ref Motion m)
        {
            m.X += frame.Direction * frame.Clip.Dx * frame.Track.Mult;
            m.Y += frame.Direction * frame.Clip.Dy * frame.Track.Mult;
        }

        public static void OnClipEnd(in Frame<MoveTrack, MoveClip> frame)
        {
        }
    }

    public readonly struct Jump : ITrack<ArcTrack, ArcClip>
    {
        public static void Fold(in Frame<ArcTrack, ArcClip> frame, out float lift)
            => lift = frame.Direction * frame.Clip.Velocity * frame.Track.Scale;

        public static void ExecuteActive(in Frame<ArcTrack, ArcClip> frame, in float lift, ref Motion m, in JumpPower power)
            => m.Y += lift * power.Value;

        public static void OnClipEnd(in Frame<ArcTrack, ArcClip> frame)
        {
        }
    }

    public readonly struct ChangeStat : ITrack<StatTrack, StatClip>
    {
        public static void OnClipStart(in Frame<StatTrack, StatClip> frame, ref Stats s)
        {
            if (frame.Clip.Which == Stat.JumpCounter)
                s.JumpCounter += frame.Direction * frame.Clip.Delta;
            else
                s.Health += frame.Direction * frame.Clip.Delta;
        }
    }

    public readonly struct InputWindow : ITrack<InputTrack, InputWindowClip>
    {
        public static void ExecuteActive(in Frame<InputTrack, InputWindowClip> frame, in InputState input, ref Requests requests)
        {
            if (frame.Direction > 0 && input.Held == frame.Clip.Button)
                requests.Start(frame.Clip.OnPress.Value);
        }
    }

    public readonly struct Stomp : ITrack<StompTrack, StompClip>
    {
        public static void ExecuteActive(in Frame<StompTrack, StompClip> frame, in Position pos, in HeadIndex head, ref Events<StompHit> hits)
        {
            if (head.Value != 0)
                hits.Raise(new StompHit(frame.Clip.Radius, pos));
        }
    }

    public readonly struct Cues : ITrack<CueTrack, CueClip>
    {
        public static void OnClipStart(in Frame<CueTrack, CueClip> frame, ref Events<Cue> cues)
            => cues.Raise(new Cue(frame.Clip.Voice));
    }

    public readonly struct Footsteps : IMarker<FootstepMarker>
    {
        public static void OnMarker(in MarkerFrame<FootstepMarker> marker, ref Events<Cue> cues)
            => cues.Raise(new Cue(marker.Marker.Step));
    }

    [TimelineSystem]
    public ref partial struct Movement
    {
        public Span<Motion> Motion;
        public ReadOnlySpan<JumpPower> JumpPower;
        public Span<Stats> Stats;
        public ReadOnlySpan<InputState> Input;
        public Requests Requests;
        public Events<Cue> Cues;
    }

    [TimelineSystem]
    public ref partial struct Combat
    {
        public ReadOnlySpan<Position> Position;
        public ReadOnlySpan<HeadIndex> Heads;
        public Events<StompHit> Hits;
    }
}
