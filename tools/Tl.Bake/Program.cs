using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tl.Gen.Tlb;

namespace Tl.Bake;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
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

    private static int Bake(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: tlbake <input.json> <output.tlb> [--assembly <path>]... [--kernel <out.g.cs>] [--cache <dir>]");
            Console.Error.WriteLine("       tlbake --strip <input.tlb> <output.tlb>");
            Console.Error.WriteLine("       tlbake --report <input.tlb>");
            return 1;
        }

        string? inputPath = null;
        string? outputPath = null;
        string? kernelPath = null;
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
            else if (args[i] == "--kernel")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Error: Missing argument for --kernel");
                    return 1;
                }
                kernelPath = args[++i];
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

        if (inputPath == null || outputPath == null)
        {
            Console.Error.WriteLine("Usage: tlbake <input.json> <output.tlb> [--assembly <path>]... [--kernel <out.g.cs>] [--cache <dir>]");
            return 1;
        }

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: Input file '{inputPath}' does not exist.");
            return 1;
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
        string? kernelEntry = null;
        if (cacheDir != null)
        {
            key = BakeCacheKey.Compute(jsonBytes, assemblyBytes, kernel: kernelPath != null, strip: false);
            tlbEntry = Path.Combine(cacheDir, Convert.ToHexString(key) + ".tlb");
            kernelEntry = kernelPath == null ? null : Path.Combine(cacheDir, Convert.ToHexString(key) + ".g.cs");
            if (File.Exists(tlbEntry) && (kernelEntry == null || File.Exists(kernelEntry)))
            {
                CopyFresh(tlbEntry, outputPath);
                if (kernelEntry != null)
                    CopyFresh(kernelEntry, kernelPath!);
                Console.WriteLine($"cache: hit {BakeCacheKey.Prefix(key)}");
                return 0;
            }
        }

        var json = File.ReadAllText(inputPath, Encoding.UTF8);
        var resolver = new BakerAssemblyResolver(assemblyPaths);
        var bytes = TimelineBaker.BakeJson(json, resolver);

        WriteOutput(outputPath, bytes);
        string? kernelSource = null;
        if (kernelPath != null)
        {
            kernelSource = KernelEmitter.Emit(bytes);
            File.WriteAllText(kernelPath, kernelSource);
        }

        if (cacheDir != null)
        {
            Directory.CreateDirectory(cacheDir!);
            File.WriteAllBytes(tlbEntry!, bytes);
            if (kernelEntry != null)
                File.WriteAllText(kernelEntry, kernelSource!);
            Console.WriteLine($"cache: miss {BakeCacheKey.Prefix(key!)}");
        }

        return 0;
    }

    private static int Strip(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: tlbake --strip <input.tlb> <output.tlb>");
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
            Console.Error.WriteLine("Usage: tlbake --report <input.tlb>");
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
