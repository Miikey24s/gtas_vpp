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
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Parameter] public IEnumerable<Claim>? claims { get; set; }

        public List<VPP01_RequestHeaderResDTO> ActiveOrders { get; set; } = new();
        public List<VPP01_RequestHeaderResDTO> PreviousOrders { get; set; } = new();
        public List<VPP01_RequestHeaderResDTO> AdditionalOrders { get; set; } = new();

        public bool IsLoading { get; set; }
        public bool ViewerVisible { get; set; }
        public VPP01_RequestHeaderResDTO? ViewingOrder { get; set; }

        public Guid? LastDeletedOrderId { get; set; }
        public string? LastDeletedOrderCode { get; set; }
        public VPP_PeriodInfoResDTO? PeriodInfo { get; set; }

        public DateTime CurrentOrderPeriodDate
        {
            get
            {
                var now = DateTime.Now;
                var currentMonth = new DateTime(now.Year, now.Month, 1);
                return now.Day >= 5 ? currentMonth.AddMonths(1) : currentMonth;
            }
        }
        
        public DateTime PreviousOrderPeriodDate => CurrentOrderPeriodDate.AddMonths(-1);

        public DateTime CurrentDeadlineDate => new(CurrentOrderPeriodDate.Year, CurrentOrderPeriodDate.Month, 5);
        public DateTime PeriodEndDate => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 5).AddMonths(2);
        public string PeriodEndText => PeriodEndDate.ToString("dd/MM/yyyy");
        public int RemainingDeadlineDays => Math.Max(0, (CurrentDeadlineDate.Date - DateTime.Today).Days);
        public string CurrentOrderPeriodText => $"{CurrentOrderPeriodDate:MM/yyyy}";
        public string PreviousOrderPeriodText => $"{PreviousOrderPeriodDate:MM/yyyy}";
        public string CurrentDeadlineText => CurrentDeadlineDate.ToString("HH:mm dd/MM/yyyy");
        public string RemainingDeadlineText => RemainingDeadlineDays == 0 ? "Deadline is today" : $"Remaining: {RemainingDeadlineDays} day(s)";
        public string OrdersTitle => $"Orders - {CurrentOrderPeriodText}";
        public int TotalOrders => ActiveOrders.Count + PreviousOrders.Count + AdditionalOrders.Count;
        public string TotalOrdersText => TotalOrders.ToString();
        public int TotalLines => ActiveOrders.Sum(o => o.Items?.Count ?? 0) + PreviousOrders.Sum(o => o.Items?.Count ?? 0) + AdditionalOrders.Sum(o => o.Items?.Count ?? 0);
        public string TotalLinesText => TotalLines.ToString();
        public int TotalQty => ActiveOrders.Sum(o => o.Items?.Sum(i => i.Qty) ?? 0) + PreviousOrders.Sum(o => o.Items?.Sum(i => i.Qty) ?? 0) + AdditionalOrders.Sum(o => o.Items?.Sum(i => i.Qty) ?? 0);
        public string TotalQtyText => TotalQty.ToString();
        public int AvgLinesPerOrder => TotalOrders == 0 ? 0 : (int)Math.Round((double)TotalLines / TotalOrders);
        public string AvgLinesPerOrderText => $"Average {AvgLinesPerOrder} line(s) per order";

        private bool CanView => claims.HasPermission(Permissions.RequestOrder);

        protected override async Task OnInitializedAsync()
        {
            await LoadOrdersAsync();
            await LoadPeriodInfoAsync();
            await RestoreUndoStateAsync();
        }

        private async Task LoadPeriodInfoAsync()
        {
            try
            {
                PeriodInfo = await _apiServices.GetFromApiAsync<VPP_PeriodInfoResDTO>($"{Config.VppApi.ApiVppBase}/period-info");
            }
            catch { }
        }

        private async Task RestoreUndoStateAsync()
        {
            try
            {
                var idStr = await JS.InvokeAsync<string>("localStorage.getItem", "vpp.lastCancelledOrderId");
                if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var id))
                {
                    LastDeletedOrderId = id;
                    LastDeletedOrderCode = await JS.InvokeAsync<string>("localStorage.getItem", "vpp.lastCancelledOrderCode");
                }
            }
            catch { }
        }
        protected async Task LoadOrdersAsync()
        {
            if (!CanView) return;

            IsLoading = true;
            glb.isBusyPage = true;

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
                    Summary = "Orders",
                    Detail = $"Load orders failed: {ex.Message}",
                    Duration = 6000
                });
            }
            finally
            {
                glb.isBusyPage = false;
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

        protected async Task UndoLastCancelAsync()
        {
            if (LastDeletedOrderId == null) return;

            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"{Config.VppApi.Orders}/{LastDeletedOrderId}/undo-cancel", new { });

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = "Cancel has been undone.",
                    Duration = 3000
                });

                LastDeletedOrderId = null;
                LastDeletedOrderCode = null;
                await JS.InvokeVoidAsync("localStorage.removeItem", "vpp.lastCancelledOrderId");
                await JS.InvokeVoidAsync("localStorage.removeItem", "vpp.lastCancelledOrderCode");
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Undo cancel failed: {ex.Message}",
                    Duration = 6000
                });
            }
            finally
            {
                glb.isBusyPage = false;
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected async Task CancelOrderAsync(VPP01_RequestHeaderResDTO row)
        {
            if (!CanEditOrDelete(row)) return;

            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"{Config.VppApi.Orders}/{row.Id}/cancel", new { });
                LastDeletedOrderId = row.Id;
                LastDeletedOrderCode = row.VPPCode;
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = "Order cancelled. You can Undo.",
                    Duration = 3000
                });
                // Persist undo state to localStorage
                await JS.InvokeVoidAsync("localStorage.setItem", "vpp.lastCancelledOrderId", row.Id.ToString());
                await JS.InvokeVoidAsync("localStorage.setItem", "vpp.lastCancelledOrderCode", row.VPPCode ?? "");
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Cancel failed: {ex.Message}",
                    Duration = 6000
                });
            }
            finally
            {
                glb.isBusyPage = false;
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

        protected string GetStatusText(int status) => status switch
        {
            1 => "Submitted",
            4 => "Cancelled",
            6 => "Pending",
            7 => "Approved",
            8 => "Rejected",
            _ => "-"
        };

        protected BadgeStyle GetStatusBadgeStyle(int status) => status switch
        {
            1 => BadgeStyle.Success,
            4 => BadgeStyle.Danger,
            6 => BadgeStyle.Warning,
            7 => BadgeStyle.Success,
            8 => BadgeStyle.Danger,
            _ => BadgeStyle.Light
        };
    }
}
