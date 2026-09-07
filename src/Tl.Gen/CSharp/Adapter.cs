using Tl.Gen.Model;

namespace Tl.Gen.CSharp;

public sealed class CSharpAdapter : IAdapter
{
    public static readonly CSharpAdapter Default = new();

    public IReadOnlyList<SourceFile> Emit(TimelinePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var files = new List<SourceFile>();

        // 1. General table emission (always emitted)
        var tableSource = TableEmitter.Emit(plan);
        files.Add(new SourceFile($"{plan.Definition.Name}.g.cs", tableSource));

        // 2. Specialized bake if requested and supported
        if (plan.Strategy == EmissionStrategy.SpecializedBake && BakeEmitter.CanBake(plan))
        {
            var bakeSource = BakeEmitter.Emit(plan);
            files.Add(new SourceFile($"{plan.Definition.Name}.Bake.g.cs", bakeSource));
        }

        return files;
    }
}
