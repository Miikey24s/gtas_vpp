using System.Text;
using DesignDnaStudio.Engine;

namespace DesignDnaStudio.Web.Services.Assessment;

public sealed class DeterministicDesignProfileNarrativeService : IDesignProfileNarrativeService
{
    public Task<string> CreateAsync(ProfileSnapshot profile, CancellationToken cancellationToken = default)
    {
        var strong = profile.Strongest.Where(item => item.Confidence >= 0.22d).Take(6).ToArray();
        var builder = new StringBuilder();
        builder.AppendLine("## Personal Design DNA");
        builder.AppendLine();

        if (strong.Length == 0)
        {
            builder.AppendLine("Hồ sơ vẫn đang ở giai đoạn khám phá. Cần thêm lựa chọn trực quan trước khi khóa art direction.");
            return Task.FromResult(builder.ToString());
        }

        builder.AppendLine("Các tín hiệu nổi bật hiện tại:");
        builder.AppendLine();
        foreach (var item in strong)
        {
            var definition = DesignDimensionCatalog.Get(item.Dimension);
            var preferredPole = item.Weight >= 0d ? definition.PositivePole : definition.NegativePole;
            builder.Append("- **")
                .Append(preferredPole)
                .Append("** — ")
                .Append(definition.Description)
                .Append(" (độ chắc chắn: ")
                .Append(ConfidenceLabel(item.ConfidenceLevel))
                .AppendLine(").");
        }

        builder.AppendLine();
        builder.Append("Độ phủ hiện tại là ")
            .Append(profile.Coverage.ToString("P0"));
        if (profile.Consistency.HasSufficientEvidence)
        {
            builder.Append(", độ nhất quán ")
                .Append(profile.Consistency.AgreementScore.ToString("P0"));
        }
        else
        {
            builder.Append(", consistency chưa đủ câu kiểm tra lặp ẩn");
        }

        builder.AppendLine(". Các quyết định accessibility và nghiệp vụ vẫn có quyền ưu tiên cao hơn sở thích thẩm mỹ.");

        return Task.FromResult(builder.ToString());
    }

    private static string ConfidenceLabel(string level) => level switch
    {
        "stable" => "ổn định",
        "moderate" => "khá chắc chắn",
        _ => "đang khám phá"
    };
}
