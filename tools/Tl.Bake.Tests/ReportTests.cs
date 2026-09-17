using System;
using System.IO;
using System.Text;
using Tl.Gen.Tlb;
using Xunit;

namespace Tl.Bake.Tests;

public class ReportTests
{
    private const string OracleReport =
        "tlb/total-bytes: 1198\n" +
        "tlb/hot-bytes: 824\n" +
        "tlb/metadata-bytes: 374\n" +
        "pair/table-bytes: 144\n" +
        "pair/count: 3\n" +
        "stage/count: 6\n" +
        "program/step-count: 13\n" +
        "pool/region-bytes: 96\n" +
        "frame-slot/region-bytes: 312\n" +
        "instance/state-bytes: 16\n" +
        "pool/0/track-unique-count: 1\n" +
        "pool/0/clip-unique-count: 2\n" +
        "pool/0/pool-bytes: 32\n" +
        "pool/1/track-unique-count: 1\n" +
        "pool/1/clip-unique-count: 1\n" +
        "pool/1/pool-bytes: 32\n" +
        "pool/2/track-unique-count: 1\n" +
        "pool/2/clip-unique-count: 1\n" +
        "pool/2/pool-bytes: 32\n" +
        "pool/unique-count: 7\n" +
        "pool/value-bytes: 28\n" +
        "label/root-count: 1\n" +
        "label/track-count: 2\n" +
        "label/clip-count: 4\n";

    private const string StrippedReport =
        "tlb/total-bytes: 824\n" +
        "tlb/hot-bytes: 824\n" +
        "tlb/metadata-bytes: 0\n" +
        "pair/table-bytes: 144\n" +
        "pair/count: 3\n" +
        "stage/count: 6\n" +
        "program/step-count: 13\n" +
        "pool/region-bytes: 96\n" +
        "frame-slot/region-bytes: 312\n" +
        "instance/state-bytes: 16\n" +
        "pool/0/track-unique-count: 1\n" +
        "pool/0/clip-unique-count: 2\n" +
        "pool/0/pool-bytes: 32\n" +
        "pool/1/track-unique-count: 1\n" +
        "pool/1/clip-unique-count: 1\n" +
        "pool/1/pool-bytes: 32\n" +
        "pool/2/track-unique-count: 1\n" +
        "pool/2/clip-unique-count: 1\n" +
        "pool/2/pool-bytes: 32\n" +
        "pool/unique-count: 7\n" +
        "pool/value-bytes: 28\n" +
        "label/root-count: 0\n" +
        "label/track-count: 0\n" +
        "label/clip-count: 0\n";

    private static string ReportOf(byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), "tlb_report_" + Guid.NewGuid().ToString("N") + ".tlb");
        try
        {
            File.WriteAllBytes(path, bytes);
            var original = Console.Out;
            using var capture = new StringWriter();
            try
            {
                Console.SetOut(capture);
                Assert.Equal(0, Tl.Bake.Program.Main(["--report", path]));
            }
            finally
            {
                Console.SetOut(original);
            }
            return capture.ToString();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Report_OnCommittedOracleFixture_MatchesExactExpectedValues()
    {
        Assert.Equal(OracleReport, ReportOf(TimelineBaker.BakeJson(Recording.OracleJson)));
    }

    [Fact]
    public void Report_IsDeterministic()
    {
        var bytes = TimelineBaker.BakeJson(DeterminismTests.OwnerSampleJson);
        Assert.Equal(ReportOf(bytes), ReportOf(bytes));
    }

    [Fact]
    public void StrippedReport_DiffersOnlyInMetadataDerivedValues()
    {
        var full = TimelineBaker.BakeJson(Recording.OracleJson);
        Assert.Equal(StrippedReport, ReportOf(TlbMetadata.Strip(full)));
        Assert.Equal(OracleReport, ReportOf(full));
    }

    [Fact]
    public void Report_OnNonTlb1Input_Fails()
    {
        var path = Path.Combine(Path.GetTempPath(), "tlb_report_" + Guid.NewGuid().ToString("N") + ".tlb");
        try
        {
            File.WriteAllBytes(path, [1, 2, 3]);
            var original = Console.Out;
            using var capture = new StringWriter();
            try
            {
                Console.SetOut(capture);
                Assert.NotEqual(0, Tl.Bake.Program.Main(["--report", path]));
            }
            finally
            {
                Console.SetOut(original);
            }
            Assert.Equal(string.Empty, capture.ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
