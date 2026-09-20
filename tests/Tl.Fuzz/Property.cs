using FsCheck;
using FsCheck.Fluent;

namespace Tl.Fuzz;

public static class FuzzLaws
{
    public const ulong Seed = 0x303;
    public const ulong Gamma = 0x9E37;
    public const int StartSize = 8;
    public const int MaxTest = 200;

    public static Config Replay(string name) => Config.QuickThrowOnFailure
        .WithName(name)
        .WithMaxTest(MaxTest)
        .WithReplay(Seed, Gamma, StartSize);

    public static void Verify(string name, FsCheck.Property property) => property.Check(Replay(name));

    public static Arbitrary<ushort> ArbUShort(int low, int high) => Arb.From(FsCheck.Fluent.Gen.Select(FsCheck.Fluent.Gen.Choose(low, high), value => (ushort)value));

    public static Arbitrary<ushort> ArbUShort() => ArbUShort(0, 65535);

    public static Arbitrary<bool> ArbBool() => Arb.From(FsCheck.Fluent.Gen.Elements([false, true]));

    public static Arbitrary<int> ArbLength(int maxExclusive) => Arb.From(FsCheck.Fluent.Gen.Choose(0, maxExclusive - 1));
}
