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

internal sealed record ConsumerMethod(ConsumerMethodKind Kind, IReadOnlyList<ConsumerParam> Params, IReadOnlyList<string> FoldTypes, IReadOnlyList<int> FoldSizes);

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
    IReadOnlyList<DeclarationDiagnostic> Diagnostics);

internal sealed record SystemAssignment(
    SystemDecl System,
    IReadOnlyList<SystemConsumer> Consumers,
    IReadOnlyList<DeclarationDiagnostic> Diagnostics);
