using Xunit;

namespace Tl.Systems.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AllocationCollection
{
    public const string Name = "Allocation";
}
