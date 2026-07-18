using DesignDnaStudio.Engine;

namespace DesignDnaStudio.Tests;

public sealed class ProfileSnapshotBuilderTests
{
    [Fact]
    public void EmptyModel_NeverReportsStableProfile()
    {
        var result = ProfileSnapshotBuilder.Build(
            new PreferenceModelState(),
            new ConsistencyResult(0, 0d, 0.5d, false));

        Assert.NotEqual("stable", result.MaturityLevel);
        Assert.All(result.Dimensions, item => Assert.NotEqual("stable", item.ConfidenceLevel));
    }

    [Fact]
    public void Catalog_CoversEveryDimensionExactlyOnce()
    {
        Assert.Equal(DesignVector.DimensionCount, DesignDimensionCatalog.All.Count);
        Assert.Equal(
            Enum.GetValues<DesignDimension>().OrderBy(item => item),
            DesignDimensionCatalog.All.Select(item => item.Dimension).OrderBy(item => item));
    }
}
