namespace DesignDnaStudio.Engine;

public sealed record StimulusCandidate(
    Guid Id,
    DesignVector Features,
    int TimesShown = 0,
    string ContentFamily = "dashboard",
    string? CalibrationPairKey = null);

public sealed record ComparisonHistory(
    Guid Id,
    Guid LeftStimulusId,
    Guid RightStimulusId,
    PreferenceRating? Rating,
    bool IsConsistencyProbe = false,
    Guid? ProbeOfComparisonId = null);

public sealed record SelectedPair(
    Guid LeftStimulusId,
    Guid RightStimulusId,
    bool IsConsistencyProbe,
    Guid? ProbeOfComparisonId,
    double InformationScore);

public sealed class AdaptivePairSelector(PreferenceLearner learner)
{
    public const int CalibrationAnswerTarget = 12;

    public SelectedPair? SelectNext(
        IReadOnlyList<StimulusCandidate> stimuli,
        PreferenceModelState state,
        IReadOnlyList<ComparisonHistory> history,
        int sequence,
        int consistencyProbeInterval = 10)
    {
        ArgumentNullException.ThrowIfNull(stimuli);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(history);

        if (stimuli.Count < 2)
        {
            return null;
        }

        var probe = SelectConsistencyProbe(history, state.ObservationCount, consistencyProbeInterval);
        if (probe is not null)
        {
            return probe;
        }

        var seen = history
            .Where(item => !item.IsConsistencyProbe)
            .Select(item => Normalize(item.LeftStimulusId, item.RightStimulusId))
            .ToHashSet();
        var calibrationMode = state.ObservationCount < CalibrationAnswerTarget &&
                              HasUnseenCalibrationPair(stimuli, seen);

        var candidates = new List<SelectedPair>();
        for (var leftIndex = 0; leftIndex < stimuli.Count - 1; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < stimuli.Count; rightIndex++)
            {
                var left = stimuli[leftIndex];
                var right = stimuli[rightIndex];
                if (!string.Equals(left.ContentFamily, right.ContentFamily, StringComparison.Ordinal))
                {
                    continue;
                }

                if (calibrationMode &&
                    (string.IsNullOrWhiteSpace(left.CalibrationPairKey) ||
                     !string.Equals(left.CalibrationPairKey, right.CalibrationPairKey, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (seen.Contains(Normalize(left.Id, right.Id)))
                {
                    continue;
                }

                var probability = learner.ProbabilityLeftPreferred(left.Features, right.Features, state);
                var uncertainty = 1d - Math.Abs(probability - 0.5d) * 2d;
                var diversity = Math.Clamp(left.Features.DistanceFrom(right.Features), 0d, 1d);
                var exposure = 1d / (1d + Math.Min(left.TimesShown, right.TimesShown) * 0.3d);
                var coverage = DimensionCoverage(left.Features, right.Features, state);
                var recentPenalty = RecentDimensionPenalty(left.Features, right.Features, stimuli, history);
                var score = uncertainty * 0.46d + diversity * 0.23d + coverage * 0.21d + exposure * 0.10d - recentPenalty;

                var swap = sequence % 2 != 0;
                candidates.Add(new SelectedPair(
                    swap ? right.Id : left.Id,
                    swap ? left.Id : right.Id,
                    false,
                    null,
                    score));
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        var top = candidates.OrderByDescending(item => item.InformationScore).Take(5).ToArray();
        var deterministicSeed = unchecked(
            (sequence * 73_856_093) ^
            (state.ObservationCount * 19_349_663) ^
            (history.Count * 83_492_791));
        var deterministicIndex = (deterministicSeed & int.MaxValue) % top.Length;
        return top[deterministicIndex];
    }

    private static bool HasUnseenCalibrationPair(
        IReadOnlyList<StimulusCandidate> stimuli,
        IReadOnlySet<(Guid First, Guid Second)> seen)
    {
        for (var leftIndex = 0; leftIndex < stimuli.Count - 1; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < stimuli.Count; rightIndex++)
            {
                var left = stimuli[leftIndex];
                var right = stimuli[rightIndex];
                if (!string.IsNullOrWhiteSpace(left.CalibrationPairKey) &&
                    string.Equals(left.CalibrationPairKey, right.CalibrationPairKey, StringComparison.Ordinal) &&
                    string.Equals(left.ContentFamily, right.ContentFamily, StringComparison.Ordinal) &&
                    !seen.Contains(Normalize(left.Id, right.Id)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static SelectedPair? SelectConsistencyProbe(
        IReadOnlyList<ComparisonHistory> history,
        int answerCount,
        int consistencyProbeInterval)
    {
        if (answerCount < CalibrationAnswerTarget)
        {
            return null;
        }

        var interval = Math.Max(1, consistencyProbeInterval);
        var dueProbeCount = 1 + (answerCount - CalibrationAnswerTarget) / interval;
        if (history.Count(item => item.IsConsistencyProbe) >= dueProbeCount)
        {
            return null;
        }

        var repeatedIds = history
            .Where(item => item.IsConsistencyProbe && item.ProbeOfComparisonId.HasValue)
            .Select(item => item.ProbeOfComparisonId!.Value)
            .ToHashSet();
        var answeredHistory = history.Where(item => item.Rating is not null).ToArray();

        var source = answeredHistory
            .Select((item, index) => new { Item = item, Index = index })
            .Where(entry => !entry.Item.IsConsistencyProbe && !repeatedIds.Contains(entry.Item.Id))
            .Where(entry => entry.Item.Rating is not null and not PreferenceRating.Tie)
            .Where(entry => answeredHistory.Length - entry.Index >= 8)
            .OrderBy(entry => entry.Item.Id)
            .Select(entry => entry.Item)
            .FirstOrDefault();

        return source is null
            ? null
            : new SelectedPair(source.RightStimulusId, source.LeftStimulusId, true, source.Id, 1d);
    }

    private static double RecentDimensionPenalty(
        DesignVector left,
        DesignVector right,
        IReadOnlyList<StimulusCandidate> stimuli,
        IReadOnlyList<ComparisonHistory> history)
    {
        if (history.Count == 0)
        {
            return 0d;
        }

        var lookup = stimuli.ToDictionary(item => item.Id);
        var recent = history.Where(item => item.Rating is not null).TakeLast(3);
        var currentDifference = left.Difference(right).Select(Math.Abs).ToArray();
        var overlap = 0d;
        var comparisons = 0;

        foreach (var item in recent)
        {
            if (!lookup.TryGetValue(item.LeftStimulusId, out var recentLeft) ||
                !lookup.TryGetValue(item.RightStimulusId, out var recentRight))
            {
                continue;
            }

            var recentDifference = recentLeft.Features.Difference(recentRight.Features).Select(Math.Abs).ToArray();
            overlap += currentDifference.Zip(recentDifference).Sum(pair => Math.Min(pair.First, pair.Second)) /
                       Math.Max(1d, currentDifference.Sum());
            comparisons++;
        }

        return comparisons == 0 ? 0d : Math.Clamp(overlap / comparisons * 0.12d, 0d, 0.12d);
    }

    private static double DimensionCoverage(DesignVector left, DesignVector right, PreferenceModelState state)
    {
        var difference = left.Difference(right);
        var weightedCoverage = 0d;
        var totalDifference = 0d;

        for (var index = 0; index < difference.Length; index++)
        {
            var magnitude = Math.Abs(difference[index]);
            var confidence = 1d - Math.Clamp(1d - Math.Exp(-Math.Max(0d, state.Information[index]) / 2.5d), 0d, 1d);
            weightedCoverage += magnitude * confidence;
            totalDifference += magnitude;
        }

        return totalDifference <= double.Epsilon ? 0d : Math.Clamp(weightedCoverage / totalDifference, 0d, 1d);
    }

    private static (Guid First, Guid Second) Normalize(Guid first, Guid second) =>
        first.CompareTo(second) <= 0 ? (first, second) : (second, first);
}
