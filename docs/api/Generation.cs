namespace Tl.Model
{
    public enum Kind : byte { I8, U8, I16, U16, I32, U32, I64, U64, F32, F64, Record }

    public sealed record Field(string Name, Kind Kind, Schema? Nested = null);
    public sealed record Schema(string Name, IReadOnlyList<Field> Fields);
    public readonly record struct Value(Kind Kind, ulong Bits, IReadOnlyList<Value>? Fields = null);

    public sealed record Clip(int Start, int End, IReadOnlyList<Value> Payload, Ease Ease = Ease.Linear);
    public sealed record Track(string Name, int Binding, TrackMode Mode, Schema Schema, IReadOnlyList<Clip> Clips);
    public sealed record Timeline(string Name, int Duration, bool Loop, IReadOnlyList<Track> Tracks);
}

namespace Tl.Generation
{
    public enum Severity : byte { Info, Warning, Error }
    public enum BoundaryKind : byte { ClipStart, BlendStart, ClipInstant, BlendInstant, BlendEnd, ClipEnd }

    public sealed record Diagnostic(Severity Severity, string Code, string Message, int? Track = null, int? Clip = null);
    public sealed record SourceFile(string Path, string Text);
    public readonly record struct Region(int Start, int End, int ClipA, int? ClipB);
    public readonly record struct Boundary(int Tick, int ClipA, int? ClipB, BoundaryKind Kind);
    public sealed record TrackPlan(int Track, IReadOnlyList<Region> Regions, IReadOnlyList<Boundary> Boundaries);
    public sealed record Plan(Model.Timeline Timeline, IReadOnlyList<TrackPlan> Tracks);

    public sealed record Result(IReadOnlyList<SourceFile> Files, IReadOnlyList<Diagnostic> Diagnostics)
    {
        public bool IsSuccess => throw new NotImplementedException();
    }

    public interface IAdapter
    {
        IReadOnlyList<SourceFile> Emit(Plan plan);
    }

    public static class Generator
    {
        public static Result Generate(Model.Timeline timeline, IAdapter adapter)
            => throw new NotImplementedException();
    }
}

namespace Tl.CSharp
{
    public readonly record struct Clip<T>(int Start, int End, T Payload, Ease Ease = Ease.Linear)
        where T : unmanaged;

    public static class Clip
    {
        public static Clip<T> Range<T>(int start, int end, in T payload, Ease ease = Ease.Linear)
            where T : unmanaged
            => throw new NotImplementedException();

        public static Clip<T> At<T>(int tick, in T payload)
            where T : unmanaged
            => throw new NotImplementedException();
    }

    public sealed class Timeline
    {
        private Timeline() { }

        public static Timeline Define(string name, int duration, bool loop = false)
            => throw new NotImplementedException();

        public Timeline Track<T>(
            string name, int binding, TrackMode mode, params Clip<T>[] clips)
            where T : unmanaged
            => throw new NotImplementedException();

        public Model.Timeline Build() => throw new NotImplementedException();
    }

    public sealed record Options(string Namespace);

    public sealed class Adapter(Options options) : Generation.IAdapter
    {
        public Options Options { get; } = options;

        public IReadOnlyList<Generation.SourceFile> Emit(Generation.Plan plan)
            => throw new NotImplementedException();
    }
}
