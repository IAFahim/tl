using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

public static class JobReader
{
    internal const int SlotRow = 40;

    public static JobReadResult Read(CSharpCompilation compilation) => Read(compilation, null);

    internal static JobReadResult Read(CSharpCompilation compilation, Dictionary<string, SyntaxTree>? consumerTrees)
    {
        if (compilation is null) throw new ArgumentNullException(nameof(compilation));
        return Merge(Walk(compilation, consumerTrees));
    }

    internal static IReadOnlyList<BakeDeclaration> Bakes(CSharpCompilation compilation, IReadOnlyList<JobConsumer> consumers, List<DeclarationDiagnostic> errors)
    {
        if (compilation is null) throw new ArgumentNullException(nameof(compilation));
        return Bakes(Order(Walk(compilation, null)), consumers, errors);
    }

    private static List<TypeAnalysis> Walk(CSharpCompilation compilation, Dictionary<string, SyntaxTree>? consumerTrees)
    {
        var analyses = new List<TypeAnalysis>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            foreach (var syntax in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                if (TypeAnalyzer.IsCandidate(syntax) && TypeAnalyzer.Analyze(model, syntax, CancellationToken.None) is { } analysis)
                {
                    analyses.Add(analysis);
                    if (consumerTrees is not null)
                        foreach (var consumer in analysis.Consumers)
                            if (!consumerTrees.ContainsKey(consumer.Job.TypeName))
                                consumerTrees[consumer.Job.TypeName] = tree;
                }
        }
        return analyses;
    }

    internal static JobReadResult Merge(IReadOnlyList<TypeAnalysis> analyses)
    {
        var ordered = Order(analyses);
        var broken = ordered.Find(static analysis => analysis.ContractSite is not null);
        if (broken is not null)
            return new([], [broken.ContractSite! with { Code = "TLGEN60", Message = "The exact Tl job declaration contracts could not be resolved." }], []);
        var errors = new List<DeclarationDiagnostic>();
        var consumers = new List<JobConsumer>();
        var pairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var analysis in ordered)
        {
            errors.AddRange(analysis.JobDiagnostics);
            foreach (var consumer in analysis.Consumers)
                if (pairs.Add(consumer.Job.TypeName + "\0" + consumer.TrackTypeName + "\0" + consumer.ClipTypeName))
                    consumers.Add(consumer);
        }
        return new(consumers, errors, Bakes(ordered, consumers, errors));
    }

    private static List<TypeAnalysis> Order(IReadOnlyList<TypeAnalysis> analyses)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unique = new List<TypeAnalysis>();
        foreach (var analysis in analyses)
            if (seen.Add(analysis.TypeName))
                unique.Add(analysis);
        return unique.OrderBy(static analysis => analysis.TypeName, StringComparer.Ordinal).ToList();
    }

    internal static IReadOnlyList<BakeDeclaration> Bakes(List<TypeAnalysis> ordered, IReadOnlyList<JobConsumer> consumers, List<DeclarationDiagnostic> errors)
    {
        var pairings = new Dictionary<string, List<JobConsumer>>(StringComparer.Ordinal);
        foreach (var consumer in consumers)
        {
            if (!pairings.TryGetValue(consumer.Job.TypeName, out var list)) pairings[consumer.Job.TypeName] = list = [];
            list.Add(consumer);
        }
        var discovered = new List<(BakeCandidate Candidate, List<JobConsumer> Pairs)>();
        foreach (var analysis in ordered)
        {
            errors.AddRange(analysis.BakeDiagnostics);
            foreach (var candidate in analysis.Bakes)
            {
                if (candidate.OpenConsumerTypeParameter) { errors.AddRange(candidate.Diagnostics); continue; }
                if (!pairings.TryGetValue(candidate.Declaration.ConsumerTypeName, out var pairs))
                {
                    errors.Add(candidate.Site with { Code = "TLGEN72", Message = $"Bake '{candidate.Declaration.TypeName}' binds consumer '{candidate.Declaration.ConsumerTypeName}', which is not a registered Tl.ITrack consumer; declare the consumer with its ITrack pairing before binding a bake to it." });
                    continue;
                }
                errors.AddRange(candidate.Diagnostics);
                if (candidate.Diagnostics.Count > 0) continue;
                discovered.Add((candidate, pairs));
            }
        }
        var chains = new Dictionary<string, (string TypeName, string Signature, string SignatureKey)>(StringComparer.Ordinal);
        var accepted = new List<(BakeCandidate Candidate, List<JobConsumer> Pairs)>();
        foreach (var (candidate, pairs) in discovered)
        {
            var conflict = false;
            foreach (var pair in pairs)
            {
                var key = pair.TrackTypeName + "\0" + pair.ClipTypeName;
                if (chains.TryGetValue(key, out var first) && first.SignatureKey != candidate.Declaration.SignatureKey)
                {
                    errors.Add(candidate.Site with { Code = "TLGEN74", Message = $"Bakes '{first.TypeName}' and '{candidate.Declaration.TypeName}' bind pair ({pair.TrackTypeName}, {pair.ClipTypeName}) with different bake parameter lists: '{first.Signature}' and '{candidate.Signature}'; every bake registered to one pair must declare an identical parameter list." });
                    conflict = true;
                }
            }
            if (conflict) continue;
            foreach (var pair in pairs)
            {
                var key = pair.TrackTypeName + "\0" + pair.ClipTypeName;
                if (!chains.ContainsKey(key)) chains[key] = (candidate.Declaration.TypeName, candidate.Signature, candidate.Declaration.SignatureKey);
            }
            accepted.Add((candidate, pairs));
        }
        return accepted
            .OrderBy(static item => item.Candidate.Declaration.ConsumerTypeName, StringComparer.Ordinal)
            .ThenBy(static item => item.Candidate.Declaration.SignatureKey, StringComparer.Ordinal)
            .ThenBy(static item => item.Candidate.Declaration.TypeName, StringComparer.Ordinal)
            .Select(static item => item.Candidate.Declaration with { Pairs = item.Pairs })
            .ToArray();
    }
}
