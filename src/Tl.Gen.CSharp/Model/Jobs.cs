using Tl.Gen.CSharp.Analysis;

namespace Tl.Gen.CSharp.Model;

public enum SlotMode : byte { Input, Reference, Output }

public sealed record TimelineSlot(string Name, string TypeName, SlotMode Mode, byte Size = 4);

public sealed record JobDefinition(string TypeName, IReadOnlyList<TimelineSlot> Slots, bool Dispatch = false, bool LiveFrame = false, bool MemoMethod = false);

public sealed record JobConsumer(string TrackTypeName, string ClipTypeName, JobDefinition Job);

public enum BakeModifier : byte
{
    Value,
    In,
    Ref,
}

public sealed record BakeParameter(string TypeName, BakeModifier Modifier, bool IsConsumer);

public sealed record BakeDeclaration(
    string TypeName,
    string ConsumerTypeName,
    IReadOnlyList<BakeParameter> Parameters,
    IReadOnlyList<JobConsumer> Pairs)
{
    public string SignatureKey => string.Join("\0", Parameters
        .Select(static parameter => $"{parameter.TypeName}\0{(byte)parameter.Modifier}\0{(parameter.IsConsumer ? 1 : 0)}"));
}

public sealed record JobReadResult(
    IReadOnlyList<JobConsumer> Consumers,
    IReadOnlyList<DeclarationDiagnostic> Diagnostics,
    IReadOnlyList<BakeDeclaration> Bakes);
