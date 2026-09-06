using Tl;

namespace Game.Timelines;

public readonly record struct Pose(float X, float Y);
public readonly record struct Hit(int Damage);

public static class Attack
{
    public const int Duration = 60;
    public const bool Loop = false;

    public struct State
    {
        public readonly long Tick => throw new NotImplementedException();
        public readonly bool IsInitialized => throw new NotImplementedException();
    }

    public struct Token
    {
        public readonly bool IsFresh => throw new NotImplementedException();
    }

    public static State Start(long previousTick = -1)
        => throw new NotImplementedException();

    public static void Update<TOutput>(ref State state, long tick, ref TOutput output)
        where TOutput : struct, IOutput<Pose>, IOutput<Hit>
        => throw new NotImplementedException();

    public static void Seek(ref State state, long tick)
        => throw new NotImplementedException();

    public static void Sample<TOutput>(long tick, ref TOutput output)
        where TOutput : struct, ISample<Pose>, ISample<Hit>
        => throw new NotImplementedException();

    public static void Sample<TOutput>(in State state, ref TOutput output)
        where TOutput : struct, ISample<Pose>, ISample<Hit>
        => throw new NotImplementedException();

    public static void Frames<TOutput>(long tick, Direction direction, ref TOutput output)
        where TOutput : struct, IFrames<Pose>, IFrames<Hit>
        => throw new NotImplementedException();

    public static void Frames<TOutput>(in State state, ref TOutput output)
        where TOutput : struct, IFrames<Pose>, IFrames<Hit>
        => throw new NotImplementedException();

    public static void Advance(ref State state, long tick)
        => throw new NotImplementedException();

    public static void Advance<TEvents>(ref State state, long tick, ref TEvents events)
        where TEvents : struct, IEvents<Pose>, IEvents<Hit>
        => throw new NotImplementedException();

    public static Traversal Traverse<TEvents>(long previousTick, long tick, ref TEvents events)
        where TEvents : struct, IEvents<Pose>, IEvents<Hit>
        => throw new NotImplementedException();

    public static Traversal Traverse<TEvents>(
        long previousTick, long tick, long budget, ref Token token, ref TEvents events)
        where TEvents : struct, IEvents<Pose>, IEvents<Hit>
        => throw new NotImplementedException();

    public static class Tracks
    {
        public static class Body
        {
            public const int Id = 0;
            public const int Binding = 0;
            public const int ClipCount = 2;

            public static ReadOnlySpan<Pose> Payloads
                => throw new NotImplementedException();

            public static void Sample<TOutput>(long tick, ref TOutput output)
                where TOutput : struct, ISample<Pose>
                => throw new NotImplementedException();

            public static void Sample<TOutput>(in State state, ref TOutput output)
                where TOutput : struct, ISample<Pose>
                => throw new NotImplementedException();

            public static void Frames<TOutput>(long tick, Direction direction, ref TOutput output)
                where TOutput : struct, IFrames<Pose>
                => throw new NotImplementedException();

            public static Traversal Traverse<TEvents>(long previousTick, long tick, ref TEvents events)
                where TEvents : struct, IEvents<Pose>
                => throw new NotImplementedException();

            public struct Token
            {
                public readonly bool IsFresh => throw new NotImplementedException();
            }

            public static Traversal Traverse<TEvents>(
                long previousTick, long tick, long budget, ref Token token, ref TEvents events)
                where TEvents : struct, IEvents<Pose>
                => throw new NotImplementedException();
        }

        public static class Impact
        {
            public const int Id = 1;
            public const int Binding = 1;
            public const int ClipCount = 1;

            public static ReadOnlySpan<Hit> Payloads
                => throw new NotImplementedException();

            public static void Sample<TOutput>(long tick, ref TOutput output)
                where TOutput : struct, ISample<Hit>
                => throw new NotImplementedException();

            public static void Sample<TOutput>(in State state, ref TOutput output)
                where TOutput : struct, ISample<Hit>
                => throw new NotImplementedException();

            public static void Frames<TOutput>(long tick, Direction direction, ref TOutput output)
                where TOutput : struct, IFrames<Hit>
                => throw new NotImplementedException();

            public static Traversal Traverse<TEvents>(long previousTick, long tick, ref TEvents events)
                where TEvents : struct, IEvents<Hit>
                => throw new NotImplementedException();

            public struct Token
            {
                public readonly bool IsFresh => throw new NotImplementedException();
            }

            public static Traversal Traverse<TEvents>(
                long previousTick, long tick, long budget, ref Token token, ref TEvents events)
                where TEvents : struct, IEvents<Hit>
                => throw new NotImplementedException();
        }
    }
}
