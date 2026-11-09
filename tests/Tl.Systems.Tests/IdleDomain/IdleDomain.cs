using System;
using Tl;

namespace IdleGame
{

public struct Screen { public int Caption; public int Footage; }

public struct LineClip { public int Line; }
public struct LineTrack : IBlend<LineClip>
{
    public float Scale;
    public void Blend(in LineClip first, in LineClip second, float factor, out LineClip result) => result = first;
}

public struct ReelClip { public int Reel; }
public struct ReelTrack : IBlend<ReelClip>
{
    public float Scale;
    public void Blend(in ReelClip first, in ReelClip second, float factor, out ReelClip result) => result = first;
}

[TimelineKind]
public struct Caption
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int line, ref Screen screen) => screen.Caption = line;
    public static void Idle(ref Screen screen) => screen.Caption = -1;
}

[TimelineKind]
public struct Footage
{
    public static void Combine(ref int total, in int next) => total = next;
    public static void Apply(in int reel, ref Screen screen) => screen.Footage = reel;
}

public struct Lines : ITrack<LineTrack, LineClip>
{
    [Feeds(typeof(Caption))]
    public static int Which(in Frame<LineTrack, LineClip> f) => f.Clip.Line;
}

public struct Reels : ITrack<ReelTrack, ReelClip>
{
    [Feeds(typeof(Footage))]
    public static int Which(in Frame<ReelTrack, ReelClip> f) => f.Clip.Reel;
}

[TimelineSystem]
public ref partial struct Stage
{
    public Span<Screen> Screen;
}

}
