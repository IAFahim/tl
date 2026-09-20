using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("browser")]

Console.Write(Play.SmokeRun.Launch());
Console.Write(Play.LiveAuthoring.Receipt());
Console.Write(Play.Examples.Validate());
return 0;
