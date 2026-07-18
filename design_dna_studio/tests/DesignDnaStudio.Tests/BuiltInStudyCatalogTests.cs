using System.Text.Json;
using DesignDnaStudio.Engine;
using DesignDnaStudio.Web.Data.Entities;
using DesignDnaStudio.Web.Data.Seed;

namespace DesignDnaStudio.Tests;

public sealed class BuiltInStudyCatalogTests
{
    [Fact]
    public void CalibrationPairs_AreControlledAndFullyRepresented()
    {
        var study = BuiltInStudyCatalog.Create();
        var pairs = study.Stimuli
            .Where(item => item.CalibrationPairKey is not null)
            .GroupBy(item => item.CalibrationPairKey)
            .ToArray();

        Assert.Equal(12, pairs.Length);
        foreach (var pair in pairs)
        {
            var stimuli = pair.OrderBy(item => item.SortOrder).ToArray();
            Assert.Equal(2, stimuli.Length);
            Assert.Equal(stimuli[0].ContentFamily, stimuli[1].ContentFamily);

            var differenceCount = ReadVector(stimuli[0].FeatureVectorJson)
                .Difference(ReadVector(stimuli[1].FeatureVectorJson))
                .Count(value => Math.Abs(value) >= 0.05d);
            Assert.InRange(differenceCount, 1, 12);
        }
    }

    [Fact]
    public void AdaptivePool_SupportsDeepAssessmentWithoutRepeatingOrdinaryPairs()
    {
        var study = BuiltInStudyCatalog.Create();
        var comparablePairCount = study.Stimuli
            .GroupBy(item => item.ContentFamily)
            .Sum(group => group.Count() * (group.Count() - 1) / 2);

        Assert.True(comparablePairCount >= study.MaximumComparisons * 2);
        Assert.Equal("ordinal-bt-v4-visual-contract", study.ModelVersion);
        Assert.All(study.Stimuli, item => Assert.Equal("4", item.AssetVersion));
    }

    [Fact]
    public void FeatureVectors_AreDerivedFromRenderedSpecifications()
    {
        var study = BuiltInStudyCatalog.Create();

        Assert.Equal(
            new[] { DesignDimension.Roundedness },
            ChangedDimensions(study, "angular-rounded"));
        Assert.DoesNotContain(
            DesignDimension.GradientGlow,
            ChangedDimensions(study, "muted-vivid"));
    }

    [Fact]
    public void Renderer_DefinesVisualContractsForVectorDrivingFields()
    {
        var studioRoot = FindStudioRoot();
        var renderer = File.ReadAllText(Path.Combine(
            studioRoot,
            "src",
            "DesignDnaStudio.Web",
            "Components",
            "Shared",
            "StimulusPreview.razor"));
        var styles = File.ReadAllText(Path.Combine(
            studioRoot,
            "src",
            "DesignDnaStudio.Web",
            "wwwroot",
            "app.css"));

        Assert.Contains("dna-surface-{Spec.Surface}", renderer, StringComparison.Ordinal);
        Assert.Contains("dna-imagery-{Spec.Imagery}", renderer, StringComparison.Ordinal);
        Assert.Contains("dna-disclosure-{Spec.Disclosure}", renderer, StringComparison.Ordinal);
        Assert.Contains("dna-expression-{Spec.ExpressionLevel}", renderer, StringComparison.Ordinal);

        foreach (var selector in new[]
                 {
                     ".dna-surface-light", ".dna-surface-dark", ".dna-surface-canvas",
                     ".dna-imagery-photo", ".dna-imagery-illustration", ".dna-imagery-abstract",
                     ".dna-imagery-literal", ".dna-imagery-none",
                     ".dna-disclosure-progressive", ".dna-expression-1", ".dna-expression-5"
                 })
        {
            Assert.Contains(selector, styles, StringComparison.Ordinal);
        }
    }

    private static IReadOnlyList<DesignDimension> ChangedDimensions(
        Study study,
        string calibrationPairKey)
    {
        var pair = study.Stimuli
            .Where(item => item.CalibrationPairKey == calibrationPairKey)
            .OrderBy(item => item.SortOrder)
            .ToArray();
        var difference = ReadVector(pair[0].FeatureVectorJson).Difference(ReadVector(pair[1].FeatureVectorJson));
        return Enum.GetValues<DesignDimension>()
            .Where(dimension => Math.Abs(difference[(int)dimension]) >= 0.05d)
            .ToArray();
    }

    private static DesignVector ReadVector(string json)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
        return DesignVector.FromDictionary(values ?? []);
    }

    private static string FindStudioRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DesignDnaStudio.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the DesignDNA Studio source root.");
    }
}
