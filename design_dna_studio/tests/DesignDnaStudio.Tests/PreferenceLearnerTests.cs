using DesignDnaStudio.Engine;

namespace DesignDnaStudio.Tests;

public sealed class PreferenceLearnerTests
{
    private readonly PreferenceLearner learner = new();

    [Fact]
    public void Probability_IsSymmetric_WhenSideBiasIsZero()
    {
        var state = new PreferenceModelState();
        state.Weights[(int)DesignDimension.Darkness] = 0.8d;
        state.Weights[(int)DesignDimension.Vividness] = -0.35d;
        var left = Vector((DesignDimension.Darkness, 0.9d), (DesignDimension.Vividness, -0.4d));
        var right = Vector((DesignDimension.Darkness, -0.6d), (DesignDimension.Vividness, 0.7d));

        var leftProbability = learner.ProbabilityLeftPreferred(left, right, state);
        var rightProbability = learner.ProbabilityLeftPreferred(right, left, state);

        Assert.Equal(1d, leftProbability + rightProbability, 10);
    }

    [Fact]
    public void StrongPreference_UpdatesMoreThanSlightPreference()
    {
        var left = Vector((DesignDimension.ExperimentalLayout, 1d));
        var right = Vector((DesignDimension.ExperimentalLayout, -1d));
        var strongState = new PreferenceModelState();
        var slightState = new PreferenceModelState();

        learner.Update(strongState, new PreferenceObservation(left, right, PreferenceRating.StrongLeft));
        learner.Update(slightState, new PreferenceObservation(left, right, PreferenceRating.SlightLeft));

        Assert.True(strongState.Weights[(int)DesignDimension.ExperimentalLayout] >
                    slightState.Weights[(int)DesignDimension.ExperimentalLayout]);
    }

    [Fact]
    public void Tie_ReducesExistingMargin()
    {
        var left = Vector((DesignDimension.Roundedness, 1d));
        var right = Vector((DesignDimension.Roundedness, -1d));
        var weights = new double[DesignVector.DimensionCount];
        weights[(int)DesignDimension.Roundedness] = 1.6d;
        var state = new PreferenceModelState(weights, new double[DesignVector.DimensionCount], 6);

        learner.Update(state, new PreferenceObservation(left, right, PreferenceRating.Tie, TieReason.NoVisibleDifference));

        Assert.True(state.Weights[(int)DesignDimension.Roundedness] < 1.6d);
    }

    [Fact]
    public void Skip_DoesNotChangeModel()
    {
        var state = new PreferenceModelState();
        var before = state.Weights.ToArray();

        learner.Update(state, new PreferenceObservation(
            Vector((DesignDimension.Density, 1d)),
            Vector((DesignDimension.Density, -1d)),
            null,
            SkipReason: SkipReason.CannotEvaluate));

        Assert.Equal(0, state.ObservationCount);
        Assert.Equal(before, state.Weights);
    }

    [Fact]
    public void RepeatedConsistentChoices_LearnExpectedDirection()
    {
        var state = new PreferenceModelState();
        var left = Vector((DesignDimension.TextileMateriality, 1d));
        var right = Vector((DesignDimension.TextileMateriality, -1d));

        for (var index = 0; index < 20; index++)
        {
            learner.Update(state, new PreferenceObservation(left, right, PreferenceRating.StrongLeft));
        }

        Assert.True(state.Weights[(int)DesignDimension.TextileMateriality] > 0.8d);
        Assert.True(learner.ProbabilityLeftPreferred(left, right, state) > 0.8d);
        Assert.True(state.Confidence(DesignDimension.TextileMateriality) > 0.5d);
    }

    [Fact]
    public void RatingProbabilities_SumToOne()
    {
        var probabilities = learner.RatingProbabilities(
            Vector((DesignDimension.Contrast, 1d)),
            Vector((DesignDimension.Contrast, -1d)),
            new PreferenceModelState());

        Assert.Equal(1d, probabilities.Values.Sum(), 10);
        Assert.All(probabilities.Values, value => Assert.InRange(value, 0d, 1d));
    }

    private static DesignVector Vector(params (DesignDimension Dimension, double Value)[] values)
    {
        var dictionary = values.ToDictionary(item => item.Dimension.ToString(), item => item.Value);
        return DesignVector.FromDictionary(dictionary);
    }
}
