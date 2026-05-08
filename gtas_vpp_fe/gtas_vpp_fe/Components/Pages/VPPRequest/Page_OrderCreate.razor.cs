using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.AI;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using System.Security.Claims;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Pages.VPPRequest
{
    public partial class Page_OrderCreate : IDisposable
    {
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public NotificationService NotificationService { get; set; } = default!;
        [Inject] public GlobalClass glb { get; set; } = default!;

        [SupplyParameterFromQuery] public Guid? OrderId { get; set; }
        [SupplyParameterFromQuery(Name = "isAdditional")] public string? IsAdditionalParam { get; set; }
        [SupplyParameterFromQuery(Name = "copyFrom")] public string? CopyFromParam { get; set; }

        private bool _isAdditionalOverride;
        private bool _hasLoadedOrder;

        public bool IsAdditional
        {
            get
            {
                if (_hasLoadedOrder) return _isAdditionalOverride;
                return !string.IsNullOrWhiteSpace(IsAdditionalParam) &&
                       (IsAdditionalParam.Equals("true", StringComparison.OrdinalIgnoreCase) || IsAdditionalParam == "1");
            }
        }

        public bool IsSaving { get; set; }
        public bool IsEdit => OrderId.HasValue;
        public bool IsCopyFromPrevious => !string.IsNullOrWhiteSpace(CopyFromParam) && CopyFromParam.Equals("previous", StringComparison.OrdinalIgnoreCase);
        public DateTime? LastDraftSavedAt { get; set; }
        public bool DraftRecovered { get; set; }
        public int currentStep { get; set; }

        public OrderCreateContext Context { get; set; } = new();
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();

        private readonly ProductOptionEqualityComparer _productComparer = new();

        private sealed class ProductOptionEqualityComparer : IEqualityComparer<OrderCreateStep2.ProductOption>
        {
            public bool Equals(OrderCreateStep2.ProductOption? x, OrderCreateStep2.ProductOption? y) => x?.Id == y?.Id;
            public int GetHashCode(OrderCreateStep2.ProductOption obj) => obj.Id.GetHashCode();
        }

        private sealed class OrderDraft
        {
            public string? Description { get; set; }
            public List<OrderCreateContext.SelectedItem> Items { get; set; } = new();
            public DateTime SavedAt { get; set; }
        }

        private PeriodicTimer? _draftAutoSaveTimer;
        private CancellationTokenSource? _draftAutoSaveCts;
        private volatile bool _draftDirty;

        private string DraftStorageKey
        {
            get
            {
                var userId = Claims.FirstOrDefault(x => x.Type == "UserID")?.Value ?? "anonymous";
                var orderType = IsAdditional ? "additional" : "new";
                return $"vpp.order.draft.{userId}.{orderType}.{OrderId?.ToString() ?? "new"}";
            }
        }

        protected override async Task OnInitializedAsync()
        {
            var (isAuthenticated, userClaims) = await AuthHelper.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                NavigationManager.NavigateTo("logoutprocess", true);
                return;
            }

            Claims = userClaims;

            if (!Claims.HasPermission(Permissions.RequestOrder))
            {
                NotificationService.Notify(new NotificationMessage()
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Access Denied",
                    Detail = "You do not have permission to create or edit orders.",
                    Duration = 5000
                });
                NavigationManager.NavigateTo("/dashboard?tab=0", true);
                return;
            }

            // Initialize context
            Context.Mode = IsEdit ? "edit" : (IsCopyFromPrevious ? "copy" : (IsAdditional ? "additional" : "new"));
            Context.EditOrderId = OrderId;
            Context.IsAdditional = IsAdditional;

            if (IsEdit && OrderId.HasValue)
            {
                await LoadOrderForEditAsync();
            }
            else if (IsCopyFromPrevious)
            {
                await LoadPreviousOrderItemsAsync();
                StartDraftAutoSave();
            }
            else
            {
                StartDraftAutoSave();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            if (!IsEdit)
            {
                await TryRestoreDraftAsync();
            }

            Context.OnStateChanged += () => MarkDraftDirty();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadOrderForEditAsync()
        {
            if (!OrderId.HasValue) return;

            glb.isBusyPage = true;
            try
            {
                var editingOrder = await _apiServices.GetFromApiAsync<VPP01_RequestHeaderResDTO>($"{Config.VppApi.Orders}/{OrderId.Value}");
                if (editingOrder == null)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Order",
                        Detail = "Order to update was not found.",
                        Duration = 4000
                    });
                    GoBack();
                    return;
                }

                Context.Description = editingOrder.Description;
                _isAdditionalOverride = editingOrder.IsAdditionalOrder;
                _hasLoadedOrder = true;
                Context.IsAdditional = editingOrder.IsAdditionalOrder;
                Context.SelectedItems = (editingOrder.Items ?? new())
                    .Select(x => new OrderCreateContext.SelectedItem
                    {
                        VPPId = x.VPPId,
                        VPPCode = x.VPPCode,
                        VPPName = x.VPPName,
                        Qty = x.Qty,
                        Description = x.Description
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Load order failed: {ex.Message}",
                    Duration = 5000
                });
            }
            finally
            {
                glb.isBusyPage = false;
            }
        }

        private async Task LoadPreviousOrderItemsAsync()
        {
            glb.isBusyPage = true;
            try
            {
                var previousOrder = await _apiServices.GetFromApiAsync<VPP01_RequestHeaderResDTO>($"{Config.VppApi.ApiVppBase}/orders/previous-items");
                if (previousOrder == null)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Info,
                        Summary = "Copy Previous",
                        Detail = "No previous order found to copy from.",
                        Duration = 4000
                    });
                    return;
                }

                Context.Description = previousOrder.Description;
                Context.SelectedItems = (previousOrder.Items ?? new())
                    .Select(x => new OrderCreateContext.SelectedItem
                    {
                        VPPId = x.VPPId,
                        VPPCode = x.VPPCode,
                        VPPName = x.VPPName,
                        Qty = x.Qty,
                        Description = x.Description
                    })
                    .ToList();

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Copy Previous",
                    Detail = $"Copied {Context.SelectedItems.Count} item(s) from previous order. Review and submit.",
                    Duration = 4000
                });
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Copy Previous",
                    Detail = $"Failed to load previous order: {ex.Message}",
                    Duration = 5000
                });
            }
            finally
            {
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }

        public async Task SaveDraftAsync(bool showMessage = false)
        {
            if (IsEdit) return;

            try
            {
                var draft = new OrderDraft
                {
                    Description = Context.Description,
                    Items = Context.SelectedItems,
                    SavedAt = DateTime.Now
                };

                var json = JsonSerializer.Serialize(draft);
                await JS.InvokeVoidAsync("localStorage.setItem", DraftStorageKey, json);
                LastDraftSavedAt = draft.SavedAt;
                _draftDirty = false;

                if (showMessage)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Info,
                        Summary = "Order Draft",
                        Detail = "Draft saved in browser storage.",
                        Duration = 2500
                    });
                }
            }
            catch { }
        }

        private async Task TryRestoreDraftAsync()
        {
            try
            {
                var draftJson = await JS.InvokeAsync<string>("localStorage.getItem", DraftStorageKey);
                if (string.IsNullOrWhiteSpace(draftJson)) return;

                var draft = JsonSerializer.Deserialize<OrderDraft>(draftJson);
                if (draft == null) return;

                Context.Description = draft.Description;
                Context.SelectedItems = draft.Items ?? new();
                LastDraftSavedAt = draft.SavedAt;
                DraftRecovered = Context.SelectedItems.Count > 0 || !string.IsNullOrWhiteSpace(Context.Description);
                _draftDirty = false;
            }
            catch { }
        }

        private void MarkDraftDirty()
        {
            if (IsEdit) return;
            _draftDirty = true;
            _ = SaveDraftAsync();
        }

        private void StartDraftAutoSave()
        {
            _draftAutoSaveCts = new CancellationTokenSource();
            _draftAutoSaveTimer = new PeriodicTimer(TimeSpan.FromSeconds(8));
            _ = Task.Run(() => DraftAutoSaveLoopAsync(_draftAutoSaveCts.Token));
        }

        private async Task DraftAutoSaveLoopAsync(CancellationToken token)
        {
            try
            {
                while (_draftAutoSaveTimer != null && await _draftAutoSaveTimer.WaitForNextTickAsync(token))
                {
                    if (!_draftDirty) continue;
                    await InvokeAsync(async () => await SaveDraftAsync());
                }
            }
            catch (OperationCanceledException) { }
        }

        public async Task SubmitAsync()
        {
            if (Context.SelectedItems.Count == 0)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Order",
                    Detail = "Please select at least one product.",
                    Duration = 3000
                });
                return;
            }

            if (Context.SelectedItems.Any(x => x.VPPId == Guid.Empty || x.Qty <= 0))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Order",
                    Detail = "Invalid product or quantity.",
                    Duration = 3000
                });
                return;
            }

            IsSaving = true;
            glb.isBusyPage = true;
            try
            {
                var now = DateTime.Now;
                var currentMonth = new DateTime(now.Year, now.Month, 1);
                var period = now.Day >= 5 ? currentMonth.AddMonths(1) : currentMonth;
                if (IsAdditional)
                {
                    period = now.Day >= 5 ? currentMonth : currentMonth.AddMonths(-1);
                }

                var requestItems = Context.SelectedItems.Select(x => new VPP02_ItemReqDTO
                {
                    VPPId = x.VPPId,
                    Qty = x.Qty,
                    Description = x.Description
                }).ToList();

                if (IsEdit)
                {
                    var updateReq = new VPP01_UpdateReqDTO
                    {
                        Id = OrderId!.Value,
                        Description = Context.Description,
                        IsAdditionalOrder = IsAdditional,
                        Items = requestItems
                    };

                    await _apiServices.PutFromApiAsync<VPP01_RequestHeaderResDTO>($"{Config.VppApi.Orders}/{OrderId}", updateReq);
                }
                else
                {
                    var createReq = new VPP01_CreateReqDTO
                    {
                        Y = period.Year,
                        M = period.Month,
                        Description = Context.Description,
                        IsAdditionalOrder = IsAdditional,
                        Items = requestItems
                    };

                    await _apiServices.PostFromApiAsync<VPP01_RequestHeaderResDTO>(Config.VppApi.Orders, createReq);
                    await JS.InvokeVoidAsync("localStorage.removeItem", DraftStorageKey);
                }

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = IsEdit ? "Order updated successfully." : "Order created successfully.",
                    Duration = 3000
                });

                NavigationManager.NavigateTo("/dashboard?tab=0", true);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Failed to save order: {ex.Message}",
                    Duration = 6000
                });

                await JS.InvokeVoidAsync("console.error", "Order submission error:", ex.Message);
            }
            finally
            {
                glb.isBusyPage = false;
                IsSaving = false;
                StateHasChanged();
            }
        }

        public void GoBack()
        {
            NavigationManager.NavigateTo("/dashboard?tab=0");
        }

        public void Dispose()
        {
            _draftAutoSaveCts?.Cancel();
            _draftAutoSaveTimer?.Dispose();
            _draftAutoSaveCts?.Dispose();
        }
    }
}
