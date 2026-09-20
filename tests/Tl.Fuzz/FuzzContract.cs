using Tl.Gen.Tlb;

namespace Tl.Fuzz;

public static class FuzzContract
{
    public static bool IsBakeDiagnostic(Exception e)
        => e is BakeDiagnosticException or System.Text.Json.JsonException;

    public static bool IsDocumentedCapacity(Exception e)
        => e is InvalidOperationException
            && (e.Message.StartsWith("Timeline index domain exhausted", StringComparison.Ordinal)
                || e.Message.StartsWith("Intern table exhausted", StringComparison.Ordinal)
                || e.Message.StartsWith("Intern table probe exhausted", StringComparison.Ordinal)
                || e.Message.StartsWith("Pair capacity exhausted", StringComparison.Ordinal)
                || e.Message.StartsWith("Consumer capacity exhausted", StringComparison.Ordinal));

    public static bool IsLocatedTlbDiagnostic(Exception e)
        => e is ArgumentException && e.Message.StartsWith("TLB ", StringComparison.Ordinal);
}
