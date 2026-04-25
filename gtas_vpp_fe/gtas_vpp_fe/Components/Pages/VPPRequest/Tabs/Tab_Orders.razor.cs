using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
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
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage? sp_Authentication_GetPermissionSinglePage { get; set; }

        public List<VPP01_RequestHeaderResDTO> ActiveOrders { get; set; } = new();
        public List<VPP01_RequestHeaderResDTO> PreviousOrders { get; set; } = new();
        public List<VPP01_RequestHeaderResDTO> AdditionalOrders { get; set; } = new();

        public bool IsLoading { get; set; }
        public bool ViewerVisible { get; set; }
        public VPP01_RequestHeaderResDTO? ViewingOrder { get; set; }

        public Guid? LastDeletedOrderId { get; set; }
        public string? LastDeletedOrderCode { get; set; }

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

        private bool CanView =>
            sp_Authentication_GetPermissionSinglePage?.List_Component?.Any(x =>
                (x.ComponentCode == Config.Page_ComponentCode.ComponentCode.RequestOrder && x.IsVisible)) == true;

        protected override async Task OnInitializedAsync()
        {
            await LoadOrdersAsync();
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

        protected async Task GoToEditPage(VPP01_RequestHeaderResDTO row)
        {
            NavigationManager.NavigateTo($"/dashboard/order-create?orderId={row.Id}");
            await Task.CompletedTask;
        }

        protected async Task UndoLastDeleteAsync()
        {
            if (LastDeletedOrderId == null) return;

            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"{Config.VppApi.Orders}/{LastDeletedOrderId}/undo-delete", new { });

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = "Delete has been undone.",
                    Duration = 3000
                });

                LastDeletedOrderId = null;
                LastDeletedOrderCode = null;
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Undo delete failed: {ex.Message}",
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

        protected async Task DeleteOrderAsync(VPP01_RequestHeaderResDTO row)
        {
            if (!CanEditOrDelete(row)) return;

            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"{Config.VppApi.Orders}/{row.Id}/delete", new { });
                LastDeletedOrderId = row.Id;
                LastDeletedOrderCode = row.VPPCode;
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = "Order deleted. You can Undo delete.",
                    Duration = 3000
                });
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Delete failed: {ex.Message}",
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
        protected bool CanEditOrDelete(VPP01_RequestHeaderResDTO row) => 
            IsSubmitted(row) || (row.IsAdditionalOrder && row.Status == 6); // Allow edit/delete for Submitted orders or Pending additional orders

        protected async Task SubmitOrderAsync(VPP01_RequestHeaderResDTO row)
        {
            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"{Config.VppApi.Orders}/{row.Id}/submit", new { });
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = "Order submitted.",
                    Duration = 3000
                });
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Submit failed: {ex.Message}",
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
            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"{Config.VppApi.Orders}/{row.Id}/cancel", new { });
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = "Order cancelled.",
                    Duration = 3000
                });
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
            5 => "Closed",
            6 => "Pending",
            7 => "Approved",
            8 => "Rejected",
            _ => "-"
        };

        protected BadgeStyle GetStatusBadgeStyle(int status) => status switch
        {
            1 => BadgeStyle.Success,
            4 => BadgeStyle.Danger,
            5 => BadgeStyle.Info,
            6 => BadgeStyle.Warning,
            7 => BadgeStyle.Success,
            8 => BadgeStyle.Danger,
            _ => BadgeStyle.Light
        };
    }
}