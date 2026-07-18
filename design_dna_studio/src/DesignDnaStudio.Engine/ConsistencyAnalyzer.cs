namespace DesignDnaStudio.Engine;

public sealed record ConsistencyResult(
    int ProbeCount,
    double AgreementScore,
    double LeftSelectionRate,
    bool HasPotentialSideBias)
{
    public bool HasSufficientEvidence => ProbeCount >= 2;
}

public static class ConsistencyAnalyzer
{
    public static ConsistencyResult Analyze(IReadOnlyList<ComparisonHistory> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var originals = history
            .Where(item => !item.IsConsistencyProbe)
            .ToDictionary(item => item.Id);

        var probeCount = 0;
        var agreementTotal = 0d;

        foreach (var probe in history.Where(item => item.IsConsistencyProbe && item.ProbeOfComparisonId.HasValue))
        {
            if (!originals.TryGetValue(probe.ProbeOfComparisonId!.Value, out var original))
            {
                continue;
            }

            if (original.Rating is null || probe.Rating is null)
            {
                continue;
            }

            probeCount++;
            var normalizedProbeRating = -(int)probe.Rating.Value;
            agreementTotal += 1d - Math.Abs((int)original.Rating.Value - normalizedProbeRating) / 4d;
        }

        var answered = history.Where(item => item.Rating is not null).ToArray();
        var leftSelections = answered.Count(item => (int)item.Rating!.Value > 0);
        var rightSelections = answered.Count(item => (int)item.Rating!.Value < 0);
        var directionalCount = leftSelections + rightSelections;
        var leftSelectionRate = directionalCount == 0 ? 0.5d : (double)leftSelections / directionalCount;

        return new ConsistencyResult(
            probeCount,
            probeCount == 0 ? 0d : agreementTotal / probeCount,
            leftSelectionRate,
            directionalCount >= 20 && Math.Abs(leftSelectionRate - 0.5d) > 0.15d);
    }
}
