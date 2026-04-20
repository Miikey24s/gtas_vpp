using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
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
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage? sp_Authentication_GetPermissionSinglePage { get; set; }

        public List<VPP01_RequestHeaderResDTO> Orders { get; set; } = new();
        public List<ProductOption> ProductOptions { get; set; } = new();

        public bool IsLoading { get; set; }
        public bool ViewerVisible { get; set; }
        public VPP01_RequestHeaderResDTO? ViewingOrder { get; set; }

        public bool EditorVisible { get; set; }
        public bool EditorIsEdit { get; set; }
        public EditOrderModel EditorModel { get; set; } = new();
        public Radzen.Blazor.RadzenDataGrid<EditOrderItem>? EditorItemsGrid { get; set; }
        public Guid? LastDeletedOrderId { get; set; }
        public string? LastDeletedOrderCode { get; set; }
        protected RadzenDataGrid<VPP01_RequestHeaderResDTO> ordersGrid;

        public DateTime CurrentOrderPeriodDate
        {
            get
            {
                var now = DateTime.Now;
                var currentMonth = new DateTime(now.Year, now.Month, 1);
                return now.Day > 5 ? currentMonth.AddMonths(1) : currentMonth;
            }
        }

        public DateTime CurrentDeadlineDate => new(CurrentOrderPeriodDate.Year, CurrentOrderPeriodDate.Month, 5);
        public int RemainingDeadlineDays => Math.Max(0, (CurrentDeadlineDate.Date - DateTime.Today).Days);
        public string CurrentOrderPeriodText => $"{CurrentOrderPeriodDate:MM/yyyy}";
        public string CurrentDeadlineText => CurrentDeadlineDate.ToString("dd/MM/yyyy");
        public string RemainingDeadlineText => RemainingDeadlineDays == 0 ? "Deadline is today" : $"Remaining: {RemainingDeadlineDays} day(s)";

        private bool CanView =>
            sp_Authentication_GetPermissionSinglePage?.List_Component?.Any(x =>
                (x.ComponentCode == Config.Page_ComponentCode.ComponentCode.RequestOrder)) == true;

        protected override async Task OnInitializedAsync()
        {
            await LoadProductsAsync();
            await LoadOrdersAsync();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && Orders != null && Orders.Any())
            {
                // Tự động Expand tất cả các dòng hiện có vì số lượng rất ít
                foreach (var order in Orders)
                {
                    await ordersGrid.ExpandRow(order);
                }
                StateHasChanged();
            }
        }
        private async Task LoadProductsAsync()
        {
            try
            {
                ProductOptions = await _apiServices.GetFromApiAsync<List<ProductOption>>("/api/VPPRequest/products") ?? new();
            }
            catch
            {
                ProductOptions = new();
            }
        }

        protected async Task LoadOrdersAsync()
        {
            if (!CanView) return;

            IsLoading = true;
            glb.isBusyPage = true;

            try
            {
                var data = await _apiServices.GetFromApiAsync<List<VPP01_RequestHeaderResDTO>>("/api/VPPRequest/my-orders");

                // Chỉ lấy dữ liệu của kỳ hiện tại (CurrentOrderPeriod)
                Orders = (data ?? new())
                    .Where(x => x.Y == CurrentOrderPeriodDate.Year && x.M == CurrentOrderPeriodDate.Month)
                    .OrderByDescending(x => x.UpdateDate)
                    .ToList();
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

        protected async Task UndoLastDeleteAsync()
        {
            if (LastDeletedOrderId == null) return;

            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/orders/{LastDeletedOrderId}/undo-delete", new { });

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

        protected async Task OpenCreateDialogAsync()
        {
            EditorIsEdit = false;
            // Ép cứng Y và M theo kỳ hiện tại
            EditorModel = new EditOrderModel
            {
                Y = CurrentOrderPeriodDate.Year,
                M = CurrentOrderPeriodDate.Month,
                Items = new List<EditOrderItem> { new() }
            };
            EditorVisible = true;
            await InvokeAsync(StateHasChanged);
        }

        protected async Task OpenEditDialogAsync(VPP01_RequestHeaderResDTO row)
        {
            if (!CanEditOrDelete(row)) return;

            EditorIsEdit = true;
            EditorModel = new EditOrderModel
            {
                Id = row.Id,
                Y = row.Y,
                M = row.M,
                Description = row.Description,
                Items = (row.Items ?? new()).Select(x => new EditOrderItem
                {
                    VPPId = x.VPPId,
                    Qty = x.Qty,
                    Description = x.Description
                }).ToList()
            };

            if (EditorModel.Items.Count == 0)
            {
                EditorModel.Items.Add(new EditOrderItem());
            }

            EditorVisible = true;
            await InvokeAsync(StateHasChanged);
        }

        protected async Task AddEditorItem()
        {
            EditorModel.Items.Add(new EditOrderItem());
            if (EditorItemsGrid != null)
            {
                await EditorItemsGrid.Reload();
            }
        }

        protected async Task RemoveEditorItem(EditOrderItem item)
        {
            if (EditorModel.Items.Count <= 1) return;
            EditorModel.Items.Remove(item);

            if (EditorItemsGrid != null)
            {
                await EditorItemsGrid.Reload();
            }
        }

        protected void CancelEditor()
        {
            EditorVisible = false;
            EditorModel = new EditOrderModel();
        }

        protected async Task SaveEditorAsync()
        {
            if (DateTime.Now > CurrentDeadlineDate)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Order",
                    Detail = "Cannot create/update order for a period that has passed deadline.",
                    Duration = 4000
                });
                return;
            }

            if (EditorModel.Items.Count == 0 || EditorModel.Items.Any(x => x.VPPId == Guid.Empty))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Order",
                    Detail = "Please select at least one valid product.",
                    Duration = 3500
                });
                return;
            }

            if (EditorModel.Items.GroupBy(x => x.VPPId).Any(g => g.Count() > 1))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Order",
                    Detail = "Duplicate product is not allowed.",
                    Duration = 3500
                });
                return;
            }

            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                var requestItems = EditorModel.Items
                    .Select(x => new VPP02_ItemReqDTO
                    {
                        VPPId = x.VPPId,
                        Qty = x.Qty,
                        Description = x.Description
                    })
                    .ToList();

                if (EditorIsEdit)
                {
                    var updateReq = new VPP01_UpdateReqDTO
                    {
                        Id = EditorModel.Id,
                        Status = 1,
                        Description = EditorModel.Description,
                        Items = requestItems
                    };

                    await _apiServices.PutFromApiAsync<VPP01_RequestHeaderResDTO>($"/api/VPPRequest/orders/{EditorModel.Id}", updateReq);
                }
                else
                {
                    var createReq = new VPP01_CreateReqDTO
                    {
                        Y = EditorModel.Y,
                        M = EditorModel.M,
                        Status = 1,
                        Description = EditorModel.Description,
                        Items = requestItems
                    };

                    await _apiServices.PostFromApiAsync<VPP01_RequestHeaderResDTO>("/api/VPPRequest/orders", createReq);
                }

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = EditorIsEdit ? "Order updated." : "Order created.",
                    Duration = 3000
                });

                EditorVisible = false;
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Save failed: {ex.Message}",
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
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/orders/{row.Id}/delete", new { });
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
        protected bool CanEditOrDelete(VPP01_RequestHeaderResDTO row) => IsSubmitted(row);

        protected async Task SubmitOrderAsync(VPP01_RequestHeaderResDTO row)
        {
            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/orders/{row.Id}/submit", new { });
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
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/orders/{row.Id}/cancel", new { });
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
            _ => "-"
        };

        protected BadgeStyle GetStatusBadgeStyle(int status) => status switch
        {
            1 => BadgeStyle.Success,
            4 => BadgeStyle.Danger,
            5 => BadgeStyle.Info,
            _ => BadgeStyle.Light
        };
    }
}