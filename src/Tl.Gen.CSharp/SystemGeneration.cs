using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp.Analysis;

namespace Tl.Gen.CSharp;
internal static class SystemGeneration
{
    public static (IReadOnlyList<CompileArtifact> Artifacts, IReadOnlyList<DeclarationDiagnostic> Diagnostics) Emit(CSharpCompilation compilation, string id = "")
    {
        var read = SystemReader.Read(compilation);
        var errors = read.Diagnostics.Any(static diagnostic => diagnostic.Code != "TLGEN98");
        var artifacts = errors
            ? new List<CompileArtifact>()
            : new List<CompileArtifact> { new CompileArtifact($"TlSystems{id}.g.cs", SystemEmitter.Emit(read.Assignments, id)) };
        return (artifacts, read.Diagnostics);
    }
}
