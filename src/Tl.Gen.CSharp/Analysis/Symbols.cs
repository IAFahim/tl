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

    internal static byte ResultSize(ITypeSymbol type)
        => type switch
        {
            INamedTypeSymbol { EnumUnderlyingType: { } u } => ResultSize(u),
            { SpecialType: SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte } => 1,
            { SpecialType: SpecialType.System_Char or SpecialType.System_Int16 or SpecialType.System_UInt16 } => 2,
            { SpecialType: SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single } => 4,
            { SpecialType: SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Double } => 8,
            _ => 0,
        };
}
