namespace gtas_vpp_shared.DTOs.Res.Reports;

public static class ReportInsightSources
{
    public const string Rules = "rules";
    public const string OpenAI = "openai";
}

public sealed class ReportInsightResDTO
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Highlights { get; set; } = [];
    public List<string> Risks { get; set; } = [];
    public List<string> Recommendations { get; set; } = [];
    public string Source { get; set; } = ReportInsightSources.Rules;
    public string? Model { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool IsAiGenerated => Source == ReportInsightSources.OpenAI;
}
