using System.Buffers.Binary;
using System.Text;
using Tl.Gen.Tlb;

namespace Tl.Bake;

internal static class DecodeCommand
{
    const string Usage = "Usage: tl decode <input.tlb|game.tlib> [-o <out.json|dir>] --assembly <path>...";

    internal static int Run(string[] args)
    {
        string? input = null, output = null;
        var assemblies = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is "--assembly" or "-a" or "-o" or "--out")
            {
                if (i + 1 >= args.Length) return Fail($"Missing argument for {args[i]}");
                if (args[i] is "-o" or "--out") output = args[++i];
                else assemblies.Add(args[++i]);
            }
            else if (args[i].StartsWith('-') || input != null) return Fail($"Unexpected argument '{args[i]}'");
            else input = args[i];
        }
        if (input == null || assemblies.Count == 0) return Fail(Usage);
        if (!File.Exists(input)) return Fail($"Input file '{input}' does not exist.");
        var bytes = File.ReadAllBytes(input);
        var resolver = new BakerAssemblyResolver(assemblies);
        if (bytes.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x42494C54u
            && Tlib3Inspection.IsV3(bytes))
            return LibraryV3(bytes, output);
        if (bytes.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x42494C54u)
            return Library(bytes, resolver, output ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(input))!, Path.GetFileNameWithoutExtension(input) + ".decoded"));
        var json = TimelineJson.Decode(bytes, resolver);
        if (output == null) Console.Write(json);
        else Write(output, json);
        return 0;
    }

    static int Library(byte[] lib, BakerAssemblyResolver resolver, string dir)
    {
        var manifest = TlScene.ManifestOf(lib, out var entries);
        var channels = manifest.Channels.OrderBy(c => c.Value).Select(c => c.Key).ToList();
        Directory.CreateDirectory(dir);
        foreach (var (name, tlb) in entries)
        {
            try { Write(Path.Combine(dir, name + ".json"), TimelineJson.Decode(tlb, resolver, manifest)); }
            catch (BakeDiagnosticException error) { throw new BakeDiagnosticException($"timeline '{name}': {error.Message}"); }
        }
        Write(Path.Combine(dir, "tl.project.json"), TlJson.ExportProject(entries.Select(e => e.Name + ".json").ToList(), channels, manifest.TickRate));
        Console.WriteLine($"decode: {entries.Count} timelines -> {dir}");
        if (BinaryPrimitives.ReadUInt32LittleEndian(lib.AsSpan(4)) >= 2) Console.WriteLine("decode: kind settings (project.kinds) are not stored in game.tlib; copy them from the authoring project.");
        return 0;
    }

    static int LibraryV3(byte[] lib, string? output)
    {
        var json = Tlib3Inspection.Inspect(lib);
        if (output == null) Console.Write(json);
        else if (Directory.Exists(output)) Write(Path.Combine(output, "tlib3.json"), json);
        else Write(output, json);
        return 0;
    }

    static void Write(string path, string text)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        File.WriteAllText(path, text);
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine($"Error: {message}");
        return 1;
    }
}
