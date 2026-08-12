// PAGE LOGIC: VPPRequest/Components/PeriodSettlementPanel.OrderDetails.cs
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Features.Settlement.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

public partial class PeriodSettlementPanel
{
    [Inject] private RequestsQueryClient Requests { get; set; } = default!;
    [Inject] private RequestsCommandClient RequestCommands { get; set; } = default!;
    [Inject] private RequestsExportClient RequestExports { get; set; } = default!;
    [Inject] private PostSettlementOrderCorrectionApiClient OrderCorrections { get; set; } = default!;

    private HistoryOrderDetailSheet? orderDetailSheet;
    private readonly List<VppRequestResDTO> orderDetailScope = [];
    private readonly Dictionary<Guid, VppRequestResDTO> loadedOrderDetails = [];
    private readonly List<VppOrderDetailItem> orderDetailRows = [];
    private readonly List<string> orderDetailCategories = [];
    private readonly List<string> orderDetailUnits = [];
    private VppRequestResDTO? selectedOrderDetail;
    private bool isOrderDetailOpen;
    private bool isOrderDetailLoading;
    private bool isAggregateOrderDetail;
    private bool hasOrderDetailError;
    private string orderDetailSearch = string.Empty;
    private string orderDetailCategory = string.Empty;
    private string orderDetailUnit = string.Empty;
    private int? activeOrderDetailCodeNumber;
    private int? activeOrderDetailNoteNumber;
    private VppFileExportFormat? exportingOrderFormat;
    private bool submittingPostSettlementCorrection;
    private List<PostSettlementOrderCorrectionResDTO> pendingPostSettlementCorrections = [];
    private string postSettlementDecisionReason = string.Empty;

    private Guid SelectedOrderDetailId => selectedOrderDetail?.Id ?? Guid.Empty;
    private decimal? SelectedOrderFinancialAmount => selectedOrderDetail is null
        ? null
        : isAggregateOrderDetail
            ? orderDetailScope.Sum(order => CurrentFinancialAllocations
                .Where(allocation => allocation.RequestHeaderId == order.Id)
                .Sum(allocation => allocation.GrossAmount))
            : CurrentFinancialAllocations
                .Where(allocation => allocation.RequestHeaderId == selectedOrderDetail.Id)
                .Sum(allocation => allocation.GrossAmount);
    private bool HasOrderDetailFilters => !string.IsNullOrWhiteSpace(orderDetailSearch)
        || !string.IsNullOrWhiteSpace(orderDetailCategory)
        || !string.IsNullOrWhiteSpace(orderDetailUnit);
    private bool IsPostSettlementOrderAdjustment => status?.IsSettled == true;
    private bool CanAdjustOrderBeforeSettlement => status?.IsSettled != true
        && managedPeriod?.CanAdjustOrders == true;
    private bool CanRequestPostSettlementCorrection => (IsPostSettlementOrderAdjustment
            || CanAdjustOrderBeforeSettlement)
        && !isAggregateOrderDetail
        && selectedOrderDetail?.RowVersion is { Length: > 0 };
    private string OrderAdjustmentActionText => IsPostSettlementOrderAdjustment
        ? Loc["AdjustAfterSettlement"]
        : Loc["EditOrder"];
    private string OrderAdjustmentDialogHint => IsPostSettlementOrderAdjustment
        ? Loc["PostSettlementAdjustmentHint"]
        : managedPeriod is null
            ? string.Empty
            : string.Format(
                Loc["PreSettlementAdjustmentHint"],
                DateFormatter.Format(
                    managedPeriod.PostCloseAdjustmentDeadlineLocal,
                    DateFormatter.LongDate));
    private string OrderDetailUnitHeaderLabel => Loc["UOM"].Value.Trim() switch
    {
        "ĐVT" or "DVT" => "Đơn vị",
        "UOM" => "Unit",
        var localizedLabel => localizedLabel
    };

    private IReadOnlyList<VppFilterOption<Guid>> OrderDetailOptions =>
        (orderDetailScope.Count > 1
            ? new[] { new VppFilterOption<Guid>(Guid.Empty, Loc["SettlementAllOrdersInGroup"]) }
            : [])
        .Concat(orderDetailScope.Select(order => new VppFilterOption<Guid>(
            order.Id,
            string.IsNullOrWhiteSpace(order.RequesterName)
                ? order.VppCode ?? "–"
                : $"{order.VppCode} · {order.RequesterName}")))
        .ToArray();

    private Task OpenDepartmentOrdersAsync(DepartmentSettlementRow row) =>
        OpenOrderScopeAsync(periodOrdersSnapshot.Where(order => string.Equals(
            order.DepartmentCode,
            row.DepartmentCode,
            StringComparison.CurrentCultureIgnoreCase)));

    private Task OpenRequesterOrdersAsync(RequesterSettlementRow row) =>
        OpenOrderScopeAsync(periodOrdersSnapshot.Where(order => row.UserId > 0
            ? order.CreatedByUserId == row.UserId
            : string.Equals(
                order.RequesterName,
                row.RequesterName,
                StringComparison.CurrentCultureIgnoreCase)));

    private Task OpenItemOrdersAsync(AggregatedVppItemResDTO item)
    {
        var requestCodes = item.Breakdown
            .Select(entry => entry.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        return OpenOrderScopeAsync(periodOrdersSnapshot.Where(order =>
            (order.Items?.Any(detail => detail.VppId == item.VppId) ?? false)
            || (!string.IsNullOrWhiteSpace(order.VppCode) && requestCodes.Contains(order.VppCode))));
    }

    private async Task OpenOrderScopeAsync(IEnumerable<VppRequestResDTO> orders)
    {
        var matches = orders
            .Where(order => order.Id != Guid.Empty)
            .GroupBy(order => order.Id)
            .Select(group => group.First())
            .OrderByDescending(order => order.SubmittedDate ?? order.CreatedAtUtc)
            .ThenByDescending(order => order.RevisionNumber)
            .ToList();

        if (matches.Count == 0)
        {
            Toast.Info(Loc["OrderDetails"], Loc["NoData"]);
            return;
        }

        orderDetailScope.Clear();
        orderDetailScope.AddRange(matches);
        loadedOrderDetails.Clear();
        isAggregateOrderDetail = false;
        isOrderDetailOpen = true;
        if (matches.Count > 1)
        {
            await LoadAggregateOrderDetailAsync();
            return;
        }

        await LoadOrderDetailAsync(matches[0].Id);
    }

    private async Task LoadOrderDetailAsync(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            await LoadAggregateOrderDetailAsync();
            return;
        }

        var summary = orderDetailScope.FirstOrDefault(order => order.Id == orderId);
        if (summary is null || isOrderDetailLoading)
        {
            return;
        }

        selectedOrderDetail = summary;
        isAggregateOrderDetail = false;
        isOrderDetailLoading = true;
        hasOrderDetailError = false;
        ResetOrderDetailFilters();
        await InvokeAsync(StateHasChanged);

        try
        {
            if (!loadedOrderDetails.TryGetValue(orderId, out var detail))
            {
                detail = await Requests.GetOrderAsync(orderId) ?? summary;
                loadedOrderDetails[orderId] = detail;
            }
            selectedOrderDetail = detail;
            BuildOrderDetailOptions();
            RebuildOrderDetailRows();
        }
        catch (Exception ex)
        {
            selectedOrderDetail = summary;
            hasOrderDetailError = true;
            orderDetailRows.Clear();
            orderDetailCategories.Clear();
            orderDetailUnits.Clear();
            Toast.Error(ex, Loc, "LoadDetailsFailed");
        }
        finally
        {
            isOrderDetailLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadAggregateOrderDetailAsync()
    {
        if (orderDetailScope.Count == 0 || isOrderDetailLoading)
        {
            return;
        }

        isOrderDetailLoading = true;
        hasOrderDetailError = false;
        ResetOrderDetailFilters();
        await InvokeAsync(StateHasChanged);

        try
        {
            using var gate = new SemaphoreSlim(6);
            var tasks = orderDetailScope.Select(async summary =>
            {
                if (loadedOrderDetails.TryGetValue(summary.Id, out var cached))
                {
                    return cached;
                }

                await gate.WaitAsync();
                try
                {
                    return await Requests.GetOrderAsync(summary.Id) ?? summary;
                }
                finally
                {
                    gate.Release();
                }
            }).ToArray();
            var details = await Task.WhenAll(tasks);
            foreach (var detail in details)
            {
                loadedOrderDetails[detail.Id] = detail;
            }

            var aggregateItems = details
                .SelectMany(order => order.Items ?? [])
                .GroupBy(item => item.VppId)
                .Select(group =>
                {
                    var first = group.First();
                    return new VppRequestDetailResDTO
                    {
                        VppId = group.Key,
                        VppCode = first.VppCode,
                        VppName = first.VppName,
                        CategoryName = first.CategoryName,
                        UomCode = first.UomCode,
                        UomName = first.UomName,
                        Qty = group.Sum(item => item.Qty),
                        CurrentSinglePrice = first.CurrentSinglePrice
                    };
                })
                .OrderBy(item => item.VppName)
                .ThenBy(item => item.VppCode)
                .ToList();
            var firstOrder = orderDetailScope[0];
            selectedOrderDetail = new VppRequestResDTO
            {
                Id = Guid.Empty,
                VppCode = Loc["SettlementAllOrdersInGroup"],
                Year = firstOrder.Year,
                Month = firstOrder.Month,
                PeriodState = firstOrder.PeriodState,
                Status = firstOrder.Status,
                RequesterName = Loc["SettlementAllOrdersInGroup"],
                DepartmentCode = orderDetailScope
                    .Select(order => order.DepartmentCode)
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .Count() == 1 ? firstOrder.DepartmentCode : "–",
                TotalLines = aggregateItems.Count,
                TotalQty = aggregateItems.Sum(item => item.Qty),
                TotalAmount = (long)Math.Round(orderDetailScope.Sum(order => CurrentFinancialAllocations
                    .Where(allocation => allocation.RequestHeaderId == order.Id)
                    .Sum(allocation => allocation.GrossAmount))),
                Items = aggregateItems
            };
            isAggregateOrderDetail = true;
            BuildOrderDetailOptions();
            RebuildOrderDetailRows();
        }
        catch (Exception ex)
        {
            selectedOrderDetail = null;
            isAggregateOrderDetail = false;
            hasOrderDetailError = true;
            orderDetailRows.Clear();
            Toast.Error(ex, Loc, "LoadDetailsFailed");
        }
        finally
        {
            isOrderDetailLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task RetryOrderDetailAsync() => selectedOrderDetail is null
        ? Task.CompletedTask
        : LoadOrderDetailAsync(selectedOrderDetail.Id);

    private void CloseOrderDetail() => isOrderDetailOpen = false;

    private async Task OpenPostSettlementCorrection()
    {
        if (!CanRequestPostSettlementCorrection || selectedOrderDetail is null) return;

        var result = await DialogService.OpenAsync<Dialog_PostSettlementOrderCorrection>(
            OrderAdjustmentActionText,
            new Dictionary<string, object?>
            {
                [nameof(Dialog_PostSettlementOrderCorrection.Order)] = selectedOrderDetail,
                [nameof(Dialog_PostSettlementOrderCorrection.Hint)] = OrderAdjustmentDialogHint,
                [nameof(Dialog_PostSettlementOrderCorrection.PrimaryText)] = IsPostSettlementOrderAdjustment
                    ? Loc["SendRequest"].Value
                    : Loc["SaveChanges"].Value
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Standard,
                OrderAdjustmentActionText,
                closeAriaLabel: Loc["Close"].Value));

        if (result is Dialog_PostSettlementOrderCorrection.Result request)
        {
            await SubmitPostSettlementCorrectionAsync(request);
        }
    }

    private async Task SubmitPostSettlementCorrectionAsync(
        Dialog_PostSettlementOrderCorrection.Result request)
    {
        if (submittingPostSettlementCorrection || selectedOrderDetail is null) return;
        submittingPostSettlementCorrection = true;
        try
        {
            if (IsPostSettlementOrderAdjustment)
            {
                _ = await OrderCorrections.CreateAsync(new PostSettlementOrderCorrectionCreateReqDTO
                {
                    RequestId = selectedOrderDetail.Id,
                    Action = request.Action,
                    Reason = request.Reason,
                    EmployeeNote = request.EmployeeNote,
                    RequestRowVersion = selectedOrderDetail.RowVersion,
                    Items = request.Items
                });
                await LoadPostSettlementCorrectionsAsync();
                Toast.Success(
                    Loc["RequestSent"],
                    Loc["PostSettlementAdjustmentRequested"]);
            }
            else
            {
                _ = await RequestCommands.AdjustAfterCloseAsync(
                    selectedOrderDetail.Id,
                    new VppManagerOrderAdjustmentReqDTO
                    {
                        Action = request.Action,
                        Reason = request.Reason,
                        EmployeeNote = request.EmployeeNote,
                        RowVersion = selectedOrderDetail.RowVersion,
                        IdempotencyKey = $"manager-adjust-{selectedOrderDetail.Id:N}-{Guid.NewGuid():N}",
                        Items = request.Items
                    });
                CloseOrderDetail();
                await ReloadPeriodAsync();
                Toast.Success(Loc["Saved"], Loc["OrderAdjustmentSaved"]);
            }
        }
        catch (Exception ex)
        {
            Toast.Error(OrderAdjustmentActionText, UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            submittingPostSettlementCorrection = false;
        }
    }

    private async Task LoadPostSettlementCorrectionsAsync()
    {
        if (status?.IsSettled != true)
        {
            pendingPostSettlementCorrections = [];
            return;
        }

        pendingPostSettlementCorrections = (await OrderCorrections.ListAsync(managedPeriod?.Id))
            .Where(correction => correction.Year == Year
                && correction.Month == Month
                && string.Equals(correction.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            .OrderBy(correction => correction.RequestedAtUtc)
            .ToList();
    }

    private async Task ConfirmPostSettlementCorrectionAsync(PostSettlementOrderCorrectionResDTO correction)
    {
        try
        {
            _ = await OrderCorrections.ConfirmAsync(correction.Id, new PostSettlementOrderCorrectionDecisionReqDTO
            {
                Reason = string.IsNullOrWhiteSpace(postSettlementDecisionReason)
                    ? null
                    : postSettlementDecisionReason.Trim(),
                RowVersion = correction.RowVersion
            });
            postSettlementDecisionReason = string.Empty;
            await ReloadPeriodAsync();
            Toast.Success(
                Loc["AdjustmentApproved"],
                Loc["NewSettlementVersionCreated"]);
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["AdjustmentApproved"], UiErrorMapper.GetMessage(ex, Loc));
        }
    }

    private async Task RejectPostSettlementCorrectionAsync(PostSettlementOrderCorrectionResDTO correction)
    {
        if (postSettlementDecisionReason.Trim().Length < 5)
        {
            Toast.Warning("Từ chối yêu cầu", "Nhập lý do từ chối ít nhất 5 ký tự.");
            return;
        }

        try
        {
            _ = await OrderCorrections.RejectAsync(correction.Id, new PostSettlementOrderCorrectionDecisionReqDTO
            {
                Reason = postSettlementDecisionReason.Trim(),
                RowVersion = correction.RowVersion
            });
            postSettlementDecisionReason = string.Empty;
            await LoadPostSettlementCorrectionsAsync();
            Toast.Success("Đã từ chối", "Yêu cầu không làm thay đổi đơn hoặc bảng chốt.");
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["RejectAdjustment"], UiErrorMapper.GetMessage(ex, Loc));
        }
    }

    private static string PostSettlementCorrectionActionText(string action) => action switch
    {
        "Adjust" => "Cập nhật đơn",
        "Cancel" => "Hủy đơn",
        _ => action
    };

    private void BuildOrderDetailOptions()
    {
        orderDetailCategories.Clear();
        orderDetailUnits.Clear();
        orderDetailCategories.AddRange(GetDistinctOrderItemValues(item => item.CategoryName));
        orderDetailUnits.AddRange(GetDistinctOrderItemValues(item => item.UomName));
    }

    private IReadOnlyList<string> GetDistinctOrderItemValues(
        Func<VppRequestDetailResDTO, string?> selector) =>
        (selectedOrderDetail?.Items ?? [])
            .Select(selector)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    private void ResetOrderDetailFilters()
    {
        orderDetailSearch = string.Empty;
        orderDetailCategory = string.Empty;
        orderDetailUnit = string.Empty;
        activeOrderDetailCodeNumber = null;
        activeOrderDetailNoteNumber = null;
        orderDetailRows.Clear();
        orderDetailCategories.Clear();
        orderDetailUnits.Clear();
    }

    private void RebuildOrderDetailRows()
    {
        var search = orderDetailSearch.Trim();
        var filtered = (selectedOrderDetail?.Items ?? []).Where(item =>
            (string.IsNullOrWhiteSpace(search)
             || (item.VppCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
             || (item.VppName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))
            && (string.IsNullOrWhiteSpace(orderDetailCategory)
                || string.Equals(item.CategoryName, orderDetailCategory, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrWhiteSpace(orderDetailUnit)
                || string.Equals(item.UomName, orderDetailUnit, StringComparison.CurrentCultureIgnoreCase)));

        orderDetailRows.Clear();
        orderDetailRows.AddRange(filtered.Select((item, index) => new VppOrderDetailItem(
            index + 1,
            item.VppCode,
            item.VppName ?? string.Empty,
            item.CategoryName,
            item.UomName,
            item.Qty,
            item.Description)));
    }

    private async Task OnOrderDetailSearchChangedAsync(ChangeEventArgs args)
    {
        orderDetailSearch = args.Value?.ToString() ?? string.Empty;
        await RefreshOrderDetailRowsAsync();
    }

    private async Task OnOrderDetailCategoryChangedAsync(string category)
    {
        orderDetailCategory = category;
        await RefreshOrderDetailRowsAsync();
    }

    private async Task OnOrderDetailUnitChangedAsync(string unit)
    {
        orderDetailUnit = unit;
        await RefreshOrderDetailRowsAsync();
    }

    private async Task ClearOrderDetailFiltersAsync()
    {
        orderDetailSearch = string.Empty;
        orderDetailCategory = string.Empty;
        orderDetailUnit = string.Empty;
        await RefreshOrderDetailRowsAsync();
    }

    private async Task RefreshOrderDetailRowsAsync()
    {
        activeOrderDetailCodeNumber = null;
        activeOrderDetailNoteNumber = null;
        RebuildOrderDetailRows();
        if (orderDetailSheet is not null)
        {
            await orderDetailSheet.ReloadDetailGridAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    private void ToggleOrderDetailCode(int number)
    {
        activeOrderDetailNoteNumber = null;
        activeOrderDetailCodeNumber = activeOrderDetailCodeNumber == number ? null : number;
    }

    private void ToggleOrderDetailNote(int number)
    {
        activeOrderDetailCodeNumber = null;
        activeOrderDetailNoteNumber = activeOrderDetailNoteNumber == number ? null : number;
    }

    private async Task ExportSelectedOrderDetailAsync(VppFileExportFormat format)
    {
        if (selectedOrderDetail is null || isAggregateOrderDetail || exportingOrderFormat.HasValue)
        {
            return;
        }

        exportingOrderFormat = format;
        try
        {
            var result = await RequestExports.ExportOrderAsync(selectedOrderDetail.Id, format);
            Toast.Success(
                Loc["Order"],
                Loc["ExportCompleted", result.FileName, FileSizeFormatter.Format(result.Size)]);
        }
        catch (Exception ex)
        {
            Toast.Error(ex, Loc);
        }
        finally
        {
            exportingOrderFormat = null;
        }
    }
}
