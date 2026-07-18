using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DesignDnaStudio.Engine;
using DesignDnaStudio.Web.Data.Entities;
using DesignDnaStudio.Web.Infrastructure;
using DesignDnaStudio.Web.Models;

namespace DesignDnaStudio.Web.Data.Seed;

public static class BuiltInStudyCatalog
{
    public const string StudySlug = "gtas-vpp-personal-design-dna";

    public static Study Create()
    {
        var study = new Study
        {
            Id = StableGuid("study:gtas-vpp-personal-design-dna"),
            Name = "GTAS VPP Personal Design DNA",
            Slug = StudySlug,
            Description = "Khảo sát thích nghi để xây Personal Design DNA có bằng chứng trước khi tổng hợp với PPJ Brand DNA và GTAS Product DNA.",
            Status = StudyStatus.Active,
            IsBuiltIn = true,
            MinimumComparisons = 30,
            MaximumComparisons = 60,
            ConsistencyProbeInterval = 10,
            ModelVersion = "ordinal-bt-v4-visual-contract",
            SettingsJson = JsonSerializer.Serialize(new
            {
                coldStartComparisons = 12,
                targetComparisons = 40,
                release = "pairwise-evidence-v1",
                context = "GTAS VPP / PPJ-inspired enterprise operations"
            }, StudioJson.Options)
        };

        var stimuli = new List<Stimulus>();
        var order = 0;

        AddPair(stimuli, study.Id, ref order, "light-dark", StimulusPreviewType.Dashboard,
            "Bright Operations", Spec("denim", "cyan", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 1),
            "Night Command", Spec("denim", "cyan", "dark", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 1));

        AddPair(stimuli, study.Id, ref order, "muted-vivid", StimulusPreviewType.Dashboard,
            "Muted Focus", Spec("slate", "sky", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2),
            "Electric Spectrum", Spec("electric", "lime", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2));

        AddPair(stimuli, study.Id, ref order, "cool-warm", StimulusPreviewType.Dashboard,
            "Cool Technical", Spec("denim", "cyan", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2),
            "Warm Workshop", Spec("canvas", "coral", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2));

        AddPair(stimuli, study.Id, ref order, "angular-rounded", StimulusPreviewType.Form,
            "Technical Cut", Spec("sky", "red", "light", "grid", "square", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "progressive", "form", 2),
            "Soft Layer", Spec("sky", "red", "light", "grid", "large", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "progressive", "form", 2));

        AddPair(stimuli, study.Id, ref order, "clean-texture", StimulusPreviewType.Login,
            "Digital Clean", Spec("denim", "red", "light", "split", "small", "spacious", "neutral", "clean", "border", "illustration", "minimal", "labels", "progressive", "login", 2),
            "Denim Canvas", Spec("denim", "red", "light", "split", "small", "spacious", "neutral", "denim", "border", "illustration", "minimal", "labels", "progressive", "login", 2));

        AddPair(stimuli, study.Id, ref order, "geometric-organic", StimulusPreviewType.Dashboard,
            "Measured Grid", Spec("denim", "teal", "light", "symmetric", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2),
            "Flowing Field", Spec("denim", "teal", "light", "organic", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2));

        AddPair(stimuli, study.Id, ref order, "neutral-editorial-type", StimulusPreviewType.Dashboard,
            "Neutral Scale", Spec("denim", "red", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2),
            "Editorial Signal", Spec("denim", "red", "light", "grid", "small", "balanced", "editorial", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2));

        AddPair(stimuli, study.Id, ref order, "compact-spacious-type", StimulusPreviewType.DataGrid,
            "Compact Control", Spec("slate", "sky", "light", "grid", "small", "dense", "neutral", "clean", "border", "none", "minimal", "labels", "all", "datagrid", 1),
            "Spacious Review", Spec("slate", "sky", "light", "grid", "small", "spacious", "neutral", "clean", "border", "none", "minimal", "labels", "progressive", "datagrid", 1));

        AddPair(stimuli, study.Id, ref order, "grid-bento", StimulusPreviewType.Dashboard,
            "Conventional Grid", Spec("sky", "teal", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 1),
            "Kinetic Bento", Spec("sky", "teal", "light", "bento", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "all", "dashboard", 2));

        AddPair(stimuli, study.Id, ref order, "icon-disclosure", StimulusPreviewType.Dashboard,
            "Icon Console", Spec("denim", "cyan", "light", "grid", "small", "dense", "neutral", "clean", "border", "abstract", "minimal", "icons", "all", "dashboard", 1),
            "Guided Workspace", Spec("denim", "cyan", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "progressive", "dashboard", 2));

        AddPair(stimuli, study.Id, ref order, "illustration-photo", StimulusPreviewType.Login,
            "Abstract Illustration", Spec("denim", "red", "light", "split", "small", "spacious", "neutral", "clean", "border", "illustration", "minimal", "labels", "progressive", "login", 2),
            "Factory Photography", Spec("denim", "red", "light", "split", "small", "spacious", "neutral", "clean", "border", "photo", "minimal", "labels", "progressive", "login", 2));

        AddPair(stimuli, study.Id, ref order, "corporate-local-play", StimulusPreviewType.Login,
            "Global Corporate", Spec("denim", "red", "light", "split", "small", "spacious", "neutral", "clean", "border", "illustration", "minimal", "labels", "progressive", "login", 1),
            "Vietnamese Threadline", Spec("denim", "red", "light", "split", "small", "spacious", "editorial", "denim", "border", "illustration", "thread", "labels", "progressive", "login", 5));

        AddHolistic(stimuli, study.Id, ref order, "Quiet Enterprise",
            Spec("slate", "sky", "light", "grid", "small", "balanced", "neutral", "clean", "border", "abstract", "minimal", "labels", "progressive", "dashboard", 1));
        AddHolistic(stimuli, study.Id, ref order, "Denim Precision",
            Spec("denim", "red", "canvas", "grid", "small", "balanced", "neutral", "denim", "border", "literal", "thread", "labels", "progressive", "dashboard", 3));
        AddHolistic(stimuli, study.Id, ref order, "Threadline Future",
            Spec("electric", "red", "light", "bento", "small", "balanced", "editorial", "denim", "shadow", "illustration", "thread", "labels", "progressive", "dashboard", 4));
        AddHolistic(stimuli, study.Id, ref order, "Industrial Pop",
            Spec("electric", "lime", "dark", "bento", "medium", "dense", "editorial", "clean", "glow", "abstract", "pulse", "labels", "progressive", "dashboard", 5));
        AddHolistic(stimuli, study.Id, ref order, "Vietnamese Atelier",
            Spec("canvas", "coral", "light", "organic", "medium", "spacious", "editorial", "paper", "shadow", "illustration", "flow", "labels", "progressive", "dashboard", 4));
        AddHolistic(stimuli, study.Id, ref order, "Dark Command Center",
            Spec("midnight", "cyan", "dark", "grid", "small", "dense", "neutral", "clean", "glow", "abstract", "minimal", "labels", "all", "dashboard", 3));
        AddHolistic(stimuli, study.Id, ref order, "Sustainable Flow",
            Spec("teal", "lime", "light", "organic", "medium", "balanced", "neutral", "paper", "shadow", "literal", "flow", "labels", "progressive", "dashboard", 3));
        AddHolistic(stimuli, study.Id, ref order, "Editorial Operations",
            Spec("denim", "coral", "light", "bento", "square", "spacious", "editorial", "clean", "border", "photo", "minimal", "labels", "progressive", "dashboard", 4));

        study.Stimuli = stimuli;
        return study;
    }

    private static void AddPair(
        ICollection<Stimulus> stimuli,
        Guid studyId,
        ref int order,
        string key,
        StimulusPreviewType previewType,
        string leftTitle,
        StimulusPreviewSpec leftSpec,
        string rightTitle,
        StimulusPreviewSpec rightSpec)
    {
        var contentFamily = ContextFamily(previewType);
        stimuli.Add(CreateStimulus(studyId, $"{key}:left", leftTitle, StimulusKind.Atomic, previewType, contentFamily, key, leftSpec, order++));
        stimuli.Add(CreateStimulus(studyId, $"{key}:right", rightTitle, StimulusKind.Atomic, previewType, contentFamily, key, rightSpec, order++));
    }

    private static void AddHolistic(
        ICollection<Stimulus> stimuli,
        Guid studyId,
        ref int order,
        string title,
        StimulusPreviewSpec spec)
    {
        stimuli.Add(CreateStimulus(studyId, $"holistic:{title}", title, StimulusKind.Holistic, StimulusPreviewType.Dashboard, ContextFamily(StimulusPreviewType.Dashboard), null, spec, order++));
    }

    private static Stimulus CreateStimulus(
        Guid studyId,
        string stableKey,
        string title,
        StimulusKind kind,
        StimulusPreviewType previewType,
        string contentFamily,
        string? calibrationPairKey,
        StimulusPreviewSpec spec,
        int sortOrder) => new()
        {
            Id = StableGuid($"stimulus:{stableKey}"),
            StudyId = studyId,
            Title = title,
            Description = "Controlled DesignDNA stimulus",
            Kind = kind,
            PreviewType = previewType,
            ContentFamily = contentFamily,
            CalibrationPairKey = calibrationPairKey,
            FeatureVectorJson = JsonSerializer.Serialize(VectorFromSpec(spec).ToDictionary(), StudioJson.Options),
            PreviewSpecJson = JsonSerializer.Serialize(spec, StudioJson.Options),
            SourceReference = "Built-in generated stimulus",
            AssetVersion = "4",
            AccessibilityPass = true,
            IsActive = true,
            SortOrder = sortOrder
        };

    private static DesignVector VectorFromSpec(StimulusPreviewSpec spec)
    {
        var expression = Math.Clamp((spec.ExpressionLevel - 3d) / 2d, -1d, 1d);
        var values = new Dictionary<string, double>
        {
            [nameof(DesignDimension.Darkness)] = spec.Surface switch { "dark" => 0.95d, "canvas" => -0.35d, _ => -0.85d },
            [nameof(DesignDimension.Vividness)] = spec.Palette switch { "electric" => 0.95d, "teal" => 0.45d, "canvas" => -0.15d, "slate" => -0.75d, _ => 0.05d },
            [nameof(DesignDimension.Warmth)] = spec.Palette switch { "canvas" => 0.85d, "teal" => -0.2d, "midnight" => -0.65d, "denim" => -0.45d, _ => -0.15d },
            [nameof(DesignDimension.Contrast)] = spec.Surface == "dark" || spec.Elevation == "glow" ? 0.85d : spec.Palette == "slate" ? -0.25d : 0.2d,
            [nameof(DesignDimension.Multicolor)] = spec.Palette == "electric" ? 0.9d : spec.Accent is "lime" or "coral" ? 0.35d : -0.35d,
            [nameof(DesignDimension.GradientGlow)] = spec.Elevation switch { "glow" => 0.95d, "shadow" => 0.2d, _ => -0.8d },
            [nameof(DesignDimension.Roundedness)] = spec.Radius switch { "square" => -0.95d, "small" => -0.4d, "medium" => 0.35d, "large" => 0.95d, _ => 0d },
            [nameof(DesignDimension.Depth)] = spec.Elevation switch { "glow" => 0.85d, "shadow" => 0.7d, _ => -0.7d },
            [nameof(DesignDimension.ShadowDominance)] = spec.Elevation switch { "glow" => 0.65d, "shadow" => 0.9d, _ => -0.85d },
            [nameof(DesignDimension.MaterialTexture)] = spec.Texture switch { "denim" => 0.95d, "paper" => 0.55d, _ => -0.9d },
            [nameof(DesignDimension.OrganicForm)] = spec.Layout switch { "organic" => 0.95d, "bento" => 0.25d, "symmetric" => -0.95d, "grid" => -0.8d, _ => -0.45d },
            [nameof(DesignDimension.Asymmetry)] = spec.Layout switch { "organic" => 0.85d, "bento" => 0.75d, "symmetric" => -0.95d, "grid" => -0.7d, _ => -0.25d },
            [nameof(DesignDimension.EditorialTypography)] = spec.Typography == "editorial" ? 0.95d : -0.85d,
            [nameof(DesignDimension.TypeWeight)] = spec.Typography == "editorial" ? 0.7d : -0.15d,
            [nameof(DesignDimension.TypeSpacing)] = spec.Density switch { "spacious" => 0.85d, "dense" => -0.8d, _ => 0d },
            [nameof(DesignDimension.OversizedDisplay)] = spec.Typography == "editorial" ? 0.85d : -0.65d,
            [nameof(DesignDimension.Density)] = spec.Density switch { "dense" => 0.95d, "spacious" => -0.9d, _ => 0d },
            [nameof(DesignDimension.ExperimentalLayout)] = spec.Layout switch { "bento" => 0.95d, "organic" => 0.75d, "grid" => -0.8d, "symmetric" => -0.9d, _ => -0.55d },
            [nameof(DesignDimension.DynamicLayering)] = spec.Layout is "bento" or "organic" || spec.Elevation is "shadow" or "glow" ? 0.75d : -0.65d,
            [nameof(DesignDimension.ExplicitLabels)] = spec.LabelMode == "labels" ? 0.9d : -0.9d,
            [nameof(DesignDimension.ProgressiveDisclosure)] = spec.Disclosure == "progressive" ? 0.9d : -0.85d,
            [nameof(DesignDimension.LiteralImagery)] = spec.Imagery switch { "photo" => 0.95d, "literal" => 0.75d, "illustration" => 0.25d, "none" => -0.65d, _ => -0.9d },
            [nameof(DesignDimension.Photography)] = spec.Imagery == "photo" ? 0.95d : -0.9d,
            [nameof(DesignDimension.Playfulness)] = Math.Clamp(expression + (spec.Motion is "pulse" or "flow" ? 0.2d : 0d), -1d, 1d),
            [nameof(DesignDimension.ExpressiveMotion)] = spec.Motion switch { "pulse" => 0.95d, "thread" => 0.8d, "flow" => 0.7d, _ => -0.9d },
            [nameof(DesignDimension.TextileMateriality)] = spec.Texture == "denim" || spec.Motion == "thread" ? 0.95d : spec.Texture == "paper" ? 0.2d : -0.9d,
            [nameof(DesignDimension.IndustrialPrecision)] = (spec.Layout is "grid" or "symmetric") && spec.Texture == "clean" ? 0.9d : spec.Layout == "organic" ? -0.45d : 0.25d,
            [nameof(DesignDimension.LocalVietnameseExpression)] = spec.Motion == "thread" ? 0.95d : spec.Texture == "denim" && spec.Imagery == "illustration" ? 0.55d : -0.45d
        };

        return DesignVector.FromDictionary(values);
    }

    private static string ContextFamily(StimulusPreviewType previewType) => $"context:{previewType.ToString().ToLowerInvariant()}";

    private static StimulusPreviewSpec Spec(
        string palette,
        string accent,
        string surface,
        string layout,
        string radius,
        string density,
        string typography,
        string texture,
        string elevation,
        string imagery,
        string motion,
        string labelMode,
        string disclosure,
        string context,
        int expressionLevel) => new(
            palette, accent, surface, layout, radius, density, typography, texture,
            elevation, imagery, motion, labelMode, disclosure, context, expressionLevel);

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
