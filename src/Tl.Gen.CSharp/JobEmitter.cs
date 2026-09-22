using System.Text;
using Tl.Gen.CSharp.Model;

namespace Tl.Gen.CSharp;

internal static class JobEmitter
{
    internal static string Normalize(string content) => content.Replace("\r\n", "\n").Replace('\r', '\n');

    internal static IReadOnlyList<CompileArtifact> Emit(JobReadResult model)
        => model.Consumers.Count == 0 && model.Bakes.Count == 0 ? [] : [new CompileArtifact("TlConsumerBinding.g.cs", Consumers(model.Consumers, model.Bakes))];

    internal static string Consumers(IReadOnlyList<JobConsumer> consumers) => Consumers(consumers, []);

    private static string Consumers(IReadOnlyList<JobConsumer> consumers, IReadOnlyList<BakeDeclaration> bakes)
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
        {
            var runtime = $"global::Tl.PairRuntime<{consumer.TrackTypeName}, {consumer.ClipTypeName}>";
            var liveColumns = consumer.Job.LiveColumns;
            if (consumer.Job.MemoMethod)
                W($"{runtime}.Consume(&OnMemo_{name}, &OnMemoRange_{name}, &Keys_{name});");
            if (liveColumns.Count > 0)
                W($"{runtime}.ConsumeDispatch(&OnActive_{name}, &LiveKeys_{name}, &Diag_{name});");
            else if (consumer.Job.Dispatch)
                W($"{runtime}.ConsumeDispatch(&OnActive_{name});");
            else if (consumer.Job is { MemoMethod: false, Slots.Count: > 0 })
                W($"{runtime}.Consume(&OnActive_{name}, &OnActiveRange_{name}, &Bind_{name});");
        }
        foreach (var (name, bake) in bakeItems)
            foreach (var pair in bake.Pairs)
                W($"global::Tl.BakeRuntime<{pair.TrackTypeName}, {pair.ClipTypeName}>.Bake(&Bake_{name}{StateKeys(bake)});");
        W("}");
        foreach (var (name, consumer) in items)
        {
            var job = consumer.Job;
            var liveColumns = job.LiveColumns;
            void Thunk(string method, IReadOnlyList<TimelineSlot> slots, bool frameArg, bool ranged)
            {
                W($"private static void {method}{(ranged ? "Range" : "")}_{name}(byte* __tlSlot, byte* __tlPair, ushort __tlTick, global::Tl.FrameFlags __tlFlags, void** __tlColumns, int __tlRow{(ranged ? "Start, int __tlRowCount" : "")})");
                W("{");
                if (frameArg)
                    W($"{consumer.ClipTypeName} __tlClip = default; var __tlTyped = global::Tl.TickFrame.ToFrame<{consumer.TrackTypeName}, {consumer.ClipTypeName}>(__tlSlot, __tlPair, __tlTick, __tlFlags, ref __tlClip);");
                for (var i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    W(slot.Mode == SlotMode.MemoFeed
                        ? $"var @{slot.Name} = *({slot.TypeName}*)__tlColumns[{i}];"
                        : $"var @{slot.Name} = ({slot.TypeName}*)__tlColumns[{i}];");
                }
                var rest = Arguments(slots, "[__tlRow]");
                var call = frameArg ? "in __tlTyped" + rest : rest.Length == 0 ? "" : rest.Substring(2);
                if (ranged) W("for (var __tlRow = __tlRowStart; __tlRow < __tlRowStart + __tlRowCount; __tlRow++)");
                W($"{job.TypeName}.{method}({call});");
                W("}");
            }
            if (job.Slots.Count > 0)
            {
                var memo = job.MemoMethod ? "OnMemo" : "OnActive";
                Thunk(memo, job.Slots, true, false);
                Thunk(memo, job.Slots, true, true);
                if (job.MemoMethod)
                {
                    W($"private static int Keys_{name}(ulong* __tlKeys, byte* __tlMeta)");
                    W("{");
                    for (var i = 0; i < job.Slots.Count; i++)
                    {
                        var slot = job.Slots[i];
                        W($"if (__tlKeys != null) {{ __tlKeys[{i}] = global::Tl.TypeKey<{slot.TypeName}>.Value; __tlMeta[{i}] = {slot.Size | (slot.Mode == SlotMode.Output ? 16 : 0)}; }}");
                    }
                    W($"return {job.Slots.Count};");
                    W("}");
                }
                else
                {
                    W($"private static void Bind_{name}(ulong* __tlKeys, int __tlKeyCount, byte* __tlIndices)");
                W("{");
                if (job.Slots.Count > 4)
                    W($"throw new global::System.InvalidOperationException(\"{job.TypeName}: {job.Slots.Count} gameplay parameters exceed the 4-slot consumer ABI; regenerate the binding with a matching Tl generator.\");");
                for (var i = 0; i < job.Slots.Count; i++)
                {
                    var slot = job.Slots[i];
                    W($"var __tlIdx{i} = FindKey(__tlKeys, __tlKeyCount, global::Tl.TypeKey<{slot.TypeName}>.Value);\n"
                        + $"if (__tlIdx{i} < 0) throw new global::System.ArgumentException(\"{job.TypeName}: required column missing for registered consumer: {slot.TypeName}\");\n"
                        + $"__tlIndices[{i}] = (byte)(__tlIdx{i} + 1);");
                }
                    W("}");
                }
            }
            if (liveColumns.Count > 0)
            {
                Thunk("OnActive", liveColumns, job.LiveFrame, false);
                var prefix = $"Timeline<{Plain(consumer.TrackTypeName)}, {Plain(consumer.ClipTypeName)}> consumer '{Plain(job.TypeName)}' OnActive requires ";
                W($"private static int LiveKeys_{name}(ulong* __tlKeys, byte* __tlMeta)");
                W("{");
                for (var i = 0; i < liveColumns.Count; i++)
                {
                    var slot = liveColumns[i];
                    var bits = slot.Mode == SlotMode.MemoFeed ? 64 : slot.Mode == SlotMode.Reference ? 32 : 0;
                    W($"if (__tlKeys != null) {{ __tlKeys[{i}] = global::Tl.TypeKey<{slot.TypeName}>.Value; __tlMeta[{i}] = {slot.Size | bits}; }}");
                }
                W($"return {liveColumns.Count};");
                W("}");
                W($"private static void Diag_{name}(ulong __tlKey, long __tlSlot)");
                W("{");
                for (var i = 0; i < liveColumns.Count; i++)
                {
                    var slot = liveColumns[i];
                    W($"if (__tlSlot == {i}) throw new global::System.ArgumentException(\"{prefix}a column of type {Plain(slot.TypeName)} ({slot.Name}); none was passed.\");");
                }
                W($"throw new global::System.ArgumentException(\"{prefix}caller columns that were not passed.\");");
                W("}");
            }
            else if (job.Dispatch) Thunk("OnActive", [], job.LiveFrame, false);
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
            forward.Add(parameter.IsConsumer
                ? $"{Modifier(parameter.Modifier)}__tlConsumer"
                : $"{Modifier(parameter.Modifier)}__tlArg{slots++}");
        }
        return string.Join(", ", forward);
    }

    private static string Modifier(BakeModifier modifier)
        => modifier == BakeModifier.In ? "in " : modifier == BakeModifier.Ref ? "ref " : "";

    private static string Arguments(IEnumerable<TimelineSlot> slots, string suffix = "")
        => string.Concat(slots.Select(slot => slot.Mode == SlotMode.MemoFeed
            ? $", in @{slot.Name}"
            : $", {Mode(slot)} @{slot.Name}{suffix}"));

    private static string Plain(string typeName) => typeName.Replace("global::", string.Empty);

    private static string Mode(TimelineSlot slot) => slot.Mode == SlotMode.Input ? "in" : slot.Mode == SlotMode.Output ? "out" : "ref";
    private static void Line(StringBuilder writer, string text) => writer.Append(text).Append('\n');
}
