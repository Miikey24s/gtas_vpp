using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
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
        }

        public sealed class SelectedItem
        {
            public Guid VPPId { get; set; }
            public string? VPPCode { get; set; }
            public string? VPPName { get; set; }
            public int Qty { get; set; } = 1;
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

        public bool IsLoadingProducts { get; set; }
        public bool IsSaving { get; set; }
        public bool DraftRecovered { get; set; }
        public bool IsEdit => OrderId.HasValue;
        public string? SearchText { get; set; }
        public string? Description { get; set; }
        public DateTime? LastDraftSavedAt { get; set; }
        public int TotalQty => SelectedItems.Sum(x => x.Qty);

        public List<ProductOption> ProductOptions { get; set; } = new();
        public List<SelectedItem> SelectedItems { get; set; } = new();
        public List<CategoryOption> Categories { get; set; } = new();
        public IEnumerable<Guid>? SelectedCategoryIds { get; set; } = new List<Guid>();
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();
        public RadzenDataGrid<ProductOption>? productGrid;
        private PeriodicTimer? _draftAutoSaveTimer;
        private CancellationTokenSource? _draftAutoSaveCts;
        private volatile bool _draftDirty;

        private string DraftStorageKey
        {
            get
            {
                var userId = Claims.FirstOrDefault(x => x.Type == "UserID")?.Value ?? "anonymous";
                return $"vpp.order.draft.{userId}.{OrderId?.ToString() ?? "new"}";
            }
        }

        public IEnumerable<ProductOption> FilteredProducts => ProductOptions
            .Where(x => string.IsNullOrWhiteSpace(SearchText)
                || (x.VPPCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.VPPName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

        protected override async Task OnInitializedAsync()
        {
            var (isAuthenticated, userClaims) = await AuthHelper.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                NavigationManager.NavigateTo("logoutprocess", true);
                return;
            }

            Claims = userClaims;
            await LoadCategoriesAsync();
            await LoadProductsAsync();
            if (IsEdit)
            {
                await LoadOrderForEditAsync();
            }
            else
            {
                StartDraftAutoSave();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || IsEdit) return;
            await TryRestoreDraftAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadProductsAsync()
        {
            IsLoadingProducts = true;
            glb.isBusyPage = true;
            try
            {
                var selectedCategoryIds = SelectedCategoryIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();

                if (selectedCategoryIds.Count <= 1)
                {
                    var endpoint = BuildProductsEndpoint(selectedCategoryIds.FirstOrDefault());
                    ProductOptions = await _apiServices.GetFromApiAsync<List<ProductOption>>(endpoint) ?? new();
                }
                else
                {
                    var merged = new Dictionary<Guid, ProductOption>();
                    foreach (var categoryId in selectedCategoryIds)
                    {
                        var endpoint = BuildProductsEndpoint(categoryId);
                        var products = await _apiServices.GetFromApiAsync<List<ProductOption>>(endpoint) ?? new();
                        foreach (var product in products)
                        {
                            merged[product.Id] = product;
                        }
                    }

                    ProductOptions = merged.Values.ToList();
                }

                ProductOptions = ProductOptions
                    .OrderBy(x => x.VPPCode)
                    .ToList();
            }
            catch (Exception ex)
            {
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

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _apiServices.GetFromApiAsync<List<CategoryItem>>("/api/VPPRequest/categories") ?? new();
                Categories = categories
                    .Select(x => new CategoryOption
                    {
                        Id = x.Id,
                        CategoryName = $"{x.VPPCategoryCode} - {x.VPPCategoryName}"
                    })
                    .OrderBy(x => x.CategoryName)
                    .ToList();
            }
            catch
            {
                Categories = new();
            }
        }

        private string BuildProductsEndpoint(Guid? categoryId = null)
        {
            var query = new List<string>();
            if (categoryId.HasValue)
            {
                query.Add($"categoryId={categoryId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query.Add($"search={Uri.EscapeDataString(SearchText.Trim())}");
            }

            if (query.Count == 0)
            {
                return "/api/VPPRequest/products";
            }

            return $"/api/VPPRequest/products?{string.Join("&", query)}";
        }

        private async Task LoadOrderForEditAsync()
        {
            if (!OrderId.HasValue) return;

            glb.isBusyPage = true;
            try
            {
                var orders = await _apiServices.GetFromApiAsync<List<VPP01_RequestHeaderResDTO>>("/api/VPPRequest/my-orders") ?? new();
                var editingOrder = orders.FirstOrDefault(x => x.Id == OrderId.Value);
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
                SelectedItems = (editingOrder.Items ?? new())
                    .Select(x => new SelectedItem
                    {
                        VPPId = x.VPPId,
                        VPPCode = x.VPPCode,
                        VPPName = x.VPPName,
                        Qty = x.Qty
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

        public async Task ReloadProductGridAsync()
        {
            await LoadProductsAsync();

            if (productGrid != null)
            {
                await productGrid.Reload();
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
        }

        public async Task RemoveItemAsync(SelectedItem row)
        {
            SelectedItems.Remove(row);
            MarkDraftDirty();
            await SaveDraftAsync();
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

                var requestItems = SelectedItems.Select(x => new VPP02_ItemReqDTO
                {
                    VPPId = x.VPPId,
                    Qty = x.Qty
                }).ToList();

                if (IsEdit)
                {
                    var updateReq = new VPP01_UpdateReqDTO
                    {
                        Id = OrderId!.Value,
                        Status = 1,
                        Description = Description,
                        Items = requestItems
                    };

                    await _apiServices.PutFromApiAsync<VPP01_RequestHeaderResDTO>($"/api/VPPRequest/orders/{OrderId}", updateReq);
                }
                else
                {
                    var createReq = new VPP01_CreateReqDTO
                    {
                        Y = period.Year,
                        M = period.Month,
                        Status = 1,
                        Description = Description,
                        Items = requestItems
                    };

                    await _apiServices.PostFromApiAsync<VPP01_RequestHeaderResDTO>("/api/VPPRequest/orders", createReq);
                    await JS.InvokeVoidAsync("localStorage.removeItem", DraftStorageKey);
                }

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Order",
                    Detail = IsEdit ? "Order updated successfully." : "Order created successfully.",
                    Duration = 3000
                });

                NavigationManager.NavigateTo("/dashboard/orders", true);
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
            NavigationManager.NavigateTo("/dashboard/orders");
        }

        public void Dispose()
        {
            _draftAutoSaveCts?.Cancel();
            _draftAutoSaveTimer?.Dispose();
            _draftAutoSaveCts?.Dispose();
        }
    }
}
