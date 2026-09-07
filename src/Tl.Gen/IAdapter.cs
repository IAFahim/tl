using Tl.Gen.Model;

namespace Tl.Gen;

public interface IAdapter
{
    IReadOnlyList<SourceFile> Emit(TimelinePlan plan);
}
