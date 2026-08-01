namespace gtas_vpp_fe.Components.DesignSystem.Composites;

public enum VppFileExportFormat
{
    Pdf,
    Excel,
    Csv
}

public static class VppFileExportFormatExtensions
{
    public static string ApiSuffix(this VppFileExportFormat format) => format switch
    {
        VppFileExportFormat.Pdf => "export.pdf",
        VppFileExportFormat.Excel => "export.xlsx",
        VppFileExportFormat.Csv => "export",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };
}
