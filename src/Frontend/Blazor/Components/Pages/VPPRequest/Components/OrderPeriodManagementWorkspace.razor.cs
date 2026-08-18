using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

// Điều phối danh sách kỳ đặt hàng dành cho quản lý: tải dữ liệu, lọc và mở đúng thao tác kỳ.
// UI giữ đủ hành động theo Stable Capability Surface; quyền khả dụng lấy trực tiếp từ DTO backend.
public partial class OrderPeriodManagementWorkspace
{
    [Inject] private OrderPeriodApiClient PeriodsApi { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private bool IsLoading { get; set; } = true;
    private bool IsBusy { get; set; }
    private bool HasLoadError { get; set; }
    private List<VppManagedPeriodResDTO> Periods { get; set; } = [];
    private List<VppManagedPeriodResDTO> FilteredPeriods { get; set; } = [];
    private string periodSearchText = string.Empty;
    private string selectedPeriodState = string.Empty;
    private int? selectedPeriodYear;

    private int OpenPeriodCount => Periods.Count(x => x.State == "Open");

    private string PeriodCollectionSummary =>
        HasPeriodFilters
            ? $"{FilteredPeriods.Count}/{Periods.Count} kỳ · {OpenPeriodCount} kỳ đang mở"
            : $"{Periods.Count} kỳ · {OpenPeriodCount} kỳ đang mở";

    private bool HasPeriodFilters => !string.IsNullOrWhiteSpace(periodSearchText)
        || !string.IsNullOrWhiteSpace(selectedPeriodState)
            || selectedPeriodYear.HasValue;
    private VppDataSurfaceState PeriodSurfaceState => FilteredPeriods.Count > 0
        ? VppDataSurfaceState.Populated
        : HasPeriodFilters
            ? VppDataSurfaceState.FilteredEmpty
            : VppDataSurfaceState.Empty;

    private IReadOnlyList<VppFilterOption<string>> PeriodStateOptions =>
    [
        new(string.Empty, "Tất cả trạng thái"),
        new("Open", "Đang mở"),
        new("SubmissionClosed", "Đã đóng"),
        new("Pricing", "Đang chốt"),
        new("Settled", "Đã chốt"),
        new("Scheduled", "Chưa mở")
    ];

    private IReadOnlyList<VppFilterOption<int?>> PeriodYearOptions =>
        new[] { new VppFilterOption<int?>(null, "Tất cả năm") }
            .Concat(Periods
                .Select(period => period.Year)
                .Distinct()
                .OrderByDescending(year => year)
                .Select(year => new VppFilterOption<int?>(year, year.ToString())))
            .ToArray();

    private IReadOnlyList<VppAdminActionMenuItem> PeriodSecondaryActions(VppManagedPeriodResDTO period)
    {
        var items = new List<VppAdminActionMenuItem>();

        items.Add(new(
            "extend",
            "Gia hạn kỳ",
            "event_repeat",
            () => ExtendDeadlineAsync(period),
            Disabled: !period.CanExtendDeadline,
            DisabledReason: "Không thể gia hạn kỳ ở trạng thái hiện tại."));

        return items;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        HasLoadError = false;
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            HasLoadError = true;
            Toast.Error("Kỳ đặt hàng", UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExtendDeadlineAsync(VppManagedPeriodResDTO period)
    {
        var result = await OpenPeriodExtensionDialogAsync(period);
        if (result is not VppOrderPeriodExtendDeadlineReqDTO request)
        {
            return;
        }

        await RunAsync(async () =>
        {
            request.RowVersion = period.RowVersion;
            _ = await PeriodsApi.ExtendAsync(period.Id, request);
            await LoadDataAsync();
            Toast.Success("Đã gia hạn", $"Kỳ {PeriodLabel(period)} đã có ngày đóng mới.");
        });
    }

    private async Task OpenCreatePeriodAsync()
    {
        try
        {
            var settings = await PeriodsApi.GetCurrentSettingsAsync()
                ?? new VppOrderPeriodSettingsResDTO();
            var latest = Periods
                .OrderByDescending(period => period.Year)
                .ThenByDescending(period => period.Month)
                .FirstOrDefault();
            var target = latest is null
                ? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
                : new DateTime(latest.Year, latest.Month, 1).AddMonths(1);
            var result = await DialogService.OpenAsync<Dialog_OrderPeriodCreate>(
                "Thêm kỳ",
                new Dictionary<string, object?>
                {
                    [nameof(Dialog_OrderPeriodCreate.Settings)] = settings,
                    [nameof(Dialog_OrderPeriodCreate.SuggestedYear)] = target.Year,
                    [nameof(Dialog_OrderPeriodCreate.SuggestedMonth)] = target.Month
                },
                VppAdminDialogProfiles.Create(
                    VppAdminDialogSize.Standard,
                    "Thêm kỳ",
                    closeAriaLabel: "Đóng"));

            if (result is not VppOrderPeriodManualCreateReqDTO request)
            {
                return;
            }

            await RunAsync(async () =>
            {
                _ = await PeriodsApi.CreateManualAsync(request);
                await LoadDataAsync();
                Toast.Success("Đã thêm kỳ", $"Kỳ {request.Month:00}/{request.Year} đã được tạo.");
            });
        }
        catch (Exception ex)
        {
            Toast.Error("Thêm kỳ", UiErrorMapper.GetMessage(ex, Loc));
        }
    }

    private Task<object?> OpenPeriodExtensionDialogAsync(VppManagedPeriodResDTO period) =>
        DialogService.OpenAsync<Dialog_OrderPeriodAction>(
            "Gia hạn kỳ",
            new Dictionary<string, object?>
            {
                [nameof(Dialog_OrderPeriodAction.Period)] = period
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Standard,
                "Gia hạn kỳ",
                closeAriaLabel: "Đóng"));

    private void NavigateToSettlement(VppManagedPeriodResDTO period)
        => NavigationManager.NavigateTo(
            $"/dashboard?tab=5&periodTab=review&periodYear={period.Year}&periodMonth={period.Month}");

    private static bool CanSettlePeriod(VppManagedPeriodResDTO period) =>
        period.State is "SubmissionClosed" or "Pricing";

    private Task OnPeriodSearchInput(ChangeEventArgs args)
    {
        periodSearchText = args.Value?.ToString() ?? string.Empty;
        RefreshPeriodView();
        return Task.CompletedTask;
    }

    private Task OnPeriodStateChanged(string state)
    {
        selectedPeriodState = state;
        RefreshPeriodView();
        return Task.CompletedTask;
    }

    private Task OnPeriodYearChanged(int? year)
    {
        selectedPeriodYear = year;
        RefreshPeriodView();
        return Task.CompletedTask;
    }

    private Task ClearPeriodFiltersAsync()
    {
        periodSearchText = string.Empty;
        selectedPeriodState = string.Empty;
        selectedPeriodYear = null;
        RefreshPeriodView();
        return Task.CompletedTask;
    }

    private async Task LoadDataAsync()
    {
        Periods = (await PeriodsApi.ListAsync())
            .OrderByDescending(period => period.Year)
            .ThenByDescending(period => period.Month)
            .ToList();
        RefreshPeriodView();
    }

    private void RefreshPeriodView()
    {
        // Tính một lần sau mỗi thay đổi thay vì materialize lại cùng danh sách nhiều lần trong một render.
        var normalizedSearch = periodSearchText.Trim();
        FilteredPeriods = Periods
            .Where(period => string.IsNullOrWhiteSpace(normalizedSearch)
                || PeriodLabel(period).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                || (period.LastTransitionReason?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false))
            .Where(period => string.IsNullOrWhiteSpace(selectedPeriodState)
                || string.Equals(period.State, selectedPeriodState, StringComparison.OrdinalIgnoreCase))
            .Where(period => !selectedPeriodYear.HasValue || period.Year == selectedPeriodYear.Value)
            .ToList();
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Toast.Error("Kỳ đặt hàng", UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string PeriodLabel(VppManagedPeriodResDTO period) =>
        $"{period.Month:00}/{period.Year}";

    private static string FormatDateTime(DateTime value) =>
        DateFormatter.Format(value, DateFormatter.LongDate);

}
