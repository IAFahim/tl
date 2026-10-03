using System.Runtime.InteropServices;

namespace Tl;

/// <summary>
/// One encoded run of a lane direction: [Start, End] inclusive, Kind 0 constant over V0,
/// Kind 1 ramp V0→V1 (bit-exact under the shared reader formula), Kind 2 dense run
/// whose per-tick floats live at Dense[V0 bits as offset].
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LaneSegment
{
    public const byte Constant = 0;
    public const byte Ramp = 1;
    public const byte Dense = 2;
    public ushort Start;
    public ushort End;
    public byte Kind;
    public byte Pad;
    public float V0;
    public float V1;
}

/// <summary>
/// v3 bank slot ABI: per-lane movement tables are segmented encodings (or flat-escaped
/// directions); movement records and the backward-by-position table are derived, never
/// stored. Hosts capture this struct by value and must check <see cref="AbiVersion"/>
/// against the version they were built for; a mismatch is a loud failure, never silent drift.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 72)]
public unsafe struct SlotView
{
    public const ushort AbiVersionV1 = 1;
    public const ushort AbiVersionV2 = 2;
    public const ushort AbiVersionV3 = 3;
    public uint* Directory;
    public LaneSegment* Segments;
    public float* Dense;
    public float* Forward;
    public float* Backward;
    public ulong* LaneKeys;
    public ushort Duration;
    public ushort Looping;
    public ushort Absent;
    public uint TableTicks;
    public ushort ResultCount;
    public ushort AbiVersion;
    public ulong Generation;
}
