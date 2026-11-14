using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tl.Fuzz.RuntimeDomain;
using Tl.Gen.Tlb;

namespace Tl.Fuzz.Runtime;

public enum RequestActionKind : byte
{
    Start,
    Stop,
    StopChannel,
    StopAll,
    Pause,
    Resume,
    Seek,
    Reverse,
    PauseChannel,
    ResumeChannel,
    SeekChannel,
    ReverseChannel,
    SetSpeed,
    SetChannelSpeed,
    Reset,
}

public struct FuzzRequestOp
{
    public RequestActionKind Op;
    public int Entity;
    public ushort Timeline;
    public ushort Arg;
    public byte Channel;
    public bool Flag;
}

public sealed class GeneratedLibrary
{
    public required byte[] Bytes { get; init; }
    public required int TimelineCount { get; init; }
    public required int ChannelCount { get; init; }
    public required uint[] Durations { get; init; }
}

public static class LibraryGen
{
    static readonly BakeKinds AllKinds = LibraryBake.Kinds([typeof(LibraryGen).Assembly]);

    public static byte[] BytesOf<T>(in T value) where T : unmanaged
    {
        var bytes = new byte[Unsafe.SizeOf<T>()];
        MemoryMarshal.Write(bytes, in value);
        return bytes;
    }

    public static GeneratedLibrary BuildPlainLibrary(
        ref FuzzRandom rng,
        int timelineCount,
        int channelCount,
        int maxDuration = 300,
        bool includeTriggers = true,
        bool includeMarkers = true,
        bool includeBatch = true)
    {
        var names = new string[timelineCount];
        var durations = new uint[timelineCount];
        for (var i = 0; i < timelineCount; i++)
            names[i] = $"tl_{i + 1:D4}";

        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++)
            channels[i] = $"ch_{i + 1:D4}";

        var manifest = LibraryBake.Manifest(names, channels, 60);
        var typedList = new TypedTimeline[timelineCount];

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(1 + rng.NextInt(maxDuration));
            if (dur > 4 && rng.NextInt(4) == 0)
                dur = (uint)(20 + rng.NextInt(Math.Max(1, maxDuration - 19)));
            durations[i] = dur;

            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Channel = channelCount > 0 && rng.NextInt(3) != 0 ? channels[rng.NextInt(channelCount)] : null,
            };

            var mode = rng.NextInt(4);
            if (mode == 1)
                tl.Loops = true;
            else if (mode == 2)
                tl.Hold = true;
            else if (mode == 3 && timelineCount > 0)
                tl.Next = names[rng.NextInt(timelineCount)];

            var calm = rng.NextInt(4) == 0;
            var plainTracks = calm ? 1 : 1 + rng.NextInt(3);
            for (var t = 0; t < plainTracks; t++)
            {
                var tr = new TypedTrack
                {
                    Name = $"plain_{t}",
                    Type = typeof(FuzzPlainTrack),
                    Data = BytesOf(new FuzzPlainTrack { Gain = 1 + rng.NextInt(5) }),
                };
                AddRandomPlainClips(ref rng, tr, dur, timelineCount, calm, includeTriggers && !calm);
                tl.Tracks.Add(tr);
            }

            if (includeBatch && !calm && rng.NextBool())
            {
                var btr = new TypedTrack
                {
                    Name = "batch",
                    Type = typeof(FuzzBatchTrack),
                    Data = BytesOf(new FuzzBatchTrack { Bias = rng.NextInt(7) - 3 }),
                };
                AddRandomBatchClips(ref rng, btr, dur);
                tl.Tracks.Add(btr);
            }

            if (includeMarkers && !calm && rng.NextBool())
            {
                var mcount = 1 + rng.NextInt(Math.Min(8, (int)dur));
                var last = -1;
                for (var m = 0; m < mcount; m++)
                {
                    var tick = (uint)rng.NextInt((int)dur);
                    if ((int)tick == last) continue;
                    last = (int)tick;
                    tl.Markers.Add(new TypedMarker
                    {
                        Type = typeof(FuzzPlainMarker),
                        Tick = tick,
                        Data = BytesOf(new FuzzPlainMarker { Code = 1 + rng.NextInt(50) }),
                    });
                }
            }

            typedList[i] = tl;
        }

        return AssembleTyped(typedList, channels, manifest, null);
    }

    public static GeneratedLibrary BuildKindLibrary(
        ref FuzzRandom rng,
        int timelineCount,
        int channelCount,
        int maxDuration = 300,
        bool includeEach = true,
        bool includeIdle = true,
        bool includeFloat = false,
        bool includeMarkers = true,
        IReadOnlyDictionary<string, KindOverride>? overrides = null,
        int maxInlineBytes = 64,
        int stackDepth = 1)
    {
        var names = new string[timelineCount];
        for (var i = 0; i < timelineCount; i++)
            names[i] = $"ktl_{i + 1:D4}";

        var channels = new string[channelCount];
        for (var i = 0; i < channelCount; i++)
            channels[i] = $"kch_{i + 1:D4}";

        var manifest = LibraryBake.Manifest(names, channels, 60);
        var typedList = new TypedTimeline[timelineCount];

        for (var i = 0; i < timelineCount; i++)
        {
            var dur = (uint)(1 + rng.NextInt(maxDuration));
            var tl = new TypedTimeline
            {
                Name = names[i],
                Duration = dur,
                Channel = channelCount > 0 && rng.NextInt(3) != 0 ? channels[rng.NextInt(channelCount)] : null,
            };

            var mode = rng.NextInt(4);
            if (mode == 1) tl.Loops = true;
            else if (mode == 2) tl.Hold = true;
            else if (mode == 3) tl.Next = names[rng.NextInt(timelineCount)];

            var calm = i > 0 && rng.NextInt(5) == 0;
            if (!calm)
            {
                var cTracks = 1 + rng.NextInt(2);
                for (var t = 0; t < cTracks; t++)
                {
                    var tr = new TypedTrack
                    {
                        Name = $"kc_{t}",
                        Type = typeof(KCombineTrack),
                        Data = BytesOf(new KCombineTrack { Scale = 1 + rng.NextInt(4) }),
                    };
                    AddRandomCombineClips(ref rng, tr, dur);
                    tl.Tracks.Add(tr);
                }

                if (includeEach && rng.NextBool())
                {
                    var etr = new TypedTrack
                    {
                        Name = "ke",
                        Type = typeof(KEachTrack),
                        Data = BytesOf(new KEachTrack { Scale = 1 + rng.NextInt(3) }),
                    };
                    AddRandomEachClips(ref rng, etr, dur);
                    tl.Tracks.Add(etr);
                }

                if (includeIdle && rng.NextBool())
                {
                    var itr = new TypedTrack
                    {
                        Name = "ki",
                        Type = typeof(KIdleTrack),
                        Data = BytesOf(new KIdleTrack { Bias = rng.NextInt(5) - 2 }),
                    };
                    AddRandomIdleClips(ref rng, itr, dur);
                    tl.Tracks.Add(itr);
                }

                if (includeFloat && rng.NextBool())
                {
                    var ftr = new TypedTrack
                    {
                        Name = "kf",
                        Type = typeof(KFloatTrack),
                        Data = BytesOf(new KFloatTrack { Scale = 1f + rng.NextInt(4) * 0.5f }),
                    };
                    AddRandomFloatClips(ref rng, ftr, dur);
                    tl.Tracks.Add(ftr);
                }

                if (includeMarkers && rng.NextBool())
                {
                    var mcount = 1 + rng.NextInt(Math.Min(6, (int)dur));
                    for (var m = 0; m < mcount; m++)
                    {
                        tl.Markers.Add(new TypedMarker
                        {
                            Type = typeof(KPulseMarker),
                            Tick = (uint)rng.NextInt((int)dur),
                            Data = BytesOf(new KPulseMarker { Delta = 1 + rng.NextInt(17) }),
                        });
                    }
                }
            }

            typedList[i] = tl;
        }

        var kinds = overrides == null ? AllKinds : LibraryBake.Kinds([typeof(LibraryGen).Assembly], overrides);
        return AssembleTyped(typedList, channels, manifest, kinds, maxInlineBytes, stackDepth, overrides);
    }

    public static GeneratedLibrary AssembleTyped(
        IReadOnlyList<TypedTimeline> timelines,
        IReadOnlyList<string> channels,
        LibraryManifest manifest,
        BakeKinds? kinds,
        int maxInlineBytes = 64,
        int stackDepth = 1,
        IReadOnlyDictionary<string, KindOverride>? overrides = null)
    {
        var parts = new List<LibraryPart>(timelines.Count);
        var durations = new uint[timelines.Count];
        for (var i = 0; i < timelines.Count; i++)
        {
            var tl = timelines[i];
            durations[i] = tl.Duration;
            var tlb = LibraryBake.BakeTyped(tl, manifest);
            parts.Add(new LibraryPart
            {
                Name = tl.Name!,
                Tlb = tlb,
                Kinds = kinds != null ? LibraryBake.KindPayload(tl.Name!, tlb, kinds) : null,
                Next = (ushort)LibraryAssembler.NextOf(tlb),
            });
        }

        var settings = LibrarySettings.Default;
        settings.ChannelCount = channels.Count;
        settings.MaxInlineBytes = maxInlineBytes;
        settings.StackDepth = stackDepth;
        settings.Kinds = overrides;
        var bytes = LibraryAssembler.Assemble(parts, 60, settings);
        return new GeneratedLibrary
        {
            Bytes = bytes,
            TimelineCount = timelines.Count,
            ChannelCount = channels.Count,
            Durations = durations,
        };
    }

    public static FuzzRequestOp NextRequest(ref FuzzRandom rng, int entityCount, int timelineCount, int channelCount, uint[] durations)
    {
        var op = (RequestActionKind)rng.NextInt(15);
        var entity = rng.NextInt(entityCount);
        var timeline = (ushort)(1 + rng.NextInt(Math.Max(1, timelineCount)));
        var dur = timeline <= durations.Length ? (int)durations[timeline - 1] : 16;
        var channel = channelCount > 0 ? (byte)(1 + rng.NextInt(channelCount)) : (byte)0;
        ushort arg = 0;
        if (op is RequestActionKind.Start or RequestActionKind.Seek or RequestActionKind.SeekChannel)
            arg = (ushort)rng.NextInt(Math.Max(1, dur + 2));
        else if (op is RequestActionKind.SetSpeed or RequestActionKind.SetChannelSpeed)
            arg = PickRate(ref rng);

        return new FuzzRequestOp
        {
            Op = op,
            Entity = entity,
            Timeline = timeline,
            Arg = arg,
            Channel = channel,
            Flag = rng.NextBool(),
        };
    }

    public static ushort PickRate(ref FuzzRandom rng)
    {
        var k = rng.NextInt(10);
        return k switch
        {
            0 => 0,
            1 => 16,
            2 => 1,
            3 => 8,
            4 => 12,
            5 => 24,
            6 => 32,
            7 => 48,
            8 => 64,
            _ => (ushort)(1 + rng.NextInt(64)),
        };
    }

    public static void ApplyOp(PlaybackTable players, in FuzzRequestOp op)
    {
        switch (op.Op)
        {
            case RequestActionKind.Start:
                players.Start(op.Entity, op.Timeline, op.Arg);
                break;
            case RequestActionKind.Stop:
                players.Stop(op.Entity, op.Timeline);
                break;
            case RequestActionKind.StopChannel:
                players.StopChannel(op.Entity, op.Channel);
                break;
            case RequestActionKind.StopAll:
                players.StopAll(op.Entity);
                break;
            case RequestActionKind.Pause:
                players.Pause(op.Entity, op.Timeline);
                break;
            case RequestActionKind.Resume:
                players.Resume(op.Entity, op.Timeline);
                break;
            case RequestActionKind.Seek:
                players.Seek(op.Entity, op.Timeline, op.Arg);
                break;
            case RequestActionKind.Reverse:
                players.Reverse(op.Entity, op.Timeline, op.Flag);
                break;
            case RequestActionKind.PauseChannel:
                players.PauseChannel(op.Entity, op.Channel);
                break;
            case RequestActionKind.ResumeChannel:
                players.ResumeChannel(op.Entity, op.Channel);
                break;
            case RequestActionKind.SeekChannel:
                players.SeekChannel(op.Entity, op.Channel, op.Arg);
                break;
            case RequestActionKind.ReverseChannel:
                players.ReverseChannel(op.Entity, op.Channel, op.Flag);
                break;
            case RequestActionKind.SetSpeed:
                players.SetSpeed(op.Entity, op.Timeline, op.Arg);
                break;
            case RequestActionKind.SetChannelSpeed:
                players.SetChannelSpeed(op.Entity, op.Channel, op.Arg);
                break;
            case RequestActionKind.Reset:
                players.Reset(op.Entity);
                break;
        }
    }

    public static List<(uint Start, uint End)> GenerateTrackSpans(ref FuzzRandom rng, uint duration, bool sparse)
    {
        var spans = new List<(uint Start, uint End)>();
        if (duration == 0) return spans;
        if (sparse && rng.NextInt(3) == 0)
        {
            var s = duration > 4 ? (uint)rng.NextInt((int)duration - 2) : 0u;
            var e = Math.Min(duration, s + 1 + (uint)rng.NextInt((int)Math.Max(1, duration - s)));
            spans.Add((s, e));
            return spans;
        }

        uint cursor = 0;
        var count = 1 + rng.NextInt(Math.Min(6, (int)duration));
        for (var i = 0; i < count && cursor < duration; i++)
        {
            var gap = (uint)rng.NextInt((int)Math.Max(1, (duration - cursor) / 2));
            var start = cursor + gap;
            if (start >= duration) break;
            var len = 1u + (uint)rng.NextInt((int)(duration - start));
            if (len > Math.Max(1u, duration / 2))
                len = 1u + (uint)rng.NextInt((int)Math.Max(1u, duration / 2));
            var end = Math.Min(duration, start + len);
            spans.Add((start, end));

            if (rng.NextInt(3) == 0 && start + 1 < end && end < duration)
            {
                var blendStart = start + 1;
                var blendEnd = Math.Min(duration, end + 1 + (uint)rng.NextInt((int)(duration - end)));
                spans.Add((blendStart, blendEnd));
                cursor = Math.Max(end, blendEnd);
            }
            else
            {
                cursor = end;
            }
        }
        return spans;
    }

    static void AddRandomPlainClips(ref FuzzRandom rng, TypedTrack track, uint duration, int timelineCount, bool sparse, bool includeTriggers)
    {
        foreach (var (s, e) in GenerateTrackSpans(ref rng, duration, sparse))
        {
            var trigger = includeTriggers && rng.NextInt(8) == 0 ? (ushort)(1 + rng.NextInt(timelineCount)) : (ushort)0;
            track.Clips.Add(new TypedClip
            {
                Type = typeof(FuzzPlainClip),
                Start = s,
                End = e,
                Data = BytesOf(new FuzzPlainClip
                {
                    Delta = (rng.NextBool() ? 1 : -1) * (1 + rng.NextInt(9)),
                    Tag = rng.NextInt(64),
                    TriggerTimeline = trigger,
                }),
            });
        }
    }

    static void AddRandomBatchClips(ref FuzzRandom rng, TypedTrack track, uint duration)
    {
        foreach (var (s, e) in GenerateTrackSpans(ref rng, duration, false))
        {
            track.Clips.Add(new TypedClip
            {
                Type = typeof(FuzzBatchClip),
                Start = s,
                End = e,
                Data = BytesOf(new FuzzBatchClip { Weight = rng.NextInt(11) - 5 }),
            });
        }
    }

    static void AddRandomCombineClips(ref FuzzRandom rng, TypedTrack track, uint duration)
    {
        foreach (var (s, e) in GenerateTrackSpans(ref rng, duration, false))
        {
            track.Clips.Add(new TypedClip
            {
                Type = typeof(KCombineClip),
                Start = s,
                End = e,
                Data = BytesOf(new KCombineClip
                {
                    Delta = rng.NextInt(13) - 6,
                    StartPulse = rng.NextInt(7) - 3,
                    EndPulse = rng.NextInt(7) - 3,
                }),
            });
        }
    }

    static void AddRandomEachClips(ref FuzzRandom rng, TypedTrack track, uint duration)
    {
        foreach (var (s, e) in GenerateTrackSpans(ref rng, duration, false))
        {
            track.Clips.Add(new TypedClip
            {
                Type = typeof(KEachClip),
                Start = s,
                End = e,
                Data = BytesOf(new KEachClip
                {
                    Delta = rng.NextInt(9) - 4,
                    StartPulse = rng.NextInt(5) - 2,
                    EndPulse = rng.NextInt(5) - 2,
                }),
            });
        }
    }

    static void AddRandomIdleClips(ref FuzzRandom rng, TypedTrack track, uint duration)
    {
        foreach (var (s, e) in GenerateTrackSpans(ref rng, duration, false))
        {
            track.Clips.Add(new TypedClip
            {
                Type = typeof(KIdleClip),
                Start = s,
                End = e,
                Data = BytesOf(new KIdleClip { Code = 1 + rng.NextInt(500) }),
            });
        }
    }

    static void AddRandomFloatClips(ref FuzzRandom rng, TypedTrack track, uint duration)
    {
        foreach (var (s, e) in GenerateTrackSpans(ref rng, duration, false))
        {
            track.Clips.Add(new TypedClip
            {
                Type = typeof(KFloatClip),
                Start = s,
                End = e,
                Data = BytesOf(new KFloatClip { Delta = (rng.NextInt(9) - 4) * 0.25f }),
            });
        }
    }
}
