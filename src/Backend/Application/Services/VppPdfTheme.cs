using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace gtas_vpp_be.Service.Services;

internal static class VppPdfTheme
{
    public const string PrimaryText = "#102A43";
    public const string SecondaryText = "#52667A";
    public const string Accent = "#0EA5E9";
    public const string HeaderBackground = "#EAF6FC";
    public const string Border = "#D7E1EA";

    public static IContainer TableHeaderCell(IContainer cell) => cell
        .Background(HeaderBackground)
        .BorderBottom(1)
        .BorderColor(Border)
        .PaddingVertical(5)
        .PaddingHorizontal(4);

    public static IContainer TableBodyCell(IContainer cell) => cell
        .BorderBottom(0.5f)
        .BorderColor(Border)
        .PaddingVertical(4)
        .PaddingHorizontal(4);
}
