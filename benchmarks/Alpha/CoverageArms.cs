using System.Runtime.InteropServices;
using Tl;
using Tl.TestSupport;

internal static class CoverageArms
{
    internal static void Run()
    {
        AssetSurfaceArms();
        QueryFrameArms();
        BankArms();
        SingleLaneArms();
        ConsumerArms();
        Console.WriteLine("coverage arms: asset surface, frame query, slot view, single-lane timeline and consumer installs verified");
    }

    static void Fail(string message) => throw new InvalidOperationException($"coverage arms failed: {message}");

    static (TimelineAsset, MeasuredLanes) BakeSurface()
    {
        var baker = new DomainBaker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(3))
            .Clip(0, 0, 64, new AlphaClip(5))
            .Track<BetaTrack, BetaClip>(new BetaTrack(4))
            .Clip(1, 16, 48, new BetaClip(7));
        var asset = TimelineAsset.Of(TimelineAsset.Load(baker.Looping().Bake()));
        var measured = MeasuredLanes.Measure(asset);
        if (measured.Duration != 64 || !measured.Looping)
            Fail($"measured lanes report duration={measured.Duration} looping={measured.Looping}.");
        return (asset, measured);
    }

    static void AssetSurfaceArms()
    {
        var (asset, measured) = BakeSurface();
        _ = asset.Reference;
        measured.Dispose();

        using var finite = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
            .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
            .Clip(0, 0, 64, new AlphaClip(2))
            .Bake()));
        using var finiteMeasured = MeasuredLanes.Measure(finite);
        if (finiteMeasured.Duration != 64 || finiteMeasured.Looping)
            Fail("finite measured lanes must report duration 64 without looping.");
    }

    static void QueryFrameArms()
    {
        var (asset, measured) = BakeSurface();
        try
        {
            var start = new TimelineComponent(asset.Reference) { Position = 0 };
            foreach (var frame in Timeline.Query<AlphaTrack, AlphaClip>(in start))
            {
                if (frame.TimelineTick != 0 || frame.ClipLength != 64 || frame.WithinClip != 0 || frame.TrackIndex != 0)
                    Fail($"start frame shape: tick={frame.TimelineTick} length={frame.ClipLength} within={frame.WithinClip} track={frame.TrackIndex}.");
                if (frame.IsBackward || frame.Direction != 1)
                    Fail("forward frame must not be backward.");
                if (frame.Track.Code != 3 || frame.Clip.Amount != 5)
                    Fail($"frame payload mismatch: track={frame.Track.Code} clip={frame.Clip.Amount}.");
            }

            var inside = new TimelineComponent(asset.Reference) { Position = 20 };
            foreach (var frame in Timeline.Query<BetaTrack, BetaClip>(in inside))
            {
                if (frame.TimelineTick != 20 || frame.ClipLength != 32 || frame.WithinClip != 4 || frame.TrackIndex != 1)
                    Fail($"beta frame shape flags={frame.Flags}: tick={frame.TimelineTick} length={frame.ClipLength} within={frame.WithinClip} track={frame.TrackIndex}.");
                if (frame.Clip.Amount != 7)
                    Fail($"beta frame payload mismatch: clip={frame.Clip.Amount}.");
            }

            var flags = FrameFlags.None;
            for (ushort position = 0; position < 64; position++)
            {
                var component = new TimelineComponent(asset.Reference) { Position = position };
                foreach (var frame in Timeline.Query<AlphaTrack, AlphaClip>(in component))
                {
                    flags |= frame.Flags;
                }
            }
            if ((flags & FrameFlags.ClipStart) == 0 || (flags & FrameFlags.ClipEnd) == 0
                || (flags & FrameFlags.TimelineStart) != 0 || (flags & FrameFlags.TimelineEnd) != 0)
                Fail($"swept flag union {flags} disagrees with the typed query flags.");
        }
        finally
        {
            measured.Dispose();
        }
    }

    static void BankArms()
    {
        var (asset, measured) = BakeSurface();
        try
        {
            ushort[] handles = [asset.Index];
            ushort[] positions = [1];
            float[] effects = [0f];
            Timeline<AlphaTrack, AlphaClip>.Apply(handles, positions, true, effects);
            if (effects[0] != 5f)
                Fail($"first-touch apply produced {effects[0]} instead of the clip amount 5.");

            var view = Timeline<AlphaTrack, AlphaClip>.View(asset);
            if (view.AbiVersion != SlotView.AbiVersionV1 || view.Duration != 64 || view.Looping != 1)
                Fail($"slot view header: abi={view.AbiVersion} duration={view.Duration} looping={view.Looping}.");
            if (view.Generation == 0 || view.TableTicks == 0 || view.RecordBytes == 0)
                Fail($"slot view table is empty: generation={view.Generation} ticks={view.TableTicks} recordBytes={view.RecordBytes}.");
            unsafe
            {
                if (view.Forward is null || view.ForwardRecords is null)
                    Fail("materialized slot view must expose the forward table and records.");
                if (view.Forward[0] != 5f)
                    Fail($"forward table entry 0 is {view.Forward[0]} instead of 5.");
                var record = view.ForwardRecords[0];
                if (record.Effect != 5f || record.Next != 1)
                    Fail($"forward record 0 is effect={record.Effect} next={record.Next}.");

                var byIndex = Timeline<AlphaTrack, AlphaClip>.View(handles[0]);
                if (byIndex.Duration != 64 || byIndex.Generation != view.Generation)
                    Fail("index view must resolve the same materialized slot.");
            }

            ushort[] next = [0];
            Timeline<AlphaTrack, AlphaClip>.Apply(asset.Index, positions, next, true, effects);
            if (next[0] != 2 || effects[0] != 10f)
                Fail($"fused asset apply produced next={next[0]} arrival effect={effects[0]}.");

            Timeline.Advance(asset, positions, true);
            if (positions[0] != 2)
                Fail($"asset advance left position {positions[0]}.");

            ushort[] source = [5];
            ushort[] advanced = [0];
            Timeline.Advance(asset, source, advanced, true);
            if (advanced[0] != 6)
                Fail($"asset advance copy produced {advanced[0]}.");

            ushort[] raw = [7];
            ushort[] rawNext = [0];
            Timeline.Advance(handles[0], raw, rawNext, true);
            if (rawNext[0] != 8)
                Fail($"index advance copy produced {rawNext[0]}.");

            using var genericAsset = TimelineAsset.Of(TimelineAsset.Load(new DomainBaker()
                .Track<AlphaTrack, AlphaClip>(new AlphaTrack(1))
                .Clip(0, 0, 64, new AlphaClip(1))
                .Looping()
                .Bake()));
            var wrappedIndex = new SlotIndex(genericAsset.Index);
            var wrappedPosition = new SlotPosition(10);
            var wrappedEffect = new SlotEffect(0f);
            Timeline<AlphaTrack, AlphaClip>.Apply(in wrappedIndex, in wrappedPosition, true, ref wrappedEffect);
            if (wrappedEffect.Value != 1f)
                Fail($"generic apply produced {wrappedEffect.Value}.");
            Timeline<AlphaTrack, AlphaClip>.Advance(in wrappedIndex, ref wrappedPosition, true);
            if (wrappedPosition.Value != 11)
                Fail($"generic advance left position {wrappedPosition.Value}.");
        }
        finally
        {
            measured.Dispose();
            asset.Dispose();
        }
    }

    readonly record struct SlotIndex(ushort Value);

    readonly record struct SlotPosition(ushort Value);

    readonly record struct SlotEffect(float Value);

    readonly record struct CodeLane : ITimelineLane<CodeLane>
    {
        public static ushort Duration => 64;
        public static bool Looping => true;
        public static float Effect(ushort position) => 7f;
        public static float InverseEffect(ushort position) => -7f;
    }

    static void SingleLaneArms()
    {
        ushort[] positions = [0, 32];
        float[] effects = [0f, 0f];
        Timeline<CodeLane>.Apply(positions, true, effects);
        if (effects[0] != 7f || effects[1] != 7f)
            Fail($"single-lane apply produced [{effects[0]}, {effects[1]}].");

        ushort[] source = [32];
        ushort[] next = [0];
        Timeline<CodeLane>.Apply(source, next, true, effects);
        if (next[0] != 33 || effects[0] != 14f)
            Fail($"single-lane copy apply produced next={next[0]} effect={effects[0]}.");

        Timeline<CodeLane>.Advance(positions, true);
        if (positions[0] != 1 || positions[1] != 33)
            Fail($"single-lane advance produced [{positions[0]}, {positions[1]}].");

        ushort[] wrap = [63];
        Timeline<CodeLane>.Advance(wrap, true);
        if (wrap[0] != 0)
            Fail($"looping advance from 63 produced {wrap[0]}.");

        ushort[] backward = [32];
        float[] inverse = [0f];
        Timeline<CodeLane>.Apply(backward, false, inverse);
        if (inverse[0] != -7f)
            Fail($"single-lane inverse apply produced {inverse[0]}.");
        Timeline<CodeLane>.Advance(backward, false);
        if (backward[0] != 31)
            Fail($"single-lane backward advance produced {backward[0]}.");

        ushort[] backSource = [5];
        ushort[] backNext = [0];
        Timeline<CodeLane>.Advance(backSource, backNext, false);
        if (backNext[0] != 4)
            Fail($"single-lane backward copy advance produced {backNext[0]}.");
    }

    static unsafe void Execute(byte* track, byte* clip, ushort tick, FrameFlags flags, void** outputs, int outputCount)
    {
    }

    static unsafe void ExecuteRange(byte* track, byte* clip, ushort tick, FrameFlags flags, void** outputs, int outputCount, int range)
    {
    }

    static unsafe void Bind(ulong* keys, int count, byte* slots)
    {
    }

    static unsafe void ConsumerArms()
    {
        if (PairRuntime<AlphaTrack, AlphaClip>.Key == 0)
            Fail("pair runtime key must be nonzero.");

        PairRuntime<AlphaTrack, AlphaClip>.Consume(&Execute, &Bind);
        PairRuntime<AlphaTrack, AlphaClip>.Consume(&Execute, &ExecuteRange, &Bind);
        PairRuntime<AlphaTrack, AlphaClip>.Consume(&Execute, &Bind, TickPurity.WindowConstant);
        PairRuntime<AlphaTrack, AlphaClip>.Consume(&Execute, &Bind, TickPurity.PerTick);
    }
}
