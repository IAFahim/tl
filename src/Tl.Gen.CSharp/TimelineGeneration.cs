using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

/// <summary>
/// Chained generation: the entry point a host source generator uses when it synthesizes
/// timeline consumers itself. A generator's added sources are invisible to every other
/// generator in the same pass, so the host runs this pipeline over a compilation it has
/// already augmented with <c>AddSyntaxTrees</c> and emits the returned artifacts under its
/// own <c>AddSource</c>. The artifact names carry <c>id</c> so they cannot
/// collide with the ones this package's own generator emits for authored consumers.
/// </summary>
public static class TimelineGeneration
{
    public sealed record Artifact(string RelativePath, string Content);

    public sealed record Result(IReadOnlyList<DeclarationDiagnostic> Diagnostics, IReadOnlyList<Artifact> Artifacts);

    public static Result Run(CSharpCompilation compilation, ISet<string>? consumers = null, string id = "Hosted")
    {
        var model = JobReader.Read(compilation);
        if (model.Diagnostics.Count > 0)
            return new Result(model.Diagnostics.ToArray(), []);

        var selected = (consumers is null
                ? model.Consumers
                : model.Consumers.Where(consumer => consumers.Contains(consumer.Job.TypeName)))
            .ToArray();
        // Bake declarations live in authored source this package's own generator already
        // sees, so they stay with its pass; only the synthesized consumers ride along.
        var artifacts = JobEmitter.Emit(new JobReadResult(selected, [], []), id);
        return new Result([], artifacts
            .Select(artifact => new Artifact(artifact.RelativePath, artifact.Content))
            .ToArray());
    }
}
