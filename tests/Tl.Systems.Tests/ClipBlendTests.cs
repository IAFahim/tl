using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

public sealed class ClipBlendTests
{
    const int Ticks = 90;
    const int FirstStart = 30;
    const int SecondStart = 45;
    const int FirstEnd = 60;
    const int SecondEnd = 75;
    const float FirstBrightness = 10f;
    const float SecondBrightness = 100f;

    const string Source = """
        using Tl;
        namespace ClipBlend;
        [TlState] public partial struct Lamp { public float Fading; public float FadingExplicitly; public float Staying; public float LongForm; public int Starts; public int Ends; }
        [TlClip("Blend/Fade")] public struct FadeClip : IBlend<FadeClip>
        {
            public float Brightness;
            public void Blend(in FadeClip first, in FadeClip second, float factor, out FadeClip result) => result = new FadeClip { Brightness = first.Brightness + (second.Brightness - first.Brightness) * factor };
        }
        [TlClip("Blend/ExplicitFade")] public struct ExplicitFadeClip : IBlend<ExplicitFadeClip>
        {
            public float Brightness;
            void IBlend<ExplicitFadeClip>.Blend(in ExplicitFadeClip first, in ExplicitFadeClip second, float factor, out ExplicitFadeClip result) => result = new ExplicitFadeClip { Brightness = first.Brightness + (second.Brightness - first.Brightness) * factor };
        }
        [TlClip("Blend/Stay")] public struct StayClip { public float Brightness; }
        public struct LongClip : IBlend<LongClip>
        {
            public float Brightness;
            public void Blend(in LongClip first, in LongClip second, float factor, out LongClip result) => result = new LongClip { Brightness = -1f };
        }
        public struct SnapToSecondTrack : IBlend<LongClip>
        {
            public float Unused;
            public void Blend(in LongClip first, in LongClip second, float factor, out LongClip result) => result = second;
        }
        [TlKind] public struct FadeKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Lamp lamp) => lamp.Fading = total; }
        [TlKind] public struct ExplicitFadeKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Lamp lamp) => lamp.FadingExplicitly = total; }
        [TlKind] public struct StayKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Lamp lamp) => lamp.Staying = total; }
        [TlKind] public struct LongFormKind { public static void Combine(ref float total, in float next) => total += next; public static void Apply(in float total, ref Lamp lamp) => lamp.LongForm = total; }
        [TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)] public struct StartKind { public static void Apply(in int one, ref Lamp lamp) => lamp.Starts += one; }
        [TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)] public struct EndKind { public static void Apply(in int one, ref Lamp lamp) => lamp.Ends += one; }
        public struct FadeTrack : ITlTrack<FadeClip>
        {
            [Feeds(typeof(FadeKind))] public static float BrightnessThisTick(in TlFrame<FadeClip> frame) => frame.Clip.Brightness;
            [Pulse(typeof(StartKind), On = ClipEvent.Start)] public static int CountStart(in TlFrame<FadeClip> frame) => frame.Direction;
            [Pulse(typeof(EndKind), On = ClipEvent.End)] public static int CountEnd(in TlFrame<FadeClip> frame) => frame.Direction;
        }
        public struct ExplicitFadeTrack : ITlTrack<ExplicitFadeClip>
        {
            [Feeds(typeof(ExplicitFadeKind))] public static float BrightnessThisTick(in TlFrame<ExplicitFadeClip> frame) => frame.Clip.Brightness;
        }
        public struct StayTrack : ITlTrack<StayClip>
        {
            [Feeds(typeof(StayKind))] public static float BrightnessThisTick(in TlFrame<StayClip> frame) => frame.Clip.Brightness;
        }
        public struct LongForm : ITrack<SnapToSecondTrack, LongClip>
        {
            [Feeds(typeof(LongFormKind))] public static float BrightnessThisTick(in Frame<SnapToSecondTrack, LongClip> frame) => frame.Clip.Brightness;
        }
        """;

    const string Host = """
        using System.Collections.Generic;
        using Tl;
        public static class LampHost
        {
            public static byte[] Play(byte[] library, int rows, int steps)
            {
                using var world = new TimelineWorld(library);
                using var players = new PlaybackTable(rows);
                using var kinds = new KindStore(world, players);
                /*DECL*/
                var all = new List<byte>();
                players.Start(0, 1);
                for (var step = 0; step < steps; step++)
                {
                    world.Tick(players, true);
                    new /*AUTO*/ { /*INIT*/ }.Run(world, players, true);
                    /*SNAP*/
                }
                return all.ToArray();
            }
        }
        """;

    readonly record struct LampAt(float Fading, float FadingExplicitly, float Staying, float LongForm, int Starts, int Ends);

    static readonly Lazy<(TestDomain Domain, LampAt[] Lamp)> Run = new(Play);

    static (TestDomain, LampAt[]) Play()
    {
        var domain = TestDomain.Build("ClipBlend", Source, "ClipBlend", Host);
        Assert.Empty(domain.Warnings);
        TypedTrack Overlapping(string clipName, Type? trackType = null, byte[]? trackData = null)
        {
            var clip = domain.Domain.GetType("ClipBlend." + clipName, true)!;
            var track = TestDomain.Track(clip, (FirstStart, FirstEnd, TestDomain.Pack(FirstBrightness)), (SecondStart, SecondEnd, TestDomain.Pack(SecondBrightness)));
            if (trackType is not null)
            {
                track.Type = trackType;
                track.Data = trackData;
            }
            return track;
        }
        var timeline = new TypedTimeline
        {
            Name = "lamp",
            Duration = Ticks,
            Tracks =
            {
                Overlapping("FadeClip"),
                Overlapping("ExplicitFadeClip"),
                Overlapping("StayClip"),
                Overlapping("LongClip", domain.Domain.GetType("ClipBlend.SnapToSecondTrack", true)!, TestDomain.Pack(0f)),
            },
        };
        var bytes = domain.Play("LampHost", TestDomain.Library([timeline], domain.Domain), 1, Ticks);
        Assert.Equal(Ticks * 24, bytes.Length);
        var lamp = new LampAt[Ticks];
        for (var tick = 0; tick < Ticks; tick++)
        {
            var row = bytes.AsSpan(tick * 24, 24);
            lamp[tick] = new LampAt(BitConverter.ToSingle(row), BitConverter.ToSingle(row[4..]), BitConverter.ToSingle(row[8..]), BitConverter.ToSingle(row[12..]), BitConverter.ToInt32(row[16..]), BitConverter.ToInt32(row[20..]));
        }
        return (domain, lamp);
    }

    static float Crossfade(int tick) => FirstBrightness + (SecondBrightness - FirstBrightness) * ((tick - SecondStart) / (float)(FirstEnd - SecondStart - 1));

    [Fact]
    public void TwoOverlappingClipsOfABlendingClipCrossfadeOnEveryTickOfTheOverlap()
    {
        var lamp = Run.Value.Lamp;
        for (var tick = 0; tick < FirstStart; tick++) Assert.Equal(0f, lamp[tick].Fading);
        for (var tick = FirstStart; tick < SecondStart; tick++) Assert.Equal(FirstBrightness, lamp[tick].Fading);
        for (var tick = SecondStart; tick < FirstEnd; tick++) Assert.Equal(Crossfade(tick), lamp[tick].Fading);
        Assert.Equal(FirstBrightness, lamp[SecondStart].Fading);
        Assert.Equal(SecondBrightness, lamp[FirstEnd - 1].Fading);
        for (var tick = FirstEnd; tick < Ticks; tick++) Assert.Equal(SecondBrightness, lamp[tick].Fading);
    }

    [Fact]
    public void AClipThatImplementsIBlendExplicitlyCrossfadesTheSameWay()
    {
        var lamp = Run.Value.Lamp;
        for (var tick = SecondStart; tick < FirstEnd; tick++) Assert.Equal(Crossfade(tick), lamp[tick].FadingExplicitly);
        Assert.NotEqual(FirstBrightness, lamp[SecondStart + 7].FadingExplicitly);
        Assert.NotEqual(SecondBrightness, lamp[SecondStart + 7].FadingExplicitly);
        for (var tick = 0; tick < Ticks; tick++) Assert.Equal(lamp[tick].Fading, lamp[tick].FadingExplicitly);
    }

    [Fact]
    public void AClipWithoutIBlendKeepsTheFirstClipDuringTheOverlap()
    {
        var lamp = Run.Value.Lamp;
        for (var tick = FirstStart; tick < FirstEnd; tick++) Assert.Equal(FirstBrightness, lamp[tick].Staying);
        for (var tick = FirstEnd; tick < SecondEnd; tick++) Assert.Equal(SecondBrightness, lamp[tick].Staying);
    }

    static string FrameHelperOf(string generated, string trackType)
    {
        var at = generated.IndexOf("out " + trackType + " track,", StringComparison.Ordinal);
        Assert.True(at >= 0, "no frame helper for " + trackType);
        var end = generated.IndexOf("frame = new", at, StringComparison.Ordinal);
        return generated.Substring(at, end - at);
    }

    [Fact]
    public void TheLongFormKeepsCallingTheTracksBlendWhenItsClipAlsoBlends()
    {
        var (domain, lamp) = Run.Value;
        var longForm = FrameHelperOf(domain.Generated, "global::ClipBlend.SnapToSecondTrack");
        Assert.Contains("track.Blend(in clip, in __second, blend, out clip);", longForm);
        Assert.DoesNotContain("__Blend", longForm);
        Assert.Contains("clip = __Blend(clip, in __second, blend);", FrameHelperOf(domain.Generated, "Tl.TlNoSettings<global::ClipBlend.FadeClip>"));
        for (var tick = FirstStart; tick < SecondStart; tick++) Assert.Equal(FirstBrightness, lamp[tick].LongForm);
        for (var tick = SecondStart; tick < SecondEnd; tick++) Assert.Equal(SecondBrightness, lamp[tick].LongForm);
    }

    [Fact]
    public void OverlappingClipsOnOneTrackFireTheFirstClipsStartAndTheLastClipsEndOnly()
    {
        var lamp = Run.Value.Lamp;
        for (var tick = 0; tick < Ticks; tick++)
        {
            Assert.Equal(tick >= FirstStart ? 1 : 0, lamp[tick].Starts);
            Assert.Equal(tick >= SecondEnd - 1 ? 1 : 0, lamp[tick].Ends);
        }
    }
}
