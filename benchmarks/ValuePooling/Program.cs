using System.Globalization;
using BenchmarkDotNet.Running;
using Tl.ValuePooling;

if (args is ["--parity"])
{
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    Verification.Run();
    return 0;
}

if (args is ["--run", var label, .. var rest])
{
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
    var artifacts = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "results", label, "bdn"));
    Directory.CreateDirectory(artifacts);
    RunSettings.ArtifactsPath = artifacts;
    BenchmarkRunner.Run(new[]
    {
        typeof(QueryScanSmall),
        typeof(QueryScanLarge),
        typeof(QueryScanWideSmall),
        typeof(QueryScanWideLarge),
        typeof(IndirectScanSmall),
        typeof(IndirectScanLarge),
        typeof(IndirectScanWideSmall),
        typeof(IndirectScanWideLarge),
    }, new ProtoConfig(), rest.ToArray());
    return 0;
}

Console.WriteLine("usage: --parity | --run <label> [--filter <pattern>]");
return 2;
