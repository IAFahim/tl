using Tl.Gen.CSharp.Analysis;

namespace Tl.Gen.CSharp.Model;

public enum SlotMode : byte
{
    Input,
    Reference,
}

public sealed record TimelineSlot(string Name, string TypeName, SlotMode Mode);

public sealed record JobDefinition(string TypeName, IReadOnlyList<TimelineSlot> Slots);

public sealed record JobConsumer(string TrackTypeName, string ClipTypeName, JobDefinition Job);

public sealed record JobReadResult(IReadOnlyList<JobConsumer> Consumers, IReadOnlyList<DeclarationDiagnostic> Diagnostics);
