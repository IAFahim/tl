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

    internal sealed record LibraryKindInputs(List<(string Name, string File)> Names, List<LibraryPart> Parts, uint TickRate, LibrarySettings Settings, bool Reached);

    internal static LibraryKindInputs LoadKindInputs(string dir, List<string> assemblyPaths, bool auto)
    {
        dir = Path.GetFullPath(dir);
        var libraryJson = File.Exists(dir) ? dir : Path.Combine(dir, "tl.project.json");
        dir = Path.GetDirectoryName(libraryJson)!;
        if (!File.Exists(libraryJson)) throw new BakeDiagnosticException($"tl.project.json not found in '{dir}'.");
        using var libDoc = TlJson.ReadProject(File.ReadAllBytes(libraryJson));
        var root = libDoc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new BakeDiagnosticException("tl.project.json root must be an object.");
        var tickRate = 60u;
        if (root.TryGetProperty("tickRate", out var tr))
        {
            if (tr.ValueKind != JsonValueKind.Number || !tr.TryGetUInt32(out tickRate)) throw new BakeDiagnosticException("tl.project.json 'tickRate' must be an unsigned integer.");
        }
        var channels = new List<string>();
        if (root.TryGetProperty("channels", out var ch))
        {
            if (ch.ValueKind != JsonValueKind.Array) throw new BakeDiagnosticException("tl.project.json 'channels' must be an array.");
            foreach (var c in ch.EnumerateArray())
            {
                if (c.ValueKind != JsonValueKind.String || c.GetString()!.Length == 0) throw new BakeDiagnosticException("tl.project.json channel names must be non-empty strings.");
                channels.Add(c.GetString()!);
            }
        }
        var kindSettings = KindSettingsOf(root);
        if (!root.TryGetProperty("timelines", out var tl) || tl.ValueKind != JsonValueKind.Array) throw new BakeDiagnosticException("tl.project.json needs a 'timelines' array.");
        var patterns = new List<string>();
        foreach (var t in tl.EnumerateArray())
        {
            if (t.ValueKind != JsonValueKind.String) throw new BakeDiagnosticException("tl.project.json timeline entries must be strings.");
            patterns.Add(t.GetString()!);
        }
        var files = Expand(dir, patterns);
        if (files.Count == 0) throw new BakeDiagnosticException("tl.project.json 'timelines' matched no files.");
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
        var kinds = LibraryBake.Kinds(resolver.ReferencedAssemblies);
        var parts = new List<LibraryPart>();
        var reached = false;
        for (var i = 0; i < names.Count; i++)
        {
            parts.Add(new LibraryPart { Name = names[i].Name, Tlb = tlbs[i], Kinds = LibraryBake.KindPayload(names[i].Name, tlbs[i], kinds), Next = (ushort)LibraryAssembler.NextOf(tlbs[i]) });
            reached |= LibraryBake.KindMask(names[i].Name, tlbs[i], kinds) != 0;
        }
        kindSettings.ChannelCount = channels.Count;
        return new LibraryKindInputs(names, parts, tickRate, kindSettings, reached);
    }

    static int Pack(string dir, List<string> assemblyPaths, string? outPath, string? idsPath, string? kindsPath, string ns, bool auto, bool diagnosticsJson)
    {
        dir = Path.GetFullPath(dir);
        var input = LoadKindInputs(dir, assemblyPaths, auto);
        if (File.Exists(dir)) dir = Path.GetDirectoryName(dir)!;
        var names = input.Names;
        var tickRate = input.TickRate;
        outPath ??= Path.Combine(dir, "game.tlib");
        idsPath ??= Path.Combine(dir, "Timelines.g.cs");
        var gameTlib = LibraryAssembler.Assemble(input.Parts, tickRate, input.Settings, out var capacities);
        var rows = input.Reached || kindsPath != null ? capacities : null;
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
            foreach (var f in Directory.GetFiles(subdir, pattern))
                if (!string.Equals(Path.GetFullPath(f), Path.Combine(dir, "tl.project.json"), StringComparison.OrdinalIgnoreCase)) files.Add(Path.GetFullPath(f));
        }
        return [.. files];
    }

    static LibrarySettings KindSettingsOf(JsonElement root)
    {
        var settings = LibrarySettings.Default;
        if (!root.TryGetProperty("kinds", out var kinds)) return settings;
        if (kinds.ValueKind != JsonValueKind.Object) throw new BakeDiagnosticException("tl.project.json 'kinds' must be an object.");
        var overrides = new Dictionary<string, KindOverride>(StringComparer.Ordinal);
        foreach (var entry in kinds.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Object) throw new BakeDiagnosticException($"tl.project.json kinds '{entry.Name}' must be an object.");
            int? inline = null, maxInline = null, stackDepth = null, gamePulses = null;
            foreach (var field in entry.Value.EnumerateObject())
            {
                if (field.Value.ValueKind != JsonValueKind.Number || !field.Value.TryGetInt32(out var number) || number < 0)
                    throw new BakeDiagnosticException($"tl.project.json kinds '{entry.Name}.{field.Name}' must be a non-negative integer.");
                switch (field.Name)
                {
                    case "inline" when entry.Name != "default": inline = number; break;
                    case "gamePulses" when entry.Name != "default": gamePulses = number; break;
                    case "maxInlineBytes": maxInline = number; break;
                    case "stackDepth" when entry.Name == "default": stackDepth = number; break;
                    default: throw new BakeDiagnosticException($"tl.project.json kinds '{entry.Name}': unknown key '{field.Name}'.");
                }
            }
            if (entry.Name == "default")
            {
                if (maxInline is { } m) settings.MaxInlineBytes = m;
                if (stackDepth is { } d) settings.StackDepth = d;
            }
            else overrides[entry.Name] = new KindOverride(inline, gamePulses, maxInline);
        }
        settings.Kinds = overrides;
        return settings;
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
