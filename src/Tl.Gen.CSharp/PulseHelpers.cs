using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

static class PulseHelpers
{
    internal static string Emit(IEnumerable<SystemAssignment> assignments) => string.Concat(assignments.SelectMany(static a => a.Kinds)
        .Where(static k => !k.Foreign && k.Accepts != 0 && k.Derived == DerivedKind.None && k.Id.IndexOf('+') < 0).GroupBy(static k => k.Id).Select(static g => g.First()).OrderBy(static k => k.Id, StringComparer.Ordinal)
        .Select(static k => $"namespace {(k.Id.LastIndexOf('.') is > 0 and var dot ? k.Id.Substring(0, dot) : "TlUnityGenerated")}\n{{\npublic static class {k.Name}Kind\n{{\npublic static global::Tl.KindPulses<{k.ValueTypeName}> Pulses(global::Tl.KindStore kinds) => kinds.Pulses<{k.ValueTypeName}>(\"{k.Id}\");\n}}\n}}\n"));
}
