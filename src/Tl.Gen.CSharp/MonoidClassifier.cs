using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tl.Gen.CSharp;

public enum IrKind : byte
{
    Monoid,
    Affine,
    Opaque
}

public sealed record IrResult(IrKind Kind, string Op, string Type);

public static class MonoidClassifier
{
    static readonly IrResult Opaque = new(IrKind.Opaque, "", "");

    public static IrResult Classify(MethodDeclarationSyntax? method)
    {
        try
        {
            if (method is null)
                return Opaque;
            return ClassifyCore(method);
        }
        catch
        {
            return Opaque;
        }
    }

    static IrResult ClassifyCore(MethodDeclarationSyntax? method)
    {
        if (method is null)
            return Opaque;
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count != 2)
            return Opaque;
        var first = parameters[0];
        var second = parameters[1];
        var type = TypeText(first.Type);
        if (!string.Equals(type, TypeText(second.Type), StringComparison.Ordinal))
            return Opaque;
        if (string.Equals(method.Identifier.Text, "Combine", StringComparison.Ordinal))
        {
            var returned = ReturnedExpression(method);
            if (returned is not null && TryOperation(returned, first.Identifier.Text, second.Identifier.Text, out var op))
                return new IrResult(IrKind.Monoid, op, type);
            var combined = AssignedExpression(method);
            if (combined is not null && TryAssignment(combined, first.Identifier.Text, second.Identifier.Text, out var combinedOp))
                return new IrResult(IrKind.Monoid, combinedOp, type);
            return Opaque;
        }
        if (string.Equals(method.Identifier.Text, "Apply", StringComparison.Ordinal))
        {
            var assigned = AssignedExpression(method);
            if (assigned is not null && TryAssignment(assigned, first.Identifier.Text, second.Identifier.Text, out var op))
                return new IrResult(IrKind.Affine, op, type);
            return Opaque;
        }
        return Opaque;
    }

    static ExpressionSyntax? ReturnedExpression(MethodDeclarationSyntax method)
    {
        if (method.ExpressionBody is not null)
            return method.ExpressionBody.Expression;
        var body = method.Body;
        if (body is null || body.Statements.Count != 1)
            return null;
        if (body.Statements[0] is ReturnStatementSyntax returned && returned.Expression is { } expression)
            return expression;
        return null;
    }

    static AssignmentExpressionSyntax? AssignedExpression(MethodDeclarationSyntax method)
    {
        if (method.ExpressionBody is not null)
            return Unwrap(method.ExpressionBody.Expression) as AssignmentExpressionSyntax;
        var body = method.Body;
        if (body is null || body.Statements.Count != 1)
            return null;
        if (body.Statements[0] is ExpressionStatementSyntax statement)
            return Unwrap(statement.Expression) as AssignmentExpressionSyntax;
        return null;
    }

    static bool TryOperation(ExpressionSyntax expression, string first, string second, out string op)
    {
        op = "";
        var unwrapped = Unwrap(expression);
        string candidate;
        ExpressionSyntax? left;
        ExpressionSyntax? right;
        if (unwrapped is BinaryExpressionSyntax binary && BinaryOp(binary, out candidate))
        {
            left = binary.Left;
            right = binary.Right;
        }
        else if (unwrapped is InvocationExpressionSyntax invocation && MinMaxOp(invocation, out candidate) && invocation.ArgumentList.Arguments.Count == 2)
        {
            left = invocation.ArgumentList.Arguments[0].Expression;
            right = invocation.ArgumentList.Arguments[1].Expression;
        }
        else
        {
            return false;
        }
        if (!OperandsMatch(left, right, first, second))
            return false;
        op = candidate;
        return true;
    }

    static bool TryAssignment(AssignmentExpressionSyntax assignment, string first, string second, out string op)
    {
        op = "";
        var target = IdentifierText(assignment.Left);
        if (target is null)
            return false;
        string? source;
        if (string.Equals(target, first, StringComparison.Ordinal))
            source = second;
        else if (string.Equals(target, second, StringComparison.Ordinal))
            source = first;
        else
            return false;
        if (assignment.Kind() == SyntaxKind.SimpleAssignmentExpression)
        {
            var value = Unwrap(assignment.Right);
            if (value is BinaryExpressionSyntax binary && BinaryOp(binary, out var candidate))
            {
                if (IsIdentifier(binary.Left, target) && IsIdentifier(binary.Right, source))
                {
                    op = candidate;
                    return true;
                }
                return false;
            }
            if (value is InvocationExpressionSyntax invocation && MinMaxOp(invocation, out var minmax))
            {
                var arguments = invocation.ArgumentList.Arguments;
                if (arguments.Count == 2 && IsIdentifier(arguments[0].Expression, target) && IsIdentifier(arguments[1].Expression, source))
                {
                    op = minmax;
                    return true;
                }
                return false;
            }
            return false;
        }
        if (assignment.Kind() == SyntaxKind.AddAssignmentExpression
            || assignment.Kind() == SyntaxKind.MultiplyAssignmentExpression
            || assignment.Kind() == SyntaxKind.OrAssignmentExpression
            || assignment.Kind() == SyntaxKind.AndAssignmentExpression)
        {
            if (!IsIdentifier(assignment.Right, source))
                return false;
            op = assignment.Kind() switch
            {
                SyntaxKind.AddAssignmentExpression => "+",
                SyntaxKind.MultiplyAssignmentExpression => "*",
                SyntaxKind.OrAssignmentExpression => "|",
                _ => "&"
            };
            return true;
        }
        return false;
    }

    static bool OperandsMatch(ExpressionSyntax? left, ExpressionSyntax? right, string first, string second)
        => (IsIdentifier(left, first) && IsIdentifier(right, second)) || (IsIdentifier(left, second) && IsIdentifier(right, first));

    static bool IsIdentifier(ExpressionSyntax? expression, string name)
        => expression is not null && Unwrap(expression) is IdentifierNameSyntax identifier && string.Equals(identifier.Identifier.Text, name, StringComparison.Ordinal);

    static string? IdentifierText(ExpressionSyntax? expression)
    {
        if (expression is null)
            return null;
        var unwrapped = Unwrap(expression);
        return unwrapped is IdentifierNameSyntax identifier ? identifier.Identifier.Text : null;
    }

    static bool BinaryOp(BinaryExpressionSyntax binary, out string op)
    {
        switch (binary.Kind())
        {
            case SyntaxKind.AddExpression:
                op = "+";
                return true;
            case SyntaxKind.MultiplyExpression:
                op = "*";
                return true;
            case SyntaxKind.BitwiseOrExpression:
                op = "|";
                return true;
            case SyntaxKind.BitwiseAndExpression:
                op = "&";
                return true;
            default:
                op = "";
                return false;
        }
    }

    static bool MinMaxOp(InvocationExpressionSyntax invocation, out string op)
    {
        var name = InvocationName(invocation.Expression);
        if (string.Equals(name, "Min", StringComparison.OrdinalIgnoreCase))
        {
            op = "min";
            return true;
        }
        if (string.Equals(name, "Max", StringComparison.OrdinalIgnoreCase))
        {
            op = "max";
            return true;
        }
        op = "";
        return false;
    }

    static string InvocationName(ExpressionSyntax expression)
    {
        var unwrapped = Unwrap(expression);
        if (unwrapped is IdentifierNameSyntax identifier)
            return identifier.Identifier.Text;
        if (unwrapped is GenericNameSyntax generic)
            return generic.Identifier.Text;
        if (unwrapped is MemberAccessExpressionSyntax member)
            return member.Name.Identifier.Text;
        return "";
    }

    static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;
        return expression;
    }

    static string TypeText(TypeSyntax? type) => type?.WithoutTrivia().ToString() ?? "";
}
