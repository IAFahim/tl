using System.Buffers.Binary;
using Tl.Gen.Tlb;

namespace Tl.Bake;

internal static unsafe class InspectCommand
{
    internal static int Run(string[] args, TextWriter output)
    {
        var flags = args.Where(a => a.StartsWith('-')).ToList();
        var names = args.Except(flags).ToArray();
        if (names.Length != 2 || flags.Except(["--pulses", "--markers"]).Any())
        {
            Console.Error.WriteLine("Usage: tl inspect <game.tlib> <timeline> [--pulses] [--markers]");
            return 1;
        }
        var library = File.ReadAllBytes(names[0]);
        var manifest = TlScene.ManifestOf(library, out var entries);
        if (!manifest.Timelines.TryGetValue(names[1], out var id)) throw new BakeDiagnosticException($"timeline '{names[1]}' is not in the library.");
        if (flags.Count == 0 || flags.Contains("--pulses")) Pulses(library, id, output);
        if (flags.Count == 0 || flags.Contains("--markers")) Markers(entries.First(e => e.Name == names[1]).Tlb, output);
        return 0;
    }

    static void Pulses(byte[] library, ushort id, TextWriter output)
    {
        using var world = new TimelineWorld(library);
        var kinds = world.View.Kinds;
        var timeline = kinds.IsEmpty ? null : kinds.TimelineOf(id);
        if (timeline == null) return;
        var events = kinds.Events(timeline);
        for (var tick = 0; tick < world.View.Timelines[id - 1].Duration; tick++)
            for (var e = kinds.EventStart(timeline, tick, 0); e < kinds.EventEnd(timeline, tick, 0); e++)
            {
                if (events[e].Op < (byte)KindEventOp.PulseStart) continue;
                var feeder = kinds.Feeders[events[e].Feeder];
                output.WriteLine($"{tick}\t{new[] { "start", "end", "marker" }[events[e].Op - 2]}\t{kinds.NameOf(feeder.Kind)}\t{kinds.Text(feeder.ConsumerOffset, feeder.ConsumerLength)}.{kinds.Text(feeder.MethodOffset, feeder.MethodLength)}");
            }
    }

    static void Markers(byte[] tlb, TextWriter output)
    {
        uint U(int at) => BinaryPrimitives.ReadUInt32LittleEndian(tlb.AsSpan(at));
        if (U(4) != 6) return;
        var at = (int)U(64);
        for (var i = U(68); i > 0; i--)
        {
            var size = (int)U(at + 4);
            output.WriteLine($"{U(at)}\tmarker\t0x{Convert.ToHexStringLower(tlb.AsSpan(at + 8, 8))}\t{Convert.ToHexStringLower(tlb.AsSpan(at + 16, size))}");
            at += 16 + ((size + 3) & ~3);
        }
    }
}
