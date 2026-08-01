using System.Globalization;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace gtas_vpp_fe.Components.Pages;

public abstract class ReportBase : ComponentBase, IDisposable
{
    [Inject] protected PermissionState PermissionState { get; set; } = default!;
    [Inject] protected IAPIServices Api { get; set; } = default!;
    [Inject] protected IToastService Toast { get; set; } = default!;
    [Inject] protected IBrowserFileDownloadService FileDownloads { get; set; } = default!;

    protected ReportSummaryResDTO? Summary { get; private set; }
    protected ReportInsightResDTO? Insight { get; private set; }
    protected string? LoadError { get; private set; }
    protected bool IsLoading { get; private set; }
    protected VppFileExportFormat? ExportingFormat { get; private set; }
    protected bool IsLoadingInsights { get; private set; }
    private readonly AsyncLoadVersion _loadVersion = new();
    protected string SelectedScope { get; set; } = ReportScopes.Own;
    protected int? SelectedYear { get; set; }
    protected int? SelectedMonth { get; set; }
    protected string DepartmentSearchText { get; set; } = string.Empty;

    protected bool CanViewReport => ScopeOptions.Count > 0;
    protected bool CanExport => PermissionState.HasPermission(Permissions.ReportExport);
    protected static IReadOnlyList<VppFileExportFormat> ReportExportFormats { get; } =
        [VppFileExportFormat.Pdf, VppFileExportFormat.Excel, VppFileExportFormat.Csv];
    protected IReadOnlyList<int> AvailableYears => Summary?.AvailableYears ?? [];
    protected IReadOnlyList<ReportMonthOption> MonthOptions { get; } = Enumerable.Range(1, 12)
        .Select(month => new ReportMonthOption(month, month.ToString("00", CultureInfo.InvariantCulture)))
        .ToArray();
    protected IReadOnlyList<ReportScopeDisplayOption> LocalizedScopeOptions => ScopeOptions
        .Select(option => new ReportScopeDisplayOption(option.Value, Localizer[option.ResourceKey].Value))
        .ToArray();
    protected IReadOnlyList<VppFilterOption<string>> ScopeFilterOptions => LocalizedScopeOptions
        .Select(option => new VppFilterOption<string>(option.Value, option.Label))
        .ToArray();
    protected IReadOnlyList<VppFilterOption<int?>> YearFilterOptions =>
        [new(null, Localizer["AllYears"]), .. AvailableYears.Select(year => new VppFilterOption<int?>(year, year.ToString(CultureInfo.InvariantCulture)))];
    protected IReadOnlyList<VppFilterOption<int?>> MonthFilterOptions =>
        [new(null, Localizer["AllMonths"]), .. MonthOptions.Select(month => new VppFilterOption<int?>(month.Value, month.Label))];
    protected IReadOnlyList<ReportDepartmentPointResDTO> FilteredDepartmentBreakdown => Summary?.DepartmentBreakdown
        .Where(item => string.IsNullOrWhiteSpace(DepartmentSearchText)
            || item.Code.Contains(DepartmentSearchText.Trim(), StringComparison.OrdinalIgnoreCase))
        .ToList() ?? [];
    protected IReadOnlyList<StatusChartPoint> StatusChartData => Summary?.StatusBreakdown
        .Where(item => item.OrderCount > 0)
        .Select(item => new StatusChartPoint(Localizer[item.ResourceKey], item.OrderCount))
        .ToList() ?? [];
    protected bool HasPeriodTrend => Summary?.PeriodTrend.Count >= 2;
    protected bool CanSmoothPeriodTrend => Summary?.PeriodTrend.Count >= 3;
    protected bool HasStatusChartData => StatusChartData.Count > 0;
    protected bool HasReportFilters => !string.IsNullOrWhiteSpace(DepartmentSearchText)
        || SelectedYear.HasValue
        || SelectedMonth.HasValue
        || !string.Equals(SelectedScope, GetDefaultScope(), StringComparison.Ordinal);
    protected IReadOnlyList<string> ReportStatusFills { get; } =
    [
        "var(--vpp-info)",
        "var(--vpp-success)",
        "var(--vpp-warning)",
        "var(--vpp-danger)"
    ];

    protected IReadOnlyList<ReportScopeOption> ScopeOptions
    {
        get
        {
            var options = new List<ReportScopeOption>();
            if (PermissionState.HasPermission(Permissions.ReportViewOwn))
                options.Add(new(ReportScopes.Own, "ScopeOwn"));
            if (PermissionState.HasPermission(Permissions.ReportViewDepartment))
                options.Add(new(ReportScopes.Department, "ScopeDepartment"));
            if (PermissionState.HasPermission(Permissions.ReportViewAll))
                options.Add(new(ReportScopes.All, "ScopeAll"));
            return options;
        }
    }

    [Inject] protected Microsoft.Extensions.Localization.IStringLocalizer<App> Localizer { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        PermissionState.Changed += OnPermissionStateChanged;
        await PermissionState.EnsureLoadedAsync();
        SelectedScope = GetDefaultScope();
        if (CanViewReport)
        {
            await LoadAsync();
        }
    }

    protected async Task LoadAsync()
    {
        if (!CanViewReport || IsLoading)
        {
            return;
        }

        if (!ScopeOptions.Any(option => option.Value == SelectedScope))
        {
            SelectedScope = ScopeOptions.Last().Value;
        }

        IsLoading = true;
        Insight = null;
        LoadError = null;
        var loadVersion = _loadVersion.Begin();
        try
        {
            var summary = await Api.GetFromApiAsync<ReportSummaryResDTO>(BuildEndpoint("summary"));
            if (_loadVersion.IsCurrent(loadVersion))
            {
                Summary = summary;
                LoadError = null;
            }
        }
        catch (Exception ex)
        {
            if (_loadVersion.IsCurrent(loadVersion))
            {
                LoadError = UiErrorMapper.GetMessage(ex, Localizer, "ReportLoadFailed");
                Toast.Error(Localizer["Error"], UiErrorMapper.GetMessage(ex, Localizer));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected Task OnScopeChanged(string value)
    {
        SelectedScope = value;
        return Task.CompletedTask;
    }

    protected Task OnYearChanged(int? value)
    {
        SelectedYear = value;
        return Task.CompletedTask;
    }

    protected Task OnMonthChanged(int? value)
    {
        SelectedMonth = value;
        return Task.CompletedTask;
    }

    protected async Task ExportAsync(VppFileExportFormat format)
    {
        if (!CanExport || ExportingFormat.HasValue)
        {
            return;
        }

        ExportingFormat = format;
        try
        {
            var result = await FileDownloads.DownloadFromApiAsync(BuildEndpoint(format.ApiSuffix()));
            Toast.Success(Localizer["Success"], Localizer["ExportCompleted", result.FileName, FileSizeFormatter.Format(result.Size)]);
        }
        catch (Exception ex)
        {
            Toast.Error(Localizer["Error"], UiErrorMapper.GetMessage(ex, Localizer));
        }
        finally
        {
            ExportingFormat = null;
        }
    }

    protected async Task GenerateInsightsAsync()
    {
        if (Summary is null || Summary.TotalOrders == 0 || IsLoadingInsights)
        {
            return;
        }

        IsLoadingInsights = true;
        try
        {
            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? "en" : "vi";
            Insight = await Api.GetFromApiAsync<ReportInsightResDTO>(
                $"{BuildEndpoint("insights")}&language={language}");
        }
        catch (Exception ex)
        {
            Toast.Error(Localizer["Error"], UiErrorMapper.GetMessage(ex, Localizer));
        }
        finally
        {
            IsLoadingInsights = false;
        }
    }

    protected void OnDepartmentSearchInput(ChangeEventArgs args)
    {
        DepartmentSearchText = args.Value?.ToString() ?? string.Empty;
    }

    protected async Task ClearReportFiltersAsync()
    {
        DepartmentSearchText = string.Empty;
        SelectedScope = GetDefaultScope();
        SelectedYear = null;
        SelectedMonth = null;
        await LoadAsync();
    }

    protected RenderFragment InsightList(string title, string icon, IReadOnlyList<string> items) => builder =>
    {
        builder.OpenElement(0, "article");
        builder.OpenElement(1, "h3");
        builder.OpenElement(2, "span");
        builder.AddAttribute(3, "class", $"rzi rzi-{icon}");
        builder.AddAttribute(4, "aria-hidden", "true");
        builder.CloseElement();
        builder.AddContent(5, title);
        builder.CloseElement();
        builder.OpenElement(6, "ul");
        foreach (var item in items)
        {
            builder.OpenElement(7, "li");
            builder.AddContent(8, item);
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.CloseElement();
    };

    protected static string FormatAmount(long value) =>
        value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";

    protected static string FormatDecimalAmount(decimal? value) =>
        value.HasValue
            ? value.Value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫"
            : "—";

    protected static string FormatGeneratedAt(DateTime value) =>
        value.ToLocalTime().ToString("HH:mm dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"));

    protected static string FormatChartAmount(object value) =>
        Convert.ToInt64(value, CultureInfo.InvariantCulture)
            .ToString("N0", CultureInfo.CurrentUICulture);

    private string BuildEndpoint(string action)
    {
        var query = $"scope={Uri.EscapeDataString(SelectedScope)}";
        if (SelectedYear.HasValue) query += $"&year={SelectedYear.Value}";
        if (SelectedMonth.HasValue) query += $"&month={SelectedMonth.Value}";
        return $"api/reports/{action}?{query}";
    }

    private void OnPermissionStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose() => PermissionState.Changed -= OnPermissionStateChanged;

    private string GetDefaultScope() => ScopeOptions.LastOrDefault()?.Value ?? ReportScopes.Own;

    protected sealed record StatusChartPoint(string Label, int OrderCount);
    protected sealed record ReportScopeOption(string Value, string ResourceKey);
    protected sealed record ReportScopeDisplayOption(string Value, string Label);
    protected sealed record ReportMonthOption(int Value, string Label);
}
