using System.Reflection;
using System.Reflection.Emit;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class TlKindCatalogTests
{
    [Fact]
    public void TlKindAndInheritedTrackInterfacesAreKindsAndFeeders()
    {
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TlKindNames" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => e.Name == asm.FullName ? asm : null;
        var module = asm.DefineDynamicModule("m");
        var attribute = module.DefineType("Tl.TlKindAttribute", TypeAttributes.Public | TypeAttributes.Sealed, typeof(Attribute));
        attribute.DefineDefaultConstructor(MethodAttributes.Public);
        attribute.DefineField("Inline", typeof(int), FieldAttributes.Public);
        var attributeType = attribute.CreateType();
        var kind = module.DefineType("Names.Push", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, typeof(ValueType));
        kind.SetCustomAttribute(new CustomAttributeBuilder(attributeType.GetConstructor(Type.EmptyTypes)!, [], [attributeType.GetField("Inline")!], [3]));
        var apply = kind.DefineMethod("Apply", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(float).MakeByRefType()]);
        apply.GetILGenerator().Emit(OpCodes.Ret);
        var combine = kind.DefineMethod("Combine", MethodAttributes.Public | MethodAttributes.Static, typeof(void), [typeof(float).MakeByRefType(), typeof(float).MakeByRefType()]);
        combine.GetILGenerator().Emit(OpCodes.Ret);
        var kindType = kind.CreateType();
        var track = module.DefineType("Names.ITlLikeTrack", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        track.AddInterfaceImplementation(typeof(ITrack<KindFixture.HitTrack, KindFixture.HitClip>));
        var trackType = track.CreateType();
        var consumer = module.DefineType("Names.Pusher", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, typeof(ValueType));
        consumer.AddInterfaceImplementation(trackType);
        var feed = consumer.DefineMethod("Evaluate", MethodAttributes.Public | MethodAttributes.Static, typeof(float), Type.EmptyTypes);
        var il = feed.GetILGenerator();
        il.Emit(OpCodes.Ldc_R4, 1f);
        il.Emit(OpCodes.Ret);
        feed.SetCustomAttribute(new CustomAttributeBuilder(typeof(FeedsAttribute).GetConstructor([typeof(Type)])!, [kindType]));
        consumer.CreateType();

        var catalog = KindCatalog.Discover([asm]);

        var push = Assert.Single(catalog.Kinds);
        Assert.Equal("Push", push.Name);
        Assert.Equal(3, push.Inline);
        var feeder = Assert.Single(catalog.Feeders);
        Assert.Equal(FeederTrigger.Span, feeder.Trigger);
        Assert.Equal(KindCatalog.PairKey(typeof(KindFixture.HitTrack), typeof(KindFixture.HitClip)), feeder.SourceKey);
        var row = Assert.Single(KindLayout.Capacities(catalog, [], 1, new KindSettings()));
        Assert.Equal(3, row.Capacity);
    }
}
