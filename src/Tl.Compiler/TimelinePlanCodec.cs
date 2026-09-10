using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Tl.Compiler;

public static class TimelinePlanCodec
{
    private static readonly byte[] Magic = "TLPL"u8.ToArray();
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static ImmutableArray<byte> Encode(ValidatedTimelinePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var writer = new ArrayBufferWriter<byte>();
        Write(writer, Magic);
        Write(writer, plan.FormatVersion);
        Write(writer, plan.RuntimeId);
        Write(writer, plan.Loops ? 1u : 0u);
        Write(writer, plan.Identity);
        var operations = plan.Tracks
            .Select(static track => track.Operation.Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Write(writer, (uint)operations.Length);
        foreach (var operation in operations)
            Write(writer, operation);
        var operationIndices = operations
            .Select(static (operation, index) => (operation, index))
            .ToDictionary(static item => item.operation, static item => (uint)item.index, StringComparer.Ordinal);
        Write(writer, (uint)plan.Tracks.Length);
        foreach (var track in plan.Tracks)
        {
            Write(writer, track.Index);
            Write(writer, track.Payload);
            Write(writer, operationIndices[track.Operation.Value]);
        }
        Write(writer, (uint)plan.Clips.Length);
        foreach (var clip in plan.Clips)
        {
            Write(writer, clip.TrackIndex);
            Write(writer, clip.Payload);
            Write(writer, clip.Start);
            Write(writer, clip.End);
        }
        return ImmutableArray.Create(writer.WrittenSpan.ToArray());
    }

    public static ImmutableArray<byte> Hash(ValidatedTimelinePlan plan)
        => ImmutableArray.Create(SHA256.HashData(Encode(plan).AsSpan()));

    public static bool TryDecode(ReadOnlySpan<byte> source, [NotNullWhen(true)] out ValidatedTimelinePlan? plan)
    {
        plan = null;
        var reader = new Reader(source);
        if (!reader.Take(Magic))
            return false;
        if (!reader.UInt(out var format) || format > ushort.MaxValue
            || !reader.UInt(out var runtimeId) || runtimeId > ushort.MaxValue
            || !reader.UInt(out var flags) || flags > 1
            || !reader.Text(out var identity)
            || !reader.UInt(out var operationCount) || operationCount > reader.Remaining)
            return false;
        var operations = new string[operationCount];
        for (var index = 0; index < operations.Length; index++)
        {
            if (!reader.Text(out operations[index]))
                return false;
            if (index != 0 && StringComparer.Ordinal.Compare(operations[index - 1], operations[index]) >= 0)
                return false;
        }
        if (!reader.UInt(out var trackCount)
            || trackCount > ushort.MaxValue + 1u
            || trackCount > reader.Remaining)
            return false;
        var tracks = new TrackPlan[trackCount];
        for (var index = 0; index < tracks.Length; index++)
        {
            if (!reader.UInt(out var trackIndex) || trackIndex > ushort.MaxValue
                || !reader.UInt(out var payload)
                || !reader.UInt(out var operation) || operation >= operations.Length)
                return false;
            tracks[index] = new((ushort)trackIndex, payload, new(operations[operation]));
        }
        if (!reader.UInt(out var clipCount) || clipCount > reader.Remaining)
            return false;
        var clips = new ClipPlan[clipCount];
        for (var index = 0; index < clips.Length; index++)
        {
            if (!reader.UInt(out var trackIndex) || trackIndex > ushort.MaxValue
                || !reader.UInt(out var payload)
                || !reader.UInt(out var start)
                || !reader.UInt(out var end))
                return false;
            clips[index] = new((ushort)trackIndex, payload, start, end);
        }
        if (reader.Remaining != 0)
            return false;
        try
        {
            plan = new TimelinePlan(identity, (ushort)runtimeId, flags != 0, tracks, clips, (ushort)format).Validate();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static void Write(IBufferWriter<byte> writer, ReadOnlySpan<byte> value)
    {
        value.CopyTo(writer.GetSpan(value.Length));
        writer.Advance(value.Length);
    }

    private static void Write(IBufferWriter<byte> writer, string value)
    {
        var count = Utf8.GetByteCount(value);
        Write(writer, (uint)count);
        var destination = writer.GetSpan(count);
        Utf8.GetBytes(value, destination);
        writer.Advance(count);
    }

    private static void Write(IBufferWriter<byte> writer, uint value)
    {
        var destination = writer.GetSpan(5);
        var count = 0;
        do
        {
            destination[count] = (byte)(value & 0x7f);
            value >>= 7;
            if (value != 0)
                destination[count] |= 0x80;
            count++;
        } while (value != 0);
        writer.Advance(count);
    }

    private ref struct Reader(ReadOnlySpan<byte> source)
    {
        private ReadOnlySpan<byte> _remaining = source;

        internal readonly uint Remaining => (uint)_remaining.Length;

        internal bool Take(ReadOnlySpan<byte> expected)
        {
            if (!_remaining.StartsWith(expected))
                return false;
            _remaining = _remaining[expected.Length..];
            return true;
        }

        internal bool Text(out string value)
        {
            value = "";
            if (!UInt(out var count) || count > int.MaxValue || count > _remaining.Length)
                return false;
            try
            {
                value = Utf8.GetString(_remaining[..(int)count]);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
            _remaining = _remaining[(int)count..];
            return true;
        }

        internal bool UInt(out uint value)
        {
            value = 0;
            for (var shift = 0; shift <= 28; shift += 7)
            {
                if (_remaining.IsEmpty)
                    return false;
                var current = _remaining[0];
                _remaining = _remaining[1..];
                if (shift == 28 && (current & 0xf0) != 0)
                    return false;
                value |= (uint)(current & 0x7f) << shift;
                if ((current & 0x80) == 0)
                    return shift == 0 || current != 0;
            }
            return false;
        }
    }
}
