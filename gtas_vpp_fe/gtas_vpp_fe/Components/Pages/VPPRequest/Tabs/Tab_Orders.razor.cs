using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_Orders
    {
        public sealed class ProductOption
        {
            public Guid Id { get; set; }
            public string? VPPCode { get; set; }
            public string? VPPName { get; set; }
            public string Display => $"{VPPCode} - {VPPName}";
        }

        public sealed class EditOrderItem
        {
            public Guid VPPId { get; set; }
            public int Qty { get; set; } = 1;
            public string? Description { get; set; }
        }

        public sealed class EditOrderModel
        {
            public Guid Id { get; set; }
            public int Y { get; set; }
            public int M { get; set; }
            public string? Description { get; set; }
            public List<EditOrderItem> Items { get; set; } = new();
        }

        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Parameter] public IEnumerable<Claim>? claims { get; set; }

        public List<VPP01_RequestHeaderResDTO> ActiveOrders { get; set; } = new();
        public List<VPP01_RequestHeaderResDTO> PreviousOrders { get; set; } = new();
        public List<VPP01_RequestHeaderResDTO> AdditionalOrders { get; set; } = new();

        public bool IsLoading { get; set; }
        public bool ViewerVisible { get; set; }
        public VPP01_RequestHeaderResDTO? ViewingOrder { get; set; }
        public VPP_PeriodInfoResDTO? PeriodInfo { get; set; }

        // P1: Period dates are derived from PeriodInfo (BE truth) — never DateTime.Now.
        // Fallback to "current calendar month" only while PeriodInfo is still loading,
        // never to drive submit/edit logic (which is BE-validated anyway).
        public DateTime CurrentOrderPeriodDate => PeriodInfo is { } p
            ? new DateTime(p.CurrentPeriodYear, p.CurrentPeriodMonth, 1)
            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public DateTime PreviousOrderPeriodDate => PeriodInfo is { } p
            ? new DateTime(p.PreviousPeriodYear, p.PreviousPeriodMonth, 1)
            : CurrentOrderPeriodDate.AddMonths(-1);

        public DateTime CurrentDeadlineDate => PeriodInfo?.DeadlineDate
            ?? new DateTime(CurrentOrderPeriodDate.Year, CurrentOrderPeriodDate.Month, 1).AddMonths(1).AddDays(4);

        // Period end = day before the deadline. F-29: all date strings go through DateFormatter.
        public DateTime PeriodEndDate => CurrentDeadlineDate.AddDays(-1);
        public string PeriodEndText => DateFormatter.Format(PeriodEndDate, DateFormatter.ShortDate);
        public int RemainingDeadlineDays => Math.Max(0, (CurrentDeadlineDate.Date - DateTime.Today).Days);
        public string CurrentOrderPeriodText => DateFormatter.Format(CurrentOrderPeriodDate, DateFormatter.MonthYear);
        public string PreviousOrderPeriodText => DateFormatter.Format(PreviousOrderPeriodDate, DateFormatter.MonthYear);
        public string CurrentDeadlineText => DateFormatter.Format(CurrentDeadlineDate, DateFormatter.LongDate);
        public string RemainingDeadlineText => RemainingDeadlineDays == 0 ? Loc["DeadlineIsToday"].Value : string.Format(Loc["RemainingDeadlineDaysFormat"], RemainingDeadlineDays);
        public string OrdersTitle => string.Format(Loc["OrdersTitleFormat"], CurrentOrderPeriodText);
        public int TotalOrders => ActiveOrders.Count + PreviousOrders.Count + AdditionalOrders.Count;
        public string TotalOrdersText => TotalOrders.ToString();
        public int TotalLines => ActiveOrders.Sum(o => o.Items?.Count ?? 0) + PreviousOrders.Sum(o => o.Items?.Count ?? 0) + AdditionalOrders.Sum(o => o.Items?.Count ?? 0);
        public string TotalLinesText => TotalLines.ToString();
        public int TotalQty => ActiveOrders.Sum(o => o.Items?.Sum(i => i.Qty) ?? 0) + PreviousOrders.Sum(o => o.Items?.Sum(i => i.Qty) ?? 0) + AdditionalOrders.Sum(o => o.Items?.Sum(i => i.Qty) ?? 0);
        public string TotalQtyText => TotalQty.ToString();
        public int AvgLinesPerOrder => TotalOrders == 0 ? 0 : (int)Math.Round((double)TotalLines / TotalOrders);
        public string AvgLinesPerOrderText => string.Format(Loc["AverageLinesPerOrderFormat"], AvgLinesPerOrder);



        private bool CanView => PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestOrder);

        protected override async Task OnInitializedAsync()
        {
            // P1: Load PeriodInfo FIRST so derived dates (CurrentOrderPeriodDate etc.)
            // reflect BE truth before badges/headers render.
            await LoadPeriodInfoAsync();
            await LoadOrdersAsync();
        }

        private async Task LoadPeriodInfoAsync()
        {
            try
            {
                PeriodInfo = await _apiServices.GetFromApiAsync<VPP_PeriodInfoResDTO>($"{Config.VppApi.ApiVppBase}/period-info");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Tab_Orders] Failed to load period info: {ex.Message}");
            }
        }
        protected async Task LoadOrdersAsync()
        {
            if (!CanView) return;

            IsLoading = true;

            try
            {
                var endpoint = $"{Config.VppApi.MyOrders}?years={CurrentOrderPeriodDate.Year}&months={CurrentOrderPeriodDate.Month}&years={PreviousOrderPeriodDate.Year}&months={PreviousOrderPeriodDate.Month}";
                var data = await _apiServices.GetFromApiAsync<List<VPP01_RequestHeaderResDTO>>(endpoint);

                var allOrders = (data ?? new()).OrderByDescending(x => x.UpdateDate).ToList();
                
                // Separate orders by type
                ActiveOrders = allOrders.Where(x => !x.IsAdditionalOrder && x.Y == CurrentOrderPeriodDate.Year && x.M == CurrentOrderPeriodDate.Month).ToList();
                PreviousOrders = allOrders.Where(x => !x.IsAdditionalOrder && x.Y == PreviousOrderPeriodDate.Year && x.M == PreviousOrderPeriodDate.Month).ToList();
                AdditionalOrders = allOrders.Where(x => x.IsAdditionalOrder).OrderByDescending(x => x.SubmittedDate ?? x.UpdateDate).ToList();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Orders"],
                    Detail = string.Format(Loc["LoadOrdersFailedFormat"], ex.Message),
                    Duration = 6000
                });
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected async Task ReloadAsync() => await LoadOrdersAsync();

        protected async Task GoToCreatePage(bool isAdditional = false)
        {
            NavigationManager.NavigateTo($"/dashboard/order-create?isAdditional={isAdditional}");
            await Task.CompletedTask;
        }

        protected async Task CopyPreviousAsync()
        {
            NavigationManager.NavigateTo("/dashboard/order-create?copyFrom=previous");
            await Task.CompletedTask;
        }

        protected async Task GoToEditPage(VPP01_RequestHeaderResDTO row)
        {
            NavigationManager.NavigateTo($"/dashboard/order-create?orderId={row.Id}");
            await Task.CompletedTask;
        }

        protected async Task CancelOrderAsync(VPP01_RequestHeaderResDTO row)
        {
            if (!CanEditOrDelete(row)) return;

            IsLoading = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"{Config.VppApi.Orders}/{row.Id}/cancel", new { });
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = Loc["Order"],
                    Detail = Loc["OrderCancelledSuccess"],
                    Duration = 3000
                });
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Order"],
                    Detail = string.Format(Loc["CancelFailedFormat"], ex.Message),
                    Duration = 6000
                });
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected bool IsSubmitted(VPP01_RequestHeaderResDTO row) => row.Status == 1;
        protected bool CanEditOrDelete(VPP01_RequestHeaderResDTO row) => row.CanCancel;

        protected void ViewOrder(VPP01_RequestHeaderResDTO row)
        {
            ViewingOrder = row;
            ViewerVisible = true;
            StateHasChanged();
        }

        protected void CloseViewer()
        {
            ViewerVisible = false;
            ViewingOrder = null;
        }

        // P4/F-16: Status label + badge style now come from the shared
        // StatusDisplay / StatusDisplayRadzen helpers. Local switch removed.

        public HashSet<Guid> ExpandedOrderIds { get; set; } = new();

        public void ToggleOrderCode(Guid orderId)
        {
            if (ExpandedOrderIds.Contains(orderId))
                ExpandedOrderIds.Remove(orderId);
            else
                ExpandedOrderIds.Add(orderId);
        }

        public string GetShortCode(VPP01_RequestHeaderResDTO order)
        {
            var code = order.VPPCode;
            if (string.IsNullOrEmpty(code)) return "";
            var parts = code.Split('-');
            if (parts.Length >= 2)
            {
                if (order.IsAdditionalOrder)
                {
                    return $"{parts[0]}-ADD-{parts[1]}";
                }
                return $"{parts[0]}-{parts[1]}";
            }
            return code.Length > 10 ? code.Substring(0, 10) : code;
        }

        public async Task CopyToClipboard(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
                var isVi = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = isVi ? "Đã sao chép" : "Copied",
                    Detail = isVi ? $"Đã sao chép mã đơn hàng: {text}!" : $"Copied order code: {text}!",
                    Duration = 4000
                });
            }
            catch (Exception) { }
        }
    }
}
