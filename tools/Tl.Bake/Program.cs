using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tl.Bake;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: tlbake <input.json> <output.tlb> [--assembly <path>]...");
                return 1;
            }

            string? inputPath = null;
            string? outputPath = null;
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
                Console.Error.WriteLine("Usage: tlbake <input.json> <output.tlb> [--assembly <path>]...");
                return 1;
            }

            if (!File.Exists(inputPath))
            {
                Console.Error.WriteLine($"Error: Input file '{inputPath}' does not exist.");
                return 1;
            }

            var json = File.ReadAllText(inputPath, Encoding.UTF8);
            var resolver = new BakerAssemblyResolver(assemblyPaths);
            var bytes = TimelineBaker.BakeJson(json, resolver);

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(outputPath, bytes);
            return 0;
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
}
