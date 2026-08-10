using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.Permission.Tabs;

public partial class Tab_OrderPeriodSettings
{
    [Inject] private OrderPeriodApiClient PeriodsApi { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    private static readonly string[] TimeZoneOptions = ["Asia/Ho_Chi_Minh", "UTC"];
    private bool IsLoading { get; set; } = true;
    private bool IsBusy { get; set; }
    private bool HasLoadError { get; set; }
    private VppOrderPeriodSettingsResDTO? Current { get; set; }
    private VppOrderPeriodSettingsReqDTO Settings { get; set; } = NewDefaults();
    private List<VppOrderPeriodSettingsResDTO> History { get; set; } = [];

    private string CurrentVersionLabel => Current is { VersionNumber: > 0 }
        ? $"v{Current.VersionNumber}"
        : Loc["DefaultConfiguration"];

    private string HistorySummary => Loc["SettingsVersionCount", History.Count];

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        HasLoadError = false;
        try
        {
            Current = await PeriodsApi.GetSettingsAsync();
            History = (await PeriodsApi.ListSettingsHistoryAsync()).ToList();
            Settings = Current is null ? NewDefaults() : ToRequest(Current);
        }
        catch (Exception ex)
        {
            HasLoadError = true;
            Toast.Error(Loc["OrderPeriodSettings"], UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync(VppOrderPeriodSettingsReqDTO _)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (Settings.SupplementApprovalGraceDays > Settings.PostCloseAdjustmentDays)
            {
                Toast.Warning(
                    Loc["OrderPeriodSettings"],
                    Loc["PostCloseAdjustmentMustCoverSupplement"]);
                return;
            }
            Settings.Name = Settings.Name.Trim();
            var saved = await PeriodsApi.SaveSettingsAsync(Settings);
            if (saved is null)
            {
                throw new InvalidOperationException(Loc["OrderPeriodSettingsSaveFailed"]);
            }

            Current = saved;
            History = (await PeriodsApi.ListSettingsHistoryAsync()).ToList();
            Settings = ToRequest(saved);
            Toast.Success(Loc["Saved"], Loc["OrderPeriodSettingsSavedDescription"]);
        }
        catch (Exception ex)
        {
            Toast.Error(Loc["OrderPeriodSettings"], UiErrorMapper.GetMessage(ex, Loc));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnInvalidSubmit(FormInvalidSubmitEventArgs _)
        => Toast.Warning(Loc["OrderPeriodSettings"], Loc["OrderPeriodSettingsValidationWarning"]);

    private void ResetToCurrent()
        => Settings = Current is null ? NewDefaults() : ToRequest(Current);

    private static string FormatCreatedAt(DateTime? value)
        => value.HasValue ? DateFormatter.Format(value.Value, DateFormatter.LongDate) : "–";

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

    private static VppOrderPeriodSettingsReqDTO NewDefaults()
    {
        var today = DateTime.Today;
        return new VppOrderPeriodSettingsReqDTO
        {
            Name = "Mặc định 3 kỳ · ngày 05",
            DefaultOpenPeriodCount = 3,
            DefaultNewPeriodOpenDay = 5,
            DefaultPeriodCloseDay = 5,
            LocalTimeOfDay = TimeSpan.Zero,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            SupplementApprovalGraceDays = 5,
            PostCloseAdjustmentDays = 10,
            EffectiveFromYear = today.Year,
            EffectiveFromMonth = today.Month
        };
    }
}
