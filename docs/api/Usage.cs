using Game.Timelines;
using Tl;

namespace Game;

public static class Usage
{
    public static Output Play(ReadOnlySpan<long> ticks)
    {
        var state = Attack.Start();
        var output = new Output();

        foreach (var tick in ticks)
        {
            output.X = output.Y = 0f;
            Attack.Update(ref state, tick, ref output);
        }

        return output;
    }

    public static Output Preview(long tick)
    {
        var output = new Output();
        Attack.Tracks.Body.Sample(tick, ref output);
        return output;
    }

    public static Output Reposition(ref Attack.State state, long tick)
    {
        var output = new Output();
        Attack.Seek(ref state, tick);
        Attack.Sample(in state, ref output);
        return output;
    }

    public static Frames Inspect(long tick)
    {
        var frames = new Frames();
        Attack.Frames(tick, Direction.Forward, ref frames);
        return frames;
    }

    public static Traversal Resume(
        ref Attack.State state,
        long previousTick,
        long tick,
        long budget,
        ref Attack.Token token,
        ref Output output)
    {
        var result = Attack.Traverse(previousTick, tick, budget, ref token, ref output);

        if (result.Status == Status.Completed)
        {
            Attack.Seek(ref state, tick);
            output.X = output.Y = 0f;
            Attack.Sample(in state, ref output);
        }

        return result;
    }
}

public struct Frames : IFrames<Pose>, IFrames<Hit>
{
    public int PoseCount;
    public int HitCount;
    public Tl.Frame LastPose;
    public Tl.Frame LastHit;

    public void Frame(in Tl.Frame frame, in Pose clip)
    {
        PoseCount++;
        LastPose = frame;
    }

    public void Frame(in Tl.Frame frame, in Hit clip)
    {
        HitCount++;
        LastHit = frame;
    }
}
