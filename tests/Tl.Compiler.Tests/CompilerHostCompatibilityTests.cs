using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using Xunit;

namespace Tl.Compiler.Tests;

public sealed class CompilerHostCompatibilityTests
{
    [Fact]
    public void NetStandardCompilerLoadsAndValidatesInAnIsolatedHost()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "compiler-host", "Tl.Compiler.dll");
        using var context = new CompilerLoadContext();
        var assembly = context.LoadFromAssemblyPath(path);
        var framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        var operationType = assembly.GetType("Tl.Compiler.OperationId", true)!;
        var trackType = assembly.GetType("Tl.Compiler.TrackPlan", true)!;
        var clipType = assembly.GetType("Tl.Compiler.ClipPlan", true)!;
        var planType = assembly.GetType("Tl.Compiler.TimelinePlan", true)!;
        var operation = Activator.CreateInstance(operationType, "host")!;
        var track = Activator.CreateInstance(trackType, (ushort)0, 0u, operation)!;
        var clip = Activator.CreateInstance(clipType, (ushort)0, 1u, 0u, 1u)!;
        var tracks = Array.CreateInstance(trackType, 1);
        var clips = Array.CreateInstance(clipType, 1);
        tracks.SetValue(track, 0);
        clips.SetValue(clip, 0);
        var plan = Activator.CreateInstance(planType, "host", (ushort)1, false, tracks, clips, (ushort)1)!;
        var validated = planType.GetMethod("Validate")!.Invoke(plan, null)!;
        var orderedType = assembly.GetType("Tl.Compiler.OrderedTimelinePlan", true)!;
        var operationPlanType = assembly.GetType("Tl.Compiler.OrderedOperationPlan", true)!;
        var payloadPlanType = assembly.GetType("Tl.Compiler.PayloadPlan", true)!;
        var orderedTrackType = assembly.GetType("Tl.Compiler.OrderedTrackPlan", true)!;
        var orderedClipType = assembly.GetType("Tl.Compiler.OrderedClipPlan", true)!;
        var hookType = assembly.GetType("Tl.Compiler.OrderedHookPlan", true)!;
        var ordered = Activator.CreateInstance(
            orderedType,
            "host-ordered",
            false,
            Array.CreateInstance(operationPlanType, 0),
            Array.CreateInstance(payloadPlanType, 0),
            Array.CreateInstance(orderedTrackType, 0),
            Array.CreateInstance(orderedClipType, 0),
            Array.CreateInstance(hookType, 0),
            (ushort)1)!;
        var orderedValidated = orderedType.GetMethod("Validate")!.Invoke(ordered, null)!;

        Assert.Equal(".NETStandard,Version=v2.0", framework?.FrameworkName);
        Assert.Equal(1u, validated.GetType().GetProperty("Duration")!.GetValue(validated));
        Assert.Equal(0u, orderedValidated.GetType().GetProperty("Duration")!.GetValue(orderedValidated));
    }

    private sealed class CompilerLoadContext : AssemblyLoadContext, IDisposable
    {
        internal CompilerLoadContext() : base(true) { }

        protected override Assembly? Load(AssemblyName assemblyName)
            => assemblyName.Name == "System.Collections.Immutable"
                ? Default.Assemblies.Single(assembly => assembly.GetName().Name == assemblyName.Name)
                : null;

        public void Dispose() => Unload();
    }
}
