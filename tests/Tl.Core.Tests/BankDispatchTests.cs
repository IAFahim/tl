using Xunit;

namespace Tl.Core.Tests;

public readonly record struct BankProbeClip(float Value);

public readonly record struct BankMarkClip(float Value);

public readonly record struct BankMarkTrack(float Scale) : IBlend<BankMarkClip>
{
    public void Blend(in BankMarkClip first, in BankMarkClip second, float factor, out BankMarkClip result)
        => result = first;
}

public readonly struct BankProbeTrack<T> : IBlend<BankProbeClip> where T : unmanaged
{
    public void Blend(in BankProbeClip first, in BankProbeClip second, float factor, out BankProbeClip result)
        => result = first;
}

public unsafe class BankDispatchTests
{
    static void EmptySink(byte** arguments) { }

    [Fact]
    public void BakeTableCountsRegisteredBakes()
    {
        var before = BakeTable.Count;
        BakeRuntime<BankMarkTrack, BankMarkClip>.Bake(&EmptySink, TypeKey<int>.Value);
        Assert.Equal(before + 1, BakeTable.Count);
        Assert.Equal(1, BakeRuntime<BankMarkTrack, BankMarkClip>.BakeCount);
    }

    [Fact]
    public void ProbeDisplacesWhenPairKeysShareASlot()
    {
        var before = BakeTable.Count;
        InstallProbe<BankTag0>();
        InstallProbe<BankTag1>();
        InstallProbe<BankTag2>();
        InstallProbe<BankTag3>();
        InstallProbe<BankTag4>();
        InstallProbe<BankTag5>();
        InstallProbe<BankTag6>();
        InstallProbe<BankTag7>();
        InstallProbe<BankTag8>();
        InstallProbe<BankTag9>();
        InstallProbe<BankTag10>();
        InstallProbe<BankTag11>();
        InstallProbe<BankTag12>();
        InstallProbe<BankTag13>();
        InstallProbe<BankTag14>();
        InstallProbe<BankTag15>();
        InstallProbe<BankTag16>();
        InstallProbe<BankTag17>();
        InstallProbe<BankTag18>();
        InstallProbe<BankTag19>();
        InstallProbe<BankTag20>();
        InstallProbe<BankTag21>();
        InstallProbe<BankTag22>();
        InstallProbe<BankTag23>();
        InstallProbe<BankTag24>();
        InstallProbe<BankTag25>();
        InstallProbe<BankTag26>();
        InstallProbe<BankTag27>();
        InstallProbe<BankTag28>();
        InstallProbe<BankTag29>();
        InstallProbe<BankTag30>();
        InstallProbe<BankTag31>();
        InstallProbe<BankTag32>();
        InstallProbe<BankTag33>();
        InstallProbe<BankTag34>();
        InstallProbe<BankTag35>();
        InstallProbe<BankTag36>();
        InstallProbe<BankTag37>();
        InstallProbe<BankTag38>();
        InstallProbe<BankTag39>();
        InstallProbe<BankTag40>();
        InstallProbe<BankTag41>();
        InstallProbe<BankTag42>();
        InstallProbe<BankTag43>();
        InstallProbe<BankTag44>();
        InstallProbe<BankTag45>();
        InstallProbe<BankTag46>();
        InstallProbe<BankTag47>();
        InstallProbe<BankTag48>();
        InstallProbe<BankTag49>();
        InstallProbe<BankTag50>();
        InstallProbe<BankTag51>();
        InstallProbe<BankTag52>();
        InstallProbe<BankTag53>();
        InstallProbe<BankTag54>();
        InstallProbe<BankTag55>();
        InstallProbe<BankTag56>();
        InstallProbe<BankTag57>();
        InstallProbe<BankTag58>();
        InstallProbe<BankTag59>();
        InstallProbe<BankTag60>();
        InstallProbe<BankTag61>();
        InstallProbe<BankTag62>();
        InstallProbe<BankTag63>();
        InstallProbe<BankTag64>();
        InstallProbe<BankTag65>();
        InstallProbe<BankTag66>();
        InstallProbe<BankTag67>();
        InstallProbe<BankTag68>();
        InstallProbe<BankTag69>();
        InstallProbe<BankTag70>();
        InstallProbe<BankTag71>();
        InstallProbe<BankTag72>();
        InstallProbe<BankTag73>();
        InstallProbe<BankTag74>();
        InstallProbe<BankTag75>();
        InstallProbe<BankTag76>();
        InstallProbe<BankTag77>();
        InstallProbe<BankTag78>();
        InstallProbe<BankTag79>();
        InstallProbe<BankTag80>();
        InstallProbe<BankTag81>();
        InstallProbe<BankTag82>();
        InstallProbe<BankTag83>();
        InstallProbe<BankTag84>();
        InstallProbe<BankTag85>();
        InstallProbe<BankTag86>();
        InstallProbe<BankTag87>();
        InstallProbe<BankTag88>();
        InstallProbe<BankTag89>();
        InstallProbe<BankTag90>();
        InstallProbe<BankTag91>();
        InstallProbe<BankTag92>();
        InstallProbe<BankTag93>();
        InstallProbe<BankTag94>();
        InstallProbe<BankTag95>();
        InstallProbe<BankTag96>();
        InstallProbe<BankTag97>();
        InstallProbe<BankTag98>();
        InstallProbe<BankTag99>();
        InstallProbe<BankTag100>();
        InstallProbe<BankTag101>();
        InstallProbe<BankTag102>();
        InstallProbe<BankTag103>();
        InstallProbe<BankTag104>();
        InstallProbe<BankTag105>();
        InstallProbe<BankTag106>();
        InstallProbe<BankTag107>();
        InstallProbe<BankTag108>();
        InstallProbe<BankTag109>();
        InstallProbe<BankTag110>();
        InstallProbe<BankTag111>();
        InstallProbe<BankTag112>();
        InstallProbe<BankTag113>();
        InstallProbe<BankTag114>();
        InstallProbe<BankTag115>();
        InstallProbe<BankTag116>();
        InstallProbe<BankTag117>();
        InstallProbe<BankTag118>();
        InstallProbe<BankTag119>();
        InstallProbe<BankTag120>();
        InstallProbe<BankTag121>();
        InstallProbe<BankTag122>();
        InstallProbe<BankTag123>();
        InstallProbe<BankTag124>();
        InstallProbe<BankTag125>();
        InstallProbe<BankTag126>();
        InstallProbe<BankTag127>();
        Assert.Equal(before + 128, BakeTable.Count);
        Assert.Equal(1, BakeRuntime<BankProbeTrack<BankTag0>, BankProbeClip>.BakeCount);

        var distinctHomes = new HashSet<int>(Homes);
        Assert.True(Homes.Count > distinctHomes.Count);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => BakeRuntime<BankProbeTrack<BankTag0>, BankProbeClip>.BakeParameterKey(0, 1));
        Assert.Equal(TypeKey<int>.Value, BakeRuntime<BankProbeTrack<BankTag0>, BankProbeClip>.BakeParameterKey(0, 0));
    }

    static readonly List<int> Homes = new();

    static void InstallProbe<T>() where T : unmanaged
    {
        Homes.Add((int)PairRuntime<BankProbeTrack<T>, BankProbeClip>.Key & 255);
        BakeRuntime<BankProbeTrack<T>, BankProbeClip>.Bake(&EmptySink, TypeKey<int>.Value);
    }
}

readonly struct BankTag0;

readonly struct BankTag1;

readonly struct BankTag2;

readonly struct BankTag3;

readonly struct BankTag4;

readonly struct BankTag5;

readonly struct BankTag6;

readonly struct BankTag7;

readonly struct BankTag8;

readonly struct BankTag9;

readonly struct BankTag10;

readonly struct BankTag11;

readonly struct BankTag12;

readonly struct BankTag13;

readonly struct BankTag14;

readonly struct BankTag15;

readonly struct BankTag16;

readonly struct BankTag17;

readonly struct BankTag18;

readonly struct BankTag19;

readonly struct BankTag20;

readonly struct BankTag21;

readonly struct BankTag22;

readonly struct BankTag23;

readonly struct BankTag24;

readonly struct BankTag25;

readonly struct BankTag26;

readonly struct BankTag27;

readonly struct BankTag28;

readonly struct BankTag29;

readonly struct BankTag30;

readonly struct BankTag31;

readonly struct BankTag32;

readonly struct BankTag33;

readonly struct BankTag34;

readonly struct BankTag35;

readonly struct BankTag36;

readonly struct BankTag37;

readonly struct BankTag38;

readonly struct BankTag39;

readonly struct BankTag40;

readonly struct BankTag41;

readonly struct BankTag42;

readonly struct BankTag43;

readonly struct BankTag44;

readonly struct BankTag45;

readonly struct BankTag46;

readonly struct BankTag47;

readonly struct BankTag48;

readonly struct BankTag49;

readonly struct BankTag50;

readonly struct BankTag51;

readonly struct BankTag52;

readonly struct BankTag53;

readonly struct BankTag54;

readonly struct BankTag55;

readonly struct BankTag56;

readonly struct BankTag57;

readonly struct BankTag58;

readonly struct BankTag59;

readonly struct BankTag60;

readonly struct BankTag61;

readonly struct BankTag62;

readonly struct BankTag63;

readonly struct BankTag64;

readonly struct BankTag65;

readonly struct BankTag66;

readonly struct BankTag67;

readonly struct BankTag68;

readonly struct BankTag69;

readonly struct BankTag70;

readonly struct BankTag71;

readonly struct BankTag72;

readonly struct BankTag73;

readonly struct BankTag74;

readonly struct BankTag75;

readonly struct BankTag76;

readonly struct BankTag77;

readonly struct BankTag78;

readonly struct BankTag79;

readonly struct BankTag80;

readonly struct BankTag81;

readonly struct BankTag82;

readonly struct BankTag83;

readonly struct BankTag84;

readonly struct BankTag85;

readonly struct BankTag86;

readonly struct BankTag87;

readonly struct BankTag88;

readonly struct BankTag89;

readonly struct BankTag90;

readonly struct BankTag91;

readonly struct BankTag92;

readonly struct BankTag93;

readonly struct BankTag94;

readonly struct BankTag95;

readonly struct BankTag96;

readonly struct BankTag97;

readonly struct BankTag98;

readonly struct BankTag99;

readonly struct BankTag100;

readonly struct BankTag101;

readonly struct BankTag102;

readonly struct BankTag103;

readonly struct BankTag104;

readonly struct BankTag105;

readonly struct BankTag106;

readonly struct BankTag107;

readonly struct BankTag108;

readonly struct BankTag109;

readonly struct BankTag110;

readonly struct BankTag111;

readonly struct BankTag112;

readonly struct BankTag113;

readonly struct BankTag114;

readonly struct BankTag115;

readonly struct BankTag116;

readonly struct BankTag117;

readonly struct BankTag118;

readonly struct BankTag119;

readonly struct BankTag120;

readonly struct BankTag121;

readonly struct BankTag122;

readonly struct BankTag123;

readonly struct BankTag124;

readonly struct BankTag125;

readonly struct BankTag126;

readonly struct BankTag127;
