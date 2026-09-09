using Xunit;

namespace Tl.Core.Tests;

public class BindingTests
{
    public readonly record struct BindingClip(int Value);

    public readonly struct BindingTrack : IBlend<BindingClip>
    {
        public void Blend(in BindingClip first, in BindingClip second, float t, out BindingClip result)
            => result = t < 0.5f ? first : second;
    }

    public struct BindingResult<TTag> : ITrack<BindingTrack, BindingClip, NoInput, BindingResult<TTag>>
        where TTag : unmanaged
    {
        public int Sum;

        public static void Forward(int ordinal, int count, ushort index,
            in BindingTrack track, in BindingClip clip, ClipState state,
            in uint tick, in NoInput input, ref BindingResult<TTag> result)
            => result.Sum += clip.Value;

        public static void Backward(int ordinal, int count, ushort index,
            in BindingTrack track, in BindingClip clip, ClipState state,
            in uint tick, in NoInput input, ref BindingResult<TTag> result)
            => result.Sum -= clip.Value;
    }

    [Fact]
    public void OverflowBindingsSurviveRepeatedGrowth()
    {
        var id = Timeline<BindingTrack, BindingClip>.Build(static timeline =>
        {
            var track = timeline.Track(new BindingTrack());
            timeline.Clip(in track, new BindingClip(7), 0, 10);
        }).InMemory();

        Bind<Tag00>(id); Bind<Tag01>(id); Bind<Tag02>(id); Bind<Tag03>(id); Bind<Tag04>(id);
        Bind<Tag05>(id); Bind<Tag06>(id); Bind<Tag07>(id); Bind<Tag08>(id); Bind<Tag09>(id);
        Bind<Tag10>(id); Bind<Tag11>(id); Bind<Tag12>(id); Bind<Tag13>(id); Bind<Tag14>(id);
        Bind<Tag15>(id); Bind<Tag16>(id); Bind<Tag17>(id); Bind<Tag18>(id); Bind<Tag19>(id);
        Bind<Tag20>(id);

        Verify<Tag00>(id); Verify<Tag01>(id); Verify<Tag02>(id); Verify<Tag03>(id); Verify<Tag04>(id);
        Verify<Tag05>(id); Verify<Tag06>(id); Verify<Tag07>(id); Verify<Tag08>(id); Verify<Tag09>(id);
        Verify<Tag10>(id); Verify<Tag11>(id); Verify<Tag12>(id); Verify<Tag13>(id); Verify<Tag14>(id);
        Verify<Tag15>(id); Verify<Tag16>(id); Verify<Tag17>(id); Verify<Tag18>(id); Verify<Tag19>(id);
        Verify<Tag20>(id);

        Timeline.Destroy(id);
    }

    private static void Bind<TTag>(ushort id) where TTag : unmanaged
        => Timeline<BindingTrack, BindingClip>.Bind<NoInput, BindingResult<TTag>>(id);

    private static void Verify<TTag>(ushort id) where TTag : unmanaged
    {
        var result = new BindingResult<TTag>();
        var playback = Timeline.Start(id);
        playback = Timeline.Forward(id, in playback, default(NoInput), ref result, 1u);
        Assert.Equal(7, result.Sum);
    }

    public enum Tag00 : byte { Value }
    public enum Tag01 : byte { Value }
    public enum Tag02 : byte { Value }
    public enum Tag03 : byte { Value }
    public enum Tag04 : byte { Value }
    public enum Tag05 : byte { Value }
    public enum Tag06 : byte { Value }
    public enum Tag07 : byte { Value }
    public enum Tag08 : byte { Value }
    public enum Tag09 : byte { Value }
    public enum Tag10 : byte { Value }
    public enum Tag11 : byte { Value }
    public enum Tag12 : byte { Value }
    public enum Tag13 : byte { Value }
    public enum Tag14 : byte { Value }
    public enum Tag15 : byte { Value }
    public enum Tag16 : byte { Value }
    public enum Tag17 : byte { Value }
    public enum Tag18 : byte { Value }
    public enum Tag19 : byte { Value }
    public enum Tag20 : byte { Value }
}
