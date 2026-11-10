using System.Text;
using Tl.Gen.CSharp.Analysis;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

internal static class JobEmitter
{
    internal static string Normalize(string content) => content.Replace("\r\n", "\n").Replace('\r', '\n');

    internal static IReadOnlyList<CompileArtifact> Emit(JobReadResult model, string id = "")
        => model.Consumers.Count == 0 ? [] : [new CompileArtifact($"TlConsumerBinding{id}.g.cs", Consumers(model.Consumers, id))];

    internal static string Consumers(IReadOnlyList<JobConsumer> consumers) => Consumers(consumers, "");

    internal static string Consumers(IReadOnlyList<JobConsumer> consumers, string id)
    {
        var layouts = new List<(string Track, string Clip, ulong Layout)>();
        var layoutPairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var consumer in consumers)
            if (consumer.Layout != 0 && layoutPairs.Add(consumer.TrackTypeName + "\0" + consumer.ClipTypeName))
                layouts.Add((consumer.TrackTypeName, consumer.ClipTypeName, consumer.Layout));
        var layoutTypes = new List<string>();
        var rootedTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var consumer in consumers)
        {
            if (consumer.Layout == 0 || consumer.LayoutTypes is not { Count: > 0 }) continue;
            foreach (var type in consumer.LayoutTypes)
                if (rootedTypes.Add(type))
                    layoutTypes.Add(type);
        }
        var writer = new StringBuilder();
        void W(string text) => Line(writer, text);
        var attribute = $"TlConsumerLayout{id}Attribute";
        foreach (var (track, clip, layout) in layouts)
            W($"[assembly: global::{attribute}(typeof({track}), typeof({clip}), {layout}UL)]");
        if (layouts.Count > 0)
        {
            W("[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]");
            W($"internal sealed class {attribute} : global::System.Attribute");
            W("{");
            W($"internal {attribute}(global::System.Type track, global::System.Type clip, ulong layout) {{ }}");
            W("}");
        }
        W($"internal static unsafe class TlConsumerBinding{id}");
        W("{");
        var fields = "global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields";
        foreach (var (track, clip, layout) in layouts)
        {
            W($"[global::System.Diagnostics.CodeAnalysis.DynamicDependency({fields}, typeof({track}))]");
            W($"[global::System.Diagnostics.CodeAnalysis.DynamicDependency({fields}, typeof({clip}))]");
        }
        foreach (var type in layoutTypes)
            W($"[global::System.Diagnostics.CodeAnalysis.DynamicDependency({fields}, typeof({type}))]");
        W("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        W("internal static void Install()");
        W("{");
        W("}");
        W("}");
        return writer.ToString();
    }




    private static void Line(StringBuilder writer, string text) => writer.Append(text).Append('\n');
}
