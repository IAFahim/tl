using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tl.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ChainedRunAnalyzer : DiagnosticAnalyzer
{
    const string FacadeType = "global::Tl.Gen.CSharp.TimelineGeneration";

    const string IdFragmentMessage = "TimelineGeneration.Run id '{0}' must be a non-empty ASCII identifier fragment (letter or underscore first, then letters, digits, or underscores); it is spliced into generated type and file names";

    const string GlobalFilterMessage = "Consumer filter '{0}' cannot match a timeline consumer; filters compare global::-qualified type names — write 'global::{0}'";

    static readonly DiagnosticDescriptor IdFragment = new(
        "TLGEN85",
        "Chained timeline generation id must be an ASCII identifier fragment",
        IdFragmentMessage,
        "Tl.ChainedGeneration",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The chained pass splices the id into generated type and file names; a fragment outside [A-Za-z_][A-Za-z0-9_]* fails generation with TLGEN82 after the host has already compiled. Rename the id literal to an identifier fragment.");

    static readonly DiagnosticDescriptor GlobalFilter = new(
        "TLGEN86",
        "Consumer filters must be global::-qualified",
        GlobalFilterMessage,
        "Tl.ChainedGeneration",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Chained consumer filters compare against global::-qualified type names, so an unqualified literal never matches and the chained pass reports TLGEN83 after the host has already compiled. Qualify the filter with the global:: prefix.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [IdFragment, GlobalFilter];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var called = invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null,
        };
        if (called != "Run")
            return;
        var info = context.SemanticModel.GetSymbolInfo(invocation.Expression, context.CancellationToken);
        var candidates = info.Symbol is IMethodSymbol bound ? [bound] : info.CandidateSymbols;
        if (!candidates.OfType<IMethodSymbol>().Any(static candidate =>
                candidate.IsStatic
                && candidate.ContainingType is { } facade
                && facade.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == FacadeType))
            return;

        var arguments = invocation.ArgumentList.Arguments;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            var name = argument.NameColon?.Name.Identifier.ValueText;
            if (name == "id" || (name is null && index == 3))
                CheckId(context, argument);
            if (name == "consumers" || (name is null && index == 2))
                CheckFilters(context, argument.Expression);
        }
    }

    static void CheckId(SyntaxNodeAnalysisContext context, ArgumentSyntax argument)
    {
        if (argument.Expression is not LiteralExpressionSyntax literal
            || !literal.IsKind(SyntaxKind.StringLiteralExpression))
            return;
        var id = (string)literal.Token.Value!;
        if (IdentifierFragment(id))
            return;
        context.ReportDiagnostic(Diagnostic.Create(IdFragment, argument.GetLocation(), id));
    }

    static void CheckFilters(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NullLiteralExpression))
            return;
        IEnumerable<ExpressionSyntax> elements = expression switch
        {
            CollectionExpressionSyntax collection => collection.Elements
                .OfType<ExpressionElementSyntax>()
                .Select(static element => element.Expression),
            ArrayCreationExpressionSyntax { Initializer: not null } array => array.Initializer.Expressions,
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
            ObjectCreationExpressionSyntax { Initializer: not null } creation => creation.Initializer.Expressions,
            _ => [],
        };
        foreach (var element in elements)
        {
            if (element is not LiteralExpressionSyntax entry
                || !entry.IsKind(SyntaxKind.StringLiteralExpression))
                continue;
            var text = (string)entry.Token.Value!;
            if (text.StartsWith("global::", StringComparison.Ordinal))
                continue;
            context.ReportDiagnostic(Diagnostic.Create(GlobalFilter, element.GetLocation(), text));
        }
    }

    static bool IdentifierFragment(string id)
        => id.Length > 0
            && id[0] is not ((>= '0' and <= '9'))
            && id.All(static c => c is ((>= '0' and <= '9')) or ((>= 'A' and <= 'Z')) or ((>= 'a' and <= 'z')) or '_');
}
