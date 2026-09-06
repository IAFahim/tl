namespace Tl;

public enum Direction : sbyte { Reverse = -1, None = 0, Forward = 1 }
public enum Phase : byte { None, Enter, Active, Exit }
public enum Ease : byte { Linear, QuadIn, QuadOut, CubicInOut }
public enum TrackMode : byte { Exclusive, CrossFade }
public enum Status : byte { Completed, More, TooMany, Invalid }

public readonly record struct Sample(
    int Track, int Clip, int Binding, float Weight);

public readonly record struct Frame(
    Sample Sample,
    long RawTick,
    int Tick,
    int Start,
    int End,
    Direction Direction,
    Phase Phase,
    Phase BlendPhase,
    float Progress,
    float BlendFactor);

public readonly record struct ClipEvent(
    long RawTick,
    int Tick,
    int Track,
    int Clip,
    int Binding,
    Direction Direction,
    Phase Phase);

public readonly record struct BlendEvent(
    long RawTick,
    int Tick,
    int Track,
    int ClipA,
    int ClipB,
    int Binding,
    Direction Direction,
    Phase Phase,
    float Factor);

public readonly record struct Traversal(Status Status, long Count);

public interface ISample<T> where T : unmanaged
{
    void Sample(in Sample sample, in T clip);
}

public interface IFrames<T> where T : unmanaged
{
    void Frame(in Frame frame, in T clip);
}

public interface IEvents<T> where T : unmanaged
{
    void OnClip(in ClipEvent transition, in T clip);
    void OnBlend(in BlendEvent transition, in T first, in T second);
}

public interface IOutput<T> : ISample<T>, IEvents<T> where T : unmanaged;
