using Tl;
using Tl.CSharp;
using Tl.Generation;

namespace Authoring;

public readonly record struct Pose(float X, float Y);
public readonly record struct Hit(int Damage);

public static class AttackSource
{
    public static Result Generate()
    {
        var timeline = Timeline.Define("Attack", duration: 60)
            .Track("Body", 0, TrackMode.CrossFade,
                Clip.Range(0, 40, new Pose(0f, 0f)),
                Clip.Range(30, 60, new Pose(1f, 0f)))
            .Track("Impact", 1, TrackMode.Exclusive,
                Clip.At(35, new Hit(20)))
            .Build();

        return Generator.Generate(timeline, new Adapter(new Options("Game.Timelines")));
    }
}
