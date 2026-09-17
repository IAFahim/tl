using System.Globalization;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Running;
using HandleInterning;

_ = Native.SetErrorMode(0x0001u | 0x0002u | 0x8000u);

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

if (args.Contains("--bench"))
{
    BenchmarkSwitcher.FromAssembly(typeof(BenchConfig).Assembly).Run(args.Where(a => a != "--bench").ToArray());
    return 0;
}

var stress = Stress.RunAll(args);
var filtered = args.Any(a => !a.StartsWith("--"));
var measure = filtered ? 0 : Measure.RunAll();
return stress != 0 || measure != 0 ? 1 : 0;

internal static partial class Native
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint SetErrorMode(uint uMode);
}
