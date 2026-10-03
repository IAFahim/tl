using System.Runtime.InteropServices;

namespace Tl;

/// <summary>
/// v2 bank slot ABI: the flat float tables stay; movement records are no longer stored per
/// block — consumers derive them from the tables and the duration/looping rules. Hosts capture
/// this struct by value and must check <see cref="AbiVersion"/> against the version they were
/// built for; a mismatch is a loud failure, never silent drift.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 56)]
public unsafe struct SlotView
{
    public const ushort AbiVersionV1 = 1;
    public const ushort AbiVersionV2 = 2;
    public float* Forward;
    public float* Backward;
    public float* BackwardByPosition;
    public ulong* LaneKeys;
    public ushort Duration;
    public ushort Looping;
    public ushort Absent;
    public uint TableTicks;
    public ushort ResultCount;
    public ushort AbiVersion;
    public ulong Generation;
}
