namespace DesignDnaStudio.Engine;

public sealed class PreferenceModelState
{
    public PreferenceModelState()
        : this(new double[DesignVector.DimensionCount], new double[DesignVector.DimensionCount], 0)
    {
    }

    public PreferenceModelState(
        IEnumerable<double> weights,
        IEnumerable<double> information,
        int observationCount,
        double sideBias = 0d,
        double sideBiasInformation = 0d)
    {
        Weights = weights.ToArray();
        Information = information.ToArray();

        if (Weights.Length != DesignVector.DimensionCount || Information.Length != DesignVector.DimensionCount)
        {
            throw new ArgumentException("Preference model arrays do not match the design dimension count.");
        }

        ObservationCount = Math.Max(0, observationCount);
        SideBias = sideBias;
        SideBiasInformation = Math.Max(0d, sideBiasInformation);
    }

    public double[] Weights { get; }

    public double[] Information { get; }

    public int ObservationCount { get; private set; }

    public double SideBias { get; internal set; }

    public double SideBiasInformation { get; internal set; }

    internal void IncrementObservationCount() => ObservationCount++;

    public double Confidence(DesignDimension dimension)
    {
        var information = Math.Max(0d, Information[(int)dimension]);
        return Math.Clamp(1d - Math.Exp(-information / 2.5d), 0d, 1d);
    }
}
