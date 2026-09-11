using System.Runtime.CompilerServices;
using Tl;

public enum TickPattern
{
    Forward,
    Alternating,
}

public enum TimelineShape
{
    OneTrack,
    ThreeTracks,
    SixteenTracks,
    TwoHundredFiftySixTracks,
    Gap,
    Blend,
}

public struct Accumulator
{
    public long Value;
    public long Order;
    public long GameTickSum;
    public int Calls;
}

public readonly record struct AlphaClip(int Amount);
public readonly record struct BetaClip(int Amount);

public readonly record struct AlphaTrack(int Code) : IBlend<AlphaClip>
{
    public void Blend(in AlphaClip first, in AlphaClip second, float factor, out AlphaClip result)
        => result = new((int)(first.Amount + (second.Amount - first.Amount) * factor));
}

public readonly record struct BetaTrack(int Code) : IBlend<BetaClip>
{
    public void Blend(in BetaClip first, in BetaClip second, float factor, out BetaClip result)
        => result = new((int)(first.Amount + (second.Amount - first.Amount) * factor));
}

public readonly struct AlphaJob : ITimelineJob<AlphaTrack, AlphaClip>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(in Frame<AlphaTrack, AlphaClip> frame, ref Accumulator accumulator)
        => Kernels.Alpha(frame.Direction, frame.Track.Code, frame.Clip.Amount, frame.GameTick, ref accumulator);
}

public readonly struct BetaJob : ITimelineJob<BetaTrack, BetaClip>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(in Frame<BetaTrack, BetaClip> frame, ref Accumulator accumulator)
        => Kernels.Beta(frame.Direction, frame.Track.Code, frame.Clip.Amount, frame.GameTick, ref accumulator);
}

public readonly partial struct MixedTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var first = builder.Track(new AlphaTrack(1)).Use<AlphaJob>();
        var middle = builder.Track(new BetaTrack(2)).Use<BetaJob>();
        var last = builder.Track(new AlphaTrack(3)).Use<AlphaJob>();
        builder.Clip(first, new AlphaClip(1), 0u, 64u);
        builder.Clip(middle, new BetaClip(2), 0u, 64u);
        builder.Clip(last, new AlphaClip(3), 0u, 64u);
        builder.Looping();
    }
}

public readonly partial struct OneTrackTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(1)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(1), 0u, 64u);
        builder.Looping();
    }
}

public readonly partial struct SixteenTrackTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track01 = builder.Track(new AlphaTrack(1)).Use<AlphaJob>();
        var track02 = builder.Track(new AlphaTrack(2)).Use<AlphaJob>();
        var track03 = builder.Track(new AlphaTrack(3)).Use<AlphaJob>();
        var track04 = builder.Track(new AlphaTrack(4)).Use<AlphaJob>();
        var track05 = builder.Track(new AlphaTrack(5)).Use<AlphaJob>();
        var track06 = builder.Track(new AlphaTrack(6)).Use<AlphaJob>();
        var track07 = builder.Track(new AlphaTrack(7)).Use<AlphaJob>();
        var track08 = builder.Track(new AlphaTrack(8)).Use<AlphaJob>();
        var track09 = builder.Track(new AlphaTrack(9)).Use<AlphaJob>();
        var track10 = builder.Track(new AlphaTrack(10)).Use<AlphaJob>();
        var track11 = builder.Track(new AlphaTrack(11)).Use<AlphaJob>();
        var track12 = builder.Track(new AlphaTrack(12)).Use<AlphaJob>();
        var track13 = builder.Track(new AlphaTrack(13)).Use<AlphaJob>();
        var track14 = builder.Track(new AlphaTrack(14)).Use<AlphaJob>();
        var track15 = builder.Track(new AlphaTrack(15)).Use<AlphaJob>();
        var track16 = builder.Track(new AlphaTrack(16)).Use<AlphaJob>();
        builder.Clip(track01, new AlphaClip(1), 0u, 64u);
        builder.Clip(track02, new AlphaClip(2), 0u, 64u);
        builder.Clip(track03, new AlphaClip(3), 0u, 64u);
        builder.Clip(track04, new AlphaClip(4), 0u, 64u);
        builder.Clip(track05, new AlphaClip(5), 0u, 64u);
        builder.Clip(track06, new AlphaClip(6), 0u, 64u);
        builder.Clip(track07, new AlphaClip(7), 0u, 64u);
        builder.Clip(track08, new AlphaClip(8), 0u, 64u);
        builder.Clip(track09, new AlphaClip(9), 0u, 64u);
        builder.Clip(track10, new AlphaClip(10), 0u, 64u);
        builder.Clip(track11, new AlphaClip(11), 0u, 64u);
        builder.Clip(track12, new AlphaClip(12), 0u, 64u);
        builder.Clip(track13, new AlphaClip(13), 0u, 64u);
        builder.Clip(track14, new AlphaClip(14), 0u, 64u);
        builder.Clip(track15, new AlphaClip(15), 0u, 64u);
        builder.Clip(track16, new AlphaClip(16), 0u, 64u);
        builder.Looping();
    }
}

public readonly partial struct TwoHundredFiftySixTrackTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track001 = builder.Track(new AlphaTrack(1)).Use<AlphaJob>();
        builder.Clip(track001, new AlphaClip(1), 0u, 64u);
        var track002 = builder.Track(new AlphaTrack(2)).Use<AlphaJob>();
        builder.Clip(track002, new AlphaClip(2), 0u, 64u);
        var track003 = builder.Track(new AlphaTrack(3)).Use<AlphaJob>();
        builder.Clip(track003, new AlphaClip(3), 0u, 64u);
        var track004 = builder.Track(new AlphaTrack(4)).Use<AlphaJob>();
        builder.Clip(track004, new AlphaClip(4), 0u, 64u);
        var track005 = builder.Track(new AlphaTrack(5)).Use<AlphaJob>();
        builder.Clip(track005, new AlphaClip(5), 0u, 64u);
        var track006 = builder.Track(new AlphaTrack(6)).Use<AlphaJob>();
        builder.Clip(track006, new AlphaClip(6), 0u, 64u);
        var track007 = builder.Track(new AlphaTrack(7)).Use<AlphaJob>();
        builder.Clip(track007, new AlphaClip(7), 0u, 64u);
        var track008 = builder.Track(new AlphaTrack(8)).Use<AlphaJob>();
        builder.Clip(track008, new AlphaClip(8), 0u, 64u);
        var track009 = builder.Track(new AlphaTrack(9)).Use<AlphaJob>();
        builder.Clip(track009, new AlphaClip(9), 0u, 64u);
        var track010 = builder.Track(new AlphaTrack(10)).Use<AlphaJob>();
        builder.Clip(track010, new AlphaClip(10), 0u, 64u);
        var track011 = builder.Track(new AlphaTrack(11)).Use<AlphaJob>();
        builder.Clip(track011, new AlphaClip(11), 0u, 64u);
        var track012 = builder.Track(new AlphaTrack(12)).Use<AlphaJob>();
        builder.Clip(track012, new AlphaClip(12), 0u, 64u);
        var track013 = builder.Track(new AlphaTrack(13)).Use<AlphaJob>();
        builder.Clip(track013, new AlphaClip(13), 0u, 64u);
        var track014 = builder.Track(new AlphaTrack(14)).Use<AlphaJob>();
        builder.Clip(track014, new AlphaClip(14), 0u, 64u);
        var track015 = builder.Track(new AlphaTrack(15)).Use<AlphaJob>();
        builder.Clip(track015, new AlphaClip(15), 0u, 64u);
        var track016 = builder.Track(new AlphaTrack(16)).Use<AlphaJob>();
        builder.Clip(track016, new AlphaClip(16), 0u, 64u);
        var track017 = builder.Track(new AlphaTrack(17)).Use<AlphaJob>();
        builder.Clip(track017, new AlphaClip(17), 0u, 64u);
        var track018 = builder.Track(new AlphaTrack(18)).Use<AlphaJob>();
        builder.Clip(track018, new AlphaClip(18), 0u, 64u);
        var track019 = builder.Track(new AlphaTrack(19)).Use<AlphaJob>();
        builder.Clip(track019, new AlphaClip(19), 0u, 64u);
        var track020 = builder.Track(new AlphaTrack(20)).Use<AlphaJob>();
        builder.Clip(track020, new AlphaClip(20), 0u, 64u);
        var track021 = builder.Track(new AlphaTrack(21)).Use<AlphaJob>();
        builder.Clip(track021, new AlphaClip(21), 0u, 64u);
        var track022 = builder.Track(new AlphaTrack(22)).Use<AlphaJob>();
        builder.Clip(track022, new AlphaClip(22), 0u, 64u);
        var track023 = builder.Track(new AlphaTrack(23)).Use<AlphaJob>();
        builder.Clip(track023, new AlphaClip(23), 0u, 64u);
        var track024 = builder.Track(new AlphaTrack(24)).Use<AlphaJob>();
        builder.Clip(track024, new AlphaClip(24), 0u, 64u);
        var track025 = builder.Track(new AlphaTrack(25)).Use<AlphaJob>();
        builder.Clip(track025, new AlphaClip(25), 0u, 64u);
        var track026 = builder.Track(new AlphaTrack(26)).Use<AlphaJob>();
        builder.Clip(track026, new AlphaClip(26), 0u, 64u);
        var track027 = builder.Track(new AlphaTrack(27)).Use<AlphaJob>();
        builder.Clip(track027, new AlphaClip(27), 0u, 64u);
        var track028 = builder.Track(new AlphaTrack(28)).Use<AlphaJob>();
        builder.Clip(track028, new AlphaClip(28), 0u, 64u);
        var track029 = builder.Track(new AlphaTrack(29)).Use<AlphaJob>();
        builder.Clip(track029, new AlphaClip(29), 0u, 64u);
        var track030 = builder.Track(new AlphaTrack(30)).Use<AlphaJob>();
        builder.Clip(track030, new AlphaClip(30), 0u, 64u);
        var track031 = builder.Track(new AlphaTrack(31)).Use<AlphaJob>();
        builder.Clip(track031, new AlphaClip(31), 0u, 64u);
        var track032 = builder.Track(new AlphaTrack(32)).Use<AlphaJob>();
        builder.Clip(track032, new AlphaClip(32), 0u, 64u);
        var track033 = builder.Track(new AlphaTrack(33)).Use<AlphaJob>();
        builder.Clip(track033, new AlphaClip(33), 0u, 64u);
        var track034 = builder.Track(new AlphaTrack(34)).Use<AlphaJob>();
        builder.Clip(track034, new AlphaClip(34), 0u, 64u);
        var track035 = builder.Track(new AlphaTrack(35)).Use<AlphaJob>();
        builder.Clip(track035, new AlphaClip(35), 0u, 64u);
        var track036 = builder.Track(new AlphaTrack(36)).Use<AlphaJob>();
        builder.Clip(track036, new AlphaClip(36), 0u, 64u);
        var track037 = builder.Track(new AlphaTrack(37)).Use<AlphaJob>();
        builder.Clip(track037, new AlphaClip(37), 0u, 64u);
        var track038 = builder.Track(new AlphaTrack(38)).Use<AlphaJob>();
        builder.Clip(track038, new AlphaClip(38), 0u, 64u);
        var track039 = builder.Track(new AlphaTrack(39)).Use<AlphaJob>();
        builder.Clip(track039, new AlphaClip(39), 0u, 64u);
        var track040 = builder.Track(new AlphaTrack(40)).Use<AlphaJob>();
        builder.Clip(track040, new AlphaClip(40), 0u, 64u);
        var track041 = builder.Track(new AlphaTrack(41)).Use<AlphaJob>();
        builder.Clip(track041, new AlphaClip(41), 0u, 64u);
        var track042 = builder.Track(new AlphaTrack(42)).Use<AlphaJob>();
        builder.Clip(track042, new AlphaClip(42), 0u, 64u);
        var track043 = builder.Track(new AlphaTrack(43)).Use<AlphaJob>();
        builder.Clip(track043, new AlphaClip(43), 0u, 64u);
        var track044 = builder.Track(new AlphaTrack(44)).Use<AlphaJob>();
        builder.Clip(track044, new AlphaClip(44), 0u, 64u);
        var track045 = builder.Track(new AlphaTrack(45)).Use<AlphaJob>();
        builder.Clip(track045, new AlphaClip(45), 0u, 64u);
        var track046 = builder.Track(new AlphaTrack(46)).Use<AlphaJob>();
        builder.Clip(track046, new AlphaClip(46), 0u, 64u);
        var track047 = builder.Track(new AlphaTrack(47)).Use<AlphaJob>();
        builder.Clip(track047, new AlphaClip(47), 0u, 64u);
        var track048 = builder.Track(new AlphaTrack(48)).Use<AlphaJob>();
        builder.Clip(track048, new AlphaClip(48), 0u, 64u);
        var track049 = builder.Track(new AlphaTrack(49)).Use<AlphaJob>();
        builder.Clip(track049, new AlphaClip(49), 0u, 64u);
        var track050 = builder.Track(new AlphaTrack(50)).Use<AlphaJob>();
        builder.Clip(track050, new AlphaClip(50), 0u, 64u);
        var track051 = builder.Track(new AlphaTrack(51)).Use<AlphaJob>();
        builder.Clip(track051, new AlphaClip(51), 0u, 64u);
        var track052 = builder.Track(new AlphaTrack(52)).Use<AlphaJob>();
        builder.Clip(track052, new AlphaClip(52), 0u, 64u);
        var track053 = builder.Track(new AlphaTrack(53)).Use<AlphaJob>();
        builder.Clip(track053, new AlphaClip(53), 0u, 64u);
        var track054 = builder.Track(new AlphaTrack(54)).Use<AlphaJob>();
        builder.Clip(track054, new AlphaClip(54), 0u, 64u);
        var track055 = builder.Track(new AlphaTrack(55)).Use<AlphaJob>();
        builder.Clip(track055, new AlphaClip(55), 0u, 64u);
        var track056 = builder.Track(new AlphaTrack(56)).Use<AlphaJob>();
        builder.Clip(track056, new AlphaClip(56), 0u, 64u);
        var track057 = builder.Track(new AlphaTrack(57)).Use<AlphaJob>();
        builder.Clip(track057, new AlphaClip(57), 0u, 64u);
        var track058 = builder.Track(new AlphaTrack(58)).Use<AlphaJob>();
        builder.Clip(track058, new AlphaClip(58), 0u, 64u);
        var track059 = builder.Track(new AlphaTrack(59)).Use<AlphaJob>();
        builder.Clip(track059, new AlphaClip(59), 0u, 64u);
        var track060 = builder.Track(new AlphaTrack(60)).Use<AlphaJob>();
        builder.Clip(track060, new AlphaClip(60), 0u, 64u);
        var track061 = builder.Track(new AlphaTrack(61)).Use<AlphaJob>();
        builder.Clip(track061, new AlphaClip(61), 0u, 64u);
        var track062 = builder.Track(new AlphaTrack(62)).Use<AlphaJob>();
        builder.Clip(track062, new AlphaClip(62), 0u, 64u);
        var track063 = builder.Track(new AlphaTrack(63)).Use<AlphaJob>();
        builder.Clip(track063, new AlphaClip(63), 0u, 64u);
        var track064 = builder.Track(new AlphaTrack(64)).Use<AlphaJob>();
        builder.Clip(track064, new AlphaClip(64), 0u, 64u);
        var track065 = builder.Track(new AlphaTrack(65)).Use<AlphaJob>();
        builder.Clip(track065, new AlphaClip(65), 0u, 64u);
        var track066 = builder.Track(new AlphaTrack(66)).Use<AlphaJob>();
        builder.Clip(track066, new AlphaClip(66), 0u, 64u);
        var track067 = builder.Track(new AlphaTrack(67)).Use<AlphaJob>();
        builder.Clip(track067, new AlphaClip(67), 0u, 64u);
        var track068 = builder.Track(new AlphaTrack(68)).Use<AlphaJob>();
        builder.Clip(track068, new AlphaClip(68), 0u, 64u);
        var track069 = builder.Track(new AlphaTrack(69)).Use<AlphaJob>();
        builder.Clip(track069, new AlphaClip(69), 0u, 64u);
        var track070 = builder.Track(new AlphaTrack(70)).Use<AlphaJob>();
        builder.Clip(track070, new AlphaClip(70), 0u, 64u);
        var track071 = builder.Track(new AlphaTrack(71)).Use<AlphaJob>();
        builder.Clip(track071, new AlphaClip(71), 0u, 64u);
        var track072 = builder.Track(new AlphaTrack(72)).Use<AlphaJob>();
        builder.Clip(track072, new AlphaClip(72), 0u, 64u);
        var track073 = builder.Track(new AlphaTrack(73)).Use<AlphaJob>();
        builder.Clip(track073, new AlphaClip(73), 0u, 64u);
        var track074 = builder.Track(new AlphaTrack(74)).Use<AlphaJob>();
        builder.Clip(track074, new AlphaClip(74), 0u, 64u);
        var track075 = builder.Track(new AlphaTrack(75)).Use<AlphaJob>();
        builder.Clip(track075, new AlphaClip(75), 0u, 64u);
        var track076 = builder.Track(new AlphaTrack(76)).Use<AlphaJob>();
        builder.Clip(track076, new AlphaClip(76), 0u, 64u);
        var track077 = builder.Track(new AlphaTrack(77)).Use<AlphaJob>();
        builder.Clip(track077, new AlphaClip(77), 0u, 64u);
        var track078 = builder.Track(new AlphaTrack(78)).Use<AlphaJob>();
        builder.Clip(track078, new AlphaClip(78), 0u, 64u);
        var track079 = builder.Track(new AlphaTrack(79)).Use<AlphaJob>();
        builder.Clip(track079, new AlphaClip(79), 0u, 64u);
        var track080 = builder.Track(new AlphaTrack(80)).Use<AlphaJob>();
        builder.Clip(track080, new AlphaClip(80), 0u, 64u);
        var track081 = builder.Track(new AlphaTrack(81)).Use<AlphaJob>();
        builder.Clip(track081, new AlphaClip(81), 0u, 64u);
        var track082 = builder.Track(new AlphaTrack(82)).Use<AlphaJob>();
        builder.Clip(track082, new AlphaClip(82), 0u, 64u);
        var track083 = builder.Track(new AlphaTrack(83)).Use<AlphaJob>();
        builder.Clip(track083, new AlphaClip(83), 0u, 64u);
        var track084 = builder.Track(new AlphaTrack(84)).Use<AlphaJob>();
        builder.Clip(track084, new AlphaClip(84), 0u, 64u);
        var track085 = builder.Track(new AlphaTrack(85)).Use<AlphaJob>();
        builder.Clip(track085, new AlphaClip(85), 0u, 64u);
        var track086 = builder.Track(new AlphaTrack(86)).Use<AlphaJob>();
        builder.Clip(track086, new AlphaClip(86), 0u, 64u);
        var track087 = builder.Track(new AlphaTrack(87)).Use<AlphaJob>();
        builder.Clip(track087, new AlphaClip(87), 0u, 64u);
        var track088 = builder.Track(new AlphaTrack(88)).Use<AlphaJob>();
        builder.Clip(track088, new AlphaClip(88), 0u, 64u);
        var track089 = builder.Track(new AlphaTrack(89)).Use<AlphaJob>();
        builder.Clip(track089, new AlphaClip(89), 0u, 64u);
        var track090 = builder.Track(new AlphaTrack(90)).Use<AlphaJob>();
        builder.Clip(track090, new AlphaClip(90), 0u, 64u);
        var track091 = builder.Track(new AlphaTrack(91)).Use<AlphaJob>();
        builder.Clip(track091, new AlphaClip(91), 0u, 64u);
        var track092 = builder.Track(new AlphaTrack(92)).Use<AlphaJob>();
        builder.Clip(track092, new AlphaClip(92), 0u, 64u);
        var track093 = builder.Track(new AlphaTrack(93)).Use<AlphaJob>();
        builder.Clip(track093, new AlphaClip(93), 0u, 64u);
        var track094 = builder.Track(new AlphaTrack(94)).Use<AlphaJob>();
        builder.Clip(track094, new AlphaClip(94), 0u, 64u);
        var track095 = builder.Track(new AlphaTrack(95)).Use<AlphaJob>();
        builder.Clip(track095, new AlphaClip(95), 0u, 64u);
        var track096 = builder.Track(new AlphaTrack(96)).Use<AlphaJob>();
        builder.Clip(track096, new AlphaClip(96), 0u, 64u);
        var track097 = builder.Track(new AlphaTrack(97)).Use<AlphaJob>();
        builder.Clip(track097, new AlphaClip(97), 0u, 64u);
        var track098 = builder.Track(new AlphaTrack(98)).Use<AlphaJob>();
        builder.Clip(track098, new AlphaClip(98), 0u, 64u);
        var track099 = builder.Track(new AlphaTrack(99)).Use<AlphaJob>();
        builder.Clip(track099, new AlphaClip(99), 0u, 64u);
        var track100 = builder.Track(new AlphaTrack(100)).Use<AlphaJob>();
        builder.Clip(track100, new AlphaClip(100), 0u, 64u);
        var track101 = builder.Track(new AlphaTrack(101)).Use<AlphaJob>();
        builder.Clip(track101, new AlphaClip(101), 0u, 64u);
        var track102 = builder.Track(new AlphaTrack(102)).Use<AlphaJob>();
        builder.Clip(track102, new AlphaClip(102), 0u, 64u);
        var track103 = builder.Track(new AlphaTrack(103)).Use<AlphaJob>();
        builder.Clip(track103, new AlphaClip(103), 0u, 64u);
        var track104 = builder.Track(new AlphaTrack(104)).Use<AlphaJob>();
        builder.Clip(track104, new AlphaClip(104), 0u, 64u);
        var track105 = builder.Track(new AlphaTrack(105)).Use<AlphaJob>();
        builder.Clip(track105, new AlphaClip(105), 0u, 64u);
        var track106 = builder.Track(new AlphaTrack(106)).Use<AlphaJob>();
        builder.Clip(track106, new AlphaClip(106), 0u, 64u);
        var track107 = builder.Track(new AlphaTrack(107)).Use<AlphaJob>();
        builder.Clip(track107, new AlphaClip(107), 0u, 64u);
        var track108 = builder.Track(new AlphaTrack(108)).Use<AlphaJob>();
        builder.Clip(track108, new AlphaClip(108), 0u, 64u);
        var track109 = builder.Track(new AlphaTrack(109)).Use<AlphaJob>();
        builder.Clip(track109, new AlphaClip(109), 0u, 64u);
        var track110 = builder.Track(new AlphaTrack(110)).Use<AlphaJob>();
        builder.Clip(track110, new AlphaClip(110), 0u, 64u);
        var track111 = builder.Track(new AlphaTrack(111)).Use<AlphaJob>();
        builder.Clip(track111, new AlphaClip(111), 0u, 64u);
        var track112 = builder.Track(new AlphaTrack(112)).Use<AlphaJob>();
        builder.Clip(track112, new AlphaClip(112), 0u, 64u);
        var track113 = builder.Track(new AlphaTrack(113)).Use<AlphaJob>();
        builder.Clip(track113, new AlphaClip(113), 0u, 64u);
        var track114 = builder.Track(new AlphaTrack(114)).Use<AlphaJob>();
        builder.Clip(track114, new AlphaClip(114), 0u, 64u);
        var track115 = builder.Track(new AlphaTrack(115)).Use<AlphaJob>();
        builder.Clip(track115, new AlphaClip(115), 0u, 64u);
        var track116 = builder.Track(new AlphaTrack(116)).Use<AlphaJob>();
        builder.Clip(track116, new AlphaClip(116), 0u, 64u);
        var track117 = builder.Track(new AlphaTrack(117)).Use<AlphaJob>();
        builder.Clip(track117, new AlphaClip(117), 0u, 64u);
        var track118 = builder.Track(new AlphaTrack(118)).Use<AlphaJob>();
        builder.Clip(track118, new AlphaClip(118), 0u, 64u);
        var track119 = builder.Track(new AlphaTrack(119)).Use<AlphaJob>();
        builder.Clip(track119, new AlphaClip(119), 0u, 64u);
        var track120 = builder.Track(new AlphaTrack(120)).Use<AlphaJob>();
        builder.Clip(track120, new AlphaClip(120), 0u, 64u);
        var track121 = builder.Track(new AlphaTrack(121)).Use<AlphaJob>();
        builder.Clip(track121, new AlphaClip(121), 0u, 64u);
        var track122 = builder.Track(new AlphaTrack(122)).Use<AlphaJob>();
        builder.Clip(track122, new AlphaClip(122), 0u, 64u);
        var track123 = builder.Track(new AlphaTrack(123)).Use<AlphaJob>();
        builder.Clip(track123, new AlphaClip(123), 0u, 64u);
        var track124 = builder.Track(new AlphaTrack(124)).Use<AlphaJob>();
        builder.Clip(track124, new AlphaClip(124), 0u, 64u);
        var track125 = builder.Track(new AlphaTrack(125)).Use<AlphaJob>();
        builder.Clip(track125, new AlphaClip(125), 0u, 64u);
        var track126 = builder.Track(new AlphaTrack(126)).Use<AlphaJob>();
        builder.Clip(track126, new AlphaClip(126), 0u, 64u);
        var track127 = builder.Track(new AlphaTrack(127)).Use<AlphaJob>();
        builder.Clip(track127, new AlphaClip(127), 0u, 64u);
        var track128 = builder.Track(new AlphaTrack(128)).Use<AlphaJob>();
        builder.Clip(track128, new AlphaClip(128), 0u, 64u);
        var track129 = builder.Track(new AlphaTrack(129)).Use<AlphaJob>();
        builder.Clip(track129, new AlphaClip(129), 0u, 64u);
        var track130 = builder.Track(new AlphaTrack(130)).Use<AlphaJob>();
        builder.Clip(track130, new AlphaClip(130), 0u, 64u);
        var track131 = builder.Track(new AlphaTrack(131)).Use<AlphaJob>();
        builder.Clip(track131, new AlphaClip(131), 0u, 64u);
        var track132 = builder.Track(new AlphaTrack(132)).Use<AlphaJob>();
        builder.Clip(track132, new AlphaClip(132), 0u, 64u);
        var track133 = builder.Track(new AlphaTrack(133)).Use<AlphaJob>();
        builder.Clip(track133, new AlphaClip(133), 0u, 64u);
        var track134 = builder.Track(new AlphaTrack(134)).Use<AlphaJob>();
        builder.Clip(track134, new AlphaClip(134), 0u, 64u);
        var track135 = builder.Track(new AlphaTrack(135)).Use<AlphaJob>();
        builder.Clip(track135, new AlphaClip(135), 0u, 64u);
        var track136 = builder.Track(new AlphaTrack(136)).Use<AlphaJob>();
        builder.Clip(track136, new AlphaClip(136), 0u, 64u);
        var track137 = builder.Track(new AlphaTrack(137)).Use<AlphaJob>();
        builder.Clip(track137, new AlphaClip(137), 0u, 64u);
        var track138 = builder.Track(new AlphaTrack(138)).Use<AlphaJob>();
        builder.Clip(track138, new AlphaClip(138), 0u, 64u);
        var track139 = builder.Track(new AlphaTrack(139)).Use<AlphaJob>();
        builder.Clip(track139, new AlphaClip(139), 0u, 64u);
        var track140 = builder.Track(new AlphaTrack(140)).Use<AlphaJob>();
        builder.Clip(track140, new AlphaClip(140), 0u, 64u);
        var track141 = builder.Track(new AlphaTrack(141)).Use<AlphaJob>();
        builder.Clip(track141, new AlphaClip(141), 0u, 64u);
        var track142 = builder.Track(new AlphaTrack(142)).Use<AlphaJob>();
        builder.Clip(track142, new AlphaClip(142), 0u, 64u);
        var track143 = builder.Track(new AlphaTrack(143)).Use<AlphaJob>();
        builder.Clip(track143, new AlphaClip(143), 0u, 64u);
        var track144 = builder.Track(new AlphaTrack(144)).Use<AlphaJob>();
        builder.Clip(track144, new AlphaClip(144), 0u, 64u);
        var track145 = builder.Track(new AlphaTrack(145)).Use<AlphaJob>();
        builder.Clip(track145, new AlphaClip(145), 0u, 64u);
        var track146 = builder.Track(new AlphaTrack(146)).Use<AlphaJob>();
        builder.Clip(track146, new AlphaClip(146), 0u, 64u);
        var track147 = builder.Track(new AlphaTrack(147)).Use<AlphaJob>();
        builder.Clip(track147, new AlphaClip(147), 0u, 64u);
        var track148 = builder.Track(new AlphaTrack(148)).Use<AlphaJob>();
        builder.Clip(track148, new AlphaClip(148), 0u, 64u);
        var track149 = builder.Track(new AlphaTrack(149)).Use<AlphaJob>();
        builder.Clip(track149, new AlphaClip(149), 0u, 64u);
        var track150 = builder.Track(new AlphaTrack(150)).Use<AlphaJob>();
        builder.Clip(track150, new AlphaClip(150), 0u, 64u);
        var track151 = builder.Track(new AlphaTrack(151)).Use<AlphaJob>();
        builder.Clip(track151, new AlphaClip(151), 0u, 64u);
        var track152 = builder.Track(new AlphaTrack(152)).Use<AlphaJob>();
        builder.Clip(track152, new AlphaClip(152), 0u, 64u);
        var track153 = builder.Track(new AlphaTrack(153)).Use<AlphaJob>();
        builder.Clip(track153, new AlphaClip(153), 0u, 64u);
        var track154 = builder.Track(new AlphaTrack(154)).Use<AlphaJob>();
        builder.Clip(track154, new AlphaClip(154), 0u, 64u);
        var track155 = builder.Track(new AlphaTrack(155)).Use<AlphaJob>();
        builder.Clip(track155, new AlphaClip(155), 0u, 64u);
        var track156 = builder.Track(new AlphaTrack(156)).Use<AlphaJob>();
        builder.Clip(track156, new AlphaClip(156), 0u, 64u);
        var track157 = builder.Track(new AlphaTrack(157)).Use<AlphaJob>();
        builder.Clip(track157, new AlphaClip(157), 0u, 64u);
        var track158 = builder.Track(new AlphaTrack(158)).Use<AlphaJob>();
        builder.Clip(track158, new AlphaClip(158), 0u, 64u);
        var track159 = builder.Track(new AlphaTrack(159)).Use<AlphaJob>();
        builder.Clip(track159, new AlphaClip(159), 0u, 64u);
        var track160 = builder.Track(new AlphaTrack(160)).Use<AlphaJob>();
        builder.Clip(track160, new AlphaClip(160), 0u, 64u);
        var track161 = builder.Track(new AlphaTrack(161)).Use<AlphaJob>();
        builder.Clip(track161, new AlphaClip(161), 0u, 64u);
        var track162 = builder.Track(new AlphaTrack(162)).Use<AlphaJob>();
        builder.Clip(track162, new AlphaClip(162), 0u, 64u);
        var track163 = builder.Track(new AlphaTrack(163)).Use<AlphaJob>();
        builder.Clip(track163, new AlphaClip(163), 0u, 64u);
        var track164 = builder.Track(new AlphaTrack(164)).Use<AlphaJob>();
        builder.Clip(track164, new AlphaClip(164), 0u, 64u);
        var track165 = builder.Track(new AlphaTrack(165)).Use<AlphaJob>();
        builder.Clip(track165, new AlphaClip(165), 0u, 64u);
        var track166 = builder.Track(new AlphaTrack(166)).Use<AlphaJob>();
        builder.Clip(track166, new AlphaClip(166), 0u, 64u);
        var track167 = builder.Track(new AlphaTrack(167)).Use<AlphaJob>();
        builder.Clip(track167, new AlphaClip(167), 0u, 64u);
        var track168 = builder.Track(new AlphaTrack(168)).Use<AlphaJob>();
        builder.Clip(track168, new AlphaClip(168), 0u, 64u);
        var track169 = builder.Track(new AlphaTrack(169)).Use<AlphaJob>();
        builder.Clip(track169, new AlphaClip(169), 0u, 64u);
        var track170 = builder.Track(new AlphaTrack(170)).Use<AlphaJob>();
        builder.Clip(track170, new AlphaClip(170), 0u, 64u);
        var track171 = builder.Track(new AlphaTrack(171)).Use<AlphaJob>();
        builder.Clip(track171, new AlphaClip(171), 0u, 64u);
        var track172 = builder.Track(new AlphaTrack(172)).Use<AlphaJob>();
        builder.Clip(track172, new AlphaClip(172), 0u, 64u);
        var track173 = builder.Track(new AlphaTrack(173)).Use<AlphaJob>();
        builder.Clip(track173, new AlphaClip(173), 0u, 64u);
        var track174 = builder.Track(new AlphaTrack(174)).Use<AlphaJob>();
        builder.Clip(track174, new AlphaClip(174), 0u, 64u);
        var track175 = builder.Track(new AlphaTrack(175)).Use<AlphaJob>();
        builder.Clip(track175, new AlphaClip(175), 0u, 64u);
        var track176 = builder.Track(new AlphaTrack(176)).Use<AlphaJob>();
        builder.Clip(track176, new AlphaClip(176), 0u, 64u);
        var track177 = builder.Track(new AlphaTrack(177)).Use<AlphaJob>();
        builder.Clip(track177, new AlphaClip(177), 0u, 64u);
        var track178 = builder.Track(new AlphaTrack(178)).Use<AlphaJob>();
        builder.Clip(track178, new AlphaClip(178), 0u, 64u);
        var track179 = builder.Track(new AlphaTrack(179)).Use<AlphaJob>();
        builder.Clip(track179, new AlphaClip(179), 0u, 64u);
        var track180 = builder.Track(new AlphaTrack(180)).Use<AlphaJob>();
        builder.Clip(track180, new AlphaClip(180), 0u, 64u);
        var track181 = builder.Track(new AlphaTrack(181)).Use<AlphaJob>();
        builder.Clip(track181, new AlphaClip(181), 0u, 64u);
        var track182 = builder.Track(new AlphaTrack(182)).Use<AlphaJob>();
        builder.Clip(track182, new AlphaClip(182), 0u, 64u);
        var track183 = builder.Track(new AlphaTrack(183)).Use<AlphaJob>();
        builder.Clip(track183, new AlphaClip(183), 0u, 64u);
        var track184 = builder.Track(new AlphaTrack(184)).Use<AlphaJob>();
        builder.Clip(track184, new AlphaClip(184), 0u, 64u);
        var track185 = builder.Track(new AlphaTrack(185)).Use<AlphaJob>();
        builder.Clip(track185, new AlphaClip(185), 0u, 64u);
        var track186 = builder.Track(new AlphaTrack(186)).Use<AlphaJob>();
        builder.Clip(track186, new AlphaClip(186), 0u, 64u);
        var track187 = builder.Track(new AlphaTrack(187)).Use<AlphaJob>();
        builder.Clip(track187, new AlphaClip(187), 0u, 64u);
        var track188 = builder.Track(new AlphaTrack(188)).Use<AlphaJob>();
        builder.Clip(track188, new AlphaClip(188), 0u, 64u);
        var track189 = builder.Track(new AlphaTrack(189)).Use<AlphaJob>();
        builder.Clip(track189, new AlphaClip(189), 0u, 64u);
        var track190 = builder.Track(new AlphaTrack(190)).Use<AlphaJob>();
        builder.Clip(track190, new AlphaClip(190), 0u, 64u);
        var track191 = builder.Track(new AlphaTrack(191)).Use<AlphaJob>();
        builder.Clip(track191, new AlphaClip(191), 0u, 64u);
        var track192 = builder.Track(new AlphaTrack(192)).Use<AlphaJob>();
        builder.Clip(track192, new AlphaClip(192), 0u, 64u);
        var track193 = builder.Track(new AlphaTrack(193)).Use<AlphaJob>();
        builder.Clip(track193, new AlphaClip(193), 0u, 64u);
        var track194 = builder.Track(new AlphaTrack(194)).Use<AlphaJob>();
        builder.Clip(track194, new AlphaClip(194), 0u, 64u);
        var track195 = builder.Track(new AlphaTrack(195)).Use<AlphaJob>();
        builder.Clip(track195, new AlphaClip(195), 0u, 64u);
        var track196 = builder.Track(new AlphaTrack(196)).Use<AlphaJob>();
        builder.Clip(track196, new AlphaClip(196), 0u, 64u);
        var track197 = builder.Track(new AlphaTrack(197)).Use<AlphaJob>();
        builder.Clip(track197, new AlphaClip(197), 0u, 64u);
        var track198 = builder.Track(new AlphaTrack(198)).Use<AlphaJob>();
        builder.Clip(track198, new AlphaClip(198), 0u, 64u);
        var track199 = builder.Track(new AlphaTrack(199)).Use<AlphaJob>();
        builder.Clip(track199, new AlphaClip(199), 0u, 64u);
        var track200 = builder.Track(new AlphaTrack(200)).Use<AlphaJob>();
        builder.Clip(track200, new AlphaClip(200), 0u, 64u);
        var track201 = builder.Track(new AlphaTrack(201)).Use<AlphaJob>();
        builder.Clip(track201, new AlphaClip(201), 0u, 64u);
        var track202 = builder.Track(new AlphaTrack(202)).Use<AlphaJob>();
        builder.Clip(track202, new AlphaClip(202), 0u, 64u);
        var track203 = builder.Track(new AlphaTrack(203)).Use<AlphaJob>();
        builder.Clip(track203, new AlphaClip(203), 0u, 64u);
        var track204 = builder.Track(new AlphaTrack(204)).Use<AlphaJob>();
        builder.Clip(track204, new AlphaClip(204), 0u, 64u);
        var track205 = builder.Track(new AlphaTrack(205)).Use<AlphaJob>();
        builder.Clip(track205, new AlphaClip(205), 0u, 64u);
        var track206 = builder.Track(new AlphaTrack(206)).Use<AlphaJob>();
        builder.Clip(track206, new AlphaClip(206), 0u, 64u);
        var track207 = builder.Track(new AlphaTrack(207)).Use<AlphaJob>();
        builder.Clip(track207, new AlphaClip(207), 0u, 64u);
        var track208 = builder.Track(new AlphaTrack(208)).Use<AlphaJob>();
        builder.Clip(track208, new AlphaClip(208), 0u, 64u);
        var track209 = builder.Track(new AlphaTrack(209)).Use<AlphaJob>();
        builder.Clip(track209, new AlphaClip(209), 0u, 64u);
        var track210 = builder.Track(new AlphaTrack(210)).Use<AlphaJob>();
        builder.Clip(track210, new AlphaClip(210), 0u, 64u);
        var track211 = builder.Track(new AlphaTrack(211)).Use<AlphaJob>();
        builder.Clip(track211, new AlphaClip(211), 0u, 64u);
        var track212 = builder.Track(new AlphaTrack(212)).Use<AlphaJob>();
        builder.Clip(track212, new AlphaClip(212), 0u, 64u);
        var track213 = builder.Track(new AlphaTrack(213)).Use<AlphaJob>();
        builder.Clip(track213, new AlphaClip(213), 0u, 64u);
        var track214 = builder.Track(new AlphaTrack(214)).Use<AlphaJob>();
        builder.Clip(track214, new AlphaClip(214), 0u, 64u);
        var track215 = builder.Track(new AlphaTrack(215)).Use<AlphaJob>();
        builder.Clip(track215, new AlphaClip(215), 0u, 64u);
        var track216 = builder.Track(new AlphaTrack(216)).Use<AlphaJob>();
        builder.Clip(track216, new AlphaClip(216), 0u, 64u);
        var track217 = builder.Track(new AlphaTrack(217)).Use<AlphaJob>();
        builder.Clip(track217, new AlphaClip(217), 0u, 64u);
        var track218 = builder.Track(new AlphaTrack(218)).Use<AlphaJob>();
        builder.Clip(track218, new AlphaClip(218), 0u, 64u);
        var track219 = builder.Track(new AlphaTrack(219)).Use<AlphaJob>();
        builder.Clip(track219, new AlphaClip(219), 0u, 64u);
        var track220 = builder.Track(new AlphaTrack(220)).Use<AlphaJob>();
        builder.Clip(track220, new AlphaClip(220), 0u, 64u);
        var track221 = builder.Track(new AlphaTrack(221)).Use<AlphaJob>();
        builder.Clip(track221, new AlphaClip(221), 0u, 64u);
        var track222 = builder.Track(new AlphaTrack(222)).Use<AlphaJob>();
        builder.Clip(track222, new AlphaClip(222), 0u, 64u);
        var track223 = builder.Track(new AlphaTrack(223)).Use<AlphaJob>();
        builder.Clip(track223, new AlphaClip(223), 0u, 64u);
        var track224 = builder.Track(new AlphaTrack(224)).Use<AlphaJob>();
        builder.Clip(track224, new AlphaClip(224), 0u, 64u);
        var track225 = builder.Track(new AlphaTrack(225)).Use<AlphaJob>();
        builder.Clip(track225, new AlphaClip(225), 0u, 64u);
        var track226 = builder.Track(new AlphaTrack(226)).Use<AlphaJob>();
        builder.Clip(track226, new AlphaClip(226), 0u, 64u);
        var track227 = builder.Track(new AlphaTrack(227)).Use<AlphaJob>();
        builder.Clip(track227, new AlphaClip(227), 0u, 64u);
        var track228 = builder.Track(new AlphaTrack(228)).Use<AlphaJob>();
        builder.Clip(track228, new AlphaClip(228), 0u, 64u);
        var track229 = builder.Track(new AlphaTrack(229)).Use<AlphaJob>();
        builder.Clip(track229, new AlphaClip(229), 0u, 64u);
        var track230 = builder.Track(new AlphaTrack(230)).Use<AlphaJob>();
        builder.Clip(track230, new AlphaClip(230), 0u, 64u);
        var track231 = builder.Track(new AlphaTrack(231)).Use<AlphaJob>();
        builder.Clip(track231, new AlphaClip(231), 0u, 64u);
        var track232 = builder.Track(new AlphaTrack(232)).Use<AlphaJob>();
        builder.Clip(track232, new AlphaClip(232), 0u, 64u);
        var track233 = builder.Track(new AlphaTrack(233)).Use<AlphaJob>();
        builder.Clip(track233, new AlphaClip(233), 0u, 64u);
        var track234 = builder.Track(new AlphaTrack(234)).Use<AlphaJob>();
        builder.Clip(track234, new AlphaClip(234), 0u, 64u);
        var track235 = builder.Track(new AlphaTrack(235)).Use<AlphaJob>();
        builder.Clip(track235, new AlphaClip(235), 0u, 64u);
        var track236 = builder.Track(new AlphaTrack(236)).Use<AlphaJob>();
        builder.Clip(track236, new AlphaClip(236), 0u, 64u);
        var track237 = builder.Track(new AlphaTrack(237)).Use<AlphaJob>();
        builder.Clip(track237, new AlphaClip(237), 0u, 64u);
        var track238 = builder.Track(new AlphaTrack(238)).Use<AlphaJob>();
        builder.Clip(track238, new AlphaClip(238), 0u, 64u);
        var track239 = builder.Track(new AlphaTrack(239)).Use<AlphaJob>();
        builder.Clip(track239, new AlphaClip(239), 0u, 64u);
        var track240 = builder.Track(new AlphaTrack(240)).Use<AlphaJob>();
        builder.Clip(track240, new AlphaClip(240), 0u, 64u);
        var track241 = builder.Track(new AlphaTrack(241)).Use<AlphaJob>();
        builder.Clip(track241, new AlphaClip(241), 0u, 64u);
        var track242 = builder.Track(new AlphaTrack(242)).Use<AlphaJob>();
        builder.Clip(track242, new AlphaClip(242), 0u, 64u);
        var track243 = builder.Track(new AlphaTrack(243)).Use<AlphaJob>();
        builder.Clip(track243, new AlphaClip(243), 0u, 64u);
        var track244 = builder.Track(new AlphaTrack(244)).Use<AlphaJob>();
        builder.Clip(track244, new AlphaClip(244), 0u, 64u);
        var track245 = builder.Track(new AlphaTrack(245)).Use<AlphaJob>();
        builder.Clip(track245, new AlphaClip(245), 0u, 64u);
        var track246 = builder.Track(new AlphaTrack(246)).Use<AlphaJob>();
        builder.Clip(track246, new AlphaClip(246), 0u, 64u);
        var track247 = builder.Track(new AlphaTrack(247)).Use<AlphaJob>();
        builder.Clip(track247, new AlphaClip(247), 0u, 64u);
        var track248 = builder.Track(new AlphaTrack(248)).Use<AlphaJob>();
        builder.Clip(track248, new AlphaClip(248), 0u, 64u);
        var track249 = builder.Track(new AlphaTrack(249)).Use<AlphaJob>();
        builder.Clip(track249, new AlphaClip(249), 0u, 64u);
        var track250 = builder.Track(new AlphaTrack(250)).Use<AlphaJob>();
        builder.Clip(track250, new AlphaClip(250), 0u, 64u);
        var track251 = builder.Track(new AlphaTrack(251)).Use<AlphaJob>();
        builder.Clip(track251, new AlphaClip(251), 0u, 64u);
        var track252 = builder.Track(new AlphaTrack(252)).Use<AlphaJob>();
        builder.Clip(track252, new AlphaClip(252), 0u, 64u);
        var track253 = builder.Track(new AlphaTrack(253)).Use<AlphaJob>();
        builder.Clip(track253, new AlphaClip(253), 0u, 64u);
        var track254 = builder.Track(new AlphaTrack(254)).Use<AlphaJob>();
        builder.Clip(track254, new AlphaClip(254), 0u, 64u);
        var track255 = builder.Track(new AlphaTrack(255)).Use<AlphaJob>();
        builder.Clip(track255, new AlphaClip(255), 0u, 64u);
        var track256 = builder.Track(new AlphaTrack(256)).Use<AlphaJob>();
        builder.Clip(track256, new AlphaClip(256), 0u, 64u);
        builder.Looping();
    }
}

public readonly partial struct GapTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(4)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(1), 0u, 16u);
        builder.Clip(track, new AlphaClip(3), 32u, 64u);
        builder.Looping();
    }
}

public readonly partial struct BlendTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new AlphaTrack(5)).Use<AlphaJob>();
        builder.Clip(track, new AlphaClip(1), 0u, 64u);
        builder.Clip(track, new AlphaClip(9), 0u, 64u);
        builder.Looping();
    }
}

public readonly record struct FirstInput(int Value);
public readonly record struct SecondInput(int Value);
public readonly record struct ThirdInput(int Value);
public readonly record struct ComponentTrack(int Code) : IBlend<ComponentClip>
{
    public void Blend(in ComponentClip first, in ComponentClip second, float factor, out ComponentClip result)
        => result = new((int)(first.Amount + (second.Amount - first.Amount) * factor));
}

public readonly record struct ComponentClip(int Amount);

public readonly struct ComponentJob : ITimelineJob<ComponentTrack, ComponentClip>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Execute(
        in Frame<ComponentTrack, ComponentClip> frame,
        in FirstInput first,
        in SecondInput second,
        in ThirdInput third,
        ref Accumulator accumulator)
        => Kernels.Component(
            frame.Direction,
            frame.Track.Code,
            frame.Clip.Amount,
            frame.GameTick,
            first.Value,
            second.Value,
            third.Value,
            ref accumulator);
}

public readonly partial struct ComponentTimeline : ITimeline
{
    public static void Define(scoped Builder builder)
    {
        var track = builder.Track(new ComponentTrack(1)).Use<ComponentJob>();
        builder.Clip(track, new ComponentClip(1), 0u, 64u);
        builder.Looping();
    }
}

public readonly struct BenchmarkRows;
public readonly struct OneTrackRows;
public readonly struct ThreeTrackRows;
public readonly struct SixteenTrackRows;
public readonly struct TwoHundredFiftySixTrackRows;
public readonly struct GapRows;
public readonly struct BlendRows;
public readonly struct ComponentRows;
public readonly struct MixedShapeRows;

public readonly partial struct BenchmarkCatalog : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<BenchmarkRows>().Asset<MixedTimeline>();
    }
}

public readonly partial struct ShapeCatalog : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<OneTrackRows>().Asset<OneTrackTimeline>();
        builder.Schema<ThreeTrackRows>().Asset<MixedTimeline>();
        builder.Schema<SixteenTrackRows>().Asset<SixteenTrackTimeline>();
        builder.Schema<TwoHundredFiftySixTrackRows>().Asset<TwoHundredFiftySixTrackTimeline>();
        builder.Schema<GapRows>().Asset<GapTimeline>();
        builder.Schema<BlendRows>().Asset<BlendTimeline>();
        builder.Schema<ComponentRows>().Asset<ComponentTimeline>();
    }
}

public readonly partial struct MixedShapeCatalog : ITimelineCatalog
{
    public static void Define(scoped CatalogBuilder builder)
    {
        builder.Schema<MixedShapeRows>()
            .Asset<OneTrackTimeline>()
            .Asset<MixedTimeline>()
            .Asset<SixteenTrackTimeline>()
            .Asset<GapTimeline>()
            .Asset<BlendTimeline>();
    }
}

public readonly record struct BenchmarkReceipt(
    long StateHash,
    long ValueHash,
    long OrderHash,
    long GameTickHash,
    long Calls);

internal static class Kernels
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Alpha(int direction, int code, int amount, uint gameTick, ref Accumulator accumulator)
    {
        accumulator.Value = unchecked(accumulator.Value * 31 + direction * amount);
        accumulator.Order = unchecked(accumulator.Order * 10 + code);
        accumulator.GameTickSum = unchecked(accumulator.GameTickSum + gameTick);
        accumulator.Calls++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Beta(int direction, int code, int amount, uint gameTick, ref Accumulator accumulator)
    {
        accumulator.Value = unchecked(accumulator.Value * 37 + direction * amount);
        accumulator.Order = unchecked(accumulator.Order * 10 + code);
        accumulator.GameTickSum = unchecked(accumulator.GameTickSum + gameTick);
        accumulator.Calls++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Component(
        int direction,
        int code,
        int amount,
        uint gameTick,
        int first,
        int second,
        int third,
        ref Accumulator accumulator)
    {
        accumulator.Value = unchecked(accumulator.Value * 31 + direction * (amount + first + second + third));
        accumulator.Order = unchecked(accumulator.Order * 10 + code);
        accumulator.GameTickSum = unchecked(accumulator.GameTickSum + gameTick);
        accumulator.Calls++;
    }
}
