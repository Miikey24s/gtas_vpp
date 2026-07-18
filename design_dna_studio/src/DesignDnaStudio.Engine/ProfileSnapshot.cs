namespace DesignDnaStudio.Engine;

public sealed record DimensionPreference(
    DesignDimension Dimension,
    double Weight,
    double Confidence,
    string ConfidenceLevel)
{
    public bool HasDirectionalSignal => Confidence >= 0.12d && Math.Abs(Weight) >= 0.05d;
}

public sealed record ProfileSnapshot(
    IReadOnlyList<DimensionPreference> Dimensions,
    double Coverage,
    ConsistencyResult Consistency,
    int AnswerCount,
    string MaturityLevel)
{
    public IReadOnlyList<DimensionPreference> Strongest => Dimensions
        .Where(item => item.HasDirectionalSignal)
        .OrderByDescending(item => Math.Abs(item.Weight) * item.Confidence)
        .Take(8)
        .ToArray();
}

public static class ProfileSnapshotBuilder
{
    public static ProfileSnapshot Build(PreferenceModelState state, ConsistencyResult consistency)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(consistency);

        var dimensions = Enum.GetValues<DesignDimension>()
            .Select(dimension =>
            {
                var confidence = state.Confidence(dimension);
                var level = confidence switch
                {
                    >= 0.72d when consistency.HasSufficientEvidence && consistency.AgreementScore >= 0.7d => "stable",
                    >= 0.42d => "moderate",
                    _ => "emerging"
                };

                return new DimensionPreference(
                    dimension,
                    state.Weights[(int)dimension],
                    confidence,
                    level);
            })
            .ToArray();

        var coverage = dimensions.Average(item => item.Confidence);
        var directionalCount = dimensions.Count(item => item.HasDirectionalSignal);
        var stableDirectionalCount = dimensions.Count(item =>
            item.HasDirectionalSignal && item.ConfidenceLevel == "stable");
        var maturity = state.ObservationCount switch
        {
            < 12 => "calibrating",
            _ when directionalCount == 0 => "exploring",
            _ when coverage >= 0.7d && consistency.HasSufficientEvidence && consistency.AgreementScore >= 0.7d && stableDirectionalCount >= 6 => "stable",
            _ when coverage >= 0.4d && directionalCount >= 3 => "developing",
            _ => "exploring"
        };

        return new ProfileSnapshot(dimensions, coverage, consistency, state.ObservationCount, maturity);
    }
}
