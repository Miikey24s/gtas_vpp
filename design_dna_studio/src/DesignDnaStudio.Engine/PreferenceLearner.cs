namespace DesignDnaStudio.Engine;

public sealed class PreferenceLearner
{
    private const double L2Regularization = 0.025d;
    private const double SideBiasRegularization = 0.08d;
    private const double BaseLearningRate = 0.18d;
    private const double InnerThreshold = 0.55d;
    private const double OuterThreshold = 1.45d;

    public double Score(DesignVector vector, PreferenceModelState state)
    {
        ArgumentNullException.ThrowIfNull(vector);
        ArgumentNullException.ThrowIfNull(state);
        return vector.Dot(state.Weights);
    }

    public double ProbabilityLeftPreferred(DesignVector left, DesignVector right, PreferenceModelState state)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentNullException.ThrowIfNull(state);

        var margin = Dot(left.Difference(right), state.Weights) + state.SideBias;
        return Sigmoid(margin);
    }

    public void Update(PreferenceModelState state, PreferenceObservation observation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(observation);

        if (observation.IsSkipped)
        {
            return;
        }

        var difference = observation.Left.Difference(observation.Right);
        var margin = Dot(difference, state.Weights) + state.SideBias;
        var (probability, logGradient) = OrdinalProbabilityAndGradient(observation.Rating!.Value, margin);
        var learningRate = BaseLearningRate / Math.Sqrt(1d + state.ObservationCount * 0.06d);

        for (var index = 0; index < DesignVector.DimensionCount; index++)
        {
            var gradient = logGradient * difference[index];
            state.Weights[index] += learningRate * (gradient - L2Regularization * state.Weights[index]);

            var observedInformation = Math.Max(0.0001d, probability) * logGradient * logGradient * difference[index] * difference[index];
            state.Information[index] += observedInformation;
        }

        state.SideBias += learningRate * (logGradient - SideBiasRegularization * state.SideBias);
        state.SideBiasInformation += Math.Max(0.0001d, probability) * logGradient * logGradient;

        state.IncrementObservationCount();
    }

    public IReadOnlyDictionary<PreferenceRating, double> RatingProbabilities(
        DesignVector left,
        DesignVector right,
        PreferenceModelState state)
    {
        var margin = Dot(left.Difference(right), state.Weights) + state.SideBias;
        return Enum.GetValues<PreferenceRating>()
            .ToDictionary(rating => rating, rating => OrdinalProbabilityAndGradient(rating, margin).Probability);
    }

    private static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        var result = 0d;
        for (var index = 0; index < left.Count; index++)
        {
            result += left[index] * right[index];
        }

        return result;
    }

    private static double Sigmoid(double value)
    {
        if (value >= 0d)
        {
            var exponent = Math.Exp(-value);
            return 1d / (1d + exponent);
        }

        var negativeExponent = Math.Exp(value);
        return negativeExponent / (1d + negativeExponent);
    }

    private static (double Probability, double LogGradient) OrdinalProbabilityAndGradient(
        PreferenceRating rating,
        double margin)
    {
        var category = (int)rating + 2;
        var boundaries = new[]
        {
            double.NegativeInfinity,
            -OuterThreshold,
            -InnerThreshold,
            InnerThreshold,
            OuterThreshold,
            double.PositiveInfinity
        };

        var lower = boundaries[category];
        var upper = boundaries[category + 1];
        var lowerCdf = double.IsNegativeInfinity(lower) ? 0d : Sigmoid(lower - margin);
        var upperCdf = double.IsPositiveInfinity(upper) ? 1d : Sigmoid(upper - margin);
        var probability = Math.Max(1e-9d, upperCdf - lowerCdf);

        var lowerDerivative = double.IsNegativeInfinity(lower) ? 0d : lowerCdf * (1d - lowerCdf);
        var upperDerivative = double.IsPositiveInfinity(upper) ? 0d : upperCdf * (1d - upperCdf);
        var probabilityDerivative = lowerDerivative - upperDerivative;

        return (probability, probabilityDerivative / probability);
    }
}
