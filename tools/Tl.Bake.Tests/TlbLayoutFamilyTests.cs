using Tl;
using Tl.Gen.Tlb;
using Xunit;

[assembly: Tlb.TlConsumerLayoutChained(typeof(Tlb.LayoutFamilyTrack), typeof(Tlb.LayoutFamilyClip), 987654321UL)]
[assembly: Tlb.TlConsumerLayoutLeft(typeof(Tlb.LayoutConflictTrack), typeof(Tlb.LayoutConflictClip), 7UL)]
[assembly: Tlb.TlConsumerLayoutRight(typeof(Tlb.LayoutConflictTrack), typeof(Tlb.LayoutConflictClip), 9UL)]

namespace Tlb
{
    public readonly record struct LayoutFamilyClip(int Value);

    public readonly record struct LayoutFamilyTrack(int Code) : IBlend<LayoutFamilyClip>
    {
        public void Blend(in LayoutFamilyClip first, in LayoutFamilyClip second, float factor, out LayoutFamilyClip result) => result = first;
    }

    public readonly record struct LayoutConflictClip(int Value);

    public readonly record struct LayoutConflictTrack(int Code) : IBlend<LayoutConflictClip>
    {
        public void Blend(in LayoutConflictClip first, in LayoutConflictClip second, float factor, out LayoutConflictClip result) => result = first;
    }

    internal sealed class TlConsumerLayoutChainedAttribute : Attribute
    {
        internal TlConsumerLayoutChainedAttribute(Type track, Type clip, ulong layout) { }
    }

    internal sealed class TlConsumerLayoutLeftAttribute : Attribute
    {
        internal TlConsumerLayoutLeftAttribute(Type track, Type clip, ulong layout) { }
    }

    internal sealed class TlConsumerLayoutRightAttribute : Attribute
    {
        internal TlConsumerLayoutRightAttribute(Type track, Type clip, ulong layout) { }
    }
}

namespace Tl.Bake.Tests
{
    public class TlbLayoutFamilyTests
    {
        static BakerAssemblyResolver Resolver => BakerAssemblyResolver.FromAssemblies([typeof(Tlb.LayoutFamilyTrack).Assembly]);

        [Fact]
        public void LayoutScanReadsChainedAttributeFamilyMembers()
            => Assert.Equal(987654321UL, TlbLayouting.Of(Resolver, typeof(Tlb.LayoutFamilyTrack), typeof(Tlb.LayoutFamilyClip)));

        [Fact]
        public void LayoutScanReportsConflictsAcrossAttributeFamilyMembers()
        {
            var failure = Assert.Throws<BakeDiagnosticException>(
                () => TlbLayouting.Of(Resolver, typeof(Tlb.LayoutConflictTrack), typeof(Tlb.LayoutConflictClip)));
            Assert.Contains("consumer layout conflict", failure.Message, StringComparison.Ordinal);
            Assert.Contains("rebake", failure.Message, StringComparison.Ordinal);
        }
    }
}
