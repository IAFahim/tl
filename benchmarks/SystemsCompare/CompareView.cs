// Minimal unmanaged library-view builder for the compare bench (no markers).
// Mirrors the sanctioned ViewScope.Build layout step for step. View injection
// uses internal TimelineWorld.AdoptView via an InternalsVisibleTo grant for
// the compare assemblies (same pattern as Systems/Systems21).
using System.Runtime.InteropServices;
using Tl;

namespace SystemsCompare;

internal sealed unsafe class CompareView : IDisposable
{
    public sealed class StepSpec
    {
        public ulong PairKey;
        public byte[] Track = [];
        public byte[] First = [];
        public byte[]? Second = null;
        public uint WindowStart;
        public uint WindowEnd;
        public int TrackIndex;
        public uint FactorStart;
        public uint FactorSpan;
        public byte[]? Lane;
    }

    public sealed class TimelineSpec
    {
        public ushort Channel;
        public ushort Next;
        public byte Loop;
        public byte Hold;
        public uint Duration;
        public readonly List<StepSpec> Steps = new();
    }

    readonly List<nint> _owned = new();
    bool _disposed;

    public void Build(IList<TimelineSpec> timelines)
    {
        var view = new LibraryView { Version = 1, TimelineCount = timelines.Count };
        var tls = (LibraryTimeline*)Alloc((nuint)timelines.Count * (nuint)sizeof(LibraryTimeline));
        view.Timelines = tls;
        for (var t = 0; t < timelines.Count; t++)
        {
            var spec = timelines[t];
            var tl = tls + t;
            tl->Id = (ushort)(t + 1);
            tl->Channel = spec.Channel;
            tl->Next = spec.Next;
            tl->Loop = spec.Loop;
            tl->Hold = spec.Hold;
            tl->Duration = spec.Duration;
            tl->StageCount = 1;
            var stages = (LibraryStage*)Alloc((nuint)sizeof(LibraryStage));
            tl->Stages = stages;
            var steps = (LibraryStep*)Alloc((nuint)spec.Steps.Count * (nuint)sizeof(LibraryStep));
            stages->Start = 0;
            stages->End = spec.Duration;
            stages->StepCount = spec.Steps.Count;
            stages->Steps = steps;
            for (var s = 0; s < spec.Steps.Count; s++)
            {
                var step = spec.Steps[s];
                var dst = steps + s;
                dst->Pair = (uint)s;
                dst->PairKey = step.PairKey;
                dst->WindowStart = step.WindowStart;
                dst->WindowEnd = step.WindowEnd;
                dst->TrackIndex = step.TrackIndex;
                dst->FactorStart = step.FactorStart;
                dst->FactorSpan = step.FactorSpan;
                dst->TrackValueIndex = 0;
                dst->FirstValueIndex = 0;
                dst->SecondValueIndex = step.Second is null ? (ushort)0xFFFF : (ushort)1;
                var rowSize = (nuint)step.Track.Length + (nuint)step.First.Length + (nuint)(step.Second?.Length ?? 0);
                var row = (byte*)Alloc(rowSize);
                new ReadOnlySpan<byte>(step.Track).CopyTo(new Span<byte>(row, step.Track.Length));
                new ReadOnlySpan<byte>(step.First).CopyTo(new Span<byte>(row + step.Track.Length, step.First.Length));
                if (step.Second is not null)
                    new ReadOnlySpan<byte>(step.Second).CopyTo(new Span<byte>(row + step.Track.Length + step.First.Length, step.Second.Length));
                dst->FrameRow = row;
                if (step.Lane is { Length: > 0 } lane)
                {
                    var laneBlock = (byte*)Alloc((nuint)lane.Length);
                    new ReadOnlySpan<byte>(lane).CopyTo(new Span<byte>(laneBlock, lane.Length));
                    dst->FoldLane = laneBlock;
                }
                else dst->FoldLane = null;
            }
            var events = new List<LibraryEvent>();
            var flat = 0;
            foreach (var step in spec.Steps)
            {
                if (step.WindowStart < step.WindowEnd)
                {
                    events.Add(new LibraryEvent { Tick = step.WindowStart, Kind = 0, StepIndex = flat, Marker = uint.MaxValue });
                    if (step.WindowEnd - 1 != step.WindowStart)
                        events.Add(new LibraryEvent { Tick = step.WindowEnd - 1, Kind = 1, StepIndex = flat, Marker = uint.MaxValue });
                }
                flat++;
            }
            events.Sort(static (a, b) =>
            {
                if (a.Tick != b.Tick) return a.Tick < b.Tick ? -1 : 1;
                if (a.Kind != b.Kind) return a.Kind < b.Kind ? -1 : 1;
                if (a.StepIndex != b.StepIndex) return a.StepIndex < b.StepIndex ? -1 : 1;
                return a.Marker < b.Marker ? -1 : a.Marker > b.Marker ? 1 : 0;
            });
            var eventBlock = (LibraryEvent*)Alloc((nuint)Math.Max(1, events.Count) * (nuint)sizeof(LibraryEvent));
            for (var i = 0; i < events.Count; i++) eventBlock[i] = events[i];
            var words = (spec.Duration + 63) / 64;
            var bits = (ulong*)Alloc(words * 8u);
            NativeMemory.Clear(bits, words * 8u);
            foreach (var e in events) if (e.Tick < spec.Duration) bits[e.Tick >> 6] |= 1UL << (int)(e.Tick & 63);
            tl->EventCount = events.Count;
            tl->Events = eventBlock;
            tl->EventTickBits = bits;
        }
        _view = view;
    }

    LibraryView _view;

    public void InjectInto(TimelineWorld world) => world.AdoptView(_view);
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var block in _owned) NativeMemory.AlignedFree((void*)block);
        _owned.Clear();
        GC.SuppressFinalize(this);
    }

    void* Alloc(nuint size)
    {
        var block = NativeMemory.AlignedAlloc(size == 0 ? 8u : size, 8u);
        NativeMemory.Clear(block, size == 0 ? 8u : size);
        _owned.Add((nint)block);
        return block;
    }
}
