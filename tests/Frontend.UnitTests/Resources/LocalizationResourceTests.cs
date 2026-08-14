using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace gtas_vpp_fe.Tests.Resources;

public sealed partial class LocalizationResourceTests
{
    [Fact]
    public void CompactPeriodResources_DistinguishLockedPricingAndSettledStates()
    {
        var root = FindRepositoryRoot();
        var resourceDirectory = Path.Combine(root, "src", "Frontend", "Blazor", "Resources");
        var vietnameseValues = ReadValues(Path.Combine(resourceDirectory, "Components.App.resx"));
        var englishValues = ReadValues(Path.Combine(resourceDirectory, "Components.App.en.resx"));

        Assert.Equal("Đã đóng", vietnameseValues["PeriodStateSubmissionClosedCompact"]);
        Assert.Equal("Đang chốt", vietnameseValues["PeriodStatePricingCompact"]);
        Assert.Equal("Đã chốt", vietnameseValues["PeriodStateSettledCompact"]);
        Assert.Equal("Closed", englishValues["PeriodStateSubmissionClosedCompact"]);
        Assert.Equal("Settling", englishValues["PeriodStatePricingCompact"]);
        Assert.Equal("Settled", englishValues["PeriodStateSettledCompact"]);
    }

    [Fact]
    public void VietnameseResources_DoNotLeaveEnglishInterfaceCopy()
    {
        var root = FindRepositoryRoot();
        var resourceDirectory = Path.Combine(
            root, "src", "Frontend", "Blazor", "Resources");
        var vietnameseValues = ReadValues(Path.Combine(resourceDirectory, "Components.App.resx"));
        var englishValues = ReadValues(Path.Combine(resourceDirectory, "Components.App.en.resx"));
        var allowedSharedValues = new HashSet<string>(StringComparer.Ordinal)
        {
            "AppName",
            "ConfirmEmailAccent",
            "Email",
            "IsDeleted",
            "AiGenerated",
            "PriceListDataSourceCsv",
            "PriceListDataSourceExcel",
            "SettlementVatAmount",
            "SystemId"
        };

        var offenders = vietnameseValues
            .Where(pair => englishValues.TryGetValue(pair.Key, out var englishValue)
                && pair.Value.Equals(englishValue, StringComparison.Ordinal)
                && !allowedSharedValues.Contains(pair.Key))
            .Select(pair => $"{pair.Key}={pair.Value}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"Vietnamese resources still contain untranslated English UI copy: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void ComponentResources_ArePairedUniqueAndCoverLiteralKeys()
    {
        var root = FindRepositoryRoot();
        var resourceDirectory = Path.Combine(
            root, "src", "Frontend", "Blazor", "Resources");
        var componentDirectory = Path.Combine(
            root, "src", "Frontend", "Blazor", "Components");

        var vietnameseKeys = ReadKeys(Path.Combine(resourceDirectory, "Components.App.resx"));
        var englishKeys = ReadKeys(Path.Combine(resourceDirectory, "Components.App.en.resx"));

        Assert.Equal(vietnameseKeys, englishKeys);
        Assert.False(File.Exists(Path.Combine(resourceDirectory, "App.resx")));
        Assert.False(File.Exists(Path.Combine(resourceDirectory, "App.en.resx")));

        var missingKeys = Directory
            .EnumerateFiles(componentDirectory, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => LiteralLocalizationKey().Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .Where(key => !vietnameseKeys.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.True(missingKeys.Count == 0, $"Missing localization keys: {string.Join(", ", missingKeys)}");
    }

    private static SortedSet<string> ReadKeys(string path)
    {
        var keys = XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(element => (string?)element.Attribute("name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        return new SortedSet<string>(keys, StringComparer.Ordinal);
    }

    private static Dictionary<string, string> ReadValues(string path)
    {
        return XDocument.Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }

    [GeneratedRegex("Loc\\[\"([^\"]+)\"\\]", RegexOptions.CultureInvariant)]
    private static partial Regex LiteralLocalizationKey();
}
