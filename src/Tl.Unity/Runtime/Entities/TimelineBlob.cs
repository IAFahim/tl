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
        public byte Loops;
        public BlobArray<TimelineTrackBlob> Tracks;
        public BlobArray<TimelineClipBlob> Clips;
    }

    public struct TimelineTrackBlob
    {
        public ushort Index;
        public ushort Operation;
        public uint Payload;
    }

    public struct TimelineClipBlob
    {
        public ushort TrackIndex;
        public uint Payload;
        public uint Start;
        public uint End;
    }
}
