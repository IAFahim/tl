using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Tl;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed class TypeNameCollisionTests
{
    static Type Twin(string assembly)
    {
        var builder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assembly), AssemblyBuilderAccess.Run);
        var module = builder.DefineDynamicModule(assembly);
        var type = module.DefineType("Twins.SharedClip", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, typeof(ValueType));
        type.DefineField("Value", typeof(int), FieldAttributes.Public);
        return type.CreateType();
    }

    static Assembly Saved(string assembly)
    {
        var builder = new PersistedAssemblyBuilder(new AssemblyName(assembly), typeof(object).Assembly);
        var module = builder.DefineDynamicModule(assembly);
        var type = module.DefineType("Twins.SharedClip", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, typeof(ValueType));
        type.DefineField("Value", typeof(int), FieldAttributes.Public);
        type.CreateType();
        var path = Path.Combine(Path.GetTempPath(), "tl-twin-" + Guid.NewGuid().ToString("N"), assembly + ".dll");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        builder.Save(path);
        return new System.Runtime.Loader.AssemblyLoadContext(assembly, true).LoadFromAssemblyPath(path);
    }

    [Fact]
    public void PairKeysUseTheFullTypeNameWithoutTheAssembly()
    {
        var a = Twin("TwinKeyA");
        var b = Twin("TwinKeyB");
        Assert.Equal("Twins.SharedClip", Keying.StableName(a));
        Assert.Equal(Keying.Of("Twins.SharedClip"), Keying.Of(Keying.StableName(b)));
        Assert.Equal(Keying.Of("Tl.Bake.Tests.TypeNameCollisionTests"), Keying.Of(Keying.StableName(typeof(TypeNameCollisionTests))));
    }

    [Fact]
    public void TwoTypesWithOneFullNameFailWithBothAssemblyNames()
    {
        var error = Assert.Throws<BakeDiagnosticException>(() => TypeNames.Unique([Twin("TwinB"), Twin("TwinA")]));
        Assert.Contains("'Twins.SharedClip' is defined in assembly 'TwinA' and in assembly 'TwinB'", error.Message);
    }

    [Fact]
    public void TheResolverRefusesAFullNameTwinEvenWhenTheAssemblyIsNamed()
    {
        var resolver = BakerAssemblyResolver.FromAssemblies([Saved("TwinResolveA"), Saved("TwinResolveB")]);
        var error = Assert.Throws<BakeDiagnosticException>(() => resolver.ResolveType("Twins", "SharedClip", "TwinResolveA", "clip 0"));
        Assert.Contains("TwinResolveA", error.Message);
        Assert.Contains("TwinResolveB", error.Message);
        Assert.Contains("type name collision", error.Message);
    }

    [Fact]
    public void ATypedBakeRefusesTwoTypesWithOneFullName()
    {
        var timeline = new TypedTimeline { Name = "twins", Duration = 4 };
        var first = new TypedTrack { Type = Twin("TwinTypedA") };
        first.Clips.Add(new TypedClip { Type = Twin("TwinTypedB"), Start = 0, End = 4 });
        timeline.Tracks.Add(first);
        var error = Assert.Throws<BakeDiagnosticException>(() => LibraryBake.BakeTyped(timeline));
        Assert.Contains("'Twins.SharedClip' is defined in assembly 'TwinTypedA' and in assembly 'TwinTypedB'", error.Message);
    }
}
