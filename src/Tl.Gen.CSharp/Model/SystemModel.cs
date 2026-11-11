using System.Collections.Generic;
using Tl.Gen.CSharp.Analysis;

namespace Tl.Gen.CSharp.Model;

internal enum SystemFieldKind : byte
{
    Column,
    ColumnReadOnly,
    Context,
    Requests,
    Events,
}

internal sealed record SystemField(string Name, string TypeName, SystemFieldKind Kind, string ElementTypeName);

internal sealed record SystemDecl(string TypeName, bool IsRef, ulong SystemKey, IReadOnlyList<SystemField> Fields);

internal enum ConsumerMethodKind : byte
{
    Fold,
    ExecuteActive,
    ExecuteBatch,
    OnClipStart,
    OnClipEnd,
    OnMarker,
    Feeder,
}

internal enum ConsumerParamKind : byte
{
    Frame,
    MarkerFrame,
    FoldFeed,
    FoldResult,
    BatchItems,
    Column,
    ColumnReadOnly,
    Context,
    Requests,
    Events,
}

internal sealed record ConsumerParam(string Name, string TypeName, string Modifier, ConsumerParamKind Kind, int FoldIndex, string BatchElement);

internal sealed record DirectLane(bool Eligible, string Op, string WrittenType, string WrittenMember, IReadOnlyList<int> FoldOffsets, IReadOnlyList<string> ReadTypes)
{
    public static readonly DirectLane None = new(false, "", "", "", [], []);
}

internal sealed record ConsumerMethod(ConsumerMethodKind Kind, IReadOnlyList<ConsumerParam> Params, IReadOnlyList<string> FoldTypes, IReadOnlyList<int> FoldSizes)
{
    public DirectLane Direct { get; init; } = DirectLane.None;
}

internal enum FeederTrigger : byte
{
    Span,
    Start,
    End,
    Marker,
}

internal sealed record FeederDecl(string KindName, FeederTrigger Trigger, string MethodName, string ConsumerMetadata, string ValueTypeName, ConsumerMethod Method);

internal enum DerivedKind : byte { None, Sum, Set, While }

internal enum DerivedFieldKind : byte { Integer, Float, Boolean, Other }

internal sealed record DerivedField(string Name, DerivedFieldKind Kind);

internal sealed record KindDecl(
    string Name,
    string TypeName,
    string ValueTypeName,
    bool Each,
    int Accepts,
    int Order,
    string Group,
    IReadOnlyList<ConsumerParam> ApplyParams,
    IReadOnlyList<string> MemberSystems)
{
    public bool Foreign { get; init; }
    public string Id { get; init; } = "";
    public IReadOnlyList<ConsumerParam>? IdleParams { get; init; }
    public DerivedKind Derived { get; init; }
    public IReadOnlyList<DerivedField> DerivedFields { get; init; } = [];
}

internal sealed record SystemConsumer(
    string ConsumerTypeName,
    string TrackTypeName,
    string ClipTypeName,
    string MarkerTypeName,
    bool IsMarker,
    bool TrackBlendable,
    ulong PairKey,
    IReadOnlyList<string> MemberSystems,
    IReadOnlyList<ConsumerMethod> Methods,
    IReadOnlyList<DeclarationDiagnostic> Diagnostics)
{
    public IReadOnlyList<FeederDecl> Feeders { get; init; } = [];
}

internal sealed record SystemAssignment(
    SystemDecl System,
    IReadOnlyList<SystemConsumer> Consumers,
    IReadOnlyList<DeclarationDiagnostic> Diagnostics)
{
    public IReadOnlyList<KindDecl> Kinds { get; init; } = [];
    public bool Parallel { get; init; } = true;
}
