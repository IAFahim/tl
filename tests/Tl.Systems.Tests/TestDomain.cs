using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Tl.Gen.CSharp;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Systems.Tests;

internal sealed class TestDomain
{
    public Assembly Domain = null!;
    public Assembly Host = null!;
    public byte[] Image = null!;
    public string Generated = "";
    public IReadOnlyList<string> Warnings = [];
    public string Id = "";

    static readonly CSharpParseOptions Parse = new(LanguageVersion.Latest);

    internal static byte[] Compile(string name, string source, IEnumerable<MetadataReference> references, string id, out string generated, out IReadOnlyList<string> warnings)
    {
        var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, Parse, name + ".cs")], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, optimizationLevel: OptimizationLevel.Release));
        var text = new StringBuilder();
        var warned = new List<string>();
        if (id.Length > 0)
        {
            var emitted = SystemGeneration.Emit(compilation, id);
            var errors = emitted.Diagnostics.Where(static d => d.Code is not ("TLGEN113" or "TLGEN114")).Select(static d => d.Code + ": " + d.Message).ToList();
            Assert.True(errors.Count == 0, string.Join("\n", errors));
            warned.AddRange(emitted.Diagnostics.Where(static d => d.Code is "TLGEN113" or "TLGEN114").Select(static d => d.Code + ": " + d.Message));
            foreach (var artifact in emitted.Artifacts)
            {
                text.Append(artifact.Content).Append('\n');
                compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(artifact.Content, Parse, artifact.RelativePath));
            }
        }
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        generated = text.ToString();
        warnings = warned;
        return stream.ToArray();
    }

    internal static (string Type, string Field)[] AutoColumns(string generated, string id)
    {
        var at = generated.IndexOf("ref partial struct TlAuto_" + id, StringComparison.Ordinal);
        Assert.True(at >= 0, "no auto system for " + id);
        var end = generated.IndexOf('}', at);
        return Regex.Matches(generated.Substring(at, end - at), @"Span<([^>]+)> (C\d+_\w+);").Select(static m => (m.Groups[1].Value, m.Groups[2].Value)).ToArray();
    }

    public static TestDomain Build(string name, string source, string id, string hostTemplate, params MetadataReference[] extra)
    {
        var references = EmissionTests.References().Concat(extra).ToList();
        var image = Compile(name, source, references, id, out var generated, out var warnings);
        var columns = AutoColumns(generated, id);
        var host = hostTemplate
            .Replace("/*DECL*/", string.Join("\n", columns.Select(static (c, i) => $"var a{i} = new {c.Type}[rows];")))
            .Replace("/*INIT*/", string.Join(", ", columns.Select(static (c, i) => $"{c.Field} = a{i}")))
            .Replace("/*CLEAR*/", string.Join(" ", columns.Select(static (c, i) => $"global::System.Array.Clear(a{i});")))
            .Replace("/*SNAP*/", string.Join("\n", columns.Select(static (c, i) => $"all.AddRange(global::System.Runtime.InteropServices.MemoryMarshal.AsBytes<{c.Type}>(a{i}).ToArray());")))
            .Replace("/*AUTO*/", "Tl.Generated.TlAuto_" + id);
        var hostImage = Compile(name + "Host", host, references.Append(MetadataReference.CreateFromImage(image)), "", out _, out _);
        var context = new AssemblyLoadContext(name, false);
        return new TestDomain
        {
            Id = id,
            Image = image,
            Generated = generated,
            Warnings = warnings,
            Domain = context.LoadFromStream(new MemoryStream(image)),
            Host = context.LoadFromStream(new MemoryStream(hostImage)),
        };
    }

    public byte[] Play(string hostType, byte[] library, int rows, int steps)
        => (byte[])Host.GetType(hostType, true)!.GetMethod("Play")!.Invoke(null, [library, rows, steps])!;

    internal static byte[] Pack(params object[] values)
    {
        var bytes = new List<byte>();
        var align = 1;
        foreach (var value in values)
        {
            var piece = value switch
            {
                float f => BitConverter.GetBytes(f),
                int i => BitConverter.GetBytes(i),
                bool b => new[] { (byte)(b ? 1 : 0) },
                _ => throw new ArgumentException(value.GetType().Name),
            };
            var unit = Math.Min(piece.Length, 4);
            align = Math.Max(align, unit);
            while (bytes.Count % unit != 0) bytes.Add(0);
            bytes.AddRange(piece);
        }
        while (bytes.Count % align != 0) bytes.Add(0);
        return bytes.ToArray();
    }

    internal static TypedTrack Track(Type clip, params (uint Start, uint End, byte[] Data)[] clips)
    {
        var track = new TypedTrack { Type = typeof(TlNoSettings<>).MakeGenericType(clip) };
        foreach (var (start, end, data) in clips) track.Clips.Add(new TypedClip { Type = clip, Start = start, End = end, Data = data });
        return track;
    }

    internal static byte[] Library(IReadOnlyList<TypedTimeline> timelines, params Assembly[] kindAssemblies)
    {
        var manifest = LibraryBake.Manifest(timelines.Select(static t => t.Name!).ToList(), ["move"], 60);
        var kinds = LibraryBake.Kinds(kindAssemblies);
        var parts = new List<LibraryPart>();
        foreach (var timeline in timelines)
        {
            var tlb = LibraryBake.BakeTyped(timeline, manifest);
            parts.Add(new LibraryPart { Name = timeline.Name!, Tlb = tlb, Kinds = LibraryBake.KindPayload(timeline.Name!, tlb, kinds), Next = (ushort)LibraryAssembler.NextOf(tlb) });
        }
        var settings = LibrarySettings.Default;
        settings.ChannelCount = 1;
        return LibraryAssembler.Assemble(parts, 60, settings);
    }
}
