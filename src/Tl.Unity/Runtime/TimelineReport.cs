namespace Tl
{
    public readonly struct TimelineReport
    {
        public readonly ushort PlanVersion;
        public readonly ushort TimelineId;
        public readonly uint Duration;
        public readonly ushort TrackCount;
        public readonly ushort ClipCount;
        public readonly ushort RegionCount;
        public readonly uint GeneratedSourceBytes;
        public readonly uint StaticDataBytes;
        public readonly uint BlobBytes;
        public readonly uint RuntimeHeapBytes;

        public TimelineReport(
            ushort planVersion,
            ushort timelineId,
            uint duration,
            ushort trackCount,
            ushort clipCount,
            ushort regionCount,
            uint generatedSourceBytes,
            uint staticDataBytes,
            uint blobBytes,
            uint runtimeHeapBytes)
        {
            PlanVersion = planVersion;
            TimelineId = timelineId;
            Duration = duration;
            TrackCount = trackCount;
            ClipCount = clipCount;
            RegionCount = regionCount;
            GeneratedSourceBytes = generatedSourceBytes;
            StaticDataBytes = staticDataBytes;
            BlobBytes = blobBytes;
            RuntimeHeapBytes = runtimeHeapBytes;
        }
    }
}
