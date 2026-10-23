using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp.Analysis;

public static class BakeReader
{
    public static IReadOnlyList<BakeDeclaration> Read(CSharpCompilation compilation, IReadOnlyList<JobConsumer> consumers, List<DeclarationDiagnostic> errors)
    {
        if (compilation is null) throw new ArgumentNullException(nameof(compilation));
        return JobReader.Bakes(compilation, consumers, errors);
    }
}
