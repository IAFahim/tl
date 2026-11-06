namespace K0Probe
{
  [Unity.Entities.InternalBufferCapacity(0)] public struct B8_C0 : Unity.Entities.IBufferElementData { public int a, b; }
  [Unity.Entities.InternalBufferCapacity(1)] public struct B8_C1 : Unity.Entities.IBufferElementData { public int a, b; }
  [Unity.Entities.InternalBufferCapacity(2)] public struct B8_C2 : Unity.Entities.IBufferElementData { public int a, b; }
  [Unity.Entities.InternalBufferCapacity(4)] public struct B8_C4 : Unity.Entities.IBufferElementData { public int a, b; }
  public struct B8_Def : Unity.Entities.IBufferElementData { public int a, b; }
  [Unity.Entities.InternalBufferCapacity(0)] public struct B12_C0 : Unity.Entities.IBufferElementData { public int a, b, c; }
  [Unity.Entities.InternalBufferCapacity(1)] public struct B12_C1 : Unity.Entities.IBufferElementData { public int a, b, c; }
  [Unity.Entities.InternalBufferCapacity(2)] public struct B12_C2 : Unity.Entities.IBufferElementData { public int a, b, c; }
  [Unity.Entities.InternalBufferCapacity(4)] public struct B12_C4 : Unity.Entities.IBufferElementData { public int a, b, c; }
  public struct B12_Def : Unity.Entities.IBufferElementData { public int a, b, c; }
  public struct P4 : Unity.Entities.IComponentData { public int a; }
  public struct P8 : Unity.Entities.IComponentData { public int a, b; }
  public struct P16 : Unity.Entities.IComponentData { public P8_V a, b; }
  public struct P32 : Unity.Entities.IComponentData { public P16_V a, b; }
  public struct P64 : Unity.Entities.IComponentData { public P32_V a, b; }
  public struct P128 : Unity.Entities.IComponentData { public P64_V a, b; }
  public struct P256 : Unity.Entities.IComponentData { public P128_V a, b; }
  public struct P512 : Unity.Entities.IComponentData { public P256_V a, b; }
  public struct P1024 : Unity.Entities.IComponentData { public P512_V a, b; }
  public struct P2048 : Unity.Entities.IComponentData { public P1024_V a, b; }
  public struct P4096 : Unity.Entities.IComponentData { public P2048_V a, b; }
  public struct P8192 : Unity.Entities.IComponentData { public P4096_V a, b; }
  public struct P16384 : Unity.Entities.IComponentData { public P8192_V a, b; }
  public struct P4_V { public int a; }
  public struct P8_V { public P4_V a, b; }
  public struct P16_V { public P8_V a, b; }
  public struct P32_V { public P16_V a, b; }
  public struct P64_V { public P32_V a, b; }
  public struct P128_V { public P64_V a, b; }
  public struct P256_V { public P128_V a, b; }
  public struct P512_V { public P256_V a, b; }
  public struct P1024_V { public P512_V a, b; }
  public struct P2048_V { public P1024_V a, b; }
  public struct P4096_V { public P2048_V a, b; }
  public struct P8192_V { public P4096_V a, b; }
  public struct P16384_V { public P8192_V a, b; }
}