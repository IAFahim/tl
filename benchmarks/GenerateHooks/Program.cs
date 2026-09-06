using Tl.Hooks;
using static Waffle.WaffleSyntax;

if (args.Length != 1)
    throw new ArgumentException("Supply the output file.");

var hooks = typeof(Receiver).GetInterfaces();
if (!hooks.Contains(typeof(IClip)) || !hooks.Contains(typeof(ITimelineForward)))
    throw new InvalidOperationException("Missing required hook contract.");

var body = Render($$"""
    using System.Runtime.CompilerServices;

    namespace Tl.Hooks;

    public static class ReceiverLink
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forward(ref Receiver receiver, in Frame frame)
            => receiver.OnForward(in frame);
    }
    """);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
File.WriteAllText(args[0], body);
Console.WriteLine($"Discovered {typeof(Receiver).FullName}: {string.Join(", ", hooks.Select(x => x.Name))}");
Console.WriteLine($"Generated {args[0]}; unsupported hooks omitted.");
