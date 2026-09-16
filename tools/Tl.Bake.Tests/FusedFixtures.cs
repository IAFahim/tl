using Tl;

namespace FusedBake;

public readonly record struct GaTrack0(float Scale, int Code) : IBlend<GaClip0>
{
    public void Blend(in GaClip0 first, in GaClip0 second, float factor, out GaClip0 result)
    {
        result = default;
        result.f0 = first.f0 + (second.f0 - first.f0) * factor;
        result.f1 = first.f1 + (second.f1 - first.f1) * factor;
        result.f2 = first.f2 + (second.f2 - first.f2) * factor;
        result.f3 = first.f3 + (second.f3 - first.f3) * factor;
        result.f4 = first.f4 + (second.f4 - first.f4) * factor;
        result.f5 = first.f5 + (second.f5 - first.f5) * factor;
        result.f6 = first.f6 + (second.f6 - first.f6) * factor;
        result.f7 = first.f7 + (second.f7 - first.f7) * factor;
        result.f8 = first.f8 + (second.f8 - first.f8) * factor;
        result.f9 = first.f9 + (second.f9 - first.f9) * factor;
        result.f10 = first.f10 + (second.f10 - first.f10) * factor;
        result.f11 = first.f11 + (second.f11 - first.f11) * factor;
        result.f12 = first.f12 + (second.f12 - first.f12) * factor;
        result.f13 = first.f13 + (second.f13 - first.f13) * factor;
        result.f14 = first.f14 + (second.f14 - first.f14) * factor;
        result.f15 = first.f15 + (second.f15 - first.f15) * factor;
        result.f16 = first.f16 + (second.f16 - first.f16) * factor;
        result.f17 = first.f17 + (second.f17 - first.f17) * factor;
        result.f18 = first.f18 + (second.f18 - first.f18) * factor;
        result.f19 = first.f19 + (second.f19 - first.f19) * factor;
        result.f20 = first.f20 + (second.f20 - first.f20) * factor;
        result.f21 = first.f21 + (second.f21 - first.f21) * factor;
        result.f22 = first.f22 + (second.f22 - first.f22) * factor;
        result.f23 = first.f23 + (second.f23 - first.f23) * factor;
        result.f24 = first.f24 + (second.f24 - first.f24) * factor;
        result.f25 = first.f25 + (second.f25 - first.f25) * factor;
        result.f26 = first.f26 + (second.f26 - first.f26) * factor;
        result.f27 = first.f27 + (second.f27 - first.f27) * factor;
        result.f28 = first.f28 + (second.f28 - first.f28) * factor;
        result.f29 = first.f29 + (second.f29 - first.f29) * factor;
        result.f30 = first.f30 + (second.f30 - first.f30) * factor;
        result.f31 = first.f31 + (second.f31 - first.f31) * factor;
        result.f32 = first.f32 + (second.f32 - first.f32) * factor;
        result.f33 = first.f33 + (second.f33 - first.f33) * factor;
        result.f34 = first.f34 + (second.f34 - first.f34) * factor;
        result.f35 = first.f35 + (second.f35 - first.f35) * factor;
        result.f36 = first.f36 + (second.f36 - first.f36) * factor;
        result.f37 = first.f37 + (second.f37 - first.f37) * factor;
        result.f38 = first.f38 + (second.f38 - first.f38) * factor;
        result.f39 = first.f39 + (second.f39 - first.f39) * factor;
        result.f40 = first.f40 + (second.f40 - first.f40) * factor;
        result.f41 = first.f41 + (second.f41 - first.f41) * factor;
        result.f42 = first.f42 + (second.f42 - first.f42) * factor;
        result.f43 = first.f43 + (second.f43 - first.f43) * factor;
        result.f44 = first.f44 + (second.f44 - first.f44) * factor;
        result.f45 = first.f45 + (second.f45 - first.f45) * factor;
        result.f46 = first.f46 + (second.f46 - first.f46) * factor;
        result.f47 = first.f47 + (second.f47 - first.f47) * factor;
        result.f48 = first.f48 + (second.f48 - first.f48) * factor;
        result.f49 = first.f49 + (second.f49 - first.f49) * factor;
        result.f50 = first.f50 + (second.f50 - first.f50) * factor;
        result.f51 = first.f51 + (second.f51 - first.f51) * factor;
        result.f52 = first.f52 + (second.f52 - first.f52) * factor;
        result.f53 = first.f53 + (second.f53 - first.f53) * factor;
        result.f54 = first.f54 + (second.f54 - first.f54) * factor;
        result.f55 = first.f55 + (second.f55 - first.f55) * factor;
        result.f56 = first.f56 + (second.f56 - first.f56) * factor;
        result.f57 = first.f57 + (second.f57 - first.f57) * factor;
        result.f58 = first.f58 + (second.f58 - first.f58) * factor;
        result.f59 = first.f59 + (second.f59 - first.f59) * factor;
        result.f60 = first.f60 + (second.f60 - first.f60) * factor;
        result.f61 = first.f61 + (second.f61 - first.f61) * factor;
        result.f62 = first.f62 + (second.f62 - first.f62) * factor;
        result.f63 = first.f63 + (second.f63 - first.f63) * factor;
    }
}

public struct GaClip0
{
    public float f0;
    public float f1;
    public float f2;
    public float f3;
    public float f4;
    public float f5;
    public float f6;
    public float f7;
    public float f8;
    public float f9;
    public float f10;
    public float f11;
    public float f12;
    public float f13;
    public float f14;
    public float f15;
    public float f16;
    public float f17;
    public float f18;
    public float f19;
    public float f20;
    public float f21;
    public float f22;
    public float f23;
    public float f24;
    public float f25;
    public float f26;
    public float f27;
    public float f28;
    public float f29;
    public float f30;
    public float f31;
    public float f32;
    public float f33;
    public float f34;
    public float f35;
    public float f36;
    public float f37;
    public float f38;
    public float f39;
    public float f40;
    public float f41;
    public float f42;
    public float f43;
    public float f44;
    public float f45;
    public float f46;
    public float f47;
    public float f48;
    public float f49;
    public float f50;
    public float f51;
    public float f52;
    public float f53;
    public float f54;
    public float f55;
    public float f56;
    public float f57;
    public float f58;
    public float f59;
    public float f60;
    public float f61;
    public float f62;
    public float f63;
}

public readonly record struct GaTrack1(float Scale, int Code) : IBlend<GaClip1>
{
    public void Blend(in GaClip1 first, in GaClip1 second, float factor, out GaClip1 result) => result = default;
}

public struct GaClip1
{
    public float f0;
}

public readonly record struct GaFatTrack(float Scale) : IBlend<GaFatClip>
{
    public void Blend(in GaFatClip first, in GaFatClip second, float factor, out GaFatClip result)
    {
        result = default;
        result.f0 = first.f0 + (second.f0 - first.f0) * factor;
        result.f1 = first.f1 + (second.f1 - first.f1) * factor;
        result.f2 = first.f2 + (second.f2 - first.f2) * factor;
        result.f3 = first.f3 + (second.f3 - first.f3) * factor;
        result.f4 = first.f4 + (second.f4 - first.f4) * factor;
        result.f5 = first.f5 + (second.f5 - first.f5) * factor;
        result.f6 = first.f6 + (second.f6 - first.f6) * factor;
        result.f7 = first.f7 + (second.f7 - first.f7) * factor;
        result.f8 = first.f8 + (second.f8 - first.f8) * factor;
        result.f9 = first.f9 + (second.f9 - first.f9) * factor;
        result.f10 = first.f10 + (second.f10 - first.f10) * factor;
        result.f11 = first.f11 + (second.f11 - first.f11) * factor;
        result.f12 = first.f12 + (second.f12 - first.f12) * factor;
        result.f13 = first.f13 + (second.f13 - first.f13) * factor;
        result.f14 = first.f14 + (second.f14 - first.f14) * factor;
        result.f15 = first.f15 + (second.f15 - first.f15) * factor;
        result.f16 = first.f16 + (second.f16 - first.f16) * factor;
        result.f17 = first.f17 + (second.f17 - first.f17) * factor;
        result.f18 = first.f18 + (second.f18 - first.f18) * factor;
        result.f19 = first.f19 + (second.f19 - first.f19) * factor;
        result.f20 = first.f20 + (second.f20 - first.f20) * factor;
        result.f21 = first.f21 + (second.f21 - first.f21) * factor;
        result.f22 = first.f22 + (second.f22 - first.f22) * factor;
        result.f23 = first.f23 + (second.f23 - first.f23) * factor;
        result.f24 = first.f24 + (second.f24 - first.f24) * factor;
        result.f25 = first.f25 + (second.f25 - first.f25) * factor;
        result.f26 = first.f26 + (second.f26 - first.f26) * factor;
        result.f27 = first.f27 + (second.f27 - first.f27) * factor;
        result.f28 = first.f28 + (second.f28 - first.f28) * factor;
        result.f29 = first.f29 + (second.f29 - first.f29) * factor;
        result.f30 = first.f30 + (second.f30 - first.f30) * factor;
        result.f31 = first.f31 + (second.f31 - first.f31) * factor;
        result.f32 = first.f32 + (second.f32 - first.f32) * factor;
        result.f33 = first.f33 + (second.f33 - first.f33) * factor;
        result.f34 = first.f34 + (second.f34 - first.f34) * factor;
        result.f35 = first.f35 + (second.f35 - first.f35) * factor;
        result.f36 = first.f36 + (second.f36 - first.f36) * factor;
        result.f37 = first.f37 + (second.f37 - first.f37) * factor;
        result.f38 = first.f38 + (second.f38 - first.f38) * factor;
        result.f39 = first.f39 + (second.f39 - first.f39) * factor;
        result.f40 = first.f40 + (second.f40 - first.f40) * factor;
        result.f41 = first.f41 + (second.f41 - first.f41) * factor;
        result.f42 = first.f42 + (second.f42 - first.f42) * factor;
        result.f43 = first.f43 + (second.f43 - first.f43) * factor;
        result.f44 = first.f44 + (second.f44 - first.f44) * factor;
        result.f45 = first.f45 + (second.f45 - first.f45) * factor;
        result.f46 = first.f46 + (second.f46 - first.f46) * factor;
        result.f47 = first.f47 + (second.f47 - first.f47) * factor;
        result.f48 = first.f48 + (second.f48 - first.f48) * factor;
        result.f49 = first.f49 + (second.f49 - first.f49) * factor;
        result.f50 = first.f50 + (second.f50 - first.f50) * factor;
        result.f51 = first.f51 + (second.f51 - first.f51) * factor;
        result.f52 = first.f52 + (second.f52 - first.f52) * factor;
        result.f53 = first.f53 + (second.f53 - first.f53) * factor;
        result.f54 = first.f54 + (second.f54 - first.f54) * factor;
        result.f55 = first.f55 + (second.f55 - first.f55) * factor;
        result.f56 = first.f56 + (second.f56 - first.f56) * factor;
        result.f57 = first.f57 + (second.f57 - first.f57) * factor;
        result.f58 = first.f58 + (second.f58 - first.f58) * factor;
        result.f59 = first.f59 + (second.f59 - first.f59) * factor;
        result.f60 = first.f60 + (second.f60 - first.f60) * factor;
        result.f61 = first.f61 + (second.f61 - first.f61) * factor;
        result.f62 = first.f62 + (second.f62 - first.f62) * factor;
        result.f63 = first.f63 + (second.f63 - first.f63) * factor;
    }
}

public struct GaFatClip
{
    public float f0;
    public float f1;
    public float f2;
    public float f3;
    public float f4;
    public float f5;
    public float f6;
    public float f7;
    public float f8;
    public float f9;
    public float f10;
    public float f11;
    public float f12;
    public float f13;
    public float f14;
    public float f15;
    public float f16;
    public float f17;
    public float f18;
    public float f19;
    public float f20;
    public float f21;
    public float f22;
    public float f23;
    public float f24;
    public float f25;
    public float f26;
    public float f27;
    public float f28;
    public float f29;
    public float f30;
    public float f31;
    public float f32;
    public float f33;
    public float f34;
    public float f35;
    public float f36;
    public float f37;
    public float f38;
    public float f39;
    public float f40;
    public float f41;
    public float f42;
    public float f43;
    public float f44;
    public float f45;
    public float f46;
    public float f47;
    public float f48;
    public float f49;
    public float f50;
    public float f51;
    public float f52;
    public float f53;
    public float f54;
    public float f55;
    public float f56;
    public float f57;
    public float f58;
    public float f59;
    public float f60;
    public float f61;
    public float f62;
    public float f63;
}

public readonly record struct GaPrimTrack(float Scale) : IBlend<GaPrimClip>
{
    public void Blend(in GaPrimClip first, in GaPrimClip second, float factor, out GaPrimClip result)
    {
        result = default;
        result.B = second.B;
        result.Bt = (byte)(first.Bt + (second.Bt - first.Bt) * factor);
        result.Sb = (sbyte)(first.Sb + (second.Sb - first.Sb) * factor);
        result.Sh = (short)(first.Sh + (second.Sh - first.Sh) * factor);
        result.Us = (ushort)(first.Us + (second.Us - first.Us) * factor);
        result.I = (int)(first.I + (second.I - first.I) * factor);
        result.Ui = (uint)(first.Ui + (second.Ui - first.Ui) * factor);
        result.L = (long)(first.L + (second.L - first.L) * factor);
        result.Ul = (ulong)(first.Ul + (second.Ul - first.Ul) * factor);
        result.F = first.F + (second.F - first.F) * factor;
        result.D = first.D + (second.D - first.D) * factor;
    }
}

public struct GaPrimClip
{
    public bool B;
    public byte Bt;
    public sbyte Sb;
    public short Sh;
    public ushort Us;
    public int I;
    public uint Ui;
    public long L;
    public ulong Ul;
    public float F;
    public double D;
}

public readonly record struct GaStrTrack(float Scale) : IBlend<GaStrClip>
{
    public void Blend(in GaStrClip first, in GaStrClip second, float factor, out GaStrClip result) => result = default;
}

public struct GaStrClip
{
    public char Code;
}

public readonly record struct GaBareTrack(float Scale);

public sealed class GaClassTrack : IBlend<GaClip0>
{
    public void Blend(in GaClip0 first, in GaClip0 second, float factor, out GaClip0 result) => result = default;
}
