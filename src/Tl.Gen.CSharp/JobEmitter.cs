using System.Text;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

internal static class JobEmitter
{
    internal static string Normalize(string content) => content.Replace("\r\n", "\n").Replace('\r', '\n');

    internal static IReadOnlyList<CompileArtifact> Emit(JobReadResult model)
        => model.Consumers.Count == 0 && model.Bakes.Count == 0 ? [] : [new CompileArtifact("TlConsumerBinding.g.cs", Consumers(model.Consumers, model.Bakes))];

    internal static string Consumers(IReadOnlyList<JobConsumer> consumers) => Consumers(consumers, []);

    internal static string Consumers(IReadOnlyList<JobConsumer> consumers, IReadOnlyList<BakeDeclaration> bakes)
    {
        var names = new HashSet<string>();
        var items = new List<(string Name, JobConsumer Consumer)>();
        foreach (var consumer in consumers)
        {
            var name = consumer.Job.TypeName.Split('<')[0].Split('.', ':').Last();
            while (!names.Add(name)) name += "_";
            items.Add((name, consumer));
        }
        var bakeNames = new HashSet<string>();
        var bakeItems = new List<(string Name, BakeDeclaration Bake)>();
        foreach (var bake in bakes)
        {
            var name = bake.TypeName.Split('<')[0].Split('.', ':').Last();
            while (!bakeNames.Add(name)) name += "_";
            bakeItems.Add((name, bake));
        }
        var writer = new StringBuilder();
        void W(string text) => Line(writer, text);
        W("internal static unsafe class TlConsumerBinding");
        W("{");
        W("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        W("internal static void Install()");
        W("{");
        foreach (var (name, consumer) in items)
            W(consumer.Job.Slots.Count == 0
                ? $"global::Tl.PairRuntime<{consumer.TrackTypeName}, {consumer.ClipTypeName}>.ConsumeDispatch(&OnActive_{name});"
                : $"global::Tl.PairRuntime<{consumer.TrackTypeName}, {consumer.ClipTypeName}>.Consume(&OnActive_{name}, &OnActiveRange_{name}, &Bind_{name});");
        foreach (var (name, bake) in bakeItems)
            foreach (var pair in bake.Pairs)
                W($"global::Tl.BakeRuntime<{pair.TrackTypeName}, {pair.ClipTypeName}>.Bake(&Bake_{name}{StateKeys(bake)});");
        W("}");
        foreach (var (name, consumer) in items)
        {
            var job = consumer.Job;
            W($"private static void OnActive_{name}(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow)");
            W("{");
            W($"{consumer.ClipTypeName} __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<{consumer.TrackTypeName}, {consumer.ClipTypeName}>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);");
            for (var i = 0; i < job.Slots.Count; i++)
                W($"var @{job.Slots[i].Name} = ({job.Slots[i].TypeName}*)__tlColumns[{i}];");
            W($"{job.TypeName}.OnActive(in __tlTyped{Arguments(job.Slots, "[__tlRow]")});");
            W("}");
            if (job.Slots.Count == 0) continue;
            W($"private static void OnActiveRange_{name}(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRowStart, int __tlRowCount)");
            W("{");
            W($"{consumer.ClipTypeName} __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<{consumer.TrackTypeName}, {consumer.ClipTypeName}>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);");
            for (var i = 0; i < job.Slots.Count; i++)
                W($"var @{job.Slots[i].Name} = ({job.Slots[i].TypeName}*)__tlColumns[{i}];");
            W("for (var __tlRow = __tlRowStart; __tlRow < __tlRowStart + __tlRowCount; __tlRow++)");
            W($"{job.TypeName}.OnActive(in __tlTyped{Arguments(job.Slots, "[__tlRow]")});");
            W("}");
            W($"private static void Bind_{name}(ulong* __tlKeys, int __tlKeyCount, byte* __tlIndices)");
            W("{");
            if (job.Slots.Count > 4)
                W($"throw new global::System.InvalidOperationException(\"{job.TypeName}: {job.Slots.Count} gameplay parameters exceed the 4-slot consumer ABI; regenerate the binding with a matching Tl generator.\");");
            for (var i = 0; i < job.Slots.Count; i++)
            {
                var slot = job.Slots[i];
                W($"var __tlIdx{i} = FindKey(__tlKeys, __tlKeyCount, global::Tl.TypeKey<{slot.TypeName}>.Value);");
                W($"if (__tlIdx{i} < 0) throw new global::System.ArgumentException(\"{job.TypeName}: required column missing for registered consumer: {slot.TypeName}\");");
                W($"__tlIndices[{i}] = (byte)(__tlIdx{i} + 1);");
            }
            W("}");
        }
        foreach (var (name, bake) in bakeItems)
        {
            W($"private static void Bake_{name}(byte** __tlArgs)");
            W("{");
            if (bake.Parameters.Any(static parameter => parameter.IsConsumer))
                W($"{bake.ConsumerTypeName} __tlConsumer = default;");
            var slots = 0;
            foreach (var parameter in bake.Parameters)
            {
                if (parameter.IsConsumer) continue;
                W($"ref {parameter.TypeName} __tlArg{slots} = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<{parameter.TypeName}>(__tlArgs[{slots}]);");
                slots++;
            }
            W($"{bake.TypeName}.Bake({ForwardArguments(bake)});");
            W("}");
        }
        W("private static int FindKey(ulong* k, int c, ulong v) { for (var i = 0; i < c; i++) if (k[i] == v) return i; return -1; }");
        W("}");
        return writer.ToString();
    }

    private static string StateKeys(BakeDeclaration bake)
        => string.Concat(bake.Parameters.Where(static parameter => !parameter.IsConsumer)
            .Select(parameter => $", global::Tl.TypeKey<{parameter.TypeName}>.Value"));

    private static string ForwardArguments(BakeDeclaration bake)
    {
        var forward = new List<string>();
        var slots = 0;
        foreach (var parameter in bake.Parameters)
        {
            if (parameter.IsConsumer)
                forward.Add($"{Modifier(parameter.Modifier)}__tlConsumer");
            else
                forward.Add($"{Modifier(parameter.Modifier)}__tlArg{slots++}");
        }
        return string.Join(", ", forward);
    }

    private static string Modifier(BakeModifier modifier)
        => modifier == BakeModifier.In ? "in " : modifier == BakeModifier.Ref ? "ref " : "";

    private static string Arguments(IEnumerable<TimelineSlot> slots, string suffix = "")
        => string.Concat(slots.Select(slot => $", {Mode(slot)} @{slot.Name}{suffix}"));

    private static string Mode(TimelineSlot slot) => slot.Mode == SlotMode.Input ? "in" : "ref";
    private static void Line(StringBuilder writer, string text) => writer.Append(text).Append('\n');
}
