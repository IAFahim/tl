using System;
using System.Collections.Generic;
using System.IO;
using Tl.Gen.Tlb;

namespace Tl.Bake;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length >= 1 && args[0] == "--json")
                return Json(args);

            if (args.Length >= 1 && args[0] == "--watch")
                return WatchMode.Run(args, Console.Out);

            if (args.Length >= 1 && args[0] == "--strip")
                return Strip(args);

            if (args.Length >= 1 && args[0] == "--report")
                return Report(args);

            return Bake(args);
        }
        catch (BakeDiagnosticException ex)
        {
            Console.Error.WriteLine($"Diagnostic error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int Json(string[] args)
    {
        var assemblyPaths = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i] == "--assembly" || args[i] == "-a")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Error: Missing argument for --assembly");
                    return 1;
                }
                assemblyPaths.Add(args[++i]);
            }
            else
            {
                Console.Error.WriteLine($"Error: Unexpected argument '{args[i]}'");
                return 1;
            }
        }

        if (assemblyPaths.Count == 0)
        {
            Console.Error.WriteLine("Error: --json requires at least one --assembly path");
            return 1;
        }

        var resolver = new BakerAssemblyResolver(assemblyPaths);
        Console.Write(TlbIntrospection.Introspect(resolver.ReferencedAssemblies));
        return 0;
    }

    private static int Bake(string[] args)
    {
        if (args.Length == 0)
        {
            var guessed = Lazy.FindSingleJson(Directory.GetCurrentDirectory());
            if (guessed == null) return 1;
            args = [guessed];
        }

        string? inputPath = null;
        string? outputPath = null;
        string? cacheDir = null;
        var assemblyPaths = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--assembly" || args[i] == "-a")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Error: Missing argument for --assembly");
                    return 1;
                }
                assemblyPaths.Add(args[++i]);
            }
            else if (args[i] == "--cache")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Error: Missing argument for --cache");
                    return 1;
                }
                cacheDir = args[++i];
            }
            else if (inputPath == null)
            {
                inputPath = args[i];
            }
            else if (outputPath == null)
            {
                outputPath = args[i];
            }
            else
            {
                Console.Error.WriteLine($"Error: Unexpected argument '{args[i]}'");
                return 1;
            }
        }

        if (inputPath == null)
        {
            Console.Error.WriteLine("Usage: tlb [input.json [output.tlb]] [--assembly <path>]... [--cache <dir>]");
            Console.Error.WriteLine("       tlb --strip <input.tlb> <output.tlb>");
            Console.Error.WriteLine("       tlb --report <input.tlb>");
            Console.Error.WriteLine("       tlb --json --assembly <path>...");
            Console.Error.WriteLine("       tlb --watch <input.json> <output.tlb> [--assembly <path>]... [--debounce <ms>]");
            Console.Error.WriteLine("With only an input, output defaults beside it and the assembly is discovered from the JSON's types.");
            return 1;
        }
        outputPath ??= Lazy.DefaultOutput(inputPath);

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: Input file '{inputPath}' does not exist.");
            return 1;
        }

        if (assemblyPaths.Count == 0)
        {
            var discovered = Lazy.FindAssembly(inputPath);
            if (discovered == null) return 1;
            assemblyPaths.Add(discovered);
        }

        var jsonBytes = File.ReadAllBytes(inputPath);
        var assemblyBytes = new List<byte[]>(assemblyPaths.Count);
        foreach (var path in assemblyPaths)
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Error: Referenced assembly file not found: '{path}'");
                return 1;
            }
            assemblyBytes.Add(File.ReadAllBytes(path));
        }

        byte[]? key = null;
        string? tlbEntry = null;
        if (cacheDir != null)
        {
            key = BakeCacheKey.Compute(jsonBytes, assemblyBytes, strip: false);
            tlbEntry = Path.Combine(cacheDir, Convert.ToHexString(key) + ".tlb");
            if (File.Exists(tlbEntry))
            {
                CopyFresh(tlbEntry, outputPath);
                Console.WriteLine($"cache: hit {BakeCacheKey.Prefix(key)}");
                return 0;
            }
        }

        var resolver = new BakerAssemblyResolver(assemblyPaths);
        var bytes = TimelineBaker.BakeJson(jsonBytes, resolver);

        WriteOutput(outputPath, bytes);

        if (cacheDir != null)
        {
            Directory.CreateDirectory(cacheDir!);
            File.WriteAllBytes(tlbEntry!, bytes);
            Console.WriteLine($"cache: miss {BakeCacheKey.Prefix(key!)}");
        }

        return 0;
    }

    private static int Strip(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: tlb --strip <input.tlb> <output.tlb>");
            return 1;
        }

        if (!File.Exists(args[1]))
        {
            Console.Error.WriteLine($"Error: Input file '{args[1]}' does not exist.");
            return 1;
        }

        WriteOutput(args[2], TlbMetadata.Strip(File.ReadAllBytes(args[1])));
        return 0;
    }

    private static int Report(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: tlb --report <input.tlb>");
            return 1;
        }

        if (!File.Exists(args[1]))
        {
            Console.Error.WriteLine($"Error: Input file '{args[1]}' does not exist.");
            return 1;
        }

        Console.Write(TlbReport.Report(File.ReadAllBytes(args[1])));
        return 0;
    }

    private static void CopyFresh(string cachedPath, string targetPath)
    {
        var bytes = File.ReadAllBytes(cachedPath);
        if (File.Exists(targetPath) && bytes.AsSpan().SequenceEqual(File.ReadAllBytes(targetPath)))
            return;
        WriteOutput(targetPath, bytes);
    }

    private static void WriteOutput(string outputPath, byte[] bytes)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(outputPath, bytes);
    }
}
