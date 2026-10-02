using Microsoft.CodeAnalysis;
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

    /// <summary>
    /// Runs the pipeline over a compilation the host has augmented.
    /// <paramref name="synthesized"/> must be exactly the syntax trees the host itself added:
    /// consumers declared in any other tree are authored and belong to the package generator's
    /// own pass, so the chained pass never registers them (naming one in
    /// <paramref name="consumers"/> is TLGEN84). A filter entry that matches no timeline
    /// consumer is TLGEN83; filters compare <c>global::</c>-qualified type names. Bakes whose
    /// consumer is synthesized ride along in the chained artifacts; bakes for authored
    /// consumers stay with the package pass. <paramref name="id"/> must be a non-empty ASCII
    /// identifier fragment (TLGEN82); it is spliced into generated type and file names.
    /// </summary>
    public static Result Run(CSharpCompilation compilation, ISet<SyntaxTree> synthesized, ISet<string>? consumers = null, string id = "Hosted")
    {
        if (compilation is null) throw new ArgumentNullException(nameof(compilation));
        if (synthesized is null) throw new ArgumentNullException(nameof(synthesized));
        if (!IdentifierFragment(id))
            return new Result([new DeclarationDiagnostic("", 1, 1, "TLGEN82",
                $"TimelineGeneration.Run id '{id}' must be a non-empty ASCII identifier fragment (letter or underscore first, then letters, digits, or underscores); it is spliced into generated type and file names.")], []);
        var trees = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal);
        var model = JobReader.Read(compilation, trees);
        if (model.Diagnostics.Count > 0)
            return new Result(model.Diagnostics.ToArray(), []);

        bool HostSynthesized(JobConsumer consumer)
            => trees.TryGetValue(consumer.Job.TypeName, out var tree) && synthesized.Contains(tree);

        var selected = model.Consumers.Where(HostSynthesized).ToArray();
        var errors = new List<DeclarationDiagnostic>();
        if (consumers is not null)
            foreach (var wanted in consumers)
            {
                var match = model.Consumers.FirstOrDefault(consumer => consumer.Job.TypeName == wanted);
                if (match is null)
                    errors.Add(new DeclarationDiagnostic("", 1, 1, "TLGEN83",
                        $"Consumer filter '{wanted}' matched no timeline consumer; filters compare global::-qualified type names."));
                else if (!HostSynthesized(match))
                    errors.Add(new DeclarationDiagnostic("", 1, 1, "TLGEN84",
                        $"Consumer '{wanted}' is declared in authored source; the chained pass must not register it because the package generator's own pass already does."));
            }
        if (errors.Count > 0)
            return new Result(errors.ToArray(), []);

        var selectedNames = new HashSet<string>(selected.Select(static consumer => consumer.Job.TypeName), StringComparer.Ordinal);
        var bakes = model.Bakes.Where(bake => selectedNames.Contains(bake.ConsumerTypeName)).ToArray();
        var artifacts = JobEmitter.Emit(new JobReadResult(selected, [], bakes), id);
        return new Result([], artifacts
            .Select(artifact => new Artifact(artifact.RelativePath, artifact.Content))
            .ToArray());
    }

    static bool IdentifierFragment(string id)
        => id.Length > 0
            && id[0] is not ((>= '0' and <= '9'))
            && id.All(static c => c is ((>= '0' and <= '9')) or ((>= 'A' and <= 'Z')) or ((>= 'a' and <= 'z')) or '_');
}
