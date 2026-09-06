using Game.Timelines;
using Tl;

namespace Game;

public struct Output : IOutput<Pose>, IOutput<Hit>
{
    public float X;
    public float Y;
    public int Damage;

    public void Sample(in Sample sample, in Pose clip)
    {
        X += clip.X * sample.Weight;
        Y += clip.Y * sample.Weight;
    }

    public void Sample(in Sample sample, in Hit clip) { }
    public void OnClip(in ClipEvent transition, in Pose clip) { }

    public void OnClip(in ClipEvent transition, in Hit clip)
    {
        if (transition.Direction == Direction.Forward && transition.Phase == Phase.Enter)
            Damage += clip.Damage;
    }

    public void OnBlend(in BlendEvent transition, in Pose first, in Pose second) { }
    public void OnBlend(in BlendEvent transition, in Hit first, in Hit second) { }
}

public static class Consumer
{
    public static void Update(ref Attack.State state, long tick, ref Output output)
    {
        output.X = output.Y = 0f;
        Attack.Update(ref state, tick, ref output);
    }

    public static Traversal Resume(
        long previousTick, long tick, ref Attack.Token token, ref Output output)
    {
        return Attack.Traverse(previousTick, tick, 128, ref token, ref output);
    }
}
