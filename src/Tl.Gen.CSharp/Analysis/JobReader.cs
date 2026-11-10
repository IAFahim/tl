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
            return new([], [broken.ContractSite! with { Code = "TLGEN60", Message = "The exact Tl job declaration contracts could not be resolved." }]);
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
        return new(consumers, errors);
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

}
