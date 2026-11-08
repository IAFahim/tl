using System.Buffers.Binary;
using System.Text;
using Tl.Gen.Tlb;

namespace Tl.Bake;

internal static class DecodeCommand
{
    const string Usage = "Usage: tlb decode <input.tlb|game.tlib> [-o <out.json|dir>] --assembly <path>...";

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
        if (bytes.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x42494C54u)
            return Library(bytes, resolver, output ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(input))!, Path.GetFileNameWithoutExtension(input) + ".decoded"));
        var json = TimelineJson.Decode(bytes, resolver);
        if (output == null) Console.Write(json);
        else Write(output, json);
        return 0;
    }

    static int Library(byte[] lib, BakerAssemblyResolver resolver, string dir)
    {
        uint U(int at) => BinaryPrimitives.ReadUInt32LittleEndian(lib.AsSpan(at));
        if (lib.Length < 96 || U(20) != lib.Length) throw new BakeDiagnosticException("game.tlib header is invalid.");
        var count = (int)U(8);
        var manifest = new LibraryManifest { TickRate = U(24) };
        var entries = new List<(string Name, byte[] Tlb)>();
        for (var i = 0; i < count; i++)
        {
            var at = (int)U(12) + 32 * i;
            var name = Encoding.UTF8.GetString(lib, (int)U(at + 8), (int)U(at + 12));
            entries.Add((name, lib.AsSpan((int)U(at + 16), (int)U(at + 20)).ToArray()));
            manifest.Timelines[name] = (ushort)U(at);
        }
        var channels = new SortedDictionary<int, string>();
        foreach (var (name, tlb) in entries)
        {
            var id = (int)BinaryPrimitives.ReadUInt32LittleEndian(tlb.AsSpan(56));
            if (id == 0) continue;
            var label = TlbMetadata.Read(tlb).Labels.FirstOrDefault(l => l.TrackEntry == -2).Name
                ?? throw new BakeDiagnosticException($"timeline '{name}' has channel {id} but its metadata names no channel.");
            if (channels.TryGetValue(id, out var known) && known != label) throw new BakeDiagnosticException($"channel {id} is named '{known}' and '{label}'.");
            channels[id] = label;
        }
        for (var id = 1; id <= channels.Count; id++)
            if (!channels.ContainsKey(id)) throw new BakeDiagnosticException($"game.tlib uses channel {channels.Keys.Max()} but no timeline names channel {id}; unused channel names are not stored in game.tlib.");
        foreach (var pair in channels) manifest.Channels[pair.Value] = (ushort)pair.Key;
        Directory.CreateDirectory(dir);
        foreach (var (name, tlb) in entries)
        {
            try { Write(Path.Combine(dir, name + ".json"), TimelineJson.Decode(tlb, resolver, manifest)); }
            catch (BakeDiagnosticException error) { throw new BakeDiagnosticException($"timeline '{name}': {error.Message}"); }
        }
        Write(Path.Combine(dir, "tl.project.json"), TlJson.ExportProject(entries.Select(e => e.Name + ".json").ToList(), channels.Values.ToList(), manifest.TickRate));
        Console.WriteLine($"decode: {entries.Count} timelines -> {dir}");
        if (U(4) >= 2) Console.WriteLine("decode: kind settings (project.kinds) are not stored in game.tlib; copy them from the authoring project.");
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
