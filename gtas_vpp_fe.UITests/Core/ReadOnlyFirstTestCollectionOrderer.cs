using gtas_vpp_fe.UITests.Core;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

[assembly: TestCollectionOrderer(typeof(ReadOnlyFirstTestCollectionOrderer))]

namespace gtas_vpp_fe.UITests.Core;

/// <summary>
/// Runs the shared read-only collection before the per-class mutating collections
/// and orders everything else by display name. Not required for correctness (the
/// mutating tests use separate databases) but it keeps the resource profile and
/// run order deterministic for baseline timing comparisons — xUnit v3's default
/// collection orderer randomizes the order on every run.
/// </summary>
public sealed class ReadOnlyFirstTestCollectionOrderer : ITestCollectionOrderer
{
    public IReadOnlyCollection<TTestCollection> OrderTestCollections<TTestCollection>(
        IReadOnlyCollection<TTestCollection> testCollections)
        where TTestCollection : ITestCollection
        => [.. testCollections
            .OrderBy(static collection => string.Equals(
                collection.TestCollectionDisplayName,
                ReadOnlyE2ECollection.Name,
                StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(static collection => collection.TestCollectionDisplayName, StringComparer.Ordinal)];
}
