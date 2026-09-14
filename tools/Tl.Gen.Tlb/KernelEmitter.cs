using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Tl.Gen.Tlb;

public static class KernelEmitter
{
    public static string Emit(byte[] baked)
    {
        ValidateHeader(baked);

        var stripped = TlbMetadata.Strip(baked);
        var hash = SHA256.HashData(stripped);

        var loops = BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(8)) != 0;
        var duration = BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(12));
        var stageCount = BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(20));
        var stageOffset = BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(32));

        var stages = new List<(uint End, List<(uint Slot, int Pair)> Steps)>((int)stageCount);
        for (var index = 0; index < stageCount; index++)
        {
            var at = (int)stageOffset + 16 * index;
            var end = BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(at + 4));
            var programOffset = BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(at + 8));
            var programCount = BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(at + 12));
            var steps = new List<(uint Slot, int Pair)>((int)programCount);
            for (var step = 0; step < programCount; step++)
            {
                var item = (int)programOffset + 8 * step;
                steps.Add((
                    BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(item)),
                    checked((int)BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(item + 4)))));
            }

            stages.Add((end, steps));
        }

        var source = new StringBuilder(4096);
        source.AppendLine("using System.Runtime.CompilerServices;");
        source.AppendLine("using Tl;");
        source.AppendLine();
        source.AppendLine($"internal static unsafe class TimelineKernel_{Convert.ToHexString(hash).ToLowerInvariant()}");
        source.AppendLine("{");
        source.AppendLine("    [ModuleInitializer]");
        source.AppendLine("    internal static void Install() =>");
        source.Append("        TimelineKernels.Register(");
        for (var word = 0; word < 4; word++)
        {
            source.Append("0x");
            source.Append(BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(8 * word)).ToString("X16", CultureInfo.InvariantCulture));
            source.Append(word == 3 ? ", &Tick);" : ", ");
        }

        source.AppendLine();
        source.AppendLine();
        source.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        source.AppendLine("    static unsafe bool Tick(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)");
        source.AppendLine("    {");
        source.AppendLine("        if (delta == 0 || rowCount == 0) return true;");
        source.AppendLine("        if (rowCount == 1)");
        source.AppendLine("        {");
        EmitSingleRow(source, duration, loops);
        source.AppendLine("        }");
        if (stages.Count == 0)
        {
            source.AppendLine("        var probe = Probe(asset, rows, rowCount, out _, out _);");
            source.AppendLine("        if (probe == 0) return false;");
            source.AppendLine("        return TickScalar(asset, heads, columns, rows, rowCount, gameTick, delta);");
        }
        else
        {
            source.AppendLine("        var probe = Probe(asset, rows, rowCount, out var uniformPosition, out var uniformCycle);");
            source.AppendLine("        if (probe == 0) return false;");
            source.AppendLine("        if (probe == 1) return TickUniform(asset, heads, columns, rows, rowCount, gameTick, delta, uniformPosition, uniformCycle);");
            source.AppendLine("        return TickMixed(asset, heads, columns, rows, rowCount, gameTick, delta);");
        }
        source.AppendLine("    }");
        source.AppendLine();
        EmitProbe(source);
        source.AppendLine();
        var runPairs = stages.Count == 0 ? new List<int>() : SingleStepPairs(stages);
        if (stages.Count == 0)
            EmitMultiRowScalar(source, duration, loops);
        else
        {
            EmitUniformLockstep(source, duration, loops, runPairs);
            source.AppendLine();
            EmitMixed(source, duration, loops, runPairs);
        }
        source.AppendLine();
        source.AppendLine("    static unsafe void Execute(bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns)");
        source.AppendLine("    {");
        var chunks = new List<string>();
        EmitStages(source, stages, chunks);
        source.AppendLine("    }");
        if (stages.Count > 0)
        {
            source.AppendLine();
            source.AppendLine($"    static unsafe void Run({RunParams(runPairs)}bool reverse, uint tick, uint gameTick, long cycle, FrameFlags flags, int rowStart, int rowCount, byte* asset, int* heads, void** columns)");
            source.AppendLine("    {");
            EmitRunStages(source, stages);
            source.AppendLine("    }");
        }
        foreach (var chunk in chunks)
        {
            source.AppendLine();
            source.Append(chunk);
        }
        source.Append('}');
        return source.ToString();
    }

    static void ValidateHeader(byte[] baked)
    {
        if (baked.Length < 48)
            throw new ArgumentException("Kernel emission requires validated TLB1 bytes.");
        if (BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(0)) != 0x31424C54u ||
            BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(4)) != 1u ||
            BinaryPrimitives.ReadUInt32LittleEndian(baked.AsSpan(44)) != (uint)baked.Length)
            throw new ArgumentException("Kernel emission requires validated TLB1 bytes.");
    }

    static void EmitProbe(StringBuilder source)
    {
        source.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        source.AppendLine("    static unsafe int Probe(byte* asset, TimelineComponent* rows, int rowCount, out uint uniformPosition, out long uniformCycle)");
        source.AppendLine("    {");
        source.AppendLine("        var assetAddress = (nint)asset;");
        source.AppendLine("        uniformPosition = 0; uniformCycle = 0;");
        source.AppendLine("        if (*(nint*)rows != assetAddress) return 0;");
        source.AppendLine("        uniformPosition = rows->Position;");
        source.AppendLine("        uniformCycle = rows->Cycle;");
        source.AppendLine("        var uniformPositionWord = *(long*)&rows->Position & 4294967295L;");
        source.AppendLine("        var uniform = true;");
        source.AppendLine("        var scan = 1;");
        source.AppendLine("        while (scan < rowCount)");
        source.AppendLine("        {");
        source.AppendLine("            var component = rows + scan;");
        source.AppendLine("            if (*(nint*)component != assetAddress) return 0;");
        source.AppendLine("            if (uniform && (*(long*)&component->Position & 4294967295L) != uniformPositionWord | component->Cycle != uniformCycle) uniform = false;");
        source.AppendLine("            scan++;");
        source.AppendLine("            if (scan == rowCount) break;");
        source.AppendLine("            component = rows + scan;");
        source.AppendLine("            if (*(nint*)component != assetAddress) return 0;");
        source.AppendLine("            if (uniform && (*(long*)&component->Position & 4294967295L) != uniformPositionWord | component->Cycle != uniformCycle) uniform = false;");
        source.AppendLine("            scan++;");
        source.AppendLine("        }");
        source.AppendLine("        return uniform ? 1 : 2;");
        source.AppendLine("    }");
    }

    static void EmitSingleRow(StringBuilder source, uint duration, bool loops)
    {
        source.AppendLine("            if (*(nint*)rows != (nint)asset) return false;");
        source.AppendLine("            var row = rows;");
        source.AppendLine("            var state = new TimelineState(1, row->Position, row->Cycle);");
        source.AppendLine("            if (delta == 1 || delta == -1)");
        source.AppendLine("            {");
        source.AppendLine("                var reverse = delta < 0;");
        source.AppendLine($"                if (TimelineMovement.Select(state, {N(duration)}u, {Word(loops)}, reverse, out var next, out var tick, out var cycle, out var flags))");
        source.AppendLine("                {");
        source.AppendLine("                    Execute(reverse, tick, reverse ? gameTick - 1u : gameTick, cycle, flags, 0, asset, heads, columns);");
        source.AppendLine("                    row->Position = next.Position;");
        source.AppendLine("                    row->Cycle = next.Cycle;");
        source.AppendLine("                }");
        source.AppendLine("                return true;");
        source.AppendLine("            }");
        source.AppendLine("            var isReverse = delta < 0;");
        source.AppendLine("            var remaining = isReverse ? -(long)delta : delta;");
        source.AppendLine("            while (remaining-- != 0)");
        source.AppendLine("            {");
        source.AppendLine("                if (isReverse) gameTick--;");
        source.AppendLine($"                if (!TimelineMovement.Select(state, {N(duration)}u, {Word(loops)}, isReverse, out var multiNext, out var multiTick, out var multiCycle, out var multiFlags)) break;");
        source.AppendLine("                Execute(isReverse, multiTick, gameTick, multiCycle, multiFlags, 0, asset, heads, columns);");
        source.AppendLine("                state = new TimelineState(1, multiNext.Position, multiNext.Cycle);");
        source.AppendLine("                row->Position = multiNext.Position;");
        source.AppendLine("                row->Cycle = multiNext.Cycle;");
        source.AppendLine("                if (!isReverse) gameTick++;");
        source.AppendLine("            }");
        source.AppendLine("            return true;");
    }

    static List<int> SingleStepPairs(List<(uint End, List<(uint Slot, int Pair)> Steps)> stages)
    {
        var pairs = new List<int>();
        foreach (var stage in stages)
        {
            if (stage.Steps.Count != 1) continue;
            var pair = stage.Steps[0].Pair;
            if (!pairs.Contains(pair)) pairs.Add(pair);
        }
        return pairs;
    }

    static string RunParams(List<int> pairs)
    {
        var prefix = new StringBuilder();
        foreach (var pair in pairs)
            prefix.Append($"TimelineKernelRange r{pair}, ");
        return prefix.ToString();
    }

    static string RunArgs(List<int> pairs)
    {
        var prefix = new StringBuilder();
        foreach (var pair in pairs)
            prefix.Append($"r{pair}, ");
        return prefix.ToString();
    }

    static void EmitResolutions(StringBuilder source, List<int> pairs, int indent)
    {
        foreach (var pair in pairs)
            Pad(source, indent).AppendLine($"var r{pair} = TimelineKernels.Range(heads[{N(pair)}]);");
    }

    static void EmitMixed(StringBuilder source, uint duration, bool loops, List<int> runPairs)
    {
        source.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        source.AppendLine("    static unsafe bool TickMixed(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)");
        source.AppendLine("    {");
        EmitResolutions(source, runPairs, 2);
        source.AppendLine("        var multiReverse = delta < 0;");
        source.AppendLine("        var multiRemaining = multiReverse ? -(long)delta : delta;");
        source.AppendLine("        var moved = true;");
        source.AppendLine("        while (moved && multiRemaining-- != 0)");
        source.AppendLine("        {");
        source.AppendLine("            if (multiReverse) gameTick--;");
        source.AppendLine("            moved = false;");
        source.AppendLine("            var memoReady = false;");
        source.AppendLine("            var memoPosition = 0u;");
        source.AppendLine("            var memoCycle = 0L;");
        source.AppendLine("            var memoSelected = false;");
        source.AppendLine("            var memoTick = 0u;");
        source.AppendLine("            var memoOutCycle = 0L;");
        source.AppendLine("            var memoFlags = FrameFlags.None;");
        source.AppendLine("            var memoNextPosition = 0u;");
        source.AppendLine("            var memoNextCycle = 0L;");
        source.AppendLine("            var runOpen = false;");
        source.AppendLine("            var runStart = 0;");
        source.AppendLine("            for (var row = 0; row < rowCount; row++)");
        source.AppendLine("            {");
        source.AppendLine("                var component = rows + row;");
        source.AppendLine("                if (memoReady && component->Position == memoPosition && component->Cycle == memoCycle)");
        source.AppendLine("                {");
        source.AppendLine("                    if (memoSelected)");
        source.AppendLine("                    {");
        source.AppendLine("                        component->Position = memoNextPosition;");
        source.AppendLine("                        component->Cycle = memoNextCycle;");
        source.AppendLine("                    }");
        source.AppendLine("                    continue;");
        source.AppendLine("                }");
        source.AppendLine("                if (runOpen)");
        source.AppendLine("                {");
        source.AppendLine($"                    Run({RunArgs(runPairs)}multiReverse, memoTick, gameTick, memoOutCycle, memoFlags, runStart, row - runStart, asset, heads, columns);");
        source.AppendLine("                    runOpen = false;");
        source.AppendLine("                }");
        source.AppendLine("                memoReady = true;");
        source.AppendLine("                memoPosition = component->Position;");
        source.AppendLine("                memoCycle = component->Cycle;");
        source.AppendLine($"                memoSelected = TimelineMovement.Select(new TimelineState(1, memoPosition, memoCycle), {N(duration)}u, {Word(loops)}, multiReverse, out var memoNext, out memoTick, out memoOutCycle, out memoFlags);");
        source.AppendLine("                memoNextPosition = memoNext.Position;");
        source.AppendLine("                memoNextCycle = memoNext.Cycle;");
        source.AppendLine("                if (!memoSelected) continue;");
        source.AppendLine("                moved = true;");
        source.AppendLine("                component->Position = memoNextPosition;");
        source.AppendLine("                component->Cycle = memoNextCycle;");
        source.AppendLine("                runStart = row;");
        source.AppendLine("                runOpen = true;");
        source.AppendLine("            }");
        source.AppendLine($"            if (runOpen) Run({RunArgs(runPairs)}multiReverse, memoTick, gameTick, memoOutCycle, memoFlags, runStart, rowCount - runStart, asset, heads, columns);");
        source.AppendLine("            if (!multiReverse) gameTick++;");
        source.AppendLine("        }");
        source.AppendLine("        return true;");
        source.AppendLine("    }");
    }

    static void EmitUniformLockstep(StringBuilder source, uint duration, bool loops, List<int> runPairs)
    {
        source.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        source.AppendLine("    static unsafe bool TickUniform(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta, uint uniformPosition, long uniformCycle)");
        source.AppendLine("    {");
        EmitResolutions(source, runPairs, 2);
        source.AppendLine("        var multiReverse = delta < 0;");
        source.AppendLine("        var multiRemaining = multiReverse ? -(long)delta : delta;");
        source.AppendLine("        var moved = true;");
        source.AppendLine("        while (moved && multiRemaining-- != 0)");
        source.AppendLine("        {");
        source.AppendLine("            if (multiReverse) gameTick--;");
        source.AppendLine("            moved = false;");
        source.AppendLine($"            if (TimelineMovement.Select(new TimelineState(1, uniformPosition, uniformCycle), {N(duration)}u, {Word(loops)}, multiReverse, out var uniformNext, out var uniformTick, out var uniformOutCycle, out var uniformFlags))");
        source.AppendLine("                {");
        source.AppendLine("                    moved = true;");
        source.AppendLine($"                    Run({RunArgs(runPairs)}multiReverse, uniformTick, gameTick, uniformOutCycle, uniformFlags, 0, rowCount, asset, heads, columns);");
        source.AppendLine("                    var commit = 0;");
        source.AppendLine("                    while (commit < rowCount)");
        source.AppendLine("                    {");
        source.AppendLine("                        var component = rows + commit;");
        source.AppendLine("                        component->Position = uniformNext.Position;");
        source.AppendLine("                        component->Cycle = uniformNext.Cycle;");
        source.AppendLine("                        commit++;");
        source.AppendLine("                        if (commit == rowCount) break;");
        source.AppendLine("                        component = rows + commit;");
        source.AppendLine("                        component->Position = uniformNext.Position;");
        source.AppendLine("                        component->Cycle = uniformNext.Cycle;");
        source.AppendLine("                        commit++;");
        source.AppendLine("                    }");
        source.AppendLine("                    uniformPosition = uniformNext.Position;");
        source.AppendLine("                    uniformCycle = uniformNext.Cycle;");
        source.AppendLine("                }");
        source.AppendLine("                if (!moved) break;");
        source.AppendLine("                if (!multiReverse) gameTick++;");
        source.AppendLine("            }");
        source.AppendLine("            return true;");
        source.AppendLine("    }");
    }

    static void EmitMultiRowScalar(StringBuilder source, uint duration, bool loops)
    {
        source.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveOptimization)]");
        source.AppendLine("    static unsafe bool TickScalar(byte* asset, int* heads, void** columns, TimelineComponent* rows, int rowCount, uint gameTick, int delta)");
        source.AppendLine("    {");
        source.AppendLine("        var multiReverse = delta < 0;");
        source.AppendLine("        var multiRemaining = multiReverse ? -(long)delta : delta;");
        source.AppendLine("        var moved = true;");
        source.AppendLine("        while (moved && multiRemaining-- != 0)");
        source.AppendLine("        {");
        source.AppendLine("            if (multiReverse) gameTick--;");
        source.AppendLine("            moved = false;");
        source.AppendLine("            for (var row = 0; row < rowCount; row++)");
        source.AppendLine("            {");
        source.AppendLine("                var component = rows + row;");
        source.AppendLine($"                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), {N(duration)}u, {Word(loops)}, multiReverse, out _, out var tick, out var cycle, out var flags))");
        source.AppendLine("                {");
        source.AppendLine("                    moved = true;");
        source.AppendLine("                    Execute(multiReverse, tick, gameTick, cycle, flags, row, asset, heads, columns);");
        source.AppendLine("                }");
        source.AppendLine("            }");
        source.AppendLine("            if (!moved) break;");
        source.AppendLine("            for (var row = 0; row < rowCount; row++)");
        source.AppendLine("            {");
        source.AppendLine("                var component = rows + row;");
        source.AppendLine($"                if (TimelineMovement.Select(new TimelineState(1, component->Position, component->Cycle), {N(duration)}u, {Word(loops)}, multiReverse, out var next, out _, out _, out _))");
        source.AppendLine("                {");
        source.AppendLine("                    component->Position = next.Position;");
        source.AppendLine("                    component->Cycle = next.Cycle;");
        source.AppendLine("                }");
        source.AppendLine("            }");
        source.AppendLine("            if (!multiReverse) gameTick++;");
        source.AppendLine("        }");
        source.AppendLine("        return true;");
        source.AppendLine("    }");
    }

    static void EmitStages(StringBuilder source, List<(uint End, List<(uint Slot, int Pair)> Steps)> stages, List<string> chunks)
    {
        var total = stages.Sum(stage => stage.Steps.Count);
        if (stages.Count == 0 || total == 0)
            return;
        source.AppendLine("        int* scratch = stackalloc int[64];");
        if (stages.Count == 1)
        {
            EmitStageBody(source, stages[0].Steps, 2, 0, chunks);
            return;
        }

        for (var index = 0; index < stages.Count; index++)
        {
            source.AppendLine(index == 0
                ? $"        if (tick < {N(stages[index].End)}u)"
                : $"        else if (tick < {N(stages[index].End)}u)");
            source.AppendLine("        {");
            EmitStageBody(source, stages[index].Steps, 3, index, chunks);
            source.AppendLine("        }");
        }
    }

    static void EmitRunStages(StringBuilder source, List<(uint End, List<(uint Slot, int Pair)> Steps)> stages)
    {
        if (stages.Any(stage => stage.Steps.Count == 1))
            source.AppendLine("        int* scratch = stackalloc int[64];");
        if (stages.Count == 1)
        {
            EmitRunStageBody(source, stages[0].Steps, 2);
            return;
        }

        for (var index = 0; index < stages.Count; index++)
        {
            source.AppendLine(index == 0
                ? $"        if (tick < {N(stages[index].End)}u)"
                : $"        else if (tick < {N(stages[index].End)}u)");
            source.AppendLine("        {");
            EmitRunStageBody(source, stages[index].Steps, 3);
            source.AppendLine("        }");
        }
    }

    static void EmitRunStageBody(StringBuilder source, List<(uint Slot, int Pair)> steps, int indent)
    {
        if (steps.Count == 1)
        {
            var pair = steps[0].Pair;
            var slot = $"asset + {N(steps[0].Slot)}u";
            Pad(source, indent).AppendLine($"if (r{pair}.Pointer != null) r{pair}.Pointer({slot}, gameTick, tick, cycle, flags, columns, rowStart, rowCount);");
            Pad(source, indent).AppendLine($"else TimelineKernels.ChainRange(heads[{N(pair)}], reverse, scratch, {slot}, gameTick, tick, cycle, flags, columns, rowStart, rowCount);");
            return;
        }
        if (steps.Count > 1)
        {
            Pad(source, indent).AppendLine("for (var row = rowStart; row < rowStart + rowCount; row++)");
            Pad(source, indent + 1).AppendLine("Execute(reverse, tick, gameTick, cycle, flags, row, asset, heads, columns);");
        }
    }

    static void EmitStageBody(StringBuilder source, List<(uint Slot, int Pair)> steps, int indent, int stage, List<string> chunks)
    {
        if (steps.Count == 0)
            return;
        if (steps.Count <= 16)
        {
            EmitSteps(source, steps, indent);
            return;
        }

        var chunkCount = (steps.Count + 15) / 16;
        Pad(source, indent).AppendLine("if (!reverse)");
        Pad(source, indent).AppendLine("{");
        for (var index = 0; index < chunkCount; index++)
            Pad(source, indent + 1).AppendLine($"F{stage}_{index}(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);");
        Pad(source, indent).AppendLine("}");
        Pad(source, indent).AppendLine("else");
        Pad(source, indent).AppendLine("{");
        for (var index = chunkCount - 1; index >= 0; index--)
            Pad(source, indent + 1).AppendLine($"R{stage}_{index}(tick, gameTick, cycle, flags, row, asset, heads, columns, scratch);");
        Pad(source, indent).AppendLine("}");

        for (var index = 0; index < chunkCount; index++)
        {
            var from = index * 16;
            var slice = steps.Skip(from).Take(16).ToList();
            var forward = new StringBuilder();
            forward.AppendLine($"    static unsafe void F{stage}_{index}(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)");
            forward.AppendLine("    {");
            foreach (var step in slice)
                DirectedStep(forward, step, 3, false);
            forward.Append("    }");
            chunks.Add(forward.ToString());
            var reverse = new StringBuilder();
            reverse.AppendLine($"    static unsafe void R{stage}_{index}(uint tick, uint gameTick, long cycle, FrameFlags flags, int row, byte* asset, int* heads, void** columns, int* scratch)");
            reverse.AppendLine("    {");
            for (var position = slice.Count - 1; position >= 0; position--)
                DirectedStep(reverse, slice[position], 3, true);
            reverse.Append("    }");
            chunks.Add(reverse.ToString());
        }
    }

    static void DirectedStep(StringBuilder source, (uint Slot, int Pair) step, int indent, bool reverse)
    {
        Pad(source, indent).AppendLine($"TimelineKernels.Chain(heads[{N(step.Pair)}], {Word(reverse)}, scratch, asset + {N(step.Slot)}u, gameTick, tick, cycle, flags, columns, row);");
    }

    static void EmitSteps(StringBuilder source, List<(uint Slot, int Pair)> steps, int indent)
    {
        if (steps.Count == 0)
            return;
        if (steps.Count == 1)
        {
            Step(source, steps[0], indent);
            return;
        }

        Pad(source, indent).AppendLine("if (!reverse)");
        Pad(source, indent).AppendLine("{");
        foreach (var step in steps)
            Step(source, step, indent + 1);
        Pad(source, indent).AppendLine("}");
        Pad(source, indent).AppendLine("else");
        Pad(source, indent).AppendLine("{");
        for (var index = steps.Count - 1; index >= 0; index--)
            Step(source, steps[index], indent + 1);
        Pad(source, indent).AppendLine("}");
    }

    static void Step(StringBuilder source, (uint Slot, int Pair) step, int indent) =>
        Pad(source, indent).AppendLine($"TimelineKernels.Chain(heads[{N(step.Pair)}], reverse, scratch, asset + {N(step.Slot)}u, gameTick, tick, cycle, flags, columns, row);");

    static StringBuilder Pad(StringBuilder source, int indent)
    {
        for (var index = 0; index < indent; index++)
            source.Append("    ");
        return source;
    }

    static string N(long value) => value.ToString(CultureInfo.InvariantCulture);

    static string Word(bool value) => value ? "true" : "false";
}
