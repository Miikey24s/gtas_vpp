using DesignDnaStudio.Engine;

namespace DesignDnaStudio.Tests;

public sealed class AdaptivePairSelectorTests
{
    private readonly PreferenceLearner learner = new();

    [Fact]
    public void ColdStart_OnlySelectsControlledCalibrationPair()
    {
        var selector = new AdaptivePairSelector(learner);
        var stimuli = new[]
        {
            Candidate(1, "pair-a", DesignDimension.Darkness, -1d),
            Candidate(2, "pair-a", DesignDimension.Darkness, 1d),
            Candidate(3, "pair-b", DesignDimension.Roundedness, -1d),
            Candidate(4, "pair-b", DesignDimension.Roundedness, 1d)
        };

        var selected = selector.SelectNext(stimuli, new PreferenceModelState(), [], 0);

        Assert.NotNull(selected);
        var left = stimuli.Single(item => item.Id == selected.LeftStimulusId);
        var right = stimuli.Single(item => item.Id == selected.RightStimulusId);
        Assert.Equal(left.CalibrationPairKey, right.CalibrationPairKey);
    }

    [Fact]
    public void SeenPair_IsNotSelectedAgainOutsideProbe()
    {
        var selector = new AdaptivePairSelector(learner);
        var stimuli = new[]
        {
            Candidate(1, "pair-a", DesignDimension.Darkness, -1d),
            Candidate(2, "pair-a", DesignDimension.Darkness, 1d),
            Candidate(3, "pair-b", DesignDimension.Roundedness, -1d),
            Candidate(4, "pair-b", DesignDimension.Roundedness, 1d)
        };
        var seen = new ComparisonHistory(Guid.NewGuid(), stimuli[0].Id, stimuli[1].Id, PreferenceRating.SlightLeft);

        var selected = selector.SelectNext(stimuli, new PreferenceModelState(), [seen], 1);

        Assert.NotNull(selected);
        Assert.DoesNotContain(
            new[] { selected.LeftStimulusId, selected.RightStimulusId },
            id => id == stimuli[0].Id && new[] { selected.LeftStimulusId, selected.RightStimulusId }.Contains(stimuli[1].Id));
        Assert.Equal("pair-b", stimuli.Single(item => item.Id == selected.LeftStimulusId).CalibrationPairKey);
    }

    [Fact]
    public void Probe_IsMirroredAfterColdStart()
    {
        var selector = new AdaptivePairSelector(learner);
        var first = Candidate(1, null, DesignDimension.Darkness, -1d, "holistic");
        var second = Candidate(2, null, DesignDimension.Darkness, 1d, "holistic");
        var history = Enumerable.Range(0, 12)
            .Select(index => new ComparisonHistory(
                GuidFrom(index + 100),
                index == 0 ? first.Id : GuidFrom(index + 200),
                index == 0 ? second.Id : GuidFrom(index + 300),
                PreferenceRating.SlightLeft))
            .ToArray();

        var state = new PreferenceModelState(
            new double[DesignVector.DimensionCount],
            new double[DesignVector.DimensionCount],
            AdaptivePairSelector.CalibrationAnswerTarget);
        var selected = selector.SelectNext([first, second], state, history, 12);

        Assert.NotNull(selected);
        Assert.True(selected.IsConsistencyProbe);
        Assert.Equal(second.Id, selected.LeftStimulusId);
        Assert.Equal(first.Id, selected.RightStimulusId);
        Assert.Equal(history[0].Id, selected.ProbeOfComparisonId);
    }

    [Fact]
    public void SkippedEvents_DoNotScheduleConsistencyProbe()
    {
        var selector = new AdaptivePairSelector(learner);
        var first = Candidate(1, null, DesignDimension.Darkness, -1d, "holistic");
        var second = Candidate(2, null, DesignDimension.Darkness, 1d, "holistic");
        var skippedHistory = Enumerable.Range(0, 12)
            .Select(index => new ComparisonHistory(
                GuidFrom(index + 100),
                GuidFrom(index + 200),
                GuidFrom(index + 300),
                null))
            .ToArray();

        var selected = selector.SelectNext([first, second], new PreferenceModelState(), skippedHistory, 12);

        Assert.NotNull(selected);
        Assert.False(selected.IsConsistencyProbe);
    }

    private static StimulusCandidate Candidate(
        int id,
        string? pair,
        DesignDimension dimension,
        double value,
        string family = "calibration") => new(
            GuidFrom(id),
            DesignVector.FromDictionary(new Dictionary<string, double> { [dimension.ToString()] = value }),
            0,
            family,
            pair);

    private static Guid GuidFrom(int value)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}
