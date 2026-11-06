using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tl.Gen.Tlb;

namespace Tl.Bake;

internal static class LibraryPack
{
    internal static int Run(string[] args)
    {
        string? dir = null;
        var assemblyPaths = new List<string>();
        string? outPath = null;
        string? idsPath = null;
        string? kindsPath = null;
        var ns = "Game";
        var auto = false;
        var diagnosticsJson = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--assembly" || a == "-a")
            {
                if (i + 1 >= args.Length) return Fail("Missing argument for --assembly", diagnosticsJson);
                assemblyPaths.Add(args[++i]);
            }
            else if (a == "--out") { if (i + 1 >= args.Length) return Fail("Missing argument for --out", diagnosticsJson); outPath = args[++i]; }
            else if (a == "--ids") { if (i + 1 >= args.Length) return Fail("Missing argument for --ids", diagnosticsJson); idsPath = args[++i]; }
            else if (a == "--kinds") { if (i + 1 >= args.Length) return Fail("Missing argument for --kinds", diagnosticsJson); kindsPath = args[++i]; }
            else if (a == "--namespace") { if (i + 1 >= args.Length) return Fail("Missing argument for --namespace", diagnosticsJson); ns = args[++i]; }
            else if (a == "--auto") auto = true;
            else if (a == "--diagnostics")
            {
                if (i + 1 >= args.Length || args[i + 1] != "json") return Fail("--diagnostics only supports 'json'", diagnosticsJson);
                i++; diagnosticsJson = true;
            }
            else if (a.StartsWith('-')) return Fail($"Unexpected flag '{a}'", diagnosticsJson);
            else if (dir == null) dir = a;
            else return Fail($"Unexpected argument '{a}'", diagnosticsJson);
        }
        if (dir == null) return Fail("Usage: tlb library <dir> [--assembly <path>]... [--out game.tlib] [--ids Timelines.g.cs] [--kinds Kinds.g.cs] [--namespace Game] [--auto] [--diagnostics json]", diagnosticsJson);
        try { return Pack(dir, assemblyPaths, outPath, idsPath, kindsPath, ns, auto, diagnosticsJson); }
        catch (BakeDiagnosticException ex) { return Fail(ex.Message, diagnosticsJson); }
        catch (Exception ex) { return Fail(ex.Message, diagnosticsJson); }
    }

    internal static int Fail(string message, bool json)
    {
        if (json) Console.Error.WriteLine(JsonSerializer.Serialize(new { errors = new[] { new { message } } }));
        else Console.Error.WriteLine($"Error: {message}");
        return 1;
    }

    internal sealed record LibraryKindInputs(List<(string Name, string File)> Names, List<byte[]> Blobs, uint TickRate, int ChannelCount, KindSettings Settings, KindCatalog Catalog, List<KindTimeline> KindTimelines);

    internal static LibraryKindInputs LoadKindInputs(string dir, List<string> assemblyPaths, bool auto)
    {
        dir = Path.GetFullPath(dir);
        var libraryJson = Path.Combine(dir, "library.json");
        if (!File.Exists(libraryJson)) throw new BakeDiagnosticException($"library.json not found in '{dir}'.");
        using var libDoc = JsonDocument.Parse(File.ReadAllBytes(libraryJson));
        var root = libDoc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new BakeDiagnosticException("library.json root must be an object.");
        var tickRate = 60u;
        if (root.TryGetProperty("tickRate", out var tr))
        {
            if (tr.ValueKind != JsonValueKind.Number || !tr.TryGetUInt32(out tickRate)) throw new BakeDiagnosticException("library.json 'tickRate' must be an unsigned integer.");
        }
        var channels = new List<string>();
        if (root.TryGetProperty("channels", out var ch))
        {
            if (ch.ValueKind != JsonValueKind.Array) throw new BakeDiagnosticException("library.json 'channels' must be an array.");
            foreach (var c in ch.EnumerateArray())
            {
                if (c.ValueKind != JsonValueKind.String || c.GetString()!.Length == 0) throw new BakeDiagnosticException("library.json channel names must be non-empty strings.");
                channels.Add(c.GetString()!);
            }
        }
        var kindSettings = KindSettingsOf(root);
        if (!root.TryGetProperty("timelines", out var tl) || tl.ValueKind != JsonValueKind.Array) throw new BakeDiagnosticException("library.json needs a 'timelines' array.");
        var patterns = new List<string>();
        foreach (var t in tl.EnumerateArray())
        {
            if (t.ValueKind != JsonValueKind.String) throw new BakeDiagnosticException("library.json timeline entries must be strings.");
            patterns.Add(t.GetString()!);
        }
        var files = Expand(dir, patterns);
        if (files.Count == 0) throw new BakeDiagnosticException("library.json 'timelines' matched no files.");
        var names = new List<(string Name, string File)>();
        foreach (var f in files) names.Add((TimelineName(f), f));
        names.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        for (var i = 1; i < names.Count; i++)
            if (names[i - 1].Name == names[i].Name) throw new BakeDiagnosticException($"duplicate timeline name '{names[i].Name}'.");
        var manifest = new LibraryManifest { TickRate = tickRate };
        for (var i = 0; i < names.Count; i++) manifest.Timelines[names[i].Name] = (ushort)(i + 1);
        for (var i = 0; i < channels.Count; i++)
        {
            if (manifest.Channels.ContainsKey(channels[i])) throw new BakeDiagnosticException($"duplicate channel '{channels[i]}'.");
            manifest.Channels[channels[i]] = (ushort)(i + 1);
        }
        BakerAssemblyResolver resolver;
        if (assemblyPaths.Count > 0) resolver = new BakerAssemblyResolver(assemblyPaths);
        else
        {
            var discovered = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (_, f) in names)
            {
                var found = Lazy.FindAssembly(f, auto);
                if (found != null) discovered.Add(found);
            }
            resolver = discovered.Count > 0 ? new BakerAssemblyResolver(discovered) : new BakerAssemblyResolver();
        }
        var jsons = new List<byte[]>();
        foreach (var (_, f) in names) jsons.Add(File.ReadAllBytes(f));
        var tlbs = new List<byte[]>();
        for (var i = 0; i < names.Count; i++)
        {
            try { tlbs.Add(TimelineBaker.BakeJson(jsons[i], resolver, auto, manifest)); }
            catch (BakeDiagnosticException ex) { throw new BakeDiagnosticException($"{names[i].File}: {ex.Message}"); }
        }
        var catalog = KindCatalog.Discover(resolver.ReferencedAssemblies);
        var kindTimelines = new List<KindTimeline>();
        for (var i = 0; i < names.Count; i++) kindTimelines.Add(KindTimeline.Analyze(names[i].Name, tlbs[i], catalog));
        return new LibraryKindInputs(names, tlbs, tickRate, channels.Count, kindSettings, catalog, kindTimelines);
    }

    static int Pack(string dir, List<string> assemblyPaths, string? outPath, string? idsPath, string? kindsPath, string ns, bool auto, bool diagnosticsJson)
    {
        dir = Path.GetFullPath(dir);
        var input = LoadKindInputs(dir, assemblyPaths, auto);
        var (names, tlbs, tickRate, channelCount, kindSettings, catalog, kindTimelines) = input;
        outPath ??= Path.Combine(dir, "game.tlib");
        idsPath ??= Path.Combine(dir, "Timelines.g.cs");
        var reached = kindTimelines.Any(t => t.KindMask != 0);
        List<KindCapacityRow>? rows = null;
        byte[]? kindSection = null;
        if (reached || kindsPath != null)
        {
            rows = KindLayout.Capacities(catalog, kindTimelines, channelCount, kindSettings);
            if (reached) kindSection = KindLayout.Section(catalog, kindTimelines, rows);
        }
        var gameTlib = Emit(names, tlbs, tickRate, kindSection);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        File.WriteAllBytes(outPath, gameTlib);
        var digest = Convert.ToHexString(SHA256.HashData(gameTlib)).ToLowerInvariant();
        var ids = EmitIds(ns, names, digest, tickRate);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(idsPath))!);
        File.WriteAllText(idsPath, ids);
        var kindsWritten = false;
        if (rows != null)
        {
            kindsPath ??= Path.Combine(Path.GetDirectoryName(Path.GetFullPath(idsPath))!, "Kinds.g.cs");
            kindsWritten = KindLayout.WriteIfChanged(kindsPath, KindLayout.CapacitySource(ns, rows));
        }
        if (diagnosticsJson)
        {
            var kinds = rows?.Select(r => new { name = r.Name, group = r.Group, worst = r.Worst, capacity = r.Capacity, entryBytes = r.EntryBytes, inlineBytes = r.Capacity * r.EntryBytes, setBy = r.SetBy }).ToArray();
            Console.WriteLine(JsonSerializer.Serialize(new { timelines = names.Count, bytes = gameTlib.Length, digest, @out = outPath, ids = idsPath, tickRate, kinds, kindsFile = rows == null ? null : kindsPath, kindsWritten }));
        }
        else
        {
            Console.WriteLine($"library: {names.Count} timelines, {gameTlib.Length} bytes, digest {digest[..16]} -> {outPath}");
            if (rows != null)
            {
                Console.Write(KindLayout.Report(rows));
                Console.WriteLine(kindsWritten ? $"kinds: wrote {kindsPath}" : $"kinds: {kindsPath} unchanged");
            }
        }
        return 0;
    }

    static string TimelineName(string file)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String && n.GetString()!.Length > 0)
                return n.GetString()!;
        }
        catch { }
        return Path.GetFileNameWithoutExtension(file);
    }

    static List<string> Expand(string dir, List<string> patterns)
    {
        var files = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var p in patterns)
        {
            if (p.IndexOfAny(['*', '?']) < 0)
            {
                var full = Path.GetFullPath(Path.Combine(dir, p));
                if (!File.Exists(full)) throw new BakeDiagnosticException($"timeline file not found: '{p}'.");
                files.Add(full);
                continue;
            }
            var sub = p.Replace('\\', '/');
            var slash = sub.LastIndexOf('/');
            var subdir = slash < 0 ? dir : Path.GetFullPath(Path.Combine(dir, sub[..slash]));
            var pattern = slash < 0 ? sub : sub[(slash + 1)..];
            if (!Directory.Exists(subdir)) throw new BakeDiagnosticException($"timeline glob directory not found: '{p}'.");
            foreach (var f in Directory.GetFiles(subdir, pattern)) files.Add(Path.GetFullPath(f));
        }
        return [.. files];
    }

    static KindSettings KindSettingsOf(JsonElement root)
    {
        var settings = new KindSettings();
        if (!root.TryGetProperty("kinds", out var kinds)) return settings;
        if (kinds.ValueKind != JsonValueKind.Object) throw new BakeDiagnosticException("library.json 'kinds' must be an object.");
        foreach (var entry in kinds.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Object) throw new BakeDiagnosticException($"library.json kinds '{entry.Name}' must be an object.");
            int? inline = null, maxInline = null, stackDepth = null;
            var gamePulses = 0;
            foreach (var field in entry.Value.EnumerateObject())
            {
                if (field.Value.ValueKind != JsonValueKind.Number || !field.Value.TryGetInt32(out var number) || number < 0)
                    throw new BakeDiagnosticException($"library.json kinds '{entry.Name}.{field.Name}' must be a non-negative integer.");
                switch (field.Name)
                {
                    case "inline" when entry.Name != "default": inline = number; break;
                    case "gamePulses" when entry.Name != "default": gamePulses = number; break;
                    case "maxInlineBytes": maxInline = number; break;
                    case "stackDepth" when entry.Name == "default": stackDepth = number; break;
                    default: throw new BakeDiagnosticException($"library.json kinds '{entry.Name}': unknown key '{field.Name}'.");
                }
            }
            if (entry.Name == "default")
            {
                if (maxInline is { } m) settings.MaxInlineBytes = m;
                if (stackDepth is { } d) settings.StackDepth = d;
            }
            else settings.Kinds[entry.Name] = (inline, gamePulses, maxInline);
        }
        return settings;
    }

    static byte[] Emit(List<(string Name, string File)> names, List<byte[]> tlbs, uint tickRate, byte[]? kindSection)
    {
        var offsets = new uint[names.Count];
        var lengths = new uint[names.Count];
        var uniq = new List<byte[]>();
        var uniqOff = new List<uint>();
        for (var i = 0; i < names.Count; i++)
        {
            var idx = uniq.FindIndex(u => u.AsSpan().SequenceEqual(tlbs[i]));
            if (idx < 0) { idx = uniq.Count; uniq.Add(tlbs[i]); uniqOff.Add(0); }
            offsets[i] = (uint)idx;
            lengths[i] = (uint)tlbs[i].Length;
        }
        var nameBytes = names.Select(n => Encoding.UTF8.GetBytes(n.Name)).ToList();
        uint manifestOff = 64;
        uint entries = (uint)names.Count * 32u;
        uint strings = 0;
        foreach (var b in nameBytes) strings += (uint)b.Length;
        uint blobOff = (manifestOff + entries + strings + 7u) & ~7u;
        uint cursor = blobOff;
        for (var u = 0; u < uniq.Count; u++) { uniqOff[u] = cursor; cursor += (uint)((uniq[u].Length + 7) & ~7); }
        var kindOff = kindSection == null ? 0u : cursor;
        if (kindSection != null) cursor += (uint)kindSection.Length;
        uint totalBytes = cursor + 32u;
        var bytes = new byte[totalBytes];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0), 0x42494C54u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), kindSection == null ? 1u : 2u);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), (uint)names.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), manifestOff);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), blobOff);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), totalBytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24), tickRate);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(28), kindOff);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), kindSection == null ? 0u : (uint)kindSection.Length);
        var nameAt = manifestOff + entries;
        for (var i = 0; i < names.Count; i++)
        {
            var at = (int)(manifestOff + (uint)i * 32u);
            var ch = tlbs[i].Length >= 80 && BinaryPrimitives.ReadUInt32LittleEndian(tlbs[i].AsSpan(4)) == 6u
                ? BinaryPrimitives.ReadUInt32LittleEndian(tlbs[i].AsSpan(56))
                : 0u;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at), (uint)(i + 1));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 4), ch);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 8), nameAt);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 12), (uint)nameBytes[i].Length);
            var u = (int)offsets[i];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 16), uniqOff[u]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 20), lengths[i]);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 24), 0u);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 28), 0u);
            nameBytes[i].CopyTo(bytes.AsSpan((int)nameAt));
            nameAt += (uint)nameBytes[i].Length;
        }
        for (var u = 0; u < uniq.Count; u++) uniq[u].CopyTo(bytes.AsSpan((int)uniqOff[u]));
        kindSection?.CopyTo(bytes.AsSpan((int)kindOff));
        SHA256.TryHashData(bytes.AsSpan(0, (int)(totalBytes - 32u)), bytes.AsSpan((int)(totalBytes - 32u), 32), out _);
        return bytes;
    }

    static string EmitIds(string ns, List<(string Name, string File)> names, string digest, uint tickRate)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var sb = new StringBuilder();
        sb.Append("// <auto-generated by tlb library; do not edit>\n");
        sb.Append("namespace ").Append(ns).Append("\n{\n");
        sb.Append("    public static class Timelines\n    {\n");
        foreach (var (name, _) in names)
        {
            var id = Pascal(name);
            if (!seen.Add(id)) throw new BakeDiagnosticException($"timeline id collision: '{name}' maps to '{id}'.");
            if (id == "LibraryDigest") throw new BakeDiagnosticException($"timeline id collision: '{name}' maps to reserved 'LibraryDigest'.");
            sb.Append("        public const ushort ").Append(id).Append(" = ").Append(names.FindIndex(n => n.Name == name) + 1).Append(";\n");
        }
        sb.Append("        public const string LibraryDigest = \"").Append(digest).Append("\";\n");
        sb.Append("        public const uint TickRate = ").Append(tickRate).Append(";\n");
        sb.Append("    }\n}\n");
        return sb.ToString();
    }

    static string Pascal(string name)
    {
        var parts = name.Split(['_', '-', ' ', '.'], StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            sb.Append(char.ToUpperInvariant(p[0]));
            for (var i = 1; i < p.Length; i++) sb.Append(p[i]);
        }
        if (sb.Length == 0) throw new BakeDiagnosticException("timeline name is empty.");
        if (char.IsDigit(sb[0])) sb.Insert(0, '_');
        foreach (var c in sb.ToString())
            if (!char.IsLetterOrDigit(c) && c != '_') throw new BakeDiagnosticException($"timeline name '{name}' maps to invalid identifier '{sb}'.");
        return sb.ToString();
    }
}
