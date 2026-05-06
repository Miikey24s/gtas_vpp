using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.AI;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Pages.VPPRequest
{
    public partial class Page_OrderCreate : IDisposable
    {
        public sealed class ProductOption
        {
            public Guid Id { get; set; }
            public string? VPPCode { get; set; }
            public string? VPPName { get; set; }
            public string? UOMCode { get; set; }
            public string? UOMName { get; set; }
            public string? VPPCategoryName { get; set; }
        }

        public sealed class SelectedItem
        {
            public Guid VPPId { get; set; }
            public string? VPPCode { get; set; }
            public string? VPPName { get; set; }
            public int Qty { get; set; } = 1;
            public string? Description { get; set; }
        }

        private sealed class OrderDraft
        {
            public string? Description { get; set; }
            public List<SelectedItem> Items { get; set; } = new();
            public DateTime SavedAt { get; set; }
        }

        public sealed class CategoryOption
        {
            public Guid Id { get; set; }
            public string? CategoryName { get; set; }
        }

        private sealed class CategoryItem
        {
            public Guid Id { get; set; }
            public string? VPPCategoryCode { get; set; }
            public string? VPPCategoryName { get; set; }
        }

        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        [SupplyParameterFromQuery] public Guid? OrderId { get; set; }
        [SupplyParameterFromQuery(Name = "isAdditional")] public string? IsAdditionalParam { get; set; }
        [SupplyParameterFromQuery(Name = "copyFrom")] public string? CopyFromParam { get; set; }
        
        private bool _isAdditionalOverride;
        private bool _hasLoadedOrder;
        
        public bool IsAdditional 
        { 
            get
            {
                // If we've loaded an existing order, use the override value from database
                if (_hasLoadedOrder)
                    return _isAdditionalOverride;
                    
                // Otherwise, parse from query parameter
                return !string.IsNullOrWhiteSpace(IsAdditionalParam) && 
                       (IsAdditionalParam.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                        IsAdditionalParam == "1");
            }
        }

        public bool IsLoadingProducts { get; set; }
        public bool IsSaving { get; set; }
        public bool DraftRecovered { get; set; }
        public bool IsEdit => OrderId.HasValue;
        public bool IsCopyFromPrevious => !string.IsNullOrWhiteSpace(CopyFromParam) && CopyFromParam.Equals("previous", StringComparison.OrdinalIgnoreCase);
        public string? Description { get; set; }
        public DateTime? LastDraftSavedAt { get; set; }
        public int TotalQty => SelectedItems.Sum(x => x.Qty);
        public int ProductCount { get; set; }

        // AI Properties
        public bool IsAILoading { get; set; }
        public string? AISearchText { get; set; }
        public List<AISuggestedItemDTO> AISuggestions { get; set; } = new();

        public List<ProductOption> ProductOptions { get; set; } = new();
        public List<SelectedItem> SelectedItems { get; set; } = new();
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();
        public RadzenDataGrid<ProductOption>? productGrid;
        public RadzenDataGrid<SelectedItem>? selectedItemsGrid;
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
            
            // CHECK PERMISSION: Kiểm tra quyền tạo/sửa order
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
            
            if (IsEdit)
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
                // AI: Gợi ý ban đầu các văn phòng phẩm cơ bản
                _ = GetAISuggestionsAsync("Hãy gợi ý các văn phòng phẩm cơ bản và thiết yếu cho nhân viên văn phòng.");
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            var initialTasks = new List<Task>();

            if (productGrid != null)
            {
                initialTasks.Add(productGrid.Reload());
            }

            if (!IsEdit)
            {
                initialTasks.Add(TryRestoreDraftAsync());
            }

            if (initialTasks.Count == 0) return;

            await Task.WhenAll(initialTasks);
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadProductsAsync(LoadDataArgs args)
        {
            IsLoadingProducts = true;
            glb.isBusyPage = true;
            try
            {
                var endpoint = BuildProductsEndpoint(args);
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<ProductOption>>(endpoint);
                ProductOptions = result.Data ?? new();
                ProductCount = result.TotalCount;
            }
            catch (Exception ex)
            {
                ProductOptions = new();
                ProductCount = 0;
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Order",
                    Detail = $"Load products failed: {ex.Message}",
                    Duration = 5000
                });
            }
            finally
            {
                glb.isBusyPage = false;
                IsLoadingProducts = false;
                StateHasChanged();
            }
        }

        private static string BuildProductsEndpoint(LoadDataArgs args)
        {
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(args.Filter))
            {
                query.Add($"filter={Uri.EscapeDataString(args.Filter)}");
            }

            if (args.Skip.HasValue)
            {
                query.Add($"skip={args.Skip.Value}");
            }

            if (args.Top.HasValue)
            {
                query.Add($"top={args.Top.Value}");
            }

            if (!string.IsNullOrWhiteSpace(args.OrderBy))
            {
                query.Add($"orderby={Uri.EscapeDataString(args.OrderBy)}");
            }

            return query.Count == 0
                ? "/api/VPPRequest/products"
                : $"/api/VPPRequest/products?{string.Join("&", query)}";
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

                Description = editingOrder.Description;
                _isAdditionalOverride = editingOrder.IsAdditionalOrder;
                _hasLoadedOrder = true;
                SelectedItems = (editingOrder.Items ?? new())
                    .Select(x => new SelectedItem
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

                Description = previousOrder.Description;
                SelectedItems = (previousOrder.Items ?? new())
                    .Select(x => new SelectedItem
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
                    Detail = $"Copied {SelectedItems.Count} item(s) from previous order. Review and submit.",
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

        public async Task AddItemAsync(ProductOption product)
        {
            if (SelectedItems.Any(x => x.VPPId == product.Id)) return;

            SelectedItems.Add(new SelectedItem
            {
                VPPId = product.Id,
                VPPCode = product.VPPCode,
                VPPName = product.VPPName,
                Qty = 1
            });

            MarkDraftDirty();
            await SaveDraftAsync();
            
            // Refresh both grids to update UI
            await InvokeAsync(StateHasChanged);
            if (selectedItemsGrid != null)
            {
                await selectedItemsGrid.Reload();
            }
            if (productGrid != null)
            {
                await productGrid.Reload();
            }

            // AI: Gợi ý món đồ mua kèm (không chặn UI)
            _ = GetAISuggestionsAsync($"Người dùng vừa mua món {product.VPPName}. Hãy gợi ý các món đồ thường được mua kèm với nó.");
        }

        public async Task RemoveItemAsync(SelectedItem row)
        {
            SelectedItems.Remove(row);
            MarkDraftDirty();
            await SaveDraftAsync();
            
            // Refresh both grids to update UI
            await InvokeAsync(StateHasChanged);
            if (selectedItemsGrid != null)
            {
                await selectedItemsGrid.Reload();
            }
            if (productGrid != null)
            {
                await productGrid.Reload();
            }
        }

        public async Task ClearAllItemsAsync()
        {
            if (SelectedItems.Count == 0) return;

            var confirmed = await JS.InvokeAsync<bool>("confirm", "Are you sure you want to clear all selected items?");
            if (!confirmed) return;

            SelectedItems.Clear();
            MarkDraftDirty();
            await SaveDraftAsync();
            
            // Refresh both grids to update UI
            await InvokeAsync(StateHasChanged);
            if (selectedItemsGrid != null)
            {
                await selectedItemsGrid.Reload();
            }
            if (productGrid != null)
            {
                await productGrid.Reload();
            }

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Selected Items",
                Detail = "All items have been cleared.",
                Duration = 2500
            });
        }

        public Task OnDescriptionInput(ChangeEventArgs args)
        {
            Description = args.Value?.ToString();
            MarkDraftDirty();
            return Task.CompletedTask;
        }

        public async Task SaveDraftAsync(bool showMessage = false)
        {
            if (IsEdit) return;

            try
            {
                var draft = new OrderDraft
                {
                    Description = Description,
                    Items = SelectedItems,
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
            catch
            {
            }
        }

        private async Task TryRestoreDraftAsync()
        {
            try
            {
                var draftJson = await JS.InvokeAsync<string>("localStorage.getItem", DraftStorageKey);
                if (string.IsNullOrWhiteSpace(draftJson)) return;

                var draft = JsonSerializer.Deserialize<OrderDraft>(draftJson);
                if (draft == null) return;

                Description = draft.Description;
                SelectedItems = draft.Items ?? new();
                LastDraftSavedAt = draft.SavedAt;
                DraftRecovered = SelectedItems.Count > 0 || !string.IsNullOrWhiteSpace(Description);
                _draftDirty = false;
            }
            catch
            {
            }
        }

        private void MarkDraftDirty()
        {
            if (IsEdit) return;
            _draftDirty = true;
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
            catch (OperationCanceledException)
            {
            }
        }

        public async Task SubmitAsync()
        {
            if (SelectedItems.Count == 0)
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

            if (SelectedItems.Any(x => x.VPPId == Guid.Empty || x.Qty <= 0))
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

                var requestItems = SelectedItems.Select(x => new VPP02_ItemReqDTO
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
                        Description = Description,
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
                        Description = Description,
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
                
                // Log to console for debugging
                await JS.InvokeVoidAsync("console.error", "Order submission error:", ex.Message, ex.StackTrace);
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

        public async Task GetAISuggestionsAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return;

            IsAILoading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var request = new AIChatRequestDTO { Message = prompt };
                var response = await _apiServices.PostFromApiAsync<AIChatResponseDTO>("/api/AI/chat", request);

                if (response?.IsSuccess == true && response.SuggestedItems != null)
                {
                    // Lấy top 5 items có SimilarityScore cao nhất
                    AISuggestions = response.SuggestedItems
                        .OrderByDescending(x => x.SimilarityScore)
                        .Take(5)
                        .ToList();
                }
                else
                {
                    AISuggestions = new();
                }
            }
            catch (Exception ex)
            {
                // Silent fail - AI là tính năng phụ, không làm gián đoạn workflow chính
                await JS.InvokeVoidAsync("console.warn", "AI suggestion failed:", ex.Message);
                AISuggestions = new();
            }
            finally
            {
                IsAILoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        public async Task AddSuggestedItemAsync(AISuggestedItemDTO suggestedItem)
        {
            var product = ProductOptions.FirstOrDefault(x => x.Id == suggestedItem.VPPId)
                ?? new ProductOption
                {
                    Id = suggestedItem.VPPId,
                    VPPCode = suggestedItem.VPPCode,
                    VPPName = suggestedItem.VPPName,
                    UOMName = suggestedItem.UOMName,
                    VPPCategoryName = suggestedItem.CategoryName
                };

            await AddItemAsync(product);
        }

        public async Task OnAISearchAsync()
        {
            if (string.IsNullOrWhiteSpace(AISearchText)) return;
            await GetAISuggestionsAsync(AISearchText);
        }

        public void Dispose()
        {
            _draftAutoSaveCts?.Cancel();
            _draftAutoSaveTimer?.Dispose();
            _draftAutoSaveCts?.Dispose();
        }
    }
}
