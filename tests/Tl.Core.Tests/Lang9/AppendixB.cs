using System;
using Tl;

namespace Game.Logic
{
    public readonly struct Motion
    {
        public readonly float Y;
        public Motion(float y) { Y = y; }
    }

    public readonly struct JumpPower
    {
        public readonly float Value;
        public JumpPower(float value) { Value = value; }
    }

    public enum Stat
    {
        Health = 0,
        JumpCounter = 1,
    }

    public readonly struct Stats
    {
        public readonly int Health;
        public readonly int JumpCounter;
        public Stats(int health, int jumpCounter) { Health = health; JumpCounter = jumpCounter; }
    }

    public enum Button
    {
        None = 0,
        Attack = 1,
    }

    public readonly struct InputState
    {
        public readonly Button Pressed;
        public InputState(Button pressed) { Pressed = pressed; }
    }

    public readonly struct Position
    {
        public readonly float X;
        public Position(float x) { X = x; }
    }

    public readonly struct Cue
    {
        public readonly int Code;
        public Cue(int code) { Code = code; }
    }

    public readonly struct StompHit
    {
        public readonly int Target;
        public StompHit(int target) { Target = target; }
    }

    public enum Skill
    {
        Jump = 0,
        Slide = 1,
        Hurt = 2,
    }

    public readonly struct MoveClip
    {
        public readonly float Delta;
        public MoveClip(float delta) { Delta = delta; }
    }

    public readonly struct Move : IBlend<MoveClip>
    {
        public readonly float Delta;
        public Move(float delta) { Delta = delta; }
        public void Blend(in MoveClip first, in MoveClip second, float factor, out MoveClip result)
        {
            result = new MoveClip(first.Delta + ((second.Delta - first.Delta) * factor));
        }
    }

    public readonly struct ArcClip
    {
        public readonly float Velocity;
        public ArcClip(float velocity) { Velocity = velocity; }
    }

    public readonly struct Arc : IBlend<ArcClip>
    {
        public readonly float Scale;
        public Arc(float scale) { Scale = scale; }
        public void Blend(in ArcClip first, in ArcClip second, float factor, out ArcClip result)
        {
            result = new ArcClip(first.Velocity + ((second.Velocity - first.Velocity) * factor));
        }
    }

    public readonly struct StatClip
    {
        public readonly int Unused;
        public StatClip(int unused) { Unused = unused; }
    }

    public readonly struct StatTrack : IBlend<StatClip>
    {
        public readonly Stat Which;
        public readonly int Delta;
        public StatTrack(Stat which, int delta) { Which = which; Delta = delta; }
        public void Blend(in StatClip first, in StatClip second, float factor, out StatClip result)
        {
            result = first;
        }
    }

    public readonly struct InputWindowClip
    {
        public readonly int Unused;
        public InputWindowClip(int unused) { Unused = unused; }
    }

    public readonly struct Input : IBlend<InputWindowClip>
    {
        public readonly Button Button;
        public readonly ushort OnPress;
        public Input(Button button, ushort onPress) { Button = button; OnPress = onPress; }
        public void Blend(in InputWindowClip first, in InputWindowClip second, float factor, out InputWindowClip result)
        {
            result = first;
        }
    }

    public readonly struct StompClip
    {
        public readonly float Radius;
        public StompClip(float radius) { Radius = radius; }
    }

    public readonly struct Stomp : IBlend<StompClip>
    {
        public readonly float Radius;
        public Stomp(float radius) { Radius = radius; }
        public void Blend(in StompClip first, in StompClip second, float factor, out StompClip result)
        {
            result = new StompClip(first.Radius + ((second.Radius - first.Radius) * factor));
        }
    }

    public readonly struct CueClip
    {
        public readonly int Unused;
        public CueClip(int unused) { Unused = unused; }
        public void Blend(in CueClip first, in CueClip second, float factor, out CueClip result)
        {
            result = first;
        }
    }

    public readonly struct CueTrack : IBlend<CueClip>
    {
        public readonly int Code;
        public CueTrack(int code) { Code = code; }
        public void Blend(in CueClip first, in CueClip second, float factor, out CueClip result)
        {
            result = first;
        }
    }

    public readonly struct FootstepMarker
    {
        public readonly int Step;
        public FootstepMarker(int step) { Step = step; }
    }

    public readonly struct MoveSystem : ITrack<Move, MoveClip>
    {
        public static void ExecuteActive(in Frame<Move, MoveClip> frame, ref Motion motion)
        {
            motion = new Motion(motion.Y + (frame.Direction * frame.Clip.Delta));
        }
    }

    public readonly struct Jump : ITrack<Arc, ArcClip>
    {
        public static void Fold(in Frame<Arc, ArcClip> frame, out float lift)
        {
            lift = frame.Direction * frame.Clip.Velocity * frame.Track.Scale;
        }

        public static void ExecuteActive(in Frame<Arc, ArcClip> frame, in float lift, in JumpPower power, ref Motion motion)
        {
            motion = new Motion(motion.Y + ((lift * power.Value)));
        }
    }

    public readonly struct ChangeStat : ITrack<StatTrack, StatClip>
    {
        public static void ExecuteActive(in Frame<StatTrack, StatClip> frame, ref Stats stats)
        {
            if (frame.Has(FrameFlags.ClipStart) && frame.Track.Which == Stat.JumpCounter)
                stats = new Stats(stats.Health, stats.JumpCounter + (frame.Direction * frame.Track.Delta));
        }
    }

    public readonly struct InputWindow : ITrack<Input, InputWindowClip>
    {
        public static void ExecuteActive(in Frame<Input, InputWindowClip> frame, in InputState input, ref bool fired)
        {
            if (input.Pressed == frame.Track.Button)
                fired = true;
        }
    }

    public readonly struct StompHitSystem : ITrack<Stomp, StompClip>
    {
        public static void ExecuteActive(in Frame<Stomp, StompClip> frame, in Position self, ref StompHit hit)
        {
            if (self.X < frame.Track.Radius)
                hit = new StompHit((int)self.X);
        }
    }

    public readonly struct Cues : ITrack<CueTrack, CueClip>
    {
        public static void ExecuteActive(in Frame<CueTrack, CueClip> frame, ref Cue cue)
        {
            if (frame.Has(FrameFlags.ClipStart))
                cue = new Cue(frame.Track.Code);
        }
    }

    public readonly struct AttachJumping : IBake<MoveSystem>
    {
        public static void Bake(World world, int entity)
        {
            world.MarkJumping(entity);
        }
    }

    public struct World
    {
        public void MarkJumping(int entity)
        {
        }
    }

    public static class Smoke
    {
        public static void Run(ushort jumpTimeline, ushort[] ids, ushort[] tick, float[] fx)
        {
            ushort[] clocks = new ushort[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                clocks[i] = 0;
            Timeline<Arc, ArcClip>.Apply(ids, clocks, true, fx);
            Timeline.Advance(ids, clocks, true);
            World world = new World();
            Timeline.Bake(jumpTimeline, world, 42);
            Timeline<Arc, ArcClip>.Apply(jumpTimeline, tick, true, fx);
        }
    }
}
