using Tl;

namespace FusedBake;

public readonly record struct GaTrack0(float Scale, int Code) : IBlend<GaClip0>
{
    public void Blend(in GaClip0 first, in GaClip0 second, float factor, out GaClip0 result)
    {
        result = default;
        result.F0 = first.F0 + (second.F0 - first.F0) * factor;
        result.F1 = first.F1 + (second.F1 - first.F1) * factor;
        result.F2 = first.F2 + (second.F2 - first.F2) * factor;
        result.F3 = first.F3 + (second.F3 - first.F3) * factor;
        result.F4 = first.F4 + (second.F4 - first.F4) * factor;
        result.F5 = first.F5 + (second.F5 - first.F5) * factor;
        result.F6 = first.F6 + (second.F6 - first.F6) * factor;
        result.F7 = first.F7 + (second.F7 - first.F7) * factor;
        result.F8 = first.F8 + (second.F8 - first.F8) * factor;
        result.F9 = first.F9 + (second.F9 - first.F9) * factor;
        result.F10 = first.F10 + (second.F10 - first.F10) * factor;
        result.F11 = first.F11 + (second.F11 - first.F11) * factor;
        result.F12 = first.F12 + (second.F12 - first.F12) * factor;
        result.F13 = first.F13 + (second.F13 - first.F13) * factor;
        result.F14 = first.F14 + (second.F14 - first.F14) * factor;
        result.F15 = first.F15 + (second.F15 - first.F15) * factor;
        result.F16 = first.F16 + (second.F16 - first.F16) * factor;
        result.F17 = first.F17 + (second.F17 - first.F17) * factor;
        result.F18 = first.F18 + (second.F18 - first.F18) * factor;
        result.F19 = first.F19 + (second.F19 - first.F19) * factor;
        result.F20 = first.F20 + (second.F20 - first.F20) * factor;
        result.F21 = first.F21 + (second.F21 - first.F21) * factor;
        result.F22 = first.F22 + (second.F22 - first.F22) * factor;
        result.F23 = first.F23 + (second.F23 - first.F23) * factor;
        result.F24 = first.F24 + (second.F24 - first.F24) * factor;
        result.F25 = first.F25 + (second.F25 - first.F25) * factor;
        result.F26 = first.F26 + (second.F26 - first.F26) * factor;
        result.F27 = first.F27 + (second.F27 - first.F27) * factor;
        result.F28 = first.F28 + (second.F28 - first.F28) * factor;
        result.F29 = first.F29 + (second.F29 - first.F29) * factor;
        result.F30 = first.F30 + (second.F30 - first.F30) * factor;
        result.F31 = first.F31 + (second.F31 - first.F31) * factor;
        result.F32 = first.F32 + (second.F32 - first.F32) * factor;
        result.F33 = first.F33 + (second.F33 - first.F33) * factor;
        result.F34 = first.F34 + (second.F34 - first.F34) * factor;
        result.F35 = first.F35 + (second.F35 - first.F35) * factor;
        result.F36 = first.F36 + (second.F36 - first.F36) * factor;
        result.F37 = first.F37 + (second.F37 - first.F37) * factor;
        result.F38 = first.F38 + (second.F38 - first.F38) * factor;
        result.F39 = first.F39 + (second.F39 - first.F39) * factor;
        result.F40 = first.F40 + (second.F40 - first.F40) * factor;
        result.F41 = first.F41 + (second.F41 - first.F41) * factor;
        result.F42 = first.F42 + (second.F42 - first.F42) * factor;
        result.F43 = first.F43 + (second.F43 - first.F43) * factor;
        result.F44 = first.F44 + (second.F44 - first.F44) * factor;
        result.F45 = first.F45 + (second.F45 - first.F45) * factor;
        result.F46 = first.F46 + (second.F46 - first.F46) * factor;
        result.F47 = first.F47 + (second.F47 - first.F47) * factor;
        result.F48 = first.F48 + (second.F48 - first.F48) * factor;
        result.F49 = first.F49 + (second.F49 - first.F49) * factor;
        result.F50 = first.F50 + (second.F50 - first.F50) * factor;
        result.F51 = first.F51 + (second.F51 - first.F51) * factor;
        result.F52 = first.F52 + (second.F52 - first.F52) * factor;
        result.F53 = first.F53 + (second.F53 - first.F53) * factor;
        result.F54 = first.F54 + (second.F54 - first.F54) * factor;
        result.F55 = first.F55 + (second.F55 - first.F55) * factor;
        result.F56 = first.F56 + (second.F56 - first.F56) * factor;
        result.F57 = first.F57 + (second.F57 - first.F57) * factor;
        result.F58 = first.F58 + (second.F58 - first.F58) * factor;
        result.F59 = first.F59 + (second.F59 - first.F59) * factor;
        result.F60 = first.F60 + (second.F60 - first.F60) * factor;
        result.F61 = first.F61 + (second.F61 - first.F61) * factor;
        result.F62 = first.F62 + (second.F62 - first.F62) * factor;
        result.F63 = first.F63 + (second.F63 - first.F63) * factor;
    }
}

public struct GaClip0
{
    public float F0;
    public float F1;
    public float F2;
    public float F3;
    public float F4;
    public float F5;
    public float F6;
    public float F7;
    public float F8;
    public float F9;
    public float F10;
    public float F11;
    public float F12;
    public float F13;
    public float F14;
    public float F15;
    public float F16;
    public float F17;
    public float F18;
    public float F19;
    public float F20;
    public float F21;
    public float F22;
    public float F23;
    public float F24;
    public float F25;
    public float F26;
    public float F27;
    public float F28;
    public float F29;
    public float F30;
    public float F31;
    public float F32;
    public float F33;
    public float F34;
    public float F35;
    public float F36;
    public float F37;
    public float F38;
    public float F39;
    public float F40;
    public float F41;
    public float F42;
    public float F43;
    public float F44;
    public float F45;
    public float F46;
    public float F47;
    public float F48;
    public float F49;
    public float F50;
    public float F51;
    public float F52;
    public float F53;
    public float F54;
    public float F55;
    public float F56;
    public float F57;
    public float F58;
    public float F59;
    public float F60;
    public float F61;
    public float F62;
    public float F63;
}

public readonly record struct GaTrack1(float Scale, int Code) : IBlend<GaClip1>
{
    public void Blend(in GaClip1 first, in GaClip1 second, float factor, out GaClip1 result) => result = default;
}

public struct GaClip1
{
    public float F0;
}

public readonly record struct GaFatTrack(float Scale) : IBlend<GaFatClip>
{
    public void Blend(in GaFatClip first, in GaFatClip second, float factor, out GaFatClip result)
    {
        result = default;
        result.F0 = first.F0 + (second.F0 - first.F0) * factor;
        result.F1 = first.F1 + (second.F1 - first.F1) * factor;
        result.F2 = first.F2 + (second.F2 - first.F2) * factor;
        result.F3 = first.F3 + (second.F3 - first.F3) * factor;
        result.F4 = first.F4 + (second.F4 - first.F4) * factor;
        result.F5 = first.F5 + (second.F5 - first.F5) * factor;
        result.F6 = first.F6 + (second.F6 - first.F6) * factor;
        result.F7 = first.F7 + (second.F7 - first.F7) * factor;
        result.F8 = first.F8 + (second.F8 - first.F8) * factor;
        result.F9 = first.F9 + (second.F9 - first.F9) * factor;
        result.F10 = first.F10 + (second.F10 - first.F10) * factor;
        result.F11 = first.F11 + (second.F11 - first.F11) * factor;
        result.F12 = first.F12 + (second.F12 - first.F12) * factor;
        result.F13 = first.F13 + (second.F13 - first.F13) * factor;
        result.F14 = first.F14 + (second.F14 - first.F14) * factor;
        result.F15 = first.F15 + (second.F15 - first.F15) * factor;
        result.F16 = first.F16 + (second.F16 - first.F16) * factor;
        result.F17 = first.F17 + (second.F17 - first.F17) * factor;
        result.F18 = first.F18 + (second.F18 - first.F18) * factor;
        result.F19 = first.F19 + (second.F19 - first.F19) * factor;
        result.F20 = first.F20 + (second.F20 - first.F20) * factor;
        result.F21 = first.F21 + (second.F21 - first.F21) * factor;
        result.F22 = first.F22 + (second.F22 - first.F22) * factor;
        result.F23 = first.F23 + (second.F23 - first.F23) * factor;
        result.F24 = first.F24 + (second.F24 - first.F24) * factor;
        result.F25 = first.F25 + (second.F25 - first.F25) * factor;
        result.F26 = first.F26 + (second.F26 - first.F26) * factor;
        result.F27 = first.F27 + (second.F27 - first.F27) * factor;
        result.F28 = first.F28 + (second.F28 - first.F28) * factor;
        result.F29 = first.F29 + (second.F29 - first.F29) * factor;
        result.F30 = first.F30 + (second.F30 - first.F30) * factor;
        result.F31 = first.F31 + (second.F31 - first.F31) * factor;
        result.F32 = first.F32 + (second.F32 - first.F32) * factor;
        result.F33 = first.F33 + (second.F33 - first.F33) * factor;
        result.F34 = first.F34 + (second.F34 - first.F34) * factor;
        result.F35 = first.F35 + (second.F35 - first.F35) * factor;
        result.F36 = first.F36 + (second.F36 - first.F36) * factor;
        result.F37 = first.F37 + (second.F37 - first.F37) * factor;
        result.F38 = first.F38 + (second.F38 - first.F38) * factor;
        result.F39 = first.F39 + (second.F39 - first.F39) * factor;
        result.F40 = first.F40 + (second.F40 - first.F40) * factor;
        result.F41 = first.F41 + (second.F41 - first.F41) * factor;
        result.F42 = first.F42 + (second.F42 - first.F42) * factor;
        result.F43 = first.F43 + (second.F43 - first.F43) * factor;
        result.F44 = first.F44 + (second.F44 - first.F44) * factor;
        result.F45 = first.F45 + (second.F45 - first.F45) * factor;
        result.F46 = first.F46 + (second.F46 - first.F46) * factor;
        result.F47 = first.F47 + (second.F47 - first.F47) * factor;
        result.F48 = first.F48 + (second.F48 - first.F48) * factor;
        result.F49 = first.F49 + (second.F49 - first.F49) * factor;
        result.F50 = first.F50 + (second.F50 - first.F50) * factor;
        result.F51 = first.F51 + (second.F51 - first.F51) * factor;
        result.F52 = first.F52 + (second.F52 - first.F52) * factor;
        result.F53 = first.F53 + (second.F53 - first.F53) * factor;
        result.F54 = first.F54 + (second.F54 - first.F54) * factor;
        result.F55 = first.F55 + (second.F55 - first.F55) * factor;
        result.F56 = first.F56 + (second.F56 - first.F56) * factor;
        result.F57 = first.F57 + (second.F57 - first.F57) * factor;
        result.F58 = first.F58 + (second.F58 - first.F58) * factor;
        result.F59 = first.F59 + (second.F59 - first.F59) * factor;
        result.F60 = first.F60 + (second.F60 - first.F60) * factor;
        result.F61 = first.F61 + (second.F61 - first.F61) * factor;
        result.F62 = first.F62 + (second.F62 - first.F62) * factor;
        result.F63 = first.F63 + (second.F63 - first.F63) * factor;
    }
}

public struct GaFatClip
{
    public float F0;
    public float F1;
    public float F2;
    public float F3;
    public float F4;
    public float F5;
    public float F6;
    public float F7;
    public float F8;
    public float F9;
    public float F10;
    public float F11;
    public float F12;
    public float F13;
    public float F14;
    public float F15;
    public float F16;
    public float F17;
    public float F18;
    public float F19;
    public float F20;
    public float F21;
    public float F22;
    public float F23;
    public float F24;
    public float F25;
    public float F26;
    public float F27;
    public float F28;
    public float F29;
    public float F30;
    public float F31;
    public float F32;
    public float F33;
    public float F34;
    public float F35;
    public float F36;
    public float F37;
    public float F38;
    public float F39;
    public float F40;
    public float F41;
    public float F42;
    public float F43;
    public float F44;
    public float F45;
    public float F46;
    public float F47;
    public float F48;
    public float F49;
    public float F50;
    public float F51;
    public float F52;
    public float F53;
    public float F54;
    public float F55;
    public float F56;
    public float F57;
    public float F58;
    public float F59;
    public float F60;
    public float F61;
    public float F62;
    public float F63;
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
