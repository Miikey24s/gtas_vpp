using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.AI;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
    public bool IsPageLoading { get; set; } = true;
        public bool IsEdit => OrderId.HasValue;
        public bool IsCopyFromPrevious => !string.IsNullOrWhiteSpace(CopyFromParam) && CopyFromParam.Equals("previous", StringComparison.OrdinalIgnoreCase);
        public DateTime? LastDraftSavedAt { get; set; }
        public bool DraftRecovered { get; set; }
        public int currentStep { get; set; }
        public int StepCount => 3;

        public OrderCreateContext Context { get; set; } = new();
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();
        private OrderCreateStep1? _step1;

        // P1: BE owns the period truth — FE never derives Y/M from DateTime.Now.
        public VPP_PeriodInfoResDTO? PeriodInfo { get; set; }

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

        public int SelectedItemCount => Context.SelectedItems.Count;

        public string OrderModeTitle => IsEdit
            ? "Edit order"
            : IsCopyFromPrevious
                ? "Copy previous order"
                : Context.IsAdditional
                    ? "Additional order"
                    : "Create new order";

        public string OrderModeBadgeText => IsEdit
            ? "Editing existing request"
            : IsCopyFromPrevious
                ? "Copied from previous order"
                : Context.IsAdditional
                    ? "Additional approval flow"
                    : "Regular request";

        public string OrderModeSummary => IsEdit
            ? "Review the request details and update quantities before saving the existing order."
            : IsCopyFromPrevious
                ? "Previous items are preloaded so you can adjust them quickly before resubmitting."
                : Context.IsAdditional
                    ? "This request targets a closed period and will move through the admin approval flow."
                    : "Search the catalog, build the basket, and do one final check before submitting the request.";

        public DateTime TargetPeriodDate => PeriodInfo is { } p
            ? Context.IsAdditional
                ? new DateTime(p.PreviousPeriodYear, p.PreviousPeriodMonth, 1)
                : new DateTime(p.CurrentPeriodYear, p.CurrentPeriodMonth, 1)
            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public DateTime TargetPeriodStartDate => new DateTime(TargetPeriodDate.Year, TargetPeriodDate.Month, 1).AddDays(4);
        public DateTime TargetPeriodEndDate => new DateTime(TargetPeriodDate.Year, TargetPeriodDate.Month, 1).AddMonths(1).AddDays(3);
        public string TargetPeriodText => DateFormatter.Format(TargetPeriodDate, DateFormatter.MonthYear);
        public string TargetWindowText => $"{DateFormatter.Format(TargetPeriodStartDate, DateFormatter.ShortDate)} - {DateFormatter.Format(TargetPeriodEndDate, DateFormatter.ShortDate)}";

        public string DraftStatusTitle => IsEdit
            ? "Live update"
            : DraftRecovered
                ? "Restored draft"
                : "Auto-save active";

        public string DraftStatusText => IsEdit
            ? "Changes are stored when you update the existing request."
            : LastDraftSavedAt.HasValue
                ? $"Last saved at {DateFormatter.Format(LastDraftSavedAt, DateFormatter.TimeOnly)}"
                : "This browser keeps a local draft while you work.";

        public string CurrentStepTitle => currentStep switch
        {
            0 => "Set request context",
            1 => "Select products",
            _ => "Review and submit"
        };

        public string CurrentStepHint => currentStep switch
        {
            0 => "Confirm the period window, request type, and the note or reason before continuing.",
            1 => "Choose products, adjust quantities, and build the request basket.",
            _ => "Do a final review of quantities and notes before sending the order."
        };

        public string FooterStatusText => currentStep switch
        {
            0 when Context.IsAdditional && string.IsNullOrWhiteSpace(Context.Description)
                => "Additional orders require a reason before you can continue.",
            0 => $"Target window: {TargetWindowText}.",
            1 when SelectedItemCount == 0 => "No items selected yet. Start with the catalog on the left.",
            1 => $"{SelectedItemCount} item(s) selected with total quantity {Context.TotalQty}.",
            _ => SelectedItemCount == 0
                ? "Add at least one item before submitting the request."
                : $"Ready to submit {SelectedItemCount} item(s) with total quantity {Context.TotalQty}."
        };

        public string PrimaryActionText => Context.IsAdditional
            ? "Submit for approval"
            : IsEdit
                ? "Update order"
                : "Create order";

        protected override async Task OnInitializedAsync()
        {
            try
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

                // P1: Pull period truth from BE before doing anything period-sensitive.
                await LoadPeriodInfoAsync();

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
            finally
            {
                IsPageLoading = false;
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

        private Task OnWizardStepChange(int step)
        {
            if (step > currentStep && currentStep == 0 && !ValidateStep1())
            {
                return Task.CompletedTask;
            }

            currentStep = step;
            Context.NotifyStateChanged();
            return Task.CompletedTask;
        }

        private Task GoNextStepAsync()
        {
            if (currentStep == 0 && !ValidateStep1())
            {
                return Task.CompletedTask;
            }

            if (currentStep < StepCount - 1)
            {
                currentStep++;
                Context.NotifyStateChanged();
            }

            return Task.CompletedTask;
        }

        private void GoPreviousStep()
        {
            if (currentStep <= 0) return;

            currentStep--;
            Context.NotifyStateChanged();
        }

        private bool ValidateStep1()
        {
            var isValid = _step1?.ValidateStep() ?? !Context.IsAdditional || !string.IsNullOrWhiteSpace(Context.Description);
            if (!isValid)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Order",
                    Detail = "Please provide a reason for the additional order before continuing.",
                    Duration = 3500
                });
            }

            return isValid;
        }

        private async Task LoadPeriodInfoAsync()
        {
            try
            {
                PeriodInfo = await _apiServices.GetFromApiAsync<VPP_PeriodInfoResDTO>($"{Config.VppApi.ApiVppBase}/period-info");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Page_OrderCreate] Failed to load period info: {ex.Message}");
            }
        }

        private async Task LoadOrderForEditAsync()
        {
            if (!OrderId.HasValue) return;

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
        }

        private async Task LoadPreviousOrderItemsAsync()
        {
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
            if (!ValidateStep1())
            {
                currentStep = 0;
                return;
            }

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
            try
            {
                // P1: Pull Y/M from BE-authoritative PeriodInfo (loaded in OnInitializedAsync).
                // Falls back to a single fresh fetch in case the wizard sat open across the
                // deadline boundary — BE will still re-validate via PeriodCalculator on submit.
                if (PeriodInfo is null)
                {
                    await LoadPeriodInfoAsync();
                }
                if (PeriodInfo is null)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Order",
                        Detail = "Could not determine the current period. Please reload the page.",
                        Duration = 5000
                    });
                    return;
                }

                var period = IsAdditional
                    ? new DateTime(PeriodInfo.PreviousPeriodYear, PeriodInfo.PreviousPeriodMonth, 1)
                    : new DateTime(PeriodInfo.CurrentPeriodYear, PeriodInfo.CurrentPeriodMonth, 1);

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
