using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Xunit;

namespace Tl.Core.Tests;

public unsafe class LibraryV3Tests
{
    static TimelineSpec[] SampleSpecs()
    {
        var dense = new StepSpec(0, 4, 3, 0xABCDUL,
            new float[] { 0.25f, 0.5f, -0.75f, 2f },
            new float[] { 9f, 8f, 7f, 6f });
        var constant = new StepSpec(2, 6, 7, 0x1234UL,
            new float[] { 1.5f, 1.5f, 1.5f, 1.5f },
            new float[] { 1.5f, 1.5f, 1.5f, 1.5f });
        var single = new StepSpec(1, 3, 3, 0x55UL,
            new float[] { -2.5f },
            new float[] { 4f });
        return new[]
        {
            new TimelineSpec(8, true, new[] { dense, constant }),
            new TimelineSpec(5, false, new[] { single }),
        };
    }

    [Fact]
    public void WriterVerifyRoundTripAccessorsMatchInputs()
    {
        var specs = SampleSpecs();
        var blob = LibraryWriter.Write(specs);
        var result = Library.Verify(blob);
        Assert.True(result.Ok);
        Assert.Null(result.Error == LibraryError.None ? null : result.Error.ToString());
        using var library = result.Library!;
        Assert.Equal(2, library.TimelineCount);
        Assert.Equal(8u, library.Duration(0));
        Assert.True(library.Loops(0));
        Assert.Equal(5u, library.Duration(1));
        Assert.Equal(LibraryError.None, result.Error);
        Assert.Equal("Timeline0", library.TimelineName(0));
        Assert.Equal("Timeline1", library.TimelineName(1));
        Assert.Equal(2, library.StageCount);
        Assert.Equal(3, library.StepCount);
        Assert.Equal(2, library.StagePlanCount);
        Assert.Equal(1, library.EdgeCount);
        Assert.Equal(3, library.MarkerCount);
        Assert.Equal(2, library.KindCount);
        Assert.Equal(3, library.FeederCount);
        Assert.Equal(4, library.NameCount);
        var stage0 = library.Stage(0);
        Assert.Equal(0u, stage0.Start);
        Assert.Equal(8u, stage0.End);
        Assert.Equal(0u, stage0.StepIndex);
        Assert.Equal(2u, stage0.StepCount);
        var stage1 = library.Stage(1);
        Assert.Equal(0u, stage1.Start);
        Assert.Equal(5u, stage1.End);
        Assert.Equal(2u, stage1.StepIndex);
        Assert.Equal(1u, stage1.StepCount);
        var plan0 = library.StagePlan(0);
        Assert.Equal(0u, plan0.StageIndex);
        Assert.Equal(0u, plan0.StepIndex);
        Assert.Equal(2u, plan0.StepCount);
        var step0 = library.Step(0);
        Assert.Equal(0u, step0.WindowStart);
        Assert.Equal(4u, step0.WindowEnd);
        Assert.Equal(3u, step0.KindId);
        Assert.Equal(0xABCDUL, step0.PairKey);
        var step1 = library.Step(1);
        Assert.Equal(2u, step1.WindowStart);
        Assert.Equal(6u, step1.WindowEnd);
        Assert.Equal(7u, step1.KindId);
        var step2 = library.Step(2);
        Assert.Equal(1u, step2.WindowStart);
        Assert.Equal(3u, step2.WindowEnd);
        Assert.Equal(3u, step2.KindId);
        var forward = library.ForwardPlane(0);
        Assert.Equal(4, forward.Length);
        Assert.Equal(0.25f, forward[0]);
        Assert.Equal(0.5f, forward[1]);
        Assert.Equal(-0.75f, forward[2]);
        Assert.Equal(2f, forward[3]);
        var backward = library.BackwardPlane(0);
        Assert.Equal(4, backward.Length);
        Assert.Equal(9f, backward[0]);
        Assert.Equal(8f, backward[1]);
        Assert.Equal(7f, backward[2]);
        Assert.Equal(6f, backward[3]);
        var constantPlane = library.ForwardPlane(1);
        Assert.Equal(1, constantPlane.Length);
        Assert.Equal(1.5f, constantPlane[0]);
        Assert.Equal(0.25f, library.LaneValue(0, LaneDirection.Forward, 0));
        Assert.Equal(-0.75f, library.LaneValue(0, LaneDirection.Forward, 2));
        Assert.Equal(6f, library.LaneValue(0, LaneDirection.Backward, 3));
        Assert.Equal(1.5f, library.LaneValue(1, LaneDirection.Forward, 3));
        Assert.Equal(1.5f, library.LaneValue(1, LaneDirection.Backward, 5));
        Assert.Equal(-2.5f, library.LaneValue(2, LaneDirection.Forward, 2));
        Assert.Equal(4f, library.LaneValue(2, LaneDirection.Backward, 1));
        Assert.Equal(0f, library.LaneValue(0, LaneDirection.Forward, 6));
        Assert.Equal(0f, library.LaneValue(0, LaneDirection.Forward, -1));
        var edge = library.Edge(0);
        Assert.Equal(0u, edge.From);
        Assert.Equal(1u, edge.To);
        var marker = library.Marker(2);
        Assert.Equal(1u, marker.Tick);
        Assert.Equal(1u, marker.TimelineIndex);
        Assert.Equal(2u, marker.StepIndex);
        var feeder = library.Feeder(1);
        Assert.Equal(7u, feeder.KindId);
        Assert.Equal(0u, feeder.TimelineIndex);
        Assert.Equal(1u, feeder.StepIndex);
        Assert.Equal(3u, library.Kind(0).Id);
        Assert.Equal(0u, library.Kind(0).Order);
        Assert.Equal(7u, library.Kind(1).Id);
        Assert.Equal(1u, library.Kind(1).Order);
        Assert.Equal("Kind3", library.NameText((int)library.Kind(0).NameIndex));
        Assert.Equal("Kind7", library.NameText((int)library.Kind(1).NameIndex));
        Assert.Equal("Timeline0", library.NameText(0));
    }

    [Fact]
    public void ConstantLanesAreDetectedAtFillTime()
    {
        var nanStep = new StepSpec(0, 2, 1, 1UL,
            new float[] { float.NaN, float.NaN },
            new float[] { 3f, 3f });
        var denseStep = new StepSpec(0, 3, 1, 2UL,
            new float[] { 1f, 2f, 3f },
            new float[] { -1f, -1f, -1f });
        var result = Library.Verify(LibraryWriter.Write(new[]
        {
            new TimelineSpec(4, false, new[] { nanStep, denseStep }),
        }));
        Assert.True(result.Ok);
        using var library = result.Library!;
        Assert.Equal(LaneClass.Constant, library.LaneClassOf(0, LaneDirection.Forward));
        Assert.Equal(LaneClass.Constant, library.LaneClassOf(0, LaneDirection.Backward));
        Assert.Equal(LaneClass.Dense, library.LaneClassOf(1, LaneDirection.Forward));
        Assert.True(float.IsNaN(library.LaneValue(0, LaneDirection.Forward, 1)));
        Assert.Equal(3f, library.LaneValue(0, LaneDirection.Backward, 0));
        var sample = Library.Verify(LibraryWriter.Write(SampleSpecs()));
        Assert.True(sample.Ok);
        using var sampled = sample.Library!;
        Assert.Equal(LaneClass.Dense, sampled.LaneClassOf(0, LaneDirection.Forward));
        Assert.Equal(LaneClass.Dense, sampled.LaneClassOf(0, LaneDirection.Backward));
        Assert.Equal(LaneClass.Constant, sampled.LaneClassOf(1, LaneDirection.Forward));
        Assert.Equal(LaneClass.Constant, sampled.LaneClassOf(1, LaneDirection.Backward));
        Assert.Equal(LaneClass.Constant, sampled.LaneClassOf(2, LaneDirection.Forward));
    }

    [Fact]
    public void SectionsAreSixtyFourByteAligned()
    {
        var blob = LibraryWriter.Write(SampleSpecs());
        Assert.Equal(128u, BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(16)));
        for (var i = 0; i < 10; i++)
            Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(16 + i * 8)) % 64u);
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(100)) % 64u);
        Assert.Equal((uint)blob.Length, BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(8)));
    }

    [Fact]
    public void EmptyLibraryRoundTrips()
    {
        var blob = LibraryWriter.Write(Array.Empty<TimelineSpec>());
        var result = Library.Verify(blob);
        Assert.True(result.Ok);
        using var library = result.Library!;
        Assert.Equal(0, library.TimelineCount);
        Assert.Equal(0, library.StepCount);
        Assert.Equal(0u, library.Duration(0));
        Assert.Equal(0, library.ForwardPlane(0).Length);
        Assert.Equal("", library.TimelineName(0));
        Assert.Equal(0f, library.LaneValue(0, LaneDirection.Forward, 0));
    }

    [Fact]
    public void VerifyRejectsTruncatedAndOffsetLyingBytes()
    {
        var blob = LibraryWriter.Write(SampleSpecs());
        for (var length = 0; length < blob.Length; length++)
        {
            var result = Library.Verify(blob.AsSpan(0, length));
            Assert.False(result.Ok);
            Assert.Equal(LibraryError.Truncated, result.Error);
        }
        var lying = (byte[])blob.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(lying.AsSpan(32),
            BinaryPrimitives.ReadUInt32LittleEndian(lying.AsSpan(32)) + 64u);
        Resign(lying);
        var lyingResult = Library.Verify(lying);
        Assert.False(lyingResult.Ok);
        Assert.Equal(LibraryError.LayoutMismatch, lyingResult.Error);
        Assert.Equal(32, lyingResult.ErrorOffset);
        var flipped = (byte[])blob.Clone();
        flipped[200] ^= 0xFF;
        var flippedResult = Library.Verify(flipped);
        Assert.False(flippedResult.Ok);
        Assert.Equal(LibraryError.HashMismatch, flippedResult.Error);
    }

    [Fact]
    public void CorruptBuffersReturnErrorNeverThrow()
    {
        var valid = LibraryWriter.Write(SampleSpecs());
        var random = new Random(1234);
        for (var i = 0; i < 200; i++)
        {
            byte[] buffer;
            if (i < 100)
            {
                buffer = new byte[random.Next(0, 700)];
                random.NextBytes(buffer);
            }
            else
            {
                buffer = (byte[])valid.Clone();
                var mode = i % 3;
                if (mode == 0)
                {
                    var length = random.Next(0, buffer.Length);
                    buffer = buffer.AsSpan(0, length).ToArray();
                }
                else
                {
                    var flips = random.Next(1, 9);
                    for (var f = 0; f < flips; f++)
                        buffer[random.Next(0, buffer.Length)] ^= (byte)random.Next(1, 256);
                }
            }
            var result = Library.Verify(buffer);
            Assert.False(result.Ok);
            Assert.True(result.Error != LibraryError.None);
            Assert.Null(result.Library);
        }
    }

    [Fact]
    public void VerifyIsStableAcrossReVerify()
    {
        var specs = SampleSpecs();
        var first = LibraryWriter.Write(specs);
        var second = LibraryWriter.Write(specs);
        Assert.Equal(first, second);
        var firstResult = Library.Verify(first);
        Assert.True(firstResult.Ok);
        using var firstLibrary = firstResult.Library!;
        var secondResult = Library.Verify(firstLibrary.Blob);
        Assert.True(secondResult.Ok);
        using var secondLibrary = secondResult.Library!;
        Assert.Equal(firstLibrary.TimelineCount, secondLibrary.TimelineCount);
        Assert.Equal(firstLibrary.StepCount, secondLibrary.StepCount);
        Assert.Equal(firstLibrary.Duration(1), secondLibrary.Duration(1));
        Assert.True(firstLibrary.ForwardPlane(0).SequenceEqual(secondLibrary.ForwardPlane(0)));
        Assert.True(firstLibrary.BackwardPlane(0).SequenceEqual(secondLibrary.BackwardPlane(0)));
        Assert.Equal(firstLibrary.TimelineName(0), secondLibrary.TimelineName(0));
    }

    static void Resign(byte[] blob)
    {
        using var sha = SHA256.Create();
        var digest = sha.ComputeHash(blob, 0, blob.Length - 32);
        digest.CopyTo(blob, blob.Length - 32);
    }
}
