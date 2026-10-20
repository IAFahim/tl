using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

internal static class UnityJobEmitter
{
    internal static IReadOnlyList<CompileArtifact> Emit(JobReadResult model)
        => model.Consumers.Count == 0 && model.Bakes.Count == 0 ? [] : [new CompileArtifact("TlConsumerBinding.g.cs", JobEmitter.Consumers(model.Consumers, model.Bakes, ""))];
}
