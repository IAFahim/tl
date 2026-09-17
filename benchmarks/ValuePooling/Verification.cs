using System.Globalization;

namespace Tl.ValuePooling;

public static class Verification
{
    public static void Assert(Fixture fixture)
    {
        var inline = fixture.ScanInline();
        var pooledUshort = fixture.ScanPooledUshort();
        if (inline != pooledUshort) throw new InvalidOperationException($"Scan checksums diverged: {Bits(inline)} vs {Bits(pooledUshort)}.");
        if (fixture.Pool > Layout.MaxBytePool) return;
        var pooledByte = fixture.ScanPooledByte();
        if (inline != pooledByte) throw new InvalidOperationException($"Byte scan checksum diverged: {Bits(inline)} vs {Bits(pooledByte)}.");
    }

    public static void Run()
    {
        PrintLayout();
        foreach (var pool in new[] { 10, 256, 1000 })
        {
            using var fixture = Fixture.Create(4096, pool);
            var inline = fixture.ScanInline();
            var pooledUshort = fixture.ScanPooledUshort();
            var indirectInline = fixture.IndirectInline();
            var indirectPooledUshort = fixture.IndirectPooledUshort();
            var pooledByte = pool <= Layout.MaxBytePool ? fixture.ScanPooledByte() : inline;
            var indirectPooledByte = pool <= Layout.MaxBytePool ? fixture.IndirectPooledByte() : indirectInline;
            if (inline != pooledUshort) throw new InvalidOperationException($"Scan parity failed at pool {pool}.");
            if (indirectInline != indirectPooledUshort) throw new InvalidOperationException($"Indirect parity failed at pool {pool}.");
            if (inline != pooledByte) throw new InvalidOperationException($"Byte scan parity failed at pool {pool}.");
            if (indirectInline != indirectPooledByte) throw new InvalidOperationException($"Byte indirect parity failed at pool {pool}.");
            var byteText = pool <= Layout.MaxBytePool ? Bits(pooledByte) : "out-of-domain";
            var byteIndirectText = pool <= Layout.MaxBytePool ? Bits(indirectPooledByte) : "out-of-domain";
            Console.WriteLine(FormattableString.Invariant(
                $"parity rows={fixture.Rows} pool={pool} inline={Bits(inline)} byte={byteText} ushort={Bits(pooledUshort)} indirectInline={Bits(indirectInline)} indirectByte={byteIndirectText} indirectUshort={Bits(indirectPooledUshort)}"));
        }
        Console.WriteLine("PASS pooled reads are bit-identical to inline");
    }

    public static void PrintLayout()
    {
        Print<InlineSlot>("inline");
        Print<PooledByteSlot>("pooled-byte");
        Print<PooledUshortSlot>("pooled-ushort");
    }

    private static void Print<T>(string name) where T : struct
    {
        var size = Layout.SizeOf<T>();
        Console.WriteLine(FormattableString.Invariant(
            $"layout {name} type={typeof(T).Name} bytesPerSlot={size} fullSlotsPer64BLine={Layout.SlotsPerLine<T>()} lineDensity={Layout.LineDensity<T>().ToString("F3", CultureInfo.InvariantCulture)}"));
    }

    private static string Bits(float value)
        => BitConverter.SingleToUInt32Bits(value).ToString("X8", CultureInfo.InvariantCulture);
}
