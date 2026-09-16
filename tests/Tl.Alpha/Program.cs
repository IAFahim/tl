using System.Runtime.CompilerServices;
using Tl;

if (args is ["--capacity"])
{
    DataAuthoredReceipts.BatchCapacity();
    return 0;
}

if (args is ["--module-capacity"])
{
    DataAuthoredReceipts.ModuleCapacity();
    return 0;
}

Require(args.Length == 0);
DataAuthoredReceipts.All();
DataAuthoredReceipts.Memory();
return 0;

static void Require(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
{
    if (!condition)
        throw new InvalidOperationException(expression);
}
