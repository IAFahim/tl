using System.Runtime.InteropServices;

namespace Tl;

[StructLayout(LayoutKind.Sequential, Size = 64)]
public unsafe struct SlotView
{
    public const ushort AbiVersionV1 = 1;
    public float* Forward;
    public float* Backward;
    public float* BackwardByPosition;
    public LaneMovementRecord* ForwardRecords;
    public LaneMovementRecord* BackwardRecords;
    public ushort Duration;
    public ushort Looping;
    public ushort Absent;
    public uint TableTicks;
    public ushort RecordBytes;
    public ushort AbiVersion;
    public ulong Generation;
}
