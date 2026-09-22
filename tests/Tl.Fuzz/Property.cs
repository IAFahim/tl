using FsCheck;
using FsCheck.Fluent;

namespace Tl.Fuzz;

public static class FuzzLaws
{
    private const ulong Seed = 0x303;
    private const ulong Gamma = 0x9E37;
    private const int StartSize = 8;
    private const int MaxTest = 200;

    private static Config Replay(string name) => Config.QuickThrowOnFailure
        .WithName(name)
        .WithMaxTest(MaxTest)
        .WithReplay(Seed, Gamma, StartSize);

    public static void Verify(string name, Property property) => property.Check(Replay(name));

    public static Arbitrary<ushort> ArbUShort(int low, int high) => Arb.From(FsCheck.Fluent.Gen.Choose(low, high).Select(value => (ushort)value));

    public static Arbitrary<bool> ArbBool() => Arb.From(FsCheck.Fluent.Gen.Elements(false, true));

    public static Arbitrary<int> ArbLength(int maxExclusive) => Arb.From(FsCheck.Fluent.Gen.Choose(0, maxExclusive - 1));
}
