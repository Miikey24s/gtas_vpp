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

public partial class Dialog_PriceListImport
{
    private const long MaximumFileSize = 5 * 1024 * 1024;

    [Parameter] public Guid PriceListId { get; set; }
    [Parameter] public string PriceListName { get; set; } = string.Empty;
    [Parameter] public string SupplierName { get; set; } = string.Empty;
    [Inject] public PriceListImportApiClient ImportApi { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;
    [Inject] public IToastService Toast { get; set; } = default!;

    private bool IsBusy { get; set; }
    private PriceListImportPreviewResDTO? Preview { get; set; }
    private PriceListImportBatchResDTO? Completed { get; set; }
    private IReadOnlyList<PriceListImportBatchResDTO> RecentImports { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            RecentImports = await ImportApi.ListAsync(PriceListId);
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["RecentPriceImports"], UiErrorMapper.GetMessage(ex, Loc));
        }
    }

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs args)
    {
        if (IsBusy)
        {
            return;
        }

        var file = args.File;
        if (file.Size > MaximumFileSize)
        {
            Toast.Warning(Loc["PriceImport"], Loc["PriceImportFileTooLarge"]);
            return;
        }

        IsBusy = true;
        try
        {
            await using var stream = file.OpenReadStream(MaximumFileSize);
            Preview = await ImportApi.PreviewAsync(
                PriceListId,
                stream,
                file.Name,
                file.ContentType,
                CancellationToken.None);
            if (Preview is null)
            {
                throw new InvalidOperationException(Loc["PriceImportPreviewFailed"]);
            }
        }
        catch (Exception ex)
        {
            Preview = null;
            Toast.Error(Loc["PriceImport"], UiErrorMapper.GetMessage(ex, Loc, "PriceImportPreviewFailed"));
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

        if (Preview is null || !Preview.CanConfirm || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            Completed = await ImportApi.ConfirmAsync(PriceListId, Preview.Id, Preview.RowVersion);
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
        "SupplierSku" => Loc["SupplierSku"],
        "ItemName" => Loc["ProductName"],
        "UnitPrice" => Loc["Price"],
        "VatRate" => Loc["VatRate"],
        "MinimumOrderQuantity" => Loc["MinimumOrderQuantity"],
        "LeadTimeDays" => Loc["LeadTimeDays"],
        "IsDefault" => Loc["DefaultPrice"],
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
}
