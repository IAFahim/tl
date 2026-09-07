namespace Tl;

public readonly record struct TimelineOptions
{
    public static readonly TimelineOptions Default = new();

    public bool DedupStorage { get; init; } = false;

    public TimelineOptions() { }
}
