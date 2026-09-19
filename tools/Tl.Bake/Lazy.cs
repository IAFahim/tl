using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tl.Bake;

internal static class Lazy
{
    internal static string? FindSingleJson(string directory)
    {
        var candidates = Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.Ordinal).ToArray();
        if (candidates.Length == 0)
        {
            Console.Error.WriteLine($"Error: No input given and no *.json found in '{directory}'.");
            Console.Error.WriteLine("Fix: pass an input, e.g. 'tlb jump.json'.");
            return null;
        }
        if (candidates.Length > 1)
        {
            Console.Error.WriteLine($"Error: No input given and {candidates.Length} *.json files found in '{directory}':");
            foreach (var candidate in candidates)
                Console.Error.WriteLine($"  {Path.GetFileName(candidate)}");
            Console.Error.WriteLine("Fix: name one, e.g. 'tlb jump.json'.");
            return null;
        }
        return candidates[0];
    }

    internal static string DefaultOutput(string jsonPath) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(jsonPath))!, Path.GetFileNameWithoutExtension(jsonPath) + ".tlb");

    internal static string? FindAssembly(string jsonPath, bool autoNamespace = false)
    {
        var pairs = ReferencedPairs(jsonPath);
        if (pairs.Count == 0)
        {
            Console.Error.WriteLine($"Error: '{Path.GetFileName(jsonPath)}' names no track or clip types to look for.");
            Console.Error.WriteLine("Fix: 'tlb --json --assembly <path>' lists authorable pairs; give the JSON at least one track.");
            return null;
        }
        var directory = Directory.GetCurrentDirectory();
        var fingerprint = Fingerprint(pairs);
        var cache = Cache.Load(directory);
        if (cache.TryGet(Path.GetFullPath(jsonPath), fingerprint, out var cached) && DefinesAll(cached, pairs, autoNamespace))
        {
            Console.WriteLine($"assembly: {cached} (cached in tlb.db)");
            return cached;
        }
        var candidates = Sweep(directory);
        var matches = new List<string>();
        foreach (var candidate in candidates)
            if (DefinesAll(candidate, pairs, autoNamespace))
                matches.Add(candidate);
        matches = PreferRelease(matches);
        if (matches.Count == 1)
        {
            cache.Put(Path.GetFullPath(jsonPath), fingerprint, matches[0]);
            Cache.Save(directory, cache);
            Console.WriteLine($"assembly: {matches[0]} (found by scanning {candidates.Count} dlls under '{directory}')");
            return matches[0];
        }
        if (matches.Count == 0)
        {
            var missing = string.Join(", ", pairs.Select(static pair => pair.Namespace is null ? $"<auto>.{pair.Type}" : $"{pair.Namespace}.{pair.Type}"));
            Console.Error.WriteLine($"Error: No dll under '{directory}' defines the types this JSON names: {missing}.");
            Console.Error.WriteLine($"Scanned {candidates.Count} dlls. Fix: build the project or pass --assembly <path>.");
            return null;
        }
        Console.Error.WriteLine($"Error: {matches.Count} dlls define every type this JSON names:");
        foreach (var match in matches)
            Console.Error.WriteLine($"  {match}");
        Console.Error.WriteLine("Fix: pass one explicitly, e.g. 'tlb jump.json jump.tlb --assembly bin/Release/net10.0/Showcase.dll'.");
        return null;
    }

    internal static List<(string? Namespace, string Type)> ReferencedPairs(string jsonPath)
    {
        var pairs = new List<(string?, string)>();
        using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
        if (!document.RootElement.TryGetProperty("tracks", out var tracks) || tracks.ValueKind != JsonValueKind.Array) return pairs;
        foreach (var track in tracks.EnumerateArray())
        {
            AddPair(track, pairs);
            if (!track.TryGetProperty("clips", out var clips) || clips.ValueKind != JsonValueKind.Array) continue;
            foreach (var clip in clips.EnumerateArray())
                AddPair(clip, pairs);
        }
        return pairs;
    }

    static void AddPair(JsonElement element, List<(string? Namespace, string Type)> pairs)
    {
        if (!element.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String) return;
        string? ns = element.TryGetProperty("namespace", out var namespaceElement) && namespaceElement.ValueKind == JsonValueKind.String
            ? namespaceElement.GetString()!
            : null;
        var pair = (ns, type.GetString()!);
        if (!pairs.Contains(pair)) pairs.Add(pair);
    }

    static List<string> Sweep(string directory)
    {
        var skip = new HashSet<string>(StringComparer.Ordinal) { ".git", ".vs", "node_modules", "TestResults", "artifacts", "obj" };
        var dlls = new List<string>();
        var stack = new Stack<string>();
        stack.Push(directory);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(current).OrderBy(static path => path, StringComparer.Ordinal))
            {
                if (Directory.Exists(entry))
                {
                    var name = Path.GetFileName(entry);
                    if (!skip.Contains(name) && !name.StartsWith('.')) stack.Push(entry);
                    continue;
                }
                if (entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) dlls.Add(entry);
            }
        }
        return dlls;
    }

    static List<string> PreferRelease(List<string> matches)
    {
        var release = matches.Where(static path => path.Contains("Release", StringComparison.Ordinal)).ToList();
        return release.Count > 0 ? release : matches;
    }

    internal static bool DefinesAll(string assemblyPath, List<(string? Namespace, string Type)> pairs, bool autoNamespace = false)
    {
        if (!File.Exists(assemblyPath)) return false;
        try
        {
            var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            using var context = new MetadataLoadContext(new PathAssemblyResolver(
                Directory.EnumerateFiles(Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!, "*.dll")
                .Concat(Directory.EnumerateFiles(coreDir, "*.dll"))
                .Select(static path => Path.GetFullPath(path))));
            var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = [.. ex.Types.Where(static type => type != null)!];
            }
            foreach (var group in pairs)
            {
                var found = false;
                foreach (var type in types)
                    if (Defines(type, group, autoNamespace))
                    {
                        found = true;
                        break;
                    }
                if (!found) return false;
            }
            return true;
        }
        catch (Exception exception) when (exception is BadImageFormatException or IOException)
        {
            return false;
        }
    }

    static bool Defines(Type type, (string? Namespace, string Type) pair, bool autoNamespace) =>
        type.Name == pair.Type && (pair.Namespace != null
            ? type.Namespace == pair.Namespace
            : autoNamespace);

    static string Fingerprint(List<(string? Namespace, string Type)> pairs)
    {
        var builder = new StringBuilder();
        foreach (var pair in pairs.OrderBy(static pair => pair.Namespace, StringComparer.Ordinal).ThenBy(static pair => pair.Type, StringComparer.Ordinal))
            builder.Append(pair.Namespace ?? "<auto>").Append('\0').Append(pair.Type).Append('\0');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))[..16];
    }

    internal sealed class Cache
    {
        readonly List<(string Json, string Fingerprint, string Assembly)> _entries = [];

        internal bool TryGet(string jsonPath, string fingerprint, out string assemblyPath)
        {
            assemblyPath = "";
            foreach (var entry in _entries)
                if (entry.Json == jsonPath && entry.Fingerprint == fingerprint)
                {
                    assemblyPath = entry.Assembly;
                    return true;
                }
            return false;
        }

        internal void Put(string jsonPath, string fingerprint, string assemblyPath)
        {
            _entries.RemoveAll(entry => entry.Json == jsonPath);
            _entries.Add((jsonPath, fingerprint, assemblyPath));
        }

        internal static Cache Load(string directory)
        {
            var path = Path.Combine(directory, "tlb.db");
            if (!File.Exists(path)) return new Cache();
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var cache = new Cache();
                foreach (var entry in document.RootElement.GetProperty("entries").EnumerateArray())
                    cache._entries.Add((entry.GetProperty("json").GetString()!, entry.GetProperty("types").GetString()!, entry.GetProperty("assembly").GetString()!));
                return cache;
            }
            catch (Exception exception) when (exception is JsonException or IOException or KeyNotFoundException)
            {
                return new Cache();
            }
        }

        internal static void Save(string directory, Cache cache)
        {
            var path = Path.Combine(directory, "tlb.db");
            using var stream = File.Create(path);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            writer.WriteStartObject();
            writer.WriteStartArray("entries");
            foreach (var entry in cache._entries.OrderBy(static entry => entry.Json, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("json", entry.Json);
                writer.WriteString("types", entry.Fingerprint);
                writer.WriteString("assembly", entry.Assembly);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
