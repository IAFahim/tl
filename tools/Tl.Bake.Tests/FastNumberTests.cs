using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class FastNumberTests
{
    static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    static (bool Ok, int TokenEnd, uint Value) Scan32(string text)
    {
        var ok = FastNumber.ScanUnsigned32(Utf8(text), 0, text.Length, out var tokenEnd, out var value);
        return (ok, tokenEnd, value);
    }

    static (bool Ok, int TokenEnd, ulong Value) Scan64(string text)
    {
        var ok = FastNumber.ScanUnsigned64(Utf8(text), 0, text.Length, out var tokenEnd, out var value);
        return (ok, tokenEnd, value);
    }

    static (bool Ok, int TokenEnd, long Value) ScanSigned(string text, bool allowNegative, long min, long max)
    {
        var ok = FastNumber.ScanSignedInteger(Utf8(text), 0, text.Length, allowNegative, min, max, out var tokenEnd, out var value);
        return (ok, tokenEnd, value);
    }

    static (bool Ok, int TokenEnd, ulong Mantissa, int Exponent, bool Negative, bool Hard) ScanParts(string text)
    {
        var ok = FastNumber.ScanNumberParts(Utf8(text), 0, text.Length, out var tokenEnd, out var mantissa, out var exponent, out var negative, out var hard);
        return (ok, tokenEnd, mantissa, exponent, negative, hard);
    }

    [Theory]
    [InlineData("0", 0u)]
    [InlineData("42", 42u)]
    [InlineData("4294967295", 4294967295u)]
    public void ScanUnsigned32AcceptsCanonicalTokens(string text, uint expected)
    {
        var (ok, tokenEnd, value) = Scan32(text);
        Assert.True(ok);
        Assert.Equal(text.Length, tokenEnd);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("007")]
    [InlineData("4294967296")]
    [InlineData("12x")]
    [InlineData("1 2")]
    [InlineData("   ")]
    [InlineData("")]
    public void ScanUnsigned32RejectsSignsLeadingZerosOverflowAndStrayTail(string text)
        => Assert.False(Scan32(text).Ok);

    [Fact]
    public void ScanUnsigned32SkipsSurroundingWhitespace()
    {
        var (ok, tokenEnd, value) = Scan32(" \t 7\n ");
        Assert.True(ok);
        Assert.Equal(4, tokenEnd);
        Assert.Equal(7u, value);
    }

    [Theory]
    [InlineData("18446744073709551615", 18446744073709551615ul)]
    [InlineData("0", 0ul)]
    public void ScanUnsigned64AcceptsCanonicalTokens(string text, ulong expected)
    {
        var (ok, tokenEnd, value) = Scan64(text);
        Assert.True(ok);
        Assert.Equal(text.Length, tokenEnd);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("18446744073709551616")]
    [InlineData("-1")]
    [InlineData("01")]
    [InlineData("8x")]
    public void ScanUnsigned64RejectsOverflowAndMalformedTokens(string text)
        => Assert.False(Scan64(text).Ok);

    [Fact]
    public void ScanSignedIntegerAcceptsTheExactLongRange()
    {
        Assert.Equal((true, 20, long.MinValue), ScanSigned("-9223372036854775808", true, long.MinValue, long.MaxValue));
        Assert.Equal((true, 19, long.MaxValue), ScanSigned("9223372036854775807", true, long.MinValue, long.MaxValue));
        Assert.Equal((true, 1, 0), ScanSigned("0", true, long.MinValue, long.MaxValue));
        Assert.Equal((true, 2, 0), ScanSigned("-0", true, long.MinValue, long.MaxValue));
    }

    [Theory]
    [InlineData("9223372036854775808")]
    [InlineData("-9223372036854775809")]
    [InlineData("-92233720368547758080")]
    public void ScanSignedIntegerRejectsBeyondTheLongRange(string text)
        => Assert.False(ScanSigned(text, true, long.MinValue, long.MaxValue).Ok);

    [Fact]
    public void ScanSignedIntegerRejectsNegativeWhenDisallowedAndValuesOutsideTheAuthoredRange()
    {
        Assert.False(ScanSigned("-1", false, long.MinValue, long.MaxValue).Ok);
        Assert.False(ScanSigned("-5", true, -4, 10).Ok);
        Assert.False(ScanSigned("11", true, -4, 10).Ok);
        var (ok, tokenEnd, value) = ScanSigned(" 10 ", true, -4, 10);
        Assert.True(ok);
        Assert.Equal(3, tokenEnd);
        Assert.Equal(10, value);
    }

    [Theory]
    [InlineData("123", 123ul, 0, false, false)]
    [InlineData("1.5", 15ul, -1, false, false)]
    [InlineData("1e3", 1ul, 3, false, false)]
    [InlineData("-2.5e-1", 25ul, -2, true, false)]
    [InlineData("0.0", 0ul, -1, false, false)]
    [InlineData("0", 0ul, 0, false, false)]
    public void ScanNumberPartsClassifiesSoftNumbers(string text, ulong mantissa, int exponent, bool negative, bool hard)
    {
        var (ok, tokenEnd, partMantissa, partExponent, partNegative, partHard) = ScanParts(text);
        Assert.True(ok);
        Assert.Equal(text.Length, tokenEnd);
        Assert.Equal(mantissa, partMantissa);
        Assert.Equal(exponent, partExponent);
        Assert.Equal(negative, partNegative);
        Assert.Equal(hard, partHard);
    }

    [Fact]
    public void ScanNumberPartsMarksNineteenDigitsSoftAndTwentyDigitsHard()
    {
        var (ok, _, mantissa, _, _, hard) = ScanParts("9999999999999999999");
        Assert.True(ok);
        Assert.False(hard);
        Assert.Equal(9999999999999999999ul, mantissa);

        (ok, _, mantissa, _, _, hard) = ScanParts("12345678901234567890");
        Assert.True(ok);
        Assert.True(hard);
        Assert.Equal(1234567890123456789ul, mantissa);
    }

    [Theory]
    [InlineData("01.5")]
    [InlineData("1.")]
    [InlineData(".5")]
    [InlineData("1e")]
    [InlineData("1e+")]
    [InlineData("1ex")]
    [InlineData("--1")]
    [InlineData("1-2")]
    [InlineData("+1")]
    [InlineData("")]
    public void ScanNumberPartsRejectsMalformedNumberGrammar(string text)
        => Assert.False(ScanParts(text).Ok);

    [Fact]
    public void PartsToFloatReconstructsSoftNumbersExactly()
    {
        Assert.Equal(1.5f, FastNumber.PartsToFloat(15, -1, false, false));
        Assert.Equal(123f, FastNumber.PartsToFloat(123, 0, false, false));
        Assert.Equal(-0.25f, FastNumber.PartsToFloat(25, -2, true, false));
        Assert.Equal(16777215f / 10_000_000_000f, FastNumber.PartsToFloat(0xFFFFFF, -10, false, false));
        Assert.Equal(1e22f, FastNumber.PartsToFloat(1, 22, false, false));
    }

    [Fact]
    public void PartsToFloatPreservesSignedZero()
    {
        var positive = FastNumber.PartsToFloat(0, -5, false, false);
        var negative = FastNumber.PartsToFloat(0, -5, true, false);
        Assert.Equal(0f, positive);
        Assert.Equal(0u, BitConverter.SingleToUInt32Bits(positive) >> 31);
        Assert.Equal(1u, BitConverter.SingleToUInt32Bits(negative) >> 31);
    }

    [Theory]
    [InlineData(1, 0, false, true)]
    [InlineData(1, 28, false, false)]
    [InlineData(1, 23, false, false)]
    [InlineData(1, -11, false, false)]
    [InlineData(1, -401, false, false)]
    [InlineData(0, 100, false, false)]
    [InlineData(0x1000000, -10, false, false)]
    public void PartsToFloatReturnsNaNForHardAndOutOfRangeParts(ulong mantissa, int exponent, bool negative, bool hard)
        => Assert.Equal(float.NaN, FastNumber.PartsToFloat(mantissa, exponent, negative, hard));

    [Fact]
    public void PartsToDoubleReconstructsSoftNumbersExactly()
    {
        Assert.Equal(1.5, FastNumber.PartsToDouble(15, -1, false, false));
        Assert.Equal(123.0, FastNumber.PartsToDouble(123, 0, false, false));
        Assert.Equal(-0.25, FastNumber.PartsToDouble(25, -2, true, false));
        Assert.Equal(1e22, FastNumber.PartsToDouble(1, 22, false, false));
        Assert.Equal(1e-22, FastNumber.PartsToDouble(1, -22, false, false));
        Assert.Equal(9007199254740991.0, FastNumber.PartsToDouble((1ul << 53) - 1, 0, false, false));
    }

    [Fact]
    public void PartsToDoublePreservesSignedZero()
    {
        var positive = FastNumber.PartsToDouble(0, 5, false, false);
        var negative = FastNumber.PartsToDouble(0, 5, true, false);
        Assert.Equal(0.0, positive);
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(positive) >> 63);
        Assert.Equal(-1L, BitConverter.DoubleToInt64Bits(negative) >> 63);
    }

    [Theory]
    [InlineData(1, 0, false, true)]
    [InlineData(1, 23, false, false)]
    [InlineData(1, -23, false, false)]
    [InlineData(1ul << 53, 0, false, false)]
    [InlineData(0, 30, false, false)]
    public void PartsToDoubleReturnsNaNForHardAndOutOfRangeParts(ulong mantissa, int exponent, bool negative, bool hard)
        => Assert.Equal(double.NaN, FastNumber.PartsToDouble(mantissa, exponent, negative, hard));

    [Fact]
    public void SaturatedExponentsScanButBakeAsNaN()
    {
        var (ok, _, _, exponent, _, _) = ScanParts("1e425000000");
        Assert.True(ok);
        Assert.True(exponent > 27);
        Assert.Equal(float.NaN, FastNumber.PartsToFloat(1, exponent, false, false));
        Assert.Equal(double.NaN, FastNumber.PartsToDouble(1, exponent, false, false));
    }
}
