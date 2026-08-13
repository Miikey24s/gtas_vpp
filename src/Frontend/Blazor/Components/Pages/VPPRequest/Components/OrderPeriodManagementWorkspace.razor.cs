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

public partial class OrderPeriodManagementWorkspace
{
    private const string EditAction = "edit";
    private const string ExtendAction = "extend";

    [Inject] private OrderPeriodApiClient PeriodsApi { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private bool IsLoading { get; set; } = true;
    private bool IsBusy { get; set; }
    private bool HasLoadError { get; set; }
    private List<VppManagedPeriodResDTO> Periods { get; set; } = [];
    private string periodSearchText = string.Empty;
    private string selectedPeriodState = string.Empty;
    private int? selectedPeriodYear;
    private VppOrderPeriodSettingsReqDTO Settings { get; set; } = NewDefaultSettings();

    private IReadOnlyList<VppManagedPeriodResDTO> OpenPeriods => Periods
        .Where(x => x.State == "Open")
        .OrderByDescending(x => x.Year)
        .ThenByDescending(x => x.Month)
        .ToArray();

    private string PeriodCollectionSummary =>
        HasPeriodFilters
            ? $"{FilteredPeriods.Count}/{Periods.Count} kỳ · {OpenPeriods.Count} kỳ đang mở"
            : $"{Periods.Count} kỳ · {OpenPeriods.Count} kỳ đang mở";

    private bool HasPeriodFilters => !string.IsNullOrWhiteSpace(periodSearchText)
        || !string.IsNullOrWhiteSpace(selectedPeriodState)
            || selectedPeriodYear.HasValue;
    private VppDataSurfaceState PeriodSurfaceState => FilteredPeriods.Count > 0
        ? VppDataSurfaceState.Populated
        : HasPeriodFilters
            ? VppDataSurfaceState.FilteredEmpty
            : VppDataSurfaceState.Empty;

    private List<VppManagedPeriodResDTO> FilteredPeriods => Periods
        .Where(period => string.IsNullOrWhiteSpace(periodSearchText)
            || PeriodLabel(period).Contains(periodSearchText.Trim(), StringComparison.OrdinalIgnoreCase)
            || (period.LastTransitionReason?.Contains(periodSearchText.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
        .Where(period => string.IsNullOrWhiteSpace(selectedPeriodState)
            || string.Equals(period.State, selectedPeriodState, StringComparison.OrdinalIgnoreCase))
        .Where(period => !selectedPeriodYear.HasValue || period.Year == selectedPeriodYear.Value)
        .ToList();

    private IReadOnlyList<VppFilterOption<string>> PeriodStateOptions =>
    [
        new(string.Empty, "Tất cả trạng thái"),
        new("Open", "Đang mở"),
        new("SubmissionClosed", "Đã đóng"),
        new("Pricing", "Đang chốt"),
        new("Settled", "Đã chốt"),
        new("Scheduled", "Sắp mở")
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
            ExtendAction,
            "Gia hạn kỳ",
            "event_repeat",
            () => ExtendDeadlineAsync(period),
            Disabled: !period.CanExtendDeadline,
            DisabledReason: "Không thể gia hạn kỳ ở trạng thái hiện tại."));

        items.Add(new(
            EditAction,
            "Sửa lịch",
            "edit_calendar",
            () => EditScheduleAsync(period),
            Disabled: !period.CanEditSchedule,
            DisabledReason: "Không thể sửa lịch vì kỳ đã có đơn hoặc không còn ở trạng thái cho phép."));

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
            var settings = await PeriodsApi.GetCurrentSettingsAsync();
            Settings = settings is null ? NewDefaultSettings() : ToRequest(settings);
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

    private async Task EditScheduleAsync(VppManagedPeriodResDTO period)
    {
        var result = await OpenPeriodActionDialogAsync(period, EditAction);
        if (result is not VppOrderPeriodUpdateReqDTO request)
        {
            return;
        }

        await RunAsync(async () =>
        {
            request.RowVersion = period.RowVersion;
            _ = await PeriodsApi.UpdateAsync(period.Id, request);
            await LoadDataAsync();
            Toast.Success("Đã cập nhật lịch", $"Lịch kỳ {PeriodLabel(period)} đã được lưu.");
        });
    }

    private async Task ExtendDeadlineAsync(VppManagedPeriodResDTO period)
    {
        var result = await OpenPeriodActionDialogAsync(period, ExtendAction);
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

    private Task<object?> OpenPeriodActionDialogAsync(VppManagedPeriodResDTO period, string action) =>
        DialogService.OpenAsync<Dialog_OrderPeriodAction>(
            PeriodActionDialogTitle(action),
            new Dictionary<string, object?>
            {
                [nameof(Dialog_OrderPeriodAction.Period)] = period,
                [nameof(Dialog_OrderPeriodAction.Action)] = action,
                [nameof(Dialog_OrderPeriodAction.SupplementApprovalGraceDays)] = Settings.SupplementApprovalGraceDays
            },
            VppAdminDialogProfiles.Create(
                VppAdminDialogSize.Standard,
                PeriodActionDialogTitle(action),
                closeAriaLabel: "Đóng"));

    private static string PeriodActionDialogTitle(string action) => action switch
    {
        EditAction => "Sửa lịch kỳ",
        ExtendAction => "Gia hạn kỳ",
        _ => "Kỳ đặt hàng"
    };

    private void NavigateToSettlement(VppManagedPeriodResDTO period)
        => NavigationManager.NavigateTo(
            $"/dashboard?tab=5&periodTab=review&periodYear={period.Year}&periodMonth={period.Month}");

    private static bool CanSettlePeriod(VppManagedPeriodResDTO period) =>
        period.State is "SubmissionClosed" or "Pricing";

    private Task OnPeriodSearchInput(ChangeEventArgs args)
    {
        periodSearchText = args.Value?.ToString() ?? string.Empty;
        return Task.CompletedTask;
    }

    private Task OnPeriodStateChanged(string state)
    {
        selectedPeriodState = state;
        return Task.CompletedTask;
    }

    private Task OnPeriodYearChanged(int? year)
    {
        selectedPeriodYear = year;
        return Task.CompletedTask;
    }

    private Task ClearPeriodFiltersAsync()
    {
        periodSearchText = string.Empty;
        selectedPeriodState = string.Empty;
        selectedPeriodYear = null;
        return Task.CompletedTask;
    }

    private async Task LoadDataAsync()
    {
        Periods = (await PeriodsApi.ListAsync())
            .OrderByDescending(period => period.Year)
            .ThenByDescending(period => period.Month)
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

    private static VppOrderPeriodSettingsReqDTO ToRequest(VppOrderPeriodSettingsResDTO value) => new()
    {
        Name = value.Name,
        DefaultOpenPeriodCount = value.DefaultOpenPeriodCount,
        DefaultNewPeriodOpenDay = value.DefaultNewPeriodOpenDay,
        DefaultPeriodCloseDay = value.DefaultPeriodCloseDay,
        LocalTimeOfDay = value.LocalTimeOfDay,
        TimeZoneId = value.TimeZoneId,
        SupplementApprovalGraceDays = value.SupplementApprovalGraceDays,
        PostCloseAdjustmentDays = value.PostCloseAdjustmentDays,
        EffectiveFromYear = value.EffectiveFromYear,
        EffectiveFromMonth = value.EffectiveFromMonth
    };

    private static VppOrderPeriodSettingsReqDTO NewDefaultSettings()
    {
        var now = DateTime.Today;
        return new VppOrderPeriodSettingsReqDTO
        {
            Name = "Mặc định 3 kỳ",
            DefaultOpenPeriodCount = 3,
            DefaultNewPeriodOpenDay = 5,
            DefaultPeriodCloseDay = 5,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            SupplementApprovalGraceDays = 5,
            PostCloseAdjustmentDays = 10,
            EffectiveFromYear = now.Year,
            EffectiveFromMonth = now.Month
        };
    }

}
