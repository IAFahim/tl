using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tl.Core.Tests;

// Test-local fixture: the empty input marker for consumers that carry no
// read-only context. Deliberately NOT part of the public API — it is a test
// convenience; production consumers define their own input types.
public readonly struct NoInput;

public static class BitCompare
{
    public static bool Identical<T>(in T left, in T right) where T : struct
    {
        var l = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in left), 1));
        var r = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in right), 1));
        return l.SequenceEqual(r);
    }
}
