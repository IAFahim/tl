using Unity.Entities;

namespace Tl
{
    public struct TimelineBlob
    {
        public ushort PlanVersion;
        public ushort TimelineId;
        public uint Duration;
        public ushort TrackCount;
        public ushort ClipCount;
        public byte TrackKindCount;
        public byte ClipKindCount;
        public byte Loops;
        public BlobArray<TimelineRegionBlob> Regions;
        public BlobArray<TimelineFrameBlob> Frames;
        public BlobArray<ulong> StaticData;
    }

    public struct TimelineRegionBlob
    {
        public uint Start;
        public uint End;
        public ushort FrameStart;
        public ushort FrameCount;
    }

    public struct TimelineFrameBlob
    {
        public uint Start;
        public uint End;
        public ushort TrackIndex;
        public ushort Operation;
        public byte TrackKind;
        public byte ClipKind;
        public ushort DataWord;
    }
}
