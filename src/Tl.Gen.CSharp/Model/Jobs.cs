using Tl.Gen.CSharp.Analysis;

namespace Tl.Gen.CSharp.Model;

public sealed record JobDefinition(string TypeName);

public sealed record JobConsumer(string TrackTypeName, string ClipTypeName, JobDefinition Job, string Identity, ulong Layout = 0, IReadOnlyList<string>? LayoutTypes = null);

public sealed record JobReadResult(
    IReadOnlyList<JobConsumer> Consumers,
    IReadOnlyList<DeclarationDiagnostic> Diagnostics);

