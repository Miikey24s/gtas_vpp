using System.Collections.ObjectModel;

namespace DesignDnaStudio.Engine;

public sealed class DesignVector
{
    public static readonly int DimensionCount = Enum.GetValues<DesignDimension>().Length;

    private readonly double[] values;

    public DesignVector(IEnumerable<double> values)
    {
        this.values = values.ToArray();
        if (this.values.Length != DimensionCount)
        {
            throw new ArgumentException($"A design vector must contain exactly {DimensionCount} values.", nameof(values));
        }

        for (var index = 0; index < this.values.Length; index++)
        {
            this.values[index] = Math.Clamp(this.values[index], -1d, 1d);
        }
    }

    public double this[DesignDimension dimension] => values[(int)dimension];

    public IReadOnlyList<double> Values => new ReadOnlyCollection<double>(values);

    public double Dot(IReadOnlyList<double> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count != DimensionCount)
        {
            throw new ArgumentException($"Weights must contain exactly {DimensionCount} values.", nameof(weights));
        }

        var result = 0d;
        for (var index = 0; index < DimensionCount; index++)
        {
            result += values[index] * weights[index];
        }

        return result;
    }

    public double[] Difference(DesignVector other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var result = new double[DimensionCount];
        for (var index = 0; index < DimensionCount; index++)
        {
            result[index] = values[index] - other.values[index];
        }

        return result;
    }

    public double DistanceFrom(DesignVector other)
    {
        var difference = Difference(other);
        return Math.Sqrt(difference.Sum(value => value * value) / DimensionCount);
    }

    public Dictionary<string, double> ToDictionary() =>
        Enum.GetValues<DesignDimension>()
            .ToDictionary(dimension => dimension.ToString(), dimension => this[dimension]);

    public static DesignVector FromDictionary(IReadOnlyDictionary<string, double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new DesignVector(Enum.GetValues<DesignDimension>()
            .Select(dimension => values.TryGetValue(dimension.ToString(), out var value) ? value : 0d));
    }

    public static DesignVector Empty { get; } = new(new double[DimensionCount]);
}
