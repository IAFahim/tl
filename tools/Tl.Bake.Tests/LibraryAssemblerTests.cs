using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Tl;
using Tl.Bake;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public sealed unsafe class LibraryAssemblerTests
{
    static string Golden(params string[] parts)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine([dir, "golden", .. parts]);
    }

    static LibraryPack.LibraryKindInputs Inputs(string folder = "kinds")
        => LibraryPack.LoadKindInputs(Golden(folder), [typeof(KindFixture.Damage).Assembly.Location], false);

    [Fact]
    public void AssemblesTheGoldenKindLibraryFromPerTimelineParts()
    {
        var input = Inputs();
        var bytes = LibraryAssembler.Assemble(input.Parts, input.TickRate, input.Settings);
        Assert.Equal(File.ReadAllBytes(Golden("kinds", "game.tlib")), bytes);
    }

    [Fact]
    public void ReadsBackEveryTimelineAndEveryCapacity()
    {
        var input = Inputs();
        var bytes = LibraryAssembler.Assemble(input.Parts, input.TickRate, input.Settings, out var capacities);
        using var world = new TimelineWorld(bytes);
        var view = world.View;
        Assert.Equal(input.Parts.Count, view.TimelineCount);
        Assert.Equal(input.TickRate, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(24)));
        for (var i = 0; i < input.Parts.Count; i++)
        {
            var row = 64 + 32 * i;
            var name = Encoding.UTF8.GetString(bytes, (int)U32(bytes, row + 8), (int)U32(bytes, row + 12));
            Assert.Equal(input.Parts[i].Name, name);
            Assert.Equal(input.Parts[i].Tlb, bytes.AsSpan((int)U32(bytes, row + 16), (int)U32(bytes, row + 20)).ToArray());
            Assert.Equal(LibraryAssembler.ChannelOf(input.Parts[i].Tlb), U32(bytes, row + 4));
        }
        var kinds = view.Kinds;
        Assert.False(kinds.IsEmpty);
        var plain = capacities.Where(c => !c.Group).ToList();
        Assert.Equal(plain.Count, kinds.KindCount);
        for (var k = 0; k < kinds.KindCount; k++)
        {
            Assert.Equal(plain[k].Name, kinds.NameOf(k));
            Assert.Equal((uint)plain[k].Capacity, kinds.Kinds[k].Capacity);
            var group = kinds.GroupOf(k);
            if (group.Length > 0)
                Assert.Equal((uint)capacities.Single(c => c.Group && c.Name == group).Capacity, kinds.Kinds[k].GroupCapacity);
        }
        for (var i = 0; i < input.Parts.Count; i++)
            Assert.Equal(KindPayload.Read(input.Parts[i].Kinds, input.Parts[i].Name)!.KindMask, kinds.Timelines[i].KindMask);
        Assert.Equal(capacities.Select(c => c.Capacity), LibraryAssembler.Capacities(input.Parts, input.Settings).Select(c => c.Capacity));
    }

    [Fact]
    public void PayloadRoundTripsItsColumnsAndCatalog()
    {
        var input = Inputs();
        var first = KindPayload.Read(input.Parts[0].Kinds, input.Parts[0].Name)!;
        var again = KindPayload.Write(first.MaxSpans, first.MaxPulses, first.Inline, first.GamePulses, first.Section);
        Assert.Equal(input.Parts[0].Kinds, again);
        foreach (var part in input.Parts)
            Assert.True(first.SameCatalog(KindPayload.Read(part.Kinds, part.Name)!));
        Assert.Null(KindPayload.Read(null, "none"));
        var bad = (byte[])input.Parts[0].Kinds!.Clone();
        bad[0] ^= 1;
        Assert.Contains("unknown format", Assert.Throws<InvalidOperationException>(() => KindPayload.Read(bad, "x")).Message);
    }

    [Fact]
    public void OverridesSetCapacityAndUnknownNamesFail()
    {
        var input = Inputs();
        var settings = input.Settings;
        settings.Kinds = new Dictionary<string, KindOverride> { ["KindFixture.Damage"] = new(inline: 7), ["Effects"] = new(inline: 3) };
        var capacities = LibraryAssembler.Capacities(input.Parts, settings);
        Assert.Equal(7, capacities.Single(c => c.Name == "KindFixture.Damage").Capacity);
        Assert.Equal(3, capacities.Single(c => c.Group && c.Name == "Effects").Capacity);
        settings.Kinds = new Dictionary<string, KindOverride> { ["Nope"] = new(inline: 1) };
        var error = Assert.Throws<InvalidOperationException>(() => LibraryAssembler.Assemble(input.Parts, input.TickRate, settings));
        Assert.Contains("'Nope' is not a kind or kind group", error.Message);
    }

    [Fact]
    public void PartsBakedAgainstDifferentKindsFail()
    {
        var input = Inputs();
        var parts = input.Parts.ToList();
        var payload = KindPayload.Read(parts[1].Kinds, parts[1].Name)!;
        var inline = payload.Inline.ToArray();
        inline[0] = 99;
        var changed = parts[1];
        changed.Kinds = KindPayload.Write(payload.MaxSpans, payload.MaxPulses, inline, payload.GamePulses, payload.Section);
        parts[1] = changed;
        var error = Assert.Throws<InvalidOperationException>(() => LibraryAssembler.Assemble(parts, input.TickRate, input.Settings));
        Assert.Contains("different kinds", error.Message);
    }

    [Fact]
    public void PatchesNextAndTimelineReferences()
    {
        var input = Inputs("library");
        var parts = input.Parts.ToList();
        var withNext = parts.FindIndex(p => LibraryAssembler.NextOf(p.Tlb) != 0);
        Assert.True(withNext >= 0);
        var part = parts[withNext];
        part.Next = (ushort)parts.Count;
        part.Refs = [0u, 1u];
        parts[withNext] = part;
        var bytes = LibraryAssembler.Assemble(parts, input.TickRate, input.Settings);
        var row = 64 + 32 * withNext;
        var tlb = bytes.AsSpan((int)U32(bytes, row + 16), (int)U32(bytes, row + 20));
        Assert.Equal((uint)parts.Count, LibraryAssembler.NextOf(tlb));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(tlb));
        Assert.Equal(input.Parts[withNext].Tlb, part.Tlb);
        part.Refs = [(uint)part.Tlb.Length, 1u];
        parts[withNext] = part;
        Assert.Contains("outside its data", Assert.Throws<InvalidOperationException>(() => LibraryAssembler.Assemble(parts, input.TickRate, input.Settings)).Message);
    }

    [Fact]
    public void NoKindDataGivesAVersionOneLibrary()
    {
        var input = Inputs();
        var parts = input.Parts.Select(p => new LibraryPart { Name = p.Name, Tlb = p.Tlb, Next = p.Next }).ToList();
        var bytes = LibraryAssembler.Assemble(parts, input.TickRate, input.Settings, out var capacities);
        Assert.Empty(capacities);
        Assert.Equal(1u, U32(bytes, 4));
        using var world = new TimelineWorld(bytes);
        Assert.True(world.View.Kinds.IsEmpty);
    }

    static uint U32(byte[] bytes, int at) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at));
}
