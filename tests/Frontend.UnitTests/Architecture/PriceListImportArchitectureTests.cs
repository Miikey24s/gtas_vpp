using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class PriceListImportArchitectureTests
{
    [Fact]
    public void PriceImport_UsesDesignSystemPreviewAndNeverWritesBeforeConfirmation()
    {
        var page = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor");
        var dialog = ReadFrontendSource("Components/Pages/Lib/Tabs/Dialog/Dialog_PriceListImport.razor");
        var code = ReadFrontendSource("Components/Pages/Lib/Tabs/Dialog/Dialog_PriceListImport.razor.cs");

        Assert.Contains("ImportPriceList", page, StringComparison.Ordinal);
        Assert.Contains("<VppAdaptiveDialogShell", dialog, StringComparison.Ordinal);
        Assert.Contains("<VppInlineNotice", dialog, StringComparison.Ordinal);
        Assert.Contains("PriceImportDetectedColumns", dialog, StringComparison.Ordinal);
        Assert.Contains("PriceImportPreview", dialog, StringComparison.Ordinal);
        Assert.Contains("PreviewAsync", code, StringComparison.Ordinal);
        Assert.Contains("Preview.CanConfirm", dialog, StringComparison.Ordinal);
        Assert.Contains("ConfirmAsync", code, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", code, StringComparison.Ordinal);
    }

    private static string ReadFrontendSource(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Frontend/Blazor"));
        return File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
