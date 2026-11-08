namespace Unity.Mathematics
{
    public struct float3
    {
        public float x, y, z;
        public float3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static float3 operator +(float3 a, float3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static float3 operator *(float3 a, float b) => new(a.x * b, a.y * b, a.z * b);
    }
}

namespace Unity.Burst
{
    [System.AttributeUsage(System.AttributeTargets.All)]
    public sealed class BurstCompileAttribute : System.Attribute { }
}

namespace Unity.Entities
{
    public interface IComponentData { }
    public interface IBufferElementData { }
    public interface ISystem { }
    public interface IJobEntity { }
    public struct BlobAssetReference<T> where T : unmanaged { }
    public struct SystemState { }
    public struct TimeData { public float DeltaTime; }
    public static class SystemAPI { public static TimeData Time => default; }
    public static class JobEntityExtensions { public static void ScheduleParallel<T>(this T job) where T : struct, IJobEntity { } }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public sealed class UpdateInGroupAttribute : System.Attribute
    {
        public UpdateInGroupAttribute(System.Type group) { }
        public bool OrderLast { get; set; }
    }
}

namespace Unity.Transforms
{
    public struct LocalTransform { public Unity.Mathematics.float3 Position; }
}

namespace Tl
{
    public interface ITlBaker<TClip> where TClip : unmanaged
    {
        void Bake(ref TlBaker baker, in TClip clip);
    }

    public ref struct TlBaker
    {
        public void AddComponent<T>() where T : unmanaged, Unity.Entities.IComponentData { }
        public void AddBuffer<T>() where T : unmanaged, Unity.Entities.IBufferElementData { }
        public void AddBlob<T>(T component) where T : unmanaged, Unity.Entities.IComponentData { }
        public Unity.Entities.BlobAssetReference<TlCurveBlob> CreateCurve(in TlCurve curve) => default;
    }

    public sealed class TlSimulationGroup { }
}

public struct TlCurveBlob { public float First; }
