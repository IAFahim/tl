using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Tl.Fuzz.Fuzzing;

using Tl.Fuzz.Laws;

public class JsonFuzzLaws
{
    public const int Iterations = 24_000;

    static readonly string[] Vocabulary =
    [
        "name", "tracks", "clips", "start", "end", "data", "Value", "Scale", "duration", "loop",
        "namespace", "FuzzDomain", "FuzzJsonTrack", "FuzzJsonClip", "assembly", "true", "false", "null",
        "1", "0", "-1", "1.5", "65536", "4294967296", "18446744073709551616", "1e999999", "-1e999999",
        "1e-999999", "NaN", ":", "{", "}", "[", "]", "\"", ",", "\\", "\t", "\u0000", "\uFFFF", "0x10", "+1", "01",
    ];

    [Fact]
    public void MutatedAuthoringJsonNeverBreaksTheBakeContract()
    {
        var random = FuzzRandom.FromSeeds(0x303, 0xC4);
        var bases = BakeLaws.Fixtures().Select(BakeLaws.Utf8).ToList();
        bases.Add("{\"truncated\""u8.ToArray());
        var successes = 0;
        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            var input = TlbFuzzLaws.Mutate(random, bases[random.NextInt(bases.Count)]);
            try
            {
                BakeLaws.Bake(input);
                successes++;
            }
            catch (Exception e) when (FuzzContract.IsBakeDiagnostic(e))
            {
            }
            catch (Exception e)
            {
                throw new XunitException($"bake contract violated after {iteration} iterations: {e.GetType().Name}: {e.Message}\ncorpus: {Encoding.UTF8.GetString(input)}");
            }
        }
        if (successes == 0)
            throw new XunitException("no mutated document baked successfully; the mutator is too destructive");
    }

    [Fact]
    public void StructuredWordSaladNeverBreaksTheBakeContract()
    {
        var random = FuzzRandom.FromSeeds(0x303, 0xD5);
        for (var iteration = 0; iteration < Iterations / 4; iteration++)
        {
            var document = new StringBuilder();
            var tokens = random.NextInt(48);
            for (var i = 0; i < tokens; i++)
            {
                document.Append(Vocabulary[random.NextInt(Vocabulary.Length)]);
                if (random.NextBool()) document.Append(' ');
            }
            try
            {
                BakeLaws.Bake(Encoding.UTF8.GetBytes(document.ToString()));
            }
            catch (Exception e) when (FuzzContract.IsBakeDiagnostic(e))
            {
            }
            catch (Exception e)
            {
                throw new XunitException($"bake contract violated after {iteration} iterations: {e.GetType().Name}: {e.Message}\ncorpus: {document}");
            }
        }
    }

    [Fact]
    public void DeepNestingIsRejectedWithABoundedDiagnostic()
    {
        foreach (var depth in new[] { 63, 64, 65, 66, 100, 1_000, 100_000 })
        {
            var nesting = new string('[', depth) + new string(']', depth);
            var json = """{"name":"deep","duration":1,"tracks":[{"name":"t","namespace":"FuzzDomain","type":"FuzzJsonTrack","data":{"Scale":1},"clips":[{"name":"c","namespace":"FuzzDomain","type":"FuzzJsonClip","start":0,"end":1,"data":{"Value":1},"x":""" + nesting + """}]}]}""";
            try
            {
                BakeLaws.Bake(json);
                if (depth <= 63) continue;
                throw new XunitException($"nesting depth {depth} baked successfully without a depth guard");
            }
            catch (Exception e) when (FuzzContract.IsBakeDiagnostic(e))
            {
            }
        }
    }

    [Fact]
    public void InvalidUtf8IsRejectedWithALocatedOffset()
    {
        var valid = BakeLaws.Utf8(BakeLaws.Fixtures()[0]);
        foreach (var position in new[] { 0, 1, 10, valid.Length / 2, valid.Length - 1 })
        {
            var input = (byte[])valid.Clone();
            input[position] = 0xFF;
            try
            {
                BakeLaws.Bake(input);
                throw new XunitException($"invalid UTF-8 at byte {position} baked successfully");
            }
            catch (Exception e) when (FuzzContract.IsBakeDiagnostic(e))
            {
                if (!e.Message.Contains("UTF-8", StringComparison.OrdinalIgnoreCase) && position == 0)
                    throw new XunitException($"diagnostic does not identify the UTF-8 invariant: {e.Message}");
            }
        }
    }

    [Fact]
    public void SuccessfulMutatedBakesStillLoad()
    {
        var random = FuzzRandom.FromSeeds(0x303, 0xE6);
        var bases = BakeLaws.Fixtures().Select(BakeLaws.Utf8).ToList();
        var loaded = 0;
        for (var iteration = 0; iteration < Iterations / 4 && loaded < 64; iteration++)
        {
            var input = TlbFuzzLaws.Mutate(random, bases[random.NextInt(bases.Count)]);
            byte[] baked;
            try
            {
                baked = BakeLaws.Bake(input);
            }
            catch (Exception e) when (FuzzContract.IsBakeDiagnostic(e))
            {
                continue;
            }
            loaded++;
            try
            {
                using var asset = TimelineAsset.Of(TimelineAsset.Load(baked));
            }
            catch (Exception e) when (FuzzContract.IsDocumentedCapacity(e))
            {
                break;
            }
            catch (Exception e)
            {
                throw new XunitException($"baked bytes from mutated JSON violate the load contract: {e.GetType().Name}: {e.Message}");
            }
        }
        if (loaded == 0)
            throw new XunitException("no mutated document baked successfully; the mutator is too destructive");
    }
}
