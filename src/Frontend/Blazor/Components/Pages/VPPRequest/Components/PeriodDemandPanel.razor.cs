using System.Security.Claims;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

public partial class PeriodDemandPanel
{
    internal enum DemandViewMode { ByItem, ByOrder }

    [Inject] private IAPIServices ApiServices { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Parameter] public IEnumerable<Claim>? Claims { get; set; }
    [Parameter] public int Year { get; set; }
    [Parameter] public int Month { get; set; }
    [Parameter] public EventCallback OnContinueToSupply { get; set; }

    private DemandViewMode viewMode = DemandViewMode.ByItem;
    private AggregatedVppResDTO? demand;
    private int pendingAdditionalCount;
    private bool isLoading = true;
    private int loadedYear;
    private int loadedMonth;
    private string searchText = string.Empty;
    private string selectedCategory = string.Empty;
    private string selectedUom = string.Empty;

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText)
        || !string.IsNullOrWhiteSpace(selectedCategory)
        || !string.IsNullOrWhiteSpace(selectedUom);

    private List<AggregatedVppItemResDTO> FilteredItems => demand?.Items
        .Where(item => string.IsNullOrWhiteSpace(searchText)
            || (item.VppName?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || (item.VppCode?.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false))
        .Where(item => string.IsNullOrWhiteSpace(selectedCategory)
            || string.Equals(item.CategoryName, selectedCategory, StringComparison.CurrentCultureIgnoreCase))
        .Where(item => string.IsNullOrWhiteSpace(selectedUom)
            || string.Equals(item.UomName, selectedUom, StringComparison.CurrentCultureIgnoreCase))
        .ToList() ?? [];

    private IReadOnlyList<VppFilterOption<string>> CategoryOptions =>
        BuildOptions(demand?.Items.Select(item => item.CategoryName), Loc["HistoryAllCategories"]);
    private IReadOnlyList<VppFilterOption<string>> UomOptions =>
        BuildOptions(demand?.Items.Select(item => item.UomName), Loc["HistoryAllUnits"]);

    protected override async Task OnParametersSetAsync()
    {
        if (Year < 2024 || Month is < 1 or > 12 || (loadedYear == Year && loadedMonth == Month)) return;
        loadedYear = Year;
        loadedMonth = Month;
        searchText = string.Empty;
        selectedCategory = string.Empty;
        selectedUom = string.Empty;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        isLoading = true;
        try
        {
            demand = await ApiServices.GetFromApiAsync<AggregatedVppResDTO>($"{Config.VppApi.PeriodDemand}?year={Year}&month={Month}");
            var status = await ApiServices.GetFromApiAsync<PeriodSettlementResDTO>(string.Format(Config.RequestApi.PeriodSettlement.Status, Year, Month));
            pendingAdditionalCount = status?.PendingAdditionalCount ?? 0;
        }
        catch (Exception ex)
        {
            demand = null;
            pendingAdditionalCount = 0;
            Toast.Error(ex, Loc);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void SetViewMode(DemandViewMode mode) => viewMode = mode;
    private void OnSearchInput(ChangeEventArgs args) => searchText = args.Value?.ToString() ?? string.Empty;
    private Task OnCategoryChanged(string value) { selectedCategory = value; return Task.CompletedTask; }
    private Task OnUomChanged(string value) { selectedUom = value; return Task.CompletedTask; }
    private Task ClearFilters() { searchText = string.Empty; selectedCategory = string.Empty; selectedUom = string.Empty; return Task.CompletedTask; }

    private static IReadOnlyList<VppFilterOption<string>> BuildOptions(IEnumerable<string?>? values, string allLabel)
    {
        var options = new List<VppFilterOption<string>> { new(string.Empty, allLabel) };
        options.AddRange((values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .Select(value => new VppFilterOption<string>(value, value)));
        return options;
    }
}
