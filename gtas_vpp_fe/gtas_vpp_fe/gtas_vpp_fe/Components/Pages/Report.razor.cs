using System.Globalization;
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
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    protected ReportSummaryResDTO? Summary { get; private set; }
    protected ReportInsightResDTO? Insight { get; private set; }
    protected bool IsLoading { get; private set; }
    protected bool IsExporting { get; private set; }
    protected bool IsLoadingInsights { get; private set; }
    private readonly AsyncLoadVersion _loadVersion = new();
    protected string SelectedScope { get; set; } = ReportScopes.Own;
    protected int? SelectedYear { get; set; }
    protected int? SelectedMonth { get; set; }

    protected bool CanViewReport => ScopeOptions.Count > 0;
    protected bool CanExport => PermissionState.HasPermission(Permissions.ReportExport);
    protected IReadOnlyList<int> AvailableYears => Summary?.AvailableYears ?? [];
    protected IReadOnlyList<StatusChartPoint> StatusChartData => Summary?.StatusBreakdown
        .Select(item => new StatusChartPoint(Localizer[item.ResourceKey], item.OrderCount))
        .ToList() ?? [];

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
        SelectedScope = ScopeOptions.LastOrDefault()?.Value ?? ReportScopes.Own;
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
        var loadVersion = _loadVersion.Begin();
        try
        {
            var summary = await Api.GetFromApiAsync<ReportSummaryResDTO>(BuildEndpoint("summary"));
            if (_loadVersion.IsCurrent(loadVersion))
            {
                Summary = summary;
            }
        }
        catch (Exception ex)
        {
            if (_loadVersion.IsCurrent(loadVersion))
            {
                Toast.Error(Localizer["Error"], UiErrorMapper.GetMessage(ex, Localizer));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task ExportAsync()
    {
        if (!CanExport || IsExporting)
        {
            return;
        }

        IsExporting = true;
        try
        {
            var file = await Api.GetFileFromApiAsync(BuildEndpoint("export.xlsx"));
            await using var stream = new MemoryStream(file.Content, writable: false);
            using var streamReference = new DotNetStreamReference(stream);
            await JS.InvokeVoidAsync("vppDownload.fromStream", file.FileName, streamReference);
            Toast.Success(Localizer["Success"], Localizer["ReportExported"]);
        }
        catch (Exception ex)
        {
            Toast.Error(Localizer["Error"], UiErrorMapper.GetMessage(ex, Localizer));
        }
        finally
        {
            IsExporting = false;
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

    protected RenderFragment Kpi(string icon, string label, string value) => builder =>
    {
        builder.OpenElement(0, "article");
        builder.AddAttribute(1, "class", "vpp-report-kpi");
        builder.OpenElement(2, "span");
        builder.AddAttribute(3, "class", $"vpp-report-kpi-icon rzi rzi-{icon}");
        builder.AddAttribute(4, "aria-hidden", "true");
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.OpenElement(6, "span");
        builder.AddContent(7, label);
        builder.CloseElement();
        builder.OpenElement(8, "strong");
        builder.AddContent(9, value);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    };

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

    protected sealed record StatusChartPoint(string Label, int OrderCount);
    protected sealed record ReportScopeOption(string Value, string ResourceKey);
}
