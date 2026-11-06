using System.Text.Json;
using Tl.Gen.Tlb;

namespace Tl.Bake;

internal static class KindsPack
{
    internal static int Run(string[] args)
    {
        string? library = null;
        var assemblyPaths = new List<string>();
        string? outPath = null;
        var ns = "Game";
        var auto = false;
        var diagnosticsJson = false;
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--assembly" || a == "-a")
            {
                if (i + 1 >= args.Length) return LibraryPack.Fail("Missing argument for --assembly", diagnosticsJson);
                assemblyPaths.Add(args[++i]);
            }
            else if (a == "--library") { if (i + 1 >= args.Length) return LibraryPack.Fail("Missing argument for --library", diagnosticsJson); library = args[++i]; }
            else if (a == "--namespace") { if (i + 1 >= args.Length) return LibraryPack.Fail("Missing argument for --namespace", diagnosticsJson); ns = args[++i]; }
            else if (a == "--out") { if (i + 1 >= args.Length) return LibraryPack.Fail("Missing argument for --out", diagnosticsJson); outPath = args[++i]; }
            else if (a == "--auto") auto = true;
            else if (a == "--diagnostics")
            {
                if (i + 1 >= args.Length || args[i + 1] != "json") return LibraryPack.Fail("--diagnostics only supports 'json'", diagnosticsJson);
                i++; diagnosticsJson = true;
            }
            else if (a.StartsWith('-')) return LibraryPack.Fail($"Unexpected flag '{a}'", diagnosticsJson);
            else return LibraryPack.Fail($"Unexpected argument '{a}'", diagnosticsJson);
        }
        if (library == null) return LibraryPack.Fail("Usage: tlb kinds --library <dir> --namespace <ns> [--out Kinds.g.cs] [--assembly <path>]... [--auto] [--diagnostics json]", diagnosticsJson);
        try { return Emit(library, assemblyPaths, outPath, ns, auto, diagnosticsJson); }
        catch (BakeDiagnosticException ex) { return LibraryPack.Fail(ex.Message, diagnosticsJson); }
        catch (Exception ex) { return LibraryPack.Fail(ex.Message, diagnosticsJson); }
    }

    static int Emit(string library, List<string> assemblyPaths, string? outPath, string ns, bool auto, bool diagnosticsJson)
    {
        var dir = Directory.Exists(library) ? Path.GetFullPath(library)
            : File.Exists(library) ? Path.GetFullPath(Path.GetDirectoryName(library)!)
            : throw new BakeDiagnosticException($"library path '{library}' does not exist.");
        var input = LibraryPack.LoadKindInputs(dir, assemblyPaths, auto);
        var rows = KindLayout.Capacities(input.Catalog, input.KindTimelines, input.ChannelCount, input.Settings);
        var source = KindLayout.CapacitySource(ns, rows);
        var kindsWritten = false;
        if (outPath != null) kindsWritten = KindLayout.WriteIfChanged(outPath, source);
        if (diagnosticsJson)
        {
            var kinds = rows.Select(r => new { name = r.Name, group = r.Group, worst = r.Worst, capacity = r.Capacity, entryBytes = r.EntryBytes, inlineBytes = r.Capacity * r.EntryBytes, setBy = r.SetBy }).ToArray();
            Console.WriteLine(JsonSerializer.Serialize(new { timelines = input.Names.Count, @namespace = ns, kinds, kindsFile = outPath, kindsWritten }));
        }
        else
        {
            Console.Write(source);
            Console.Error.Write(KindLayout.Report(rows));
            if (outPath != null) Console.Error.WriteLine(kindsWritten ? $"kinds: wrote {outPath}" : $"kinds: {outPath} unchanged");
        }
        return 0;
    }
}
