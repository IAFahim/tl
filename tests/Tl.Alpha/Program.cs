using System.Runtime.CompilerServices;

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

if (args is ["--bank-capacity"])
{
    BankReceipts.Capacity();
    return 0;
}

if (args is ["--bank-concurrency"])
{
    BankReceipts.Concurrency();
    return 0;
}

if (args is ["--bank-views"])
{
    BankReceipts.ViewShapes();
    return 0;
}

if (args is ["--bank-workload"])
{
    BankReceipts.Workload();
    return 0;
}

if (args is ["--bank-stale"])
{
    BankReceipts.StaleSnapshot();
    return 0;
}

Require(args.Length == 0);
DataAuthoredReceipts.All();
DataAuthoredReceipts.Memory();
BankReceipts.Retained();
return 0;

static void Require(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
{
    if (!condition)
        throw new InvalidOperationException(expression);
}
