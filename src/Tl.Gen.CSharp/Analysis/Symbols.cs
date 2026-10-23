using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Tl.Gen.CSharp.Analysis;

internal static class Symbols
{
    internal static bool Same(ISymbol? left, ISymbol? right) => SymbolEqualityComparer.Default.Equals(left, right);

    internal static string Name(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    internal static SyntaxNode Site(ISymbol symbol, SyntaxNode fallback) => symbol.DeclaringSyntaxReferences.Select(static reference => reference.GetSyntax()).FirstOrDefault() ?? fallback;

    internal static DeclarationDiagnostic Site(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        return new(span.Path, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, "", "")
        {
            SpanStart = node.SpanStart, SpanLength = node.Span.Length,
            EndLine = span.EndLinePosition.Line + 1, EndColumn = span.EndLinePosition.Character + 1,
        };
    }

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
