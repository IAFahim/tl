using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tl.Gen.CSharp.Analysis;

internal sealed record TypeEntry(INamedTypeSymbol Type, TypeDeclarationSyntax Syntax);

internal static class Symbols
{
    internal static IReadOnlyList<TypeEntry> Entries(CSharpCompilation compilation)
    {
        var result = new List<TypeEntry>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var tree in compilation.SyntaxTrees)
        foreach (var syntax in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            if (compilation.GetSemanticModel(tree).GetDeclaredSymbol(syntax) is { } type && seen.Add(type))
                result.Add(new(type, syntax));
        return result;
    }

    internal static bool Same(ISymbol? left, ISymbol? right) => SymbolEqualityComparer.Default.Equals(left, right);

    internal static string Name(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    internal static SyntaxNode Site(ISymbol symbol, SyntaxNode fallback) => symbol.DeclaringSyntaxReferences.Select(static reference => reference.GetSyntax()).FirstOrDefault() ?? fallback;

    internal static void Error(ICollection<DeclarationDiagnostic> errors, SyntaxNode node, string code, string message)
    {
        var span = node.GetLocation().GetLineSpan();
        errors.Add(new(span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, code, message)
        {
            SpanStart = node.SpanStart, SpanLength = node.Span.Length,
            EndLine = span.EndLinePosition.Line + 1, EndColumn = span.EndLinePosition.Character + 1,
        });
    }
}
