namespace Tl.Gen.Model;

public sealed class TimelineDefinition
{
    public required string Name { get; init; }
    public string Namespace { get; init; } = "Tl.Generated";
    public required string TrackTypeName { get; init; }
    public required string ClipTypeName { get; init; }
    public bool Loops { get; init; }
    public List<TrackDefinition> Tracks { get; init; } = [];
    public List<ClipDefinition> Clips { get; init; } = [];
    public IReadOnlyList<string> SourceUsings { get; init; } = [];
    public string? BlendMethodBody { get; init; }
}

public sealed class TrackDefinition
{
    public ushort Index { get; init; }
    public required string TrackExpression { get; init; }
}

public sealed class ClipDefinition
{
    public ushort TrackIndex { get; init; }
    public uint Start { get; init; }
    public uint End { get; init; }
    public required string PayloadExpression { get; init; }
    public float NumericValue { get; init; }
}
