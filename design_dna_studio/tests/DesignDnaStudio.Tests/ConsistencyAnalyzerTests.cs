using DesignDnaStudio.Engine;

namespace DesignDnaStudio.Tests;

public sealed class ConsistencyAnalyzerTests
{
    [Fact]
    public void NoProbe_IsReportedAsInsufficientInsteadOfPerfect()
    {
        var result = ConsistencyAnalyzer.Analyze([]);

        Assert.Equal(0d, result.AgreementScore);
        Assert.False(result.HasSufficientEvidence);
    }

    [Fact]
    public void MirroredResponse_IsFullyConsistent()
    {
        var sourceId = Guid.NewGuid();
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        var history = new[]
        {
            new ComparisonHistory(sourceId, left, right, PreferenceRating.StrongLeft),
            new ComparisonHistory(Guid.NewGuid(), right, left, PreferenceRating.StrongRight, true, sourceId)
        };

        var result = ConsistencyAnalyzer.Analyze(history);

        Assert.Equal(1, result.ProbeCount);
        Assert.Equal(1d, result.AgreementScore);
    }

    [Fact]
    public void OppositeResponse_HasZeroAgreement()
    {
        var sourceId = Guid.NewGuid();
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        var history = new[]
        {
            new ComparisonHistory(sourceId, left, right, PreferenceRating.StrongLeft),
            new ComparisonHistory(Guid.NewGuid(), right, left, PreferenceRating.StrongLeft, true, sourceId)
        };

        var result = ConsistencyAnalyzer.Analyze(history);

        Assert.Equal(0d, result.AgreementScore);
    }

    [Fact]
    public void SustainedLeftSelection_IsFlagged()
    {
        var history = Enumerable.Range(0, 24)
            .Select(index => new ComparisonHistory(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                index < 21 ? PreferenceRating.SlightLeft : PreferenceRating.SlightRight))
            .ToArray();

        var result = ConsistencyAnalyzer.Analyze(history);

        Assert.True(result.HasPotentialSideBias);
        Assert.True(result.LeftSelectionRate > 0.8d);
    }
}
