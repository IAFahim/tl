using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Tl.Gen.Tlb;

internal readonly record struct BatchOutcome(byte[]? Bytes, Exception? Error, double Ms);

public static class TimelineBaker
{
    public static byte[] BakeJson(string json, BakerAssemblyResolver? resolver = null)
    {
        resolver ??= new BakerAssemblyResolver();
        return TimelineBakerFast.BakeJsonUtf8(Encoding.UTF8.GetBytes(json), resolver);
    }

    public static byte[] BakeJson(byte[] utf8Json, BakerAssemblyResolver? resolver = null)
    {
        if (!System.Text.Unicode.Utf8.IsValid(utf8Json))
            throw new BakeDiagnosticException($"invalid UTF-8 in authoring JSON at byte {FirstInvalidUtf8Offset(utf8Json)}: the bake input must be valid UTF-8; fix the input file encoding.");
        resolver ??= new BakerAssemblyResolver();
        return TimelineBakerFast.BakeJsonUtf8(utf8Json, resolver);
    }

    private static byte[] BakeValidated(byte[] utf8Json, BakerAssemblyResolver resolver)
    {
        if (!System.Text.Unicode.Utf8.IsValid(utf8Json))
            throw new BakeDiagnosticException($"invalid UTF-8 in authoring JSON at byte {FirstInvalidUtf8Offset(utf8Json)}: the bake input must be valid UTF-8; fix the input file encoding.");
        return TimelineBakerFast.BakeJsonUtf8(utf8Json, resolver, BakeWorkspace.Shared);
    }

    public static byte[][] BakeJsonBatch(IReadOnlyList<byte[]> utf8Jsons, BakerAssemblyResolver? resolver = null)
    {
        resolver ??= new BakerAssemblyResolver();
        var outcomes = BakeBatch(utf8Jsons, resolver);
        for (var i = 0; i < outcomes.Length; i++)
            if (outcomes[i].Error != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(outcomes[i].Error!).Throw();
        var outputs = new byte[outcomes.Length][];
        for (var i = 0; i < outcomes.Length; i++)
            outputs[i] = outcomes[i].Bytes!;
        return outputs;
    }

    private static BatchOutcome BakeOne(byte[] utf8Json, BakerAssemblyResolver resolver)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            var bytes = BakeValidated(utf8Json, resolver);
            return new BatchOutcome(bytes, null, System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        catch (Exception error)
        {
            return new BatchOutcome(null, error, System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }

    internal static BatchOutcome[] BakeBatch(IReadOnlyList<byte[]> utf8Jsons, BakerAssemblyResolver resolver)
    {
        var outcomes = new BatchOutcome[utf8Jsons.Count];
        if (utf8Jsons.Count == 0)
            return outcomes;
        var workers = Math.Min(utf8Jsons.Count, Environment.ProcessorCount);
        if (workers <= 1)
        {
            for (var i = 0; i < utf8Jsons.Count; i++)
                outcomes[i] = BakeOne(utf8Jsons[i], resolver);
            return outcomes;
        }
        using var done = new CountdownEvent(workers);
        for (var w = 0; w < workers; w++)
        {
            var worker = w;
            new Thread(() =>
            {
                for (var i = worker; i < utf8Jsons.Count; i += workers)
                    outcomes[i] = BakeOne(utf8Jsons[i], resolver);
                done.Signal();
            }).Start();
        }
        done.Wait();
        return outcomes;
    }

    private static int FirstInvalidUtf8Offset(byte[] utf8Json)
    {
        var span = utf8Json.AsSpan();
        var offset = 0;
        while (offset < span.Length)
        {
            var status = System.Text.Rune.DecodeFromUtf8(span[offset..], out _, out var consumed);
            if (status != System.Buffers.OperationStatus.Done)
                return offset;
            offset += consumed;
        }
        return span.Length;
    }
}
