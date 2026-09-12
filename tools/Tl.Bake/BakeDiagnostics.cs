using System;

namespace Tl.Bake;

public class BakeDiagnosticException : Exception
{
    public BakeDiagnosticException(string message) : base(message) { }
    public BakeDiagnosticException(string message, int line, int column) 
        : base($"[{line}:{column}] {message}") { }
}
