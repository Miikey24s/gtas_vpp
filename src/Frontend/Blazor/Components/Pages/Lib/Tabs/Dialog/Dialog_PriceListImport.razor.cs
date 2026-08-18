using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;

// Giữ luồng import theo 3 trạng thái: chọn file, preview có lỗi/cảnh báo, xác nhận ghi dữ liệu.
// Đổi bảng giá hoặc chọn file mới phải reset snapshot để không xác nhận nhầm batch cũ.
public partial class Dialog_PriceListImport
{
    private const long MaximumFileSize = 5 * 1024 * 1024;

    [Parameter] public Guid PriceListId { get; set; }
    [Parameter] public string PriceListName { get; set; } = string.Empty;
    [Parameter] public string SupplierName { get; set; } = string.Empty;
    [Parameter] public IReadOnlyList<PriceListResDTO> PriceLists { get; set; } = [];
    [Inject] public PriceListImportApiClient ImportApi { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;
    [Inject] public IToastService Toast { get; set; } = default!;

    private bool IsBusy { get; set; }
    private byte[]? SelectedFileContent { get; set; }
    private string SelectedFileName { get; set; } = string.Empty;
    private string SelectedFileContentType { get; set; } = "application/octet-stream";
    private PriceListImportAnalysisResDTO? Analysis { get; set; }
    private List<ColumnMappingState> MappingRows { get; set; } = [];
    private PriceListImportPreviewResDTO? Preview { get; set; }
    private PriceListImportBatchResDTO? Completed { get; set; }
    private IReadOnlyList<PriceListImportBatchResDTO> RecentImports { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        if (PriceListId == Guid.Empty && PriceLists.Count == 1)
        {
            PriceListId = PriceLists[0].Id;
        }

        SyncSelectedPriceList();
        await LoadRecentImportsAsync();
    }

    private async Task LoadRecentImportsAsync()
    {
        if (PriceListId == Guid.Empty)
        {
            RecentImports = [];
            return;
        }

        try
        {
            RecentImports = await ImportApi.ListAsync(PriceListId);
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["RecentPriceImports"], UiErrorMapper.GetMessage(ex, Loc));
        }
    }

    private async Task OnPriceListChangedAsync(Guid value)
    {
        if (value == PriceListId)
        {
            return;
        }

        PriceListId = value;
        SyncSelectedPriceList();
        ResetFileState();
        await LoadRecentImportsAsync();
    }

    private void SyncSelectedPriceList()
    {
        var selected = PriceLists.FirstOrDefault(item => item.Id == PriceListId);
        if (selected is null)
        {
            return;
        }

        PriceListName = selected.PriceListName ?? selected.PriceListCode ?? "–";
        SupplierName = selected.SupplierName ?? "–";
    }

    private void ResetFileState()
    {
        SelectedFileContent = null;
        SelectedFileName = string.Empty;
        SelectedFileContentType = "application/octet-stream";
        Analysis = null;
        MappingRows = [];
        Preview = null;
        Completed = null;
    }

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs args)
    {
        // Phân tích ngay sau khi chọn file để người dùng thấy lỗi trước khi có thao tác ghi.
        if (IsBusy)
        {
            return;
        }

        var file = args.File;
        var extension = Path.GetExtension(file.Name);
        if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            Toast.Warning(Loc["PriceImport"], Loc["PriceImportUnsupportedFile"]);
            return;
        }

        if (file.Size > MaximumFileSize)
        {
            Toast.Warning(Loc["PriceImport"], Loc["PriceImportFileTooLarge"]);
            return;
        }

        IsBusy = true;
        try
        {
            await using var stream = file.OpenReadStream(MaximumFileSize);
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            SelectedFileContent = buffer.ToArray();
            SelectedFileName = file.Name;
            SelectedFileContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;
            Preview = null;
            Completed = null;
            await using var analysisStream = new MemoryStream(SelectedFileContent, writable: false);
            Analysis = await ImportApi.AnalyzeAsync(
                PriceListId,
                analysisStream,
                SelectedFileName,
                SelectedFileContentType,
                CancellationToken.None);
            if (Analysis is null)
            {
                throw new InvalidOperationException(Loc["PriceImportAnalyzeFailed"]);
            }

            MappingRows = Analysis.Columns
                .Select(column => new ColumnMappingState
                {
                    ColumnIndex = column.ColumnIndex,
                    SourceColumn = column.SourceColumn,
                    SuggestedTargetField = column.SuggestedTargetField ?? string.Empty,
                    TargetField = column.SuggestedTargetField ?? string.Empty,
                    IsAiSuggested = column.IsAiSuggested,
                    SampleValues = column.SampleValues
                })
                .ToList();
            if (Analysis.CanPreviewAutomatically && MappingReady)
            {
                await LoadPreviewAsync();
            }
        }
        catch (Exception ex)
        {
            Analysis = null;
            MappingRows = [];
            Preview = null;
            SelectedFileContent = null;
            Toast.Error(Loc["PriceImport"], UiErrorMapper.GetMessage(ex, Loc, "PriceImportAnalyzeFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PrimaryAsync(MouseEventArgs _)
    {
        if (Completed is not null)
        {
            DialogService.Close(true);
            return;
        }

        if (Preview is null)
        {
            await TryLoadPreviewFromPrimaryAsync();
            return;
        }

        if (!Preview.CanConfirm || IsBusy)
        {
            return;
        }

        await TryConfirmImportAsync();
    }

    private async Task TryLoadPreviewFromPrimaryAsync()
    {
        if (Analysis is null || !MappingReady || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            // Preview chỉ đọc/kiểm tra file; chưa ghi dòng giá vào database.
            await LoadPreviewAsync();
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["PriceImport"], UiErrorMapper.GetMessage(ex, Loc, "PriceImportPreviewFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task TryConfirmImportAsync()
    {
        IsBusy = true;
        try
        {
            // Confirm gửi RowVersion của batch preview để ngăn xác nhận snapshot đã cũ.
            Completed = await ImportApi.ConfirmAsync(PriceListId, Preview!.Id, Preview.RowVersion);
            if (Completed is null)
            {
                throw new InvalidOperationException(Loc["PriceImportConfirmFailed"]);
            }
            RecentImports = await ImportApi.ListAsync(PriceListId);
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["PriceImport"], UiErrorMapper.GetMessage(ex, Loc, "PriceImportConfirmFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DownloadTemplateAsync(MouseEventArgs _)
    {
        try
        {
            await ImportApi.DownloadTemplateAsync(PriceListId);
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["DownloadTemplate"], UiErrorMapper.GetMessage(ex, Loc));
        }
    }

    private void Cancel(MouseEventArgs _) => DialogService.Close(false);

    private async Task LoadPreviewAsync()
    {
        if (SelectedFileContent is null)
        {
            throw new InvalidOperationException(Loc["PriceImportChooseFile"]);
        }

        await using var previewStream = new MemoryStream(SelectedFileContent, writable: false);
        Preview = await ImportApi.PreviewAsync(
            PriceListId,
            previewStream,
            SelectedFileName,
            SelectedFileContentType,
            MappingRows
                .Where(row => row.IsAiSuggested || !string.Equals(
                        row.TargetField,
                        row.SuggestedTargetField,
                        StringComparison.OrdinalIgnoreCase))
                .ToDictionary(row => row.ColumnIndex, row => row.TargetField),
            CancellationToken.None);
        if (Preview is null)
        {
            throw new InvalidOperationException(Loc["PriceImportPreviewFailed"]);
        }
    }

    private IReadOnlyList<VppDecisionOption<string>> MappingOptions =>
    [
        new(string.Empty, Loc["PriceImportIgnoreColumn"]),
        new("ItemCode", FieldLabel("ItemCode")),
        new("ItemName", FieldLabel("ItemName")),
        new("UnitName", FieldLabel("UnitName")),
        new("UnitPrice", FieldLabel("UnitPrice")),
        new("VatRate", FieldLabel("VatRate")),
        new("Note", FieldLabel("Note"))
    ];

    private IReadOnlyList<VppDecisionOption<Guid>> PriceListOptions => PriceLists
        .Select(item => new VppDecisionOption<Guid>(
            item.Id,
            $"{item.PriceListName ?? item.PriceListCode ?? "–"} · {item.SupplierName ?? "–"}"))
        .ToArray();

    private bool MappingHasDuplicates => MappingRows
        .Where(row => !string.IsNullOrWhiteSpace(row.TargetField))
        .GroupBy(row => row.TargetField, StringComparer.OrdinalIgnoreCase)
        .Any(group => group.Count() > 1);

    private bool MappingReady => !MappingHasDuplicates
        && MappingRows.Count(row => string.Equals(row.TargetField, "ItemCode", StringComparison.OrdinalIgnoreCase)) == 1
        && MappingRows.Count(row => string.Equals(row.TargetField, "UnitPrice", StringComparison.OrdinalIgnoreCase)) == 1;

    private string PrimaryText => Completed is not null
        ? Loc["Close"]
        : Preview is not null
            ? Loc["ConfirmImport"]
            : Analysis is not null
                ? Loc["PriceImportCheckData"]
                : Loc["PriceImportChooseFileToContinue"];

    private string? PrimaryIcon => Completed is not null
        ? null
        : Preview is not null
            ? "publish"
            : "fact_check";

    private bool PrimaryDisabled => Completed is null
        && (Preview is not null ? !Preview.CanConfirm : Analysis is null || !MappingReady);

    private static void UpdateMapping(ColumnMappingState row, string value)
        => row.TargetField = value;

    private string ActionLabel(string action) => action switch
    {
        "Add" => Loc["PriceImportAdded"],
        "Update" => Loc["PriceImportUpdated"],
        "Unchanged" => Loc["PriceImportUnchanged"],
        _ => Loc["Error"]
    };

    private static VppStatusTone ActionTone(string action) => action switch
    {
        "Add" => VppStatusTone.Success,
        "Update" => VppStatusTone.Info,
        "Unchanged" => VppStatusTone.Neutral,
        _ => VppStatusTone.Danger
    };

    private static VppStatusTone IssueTone(PriceListImportIssueResDTO issue)
        => issue.Severity == "Warning" ? VppStatusTone.Warning : VppStatusTone.Danger;

    private string FieldLabel(string field) => field switch
    {
        "ItemCode" => Loc["ProductCode"],
        "ItemName" => Loc["ProductName"],
        "UnitName" => Loc["UOM"],
        "UnitPrice" => Loc["Price"],
        "VatRate" => Loc["VatRate"],
        "Note" => Loc["Notes"],
        _ => field
    };

    private static string FormatBatchTime(DateTime value)
        => DateFormatter.Format(value, DateFormatter.LongDate);

    private string BatchStatusLabel(string status) => status switch
    {
        "Completed" => Loc["Completed"],
        "Ready" => Loc["WaitingForConfirmation"],
        "Failed" => Loc["Failed"],
        _ => Loc["Processing"]
    };

    private static VppStatusTone BatchStatusTone(string status) => status switch
    {
        "Completed" => VppStatusTone.Success,
        "Ready" => VppStatusTone.Info,
        "Failed" => VppStatusTone.Danger,
        _ => VppStatusTone.Neutral
    };

    private sealed class ColumnMappingState
    {
        public int ColumnIndex { get; init; }
        public string SourceColumn { get; init; } = string.Empty;
        public string SuggestedTargetField { get; init; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public bool IsAiSuggested { get; init; }
        public IReadOnlyList<string> SampleValues { get; init; } = [];
    }
}
