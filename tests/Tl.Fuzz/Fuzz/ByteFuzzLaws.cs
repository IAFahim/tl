using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Tl.Fuzz.Fuzzing;

using Tl.Fuzz.Laws;

public class TlbFuzzLaws
{
    public const int Iterations = 24_000;

    const int MaxSuccesses = 30_000;

    [Fact]
    public void MutationsOfValidAssetsNeverBreakTheLoadContract()
    {
        var random = FuzzRandom.FromSeeds(0x303, 0xA1);
        var bases = BaseAssets(random);
        var successes = 0;
        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            var input = Mutate(random, bases[random.NextInt(bases.Count)]);
            try
            {
                using var asset = TimelineAsset.Of(TimelineAsset.Load(input));
                if (++successes > MaxSuccesses)
                    throw new XunitException("mutation fuzz succeeded beyond the intern budget; the corpus mutated too little");
            }
            catch (Exception e) when (FuzzContract.IsLocatedTlbDiagnostic(e) || FuzzContract.IsDocumentedCapacity(e))
            {
            }
            catch (Exception e)
            {
                throw new XunitException($"Load contract violated after {iteration} iterations: {e.GetType().Name}: {e.Message}\ncorpus: {Convert.ToHexString(input)}");
            }
        }
    }

    [Fact]
    public void RandomBytesNeverBreakTheLoadContract()
    {
        var random = FuzzRandom.FromSeeds(0x303, 0xB2);
        var successes = 0;
        for (var iteration = 0; iteration < Iterations / 4; iteration++)
        {
            var input = new byte[random.NextInt(2_048) + 1];
            random.Fill(input);
            try
            {
                using var asset = TimelineAsset.Of(TimelineAsset.Load(input));
                successes++;
            }
            catch (Exception e) when (FuzzContract.IsLocatedTlbDiagnostic(e) || FuzzContract.IsDocumentedCapacity(e))
            {
            }
            catch (Exception e)
            {
                throw new XunitException($"Load contract violated on random bytes after {iteration} iterations: {e.GetType().Name}: {e.Message}\ncorpus: {Convert.ToHexString(input)}");
            }
        }
        if (successes != 0)
            throw new XunitException($"random bytes loaded {successes} times; the magic check should reject all of them");
    }

    [Fact]
    public void EveryHeaderFieldRejectsMalformationWithALocatedDiagnostic()
    {
        var bases = BaseAssets(new FuzzRandom(0x303));
        var valid = bases[0];
        var interesting = new uint[] { 0, 1, 2, 3, 7, 8, 63, 64, 65, 0x31424C53, 0x31424C54, 0x31424C55, 0x7FFFFFFF, 0x80000000, 0xFFFFFFFE, 0xFFFFFFFF };
        foreach (var field in new[] { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48 })
            foreach (var value in interesting)
            {
                var input = (byte[])valid.Clone();
                WriteUInt32(input, field, value);
                ExpectContract(input, $"header field at byte {field} = 0x{value:X}");
            }
        foreach (var length in new[] { 0, 1, 15, 31, 63, 64, 65, 111, 112, 113 })
        {
            var truncated = valid.AsSpan(0, Math.Min(length, valid.Length)).ToArray();
            ExpectContract(truncated, $"truncated to {length} bytes");
            var zeros = new byte[length];
            ExpectContract(zeros, $"zero-filled {length} bytes");
        }
    }

    static void ExpectContract(byte[] input, string context)
    {
        try
        {
            using var asset = TimelineAsset.Of(TimelineAsset.Load(input));
        }
        catch (Exception e) when (FuzzContract.IsLocatedTlbDiagnostic(e) || FuzzContract.IsDocumentedCapacity(e))
        {
            return;
        }
        catch (Exception e)
        {
            throw new XunitException($"Load contract violated for {context}: {e.GetType().Name}: {e.Message}");
        }
    }

    static List<byte[]> BaseAssets(FuzzRandom random)
    {
        var bases = new List<byte[]>
        {
            FuzzBake.Bake(0, false, 1f, []),
            FuzzBake.Bake(1, true, 0.5f, [new FuzzClipSpec(0, 1, 1f)]),
            FuzzBake.Bake(17, false, 1f, [new FuzzClipSpec(0, 8, 1f), new FuzzClipSpec(7, 17, 2f)]),
            FuzzBake.Bake(255, true, 0.25f, [new FuzzClipSpec(0, 64, 1f), new FuzzClipSpec(63, 255, 2f)]),
            FuzzBake.Bake(64, false, 2f, [new FuzzClipSpec(0, 32, 1f)]),
        };
        foreach (var json in BakeLaws.Fixtures())
            bases.Add(BakeLaws.Bake(BakeLaws.Utf8(json)));
        return bases;
    }

    internal static byte[] Mutate(FuzzRandom random, byte[] baseBytes)
    {
        var input = (byte[])baseBytes.Clone();
        var mutations = 1 + random.NextInt(4);
        for (var m = 0; m < mutations; m++)
        {
            var position = random.NextInt(input.Length);
            switch (random.NextInt(6))
            {
                case 0: input[position] ^= (byte)(1 << random.NextInt(8)); break;
                case 1: input[position] = (byte)random.NextInt(256); break;
                case 2: input[position] = input[random.NextInt(input.Length)]; break;
                case 3:
                    for (var k = 0; k < 4 && position + k < input.Length; k++) input[position + k] = 0xFF;
                    break;
                case 4:
                    for (var k = 0; k < 4 && position + k < input.Length; k++) input[position + k] = 0;
                    break;
                case 5:
                    var word = random.NextUInt();
                    for (var k = 0; k < 4 && position + k < input.Length; k++) input[position + k] = (byte)(word >> (8 * k));
                    break;
            }
        }
        return input;
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        if (offset + 4 > bytes.Length) return;
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }
}
