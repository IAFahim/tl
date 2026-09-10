using System.Collections.Immutable;
using Tl.Compiler;

namespace Tl.Gen.C;

public readonly record struct COperationBinding(
    OperationId Operation,
    string SeekSymbol);

public sealed record CBinding
{
    public CBinding(
        string symbolPrefix,
        string headerFileName,
        IEnumerable<COperationBinding> operations)
    {
        ArgumentNullException.ThrowIfNull(symbolPrefix);
        ArgumentNullException.ThrowIfNull(headerFileName);
        ArgumentNullException.ThrowIfNull(operations);
        SymbolPrefix = symbolPrefix;
        HeaderFileName = headerFileName;
        Operations = [.. operations];
    }

    public string SymbolPrefix { get; }
    public string HeaderFileName { get; }
    public ImmutableArray<COperationBinding> Operations { get; }
}

public readonly record struct CArtifact(string RelativePath, string Content)
{
    public int Utf8Bytes => System.Text.Encoding.UTF8.GetByteCount(Content);
}

public readonly record struct CEmissionReport(
    ushort PlanFormatVersion,
    ushort AbiVersion,
    int TrackCount,
    int ClipCount,
    int RegionCount,
    uint Duration,
    bool Loops,
    int SourceFileCount,
    int SourceUtf8Bytes,
    int StaticDataBytes,
    int PlaybackBytes,
    int PlaybackAlignment,
    int FrameBytes,
    int FrameAlignment,
    int RuntimeHeapBytes);

public sealed record CEmission(
    ImmutableArray<CArtifact> Artifacts,
    CEmissionReport Report);
