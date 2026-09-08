using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// Test collections run sequentially: the steady-state heap receipt measures
// process-wide retained bytes across a window, and concurrent collections
// would land their allocations inside it. The suite is I/O-free and runs in
// ~150 ms, so serialization costs nothing.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
