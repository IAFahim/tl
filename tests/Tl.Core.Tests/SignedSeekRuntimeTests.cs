using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Tl.Core.Tests;

public class SignedSeekRuntimeTests
{
    [StructLayout(LayoutKind.Sequential)]
    private struct X64FrameLayout
    {
        public nint Track;
        public nint Clip;
        public uint GameTick;
        public uint TimelineTick;
        public long Cycle;
        public ushort TrackIndex;
        public FrameFlags Flags;
    }

    public readonly struct TestTimeline : ITimeline
    {
        public static void Define(scoped Builder builder)
        {
        }
    }

    public readonly record struct TestClip(int Value);

    public readonly struct TestTrack : ITrack<TestClip>
    {
        public void Blend(in TestClip first, in TestClip second, float factor, out TestClip result)
            => result = factor < 0.5f ? first : second;
    }

    public ref struct TestData : ITimelineData<TestData>
    {
        public ushort Id;
        public int Delta;
        public int Calls;
        public bool Result;

        public static bool TrySeek(ushort id, scoped ref TestData data, int delta)
        {
            data.Id = id;
            data.Delta = delta;
            data.Calls++;
            return data.Result;
        }
    }

    [Fact]
    public void PlaybackAbiIsFrozen()
    {
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(PlaybackFlags)));
        Assert.Equal(1, Unsafe.SizeOf<PlaybackFlags>());
        Assert.Equal(16, Unsafe.SizeOf<Playback>());
        Assert.Equal(16, Unsafe.SizeOf<Playback<TestTimeline>>());
        AssertSequentialReadonlyValueType<Playback>();
        AssertSequentialReadonlyValueType<Playback<TestTimeline>>();
        AssertField<Playback>(nameof(Playback.Position), typeof(long), 0);
        AssertField<Playback>(nameof(Playback.GameTick), typeof(uint), 8);
        AssertField<Playback>(nameof(Playback.Owner), typeof(ushort), 12);
        AssertField<Playback>(nameof(Playback.Flags), typeof(PlaybackFlags), 14);
        AssertField<Playback<TestTimeline>>(nameof(Playback<TestTimeline>.Position), typeof(long), 0);
        AssertField<Playback<TestTimeline>>(nameof(Playback<TestTimeline>.GameTick), typeof(uint), 8);
        AssertField<Playback<TestTimeline>>(nameof(Playback<TestTimeline>.Flags), typeof(PlaybackFlags), 12);

        var typed = Timeline.CreateTypedPlayback<TestTimeline>(0, 4_000_000_000, PlaybackFlags.Started);
        Assert.Equal(0, typed.Position);
        Assert.Equal(4_000_000_000u, typed.GameTick);
        Assert.Equal(PlaybackFlags.Started, typed.Flags);
    }

    [Fact]
    public void FrameFlagsHaveFrozenByteValues()
    {
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(FrameFlags)));
        Assert.Equal(1, Unsafe.SizeOf<FrameFlags>());
        Assert.Equal(1, (byte)FrameFlags.ClipStart);
        Assert.Equal(2, (byte)FrameFlags.ClipEnd);
        Assert.Equal(4, (byte)FrameFlags.TimelineStart);
        Assert.Equal(8, (byte)FrameFlags.TimelineEnd);
        Assert.Equal(16, (byte)FrameFlags.CompletedBefore);
        Assert.Equal(32, (byte)FrameFlags.CompletedAfter);
        Assert.Equal(64, (byte)FrameFlags.Looping);
        Assert.Equal(128, (byte)FrameFlags.Reverse);
    }

    [Fact]
    public void FrameRepresentsIndependentBoundariesAndDerivedDirection()
    {
        var track = new TestTrack();
        var clip = new TestClip(31);
        var flags = FrameFlags.ClipStart | FrameFlags.ClipEnd |
            FrameFlags.TimelineStart | FrameFlags.TimelineEnd | FrameFlags.Reverse;
        var frame = new Frame<TestTrack, TestClip>(in track, in clip, 91, 0, -3, 7, flags);

        Assert.Equal(91u, frame.GameTick);
        Assert.Equal(0u, frame.TimelineTick);
        Assert.Equal(-3, frame.Cycle);
        Assert.Equal((ushort)7, frame.TrackIndex);
        Assert.Equal(31, frame.Clip.Value);
        Assert.Equal(-1, frame.Direction);
        Assert.True(frame.Has(FrameFlags.ClipStart | FrameFlags.ClipEnd));
        Assert.True(frame.Has(FrameFlags.TimelineStart | FrameFlags.TimelineEnd));
    }

    [Fact]
    public unsafe void FrameAbiIsFrozenOnX64()
    {
        if (IntPtr.Size != 8)
            return;

        var type = typeof(Frame<TestTrack, TestClip>);
        Assert.True(type.IsValueType);
        Assert.True(type.IsByRefLike);
        Assert.True(type.IsDefined(typeof(IsReadOnlyAttribute), false));
        Assert.Equal(LayoutKind.Sequential, type.StructLayoutAttribute!.Value);
        Assert.Equal(40, Unsafe.SizeOf<Frame<TestTrack, TestClip>>());
        AssertFieldShape(type, "_track", typeof(TestTrack).MakeByRefType());
        AssertFieldShape(type, "_clip", typeof(TestClip).MakeByRefType());
        AssertFieldShape(type, "<GameTick>k__BackingField", typeof(uint));
        AssertFieldShape(type, "<TimelineTick>k__BackingField", typeof(uint));
        AssertFieldShape(type, "<Cycle>k__BackingField", typeof(long));
        AssertFieldShape(type, "<TrackIndex>k__BackingField", typeof(ushort));
        AssertFieldShape(type, "<Flags>k__BackingField", typeof(FrameFlags));

        var track = new TestTrack();
        var clip = new TestClip(31);
        var frame = new Frame<TestTrack, TestClip>(in track, in clip, 91, 73, -3, 7, FrameFlags.Reverse);
        ref var layout = ref Unsafe.As<Frame<TestTrack, TestClip>, X64FrameLayout>(ref frame);
        Assert.Equal((nint)Unsafe.AsPointer(ref track), layout.Track);
        Assert.Equal((nint)Unsafe.AsPointer(ref clip), layout.Clip);
        Assert.Equal(91u, layout.GameTick);
        Assert.Equal(73u, layout.TimelineTick);
        Assert.Equal(-3, layout.Cycle);
        Assert.Equal((ushort)7, layout.TrackIndex);
        Assert.Equal(FrameFlags.Reverse, layout.Flags);
        AssertOffset<X64FrameLayout>(nameof(X64FrameLayout.Track), 0);
        AssertOffset<X64FrameLayout>(nameof(X64FrameLayout.Clip), 8);
        AssertOffset<X64FrameLayout>(nameof(X64FrameLayout.GameTick), 16);
        AssertOffset<X64FrameLayout>(nameof(X64FrameLayout.TimelineTick), 20);
        AssertOffset<X64FrameLayout>(nameof(X64FrameLayout.Cycle), 24);
        AssertOffset<X64FrameLayout>(nameof(X64FrameLayout.TrackIndex), 32);
        AssertOffset<X64FrameLayout>(nameof(X64FrameLayout.Flags), 34);

        AssertReadOnlyByRefReturn(type.GetProperty(nameof(Frame<TestTrack, TestClip>.Track))!);
        AssertReadOnlyByRefReturn(type.GetProperty(nameof(Frame<TestTrack, TestClip>.Clip))!);

        var constructor = Assert.Single(type.GetConstructors());
        AssertReadOnlyByRefParameter(constructor.GetParameters()[0]);
        AssertReadOnlyByRefParameter(constructor.GetParameters()[1]);
    }

    [Fact]
    public void SignedSeekApiPreservesRefAndScopedContracts()
    {
        var dataContract = typeof(ITimelineData<TestData>).GetMethod(nameof(ITimelineData<TestData>.TrySeek))!;
        Assert.True(dataContract.IsStatic);
        Assert.True(dataContract.IsAbstract);
        Assert.Equal(typeof(bool), dataContract.ReturnType);
        Assert.Equal(typeof(ushort), dataContract.GetParameters()[0].ParameterType);
        AssertScopedRefParameter(dataContract.GetParameters()[1], typeof(TestData));
        Assert.Equal(typeof(int), dataContract.GetParameters()[2].ParameterType);

        var timelineSeek = typeof(Timeline).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method => method.Name == nameof(Timeline.TrySeek));
        Assert.True(timelineSeek.IsGenericMethodDefinition);
        Assert.Equal(typeof(bool), timelineSeek.ReturnType);
        Assert.Equal(typeof(ushort), timelineSeek.GetParameters()[0].ParameterType);
        Assert.True(timelineSeek.GetParameters()[1].ParameterType.IsByRef);
        Assert.True(HasScopedRef(timelineSeek.GetParameters()[1]));
        Assert.Equal(typeof(int), timelineSeek.GetParameters()[2].ParameterType);

        var tryStart = typeof(Timeline).GetMethod(nameof(Timeline.TryStart))!;
        var startParameters = tryStart.GetParameters();
        Assert.Equal(typeof(ushort), startParameters[0].ParameterType);
        Assert.Equal(typeof(uint), startParameters[1].ParameterType);
        Assert.Equal(typeof(Playback).MakeByRefType(), startParameters[2].ParameterType);
        Assert.True(startParameters[2].IsOut);

        var tryStop = typeof(Timeline).GetMethod(nameof(Timeline.TryStop))!;
        var stopParameters = tryStop.GetParameters();
        AssertReadOnlyByRefParameter(stopParameters[1]);
        Assert.Equal(typeof(Playback).MakeByRefType(), stopParameters[2].ParameterType);
        Assert.True(stopParameters[2].IsOut);
    }

    [Fact]
    public void DynamicStartUsesBoundaryZero()
    {
        var id = Timeline.RegisterCompiled(12, false, new CompiledRoute(0, 0));

        Assert.True(Timeline.TryStart(id, 4_000_000_000, out var playback));
        Assert.Equal(0, playback.Position);
        Assert.Equal(4_000_000_000u, playback.GameTick);
        Assert.Equal(id, playback.Owner);
        Assert.Equal(PlaybackFlags.Started, playback.Flags);
    }

    [Fact]
    public void DynamicStopIsTotalAndIdempotent()
    {
        var firstId = Timeline.RegisterCompiled(12, false, new CompiledRoute(0, 0));
        var secondId = Timeline.RegisterCompiled(12, false, new CompiledRoute(0, 1));
        Assert.True(Timeline.TryStart(firstId, 73, out var started));

        Assert.False(Timeline.TryStop(secondId, in started, out var wrongOwner));
        Assert.Equal(started, wrongOwner);

        var initial = default(Playback);
        Assert.False(Timeline.TryStop(firstId, in initial, out var defaultResult));
        Assert.Equal(initial, defaultResult);

        Assert.False(Timeline.TryStop(ushort.MaxValue, in started, out var invalidId));
        Assert.Equal(started, invalidId);

        Assert.True(Timeline.TryStop(firstId, in started, out var stopped));
        Assert.True(stopped.Has(PlaybackFlags.Started | PlaybackFlags.Stopped));
        Assert.Equal(started.Position, stopped.Position);
        Assert.Equal(started.GameTick, stopped.GameTick);

        Assert.True(Timeline.TryStop(firstId, in stopped, out var stoppedAgain));
        Assert.Equal(stopped, stoppedAgain);
    }

    [Fact]
    public void InvalidDynamicStartReturnsDefaultState()
    {
        Assert.False(Timeline.TryStart(ushort.MaxValue, 97, out var playback));
        Assert.Equal(default, playback);
    }

    [Fact]
    public void DynamicSeekForwardsSignedDeltaWithoutTransformation()
    {
        var data = new TestData { Result = true };

        Assert.True(Timeline.TrySeek(41, ref data, int.MinValue));
        Assert.Equal((ushort)41, data.Id);
        Assert.Equal(int.MinValue, data.Delta);
        Assert.Equal(1, data.Calls);
    }

    private static void AssertSequentialReadonlyValueType<T>() where T : struct
    {
        var type = typeof(T);
        Assert.True(type.IsValueType);
        Assert.True(type.IsDefined(typeof(IsReadOnlyAttribute), false));
        Assert.Equal(LayoutKind.Sequential, type.StructLayoutAttribute!.Value);
    }

    private static void AssertField<T>(string name, Type fieldType, int offset) where T : struct
        => AssertField(typeof(T), name, fieldType, offset);

    private static void AssertField(Type declaringType, string name, Type fieldType, int offset)
    {
        var field = declaringType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        Assert.Equal(fieldType, field.FieldType);
        Assert.True(field.IsInitOnly);
        Assert.Equal(offset, Marshal.OffsetOf(declaringType, name).ToInt32());
    }

    private static void AssertFieldShape(Type declaringType, string name, Type fieldType)
    {
        var field = declaringType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        Assert.Equal(fieldType, field.FieldType);
        Assert.True(field.IsInitOnly);
    }

    private static void AssertOffset<T>(string name, int offset) where T : struct
        => Assert.Equal(offset, Marshal.OffsetOf<T>(name).ToInt32());

    private static void AssertReadOnlyByRefReturn(PropertyInfo property)
    {
        Assert.True(property.PropertyType.IsByRef);
        Assert.Contains(
            property.GetMethod!.ReturnParameter.GetRequiredCustomModifiers(),
            static modifier => modifier == typeof(InAttribute));
    }

    private static void AssertReadOnlyByRefParameter(ParameterInfo parameter)
    {
        Assert.True(parameter.ParameterType.IsByRef);
        Assert.True(parameter.IsIn);
        Assert.False(parameter.IsOut);
    }

    private static void AssertScopedRefParameter(ParameterInfo parameter, Type elementType)
    {
        Assert.Equal(elementType.MakeByRefType(), parameter.ParameterType);
        Assert.False(parameter.IsIn);
        Assert.False(parameter.IsOut);
        Assert.True(HasScopedRef(parameter));
    }

    private static bool HasScopedRef(ParameterInfo parameter)
        => parameter.GetCustomAttributesData().Any(static attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.ScopedRefAttribute");
}
