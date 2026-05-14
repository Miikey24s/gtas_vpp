using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
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
        [Inject] public PermissionState PermissionState { get; set; } = default!;
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
        public int StepCount => 2;

        public OrderCreateContext Context { get; set; } = new();
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();


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
            ? Loc["EditOrder"].Value
            : IsCopyFromPrevious
                ? Loc["CopyOrder"].Value
                : Context.IsAdditional
                    ? Loc["AdditionalOrder"].Value
                    : Loc["CreateOrder"].Value;

        public string OrderModeBadgeText => IsEdit
            ? Loc["WizardEditingExistingRequest"].Value
            : IsCopyFromPrevious
                ? Loc["WizardCopiedFromPreviousOrder"].Value
                : Context.IsAdditional
                    ? Loc["WizardModeAdditionalFlow"].Value
                    : Loc["WizardRegularRequest"].Value;

        public string OrderModeSummary => IsEdit
            ? Loc["WizardOrderModeSummaryEdit"].Value
            : IsCopyFromPrevious
                ? Loc["WizardOrderModeSummaryCopy"].Value
                : Context.IsAdditional
                    ? Loc["WizardOrderModeSummaryAdditional"].Value
                    : Loc["WizardOrderModeSummaryCreate"].Value;

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
            ? Loc["LiveUpdate"].Value
            : DraftRecovered
                ? Loc["RestoredDraft"].Value
                : Loc["AutoSaveActive"].Value;

        public string DraftStatusText => IsEdit
            ? Loc["ChangesStoredWhenUpdateExistingRequest"].Value
            : LastDraftSavedAt.HasValue
                ? string.Format(Loc["LastSavedAtFormat"], DateFormatter.Format(LastDraftSavedAt, DateFormatter.TimeOnly))
                : Loc["BrowserKeepsLocalDraftWhileYouWork"].Value;

        public string CurrentStepTitle => currentStep switch
        {
            0 => Loc["SelectProducts"].Value,
            _ => Loc["ReviewSubmit"].Value
        };

        public string CurrentStepHint => currentStep switch
        {
            0 => Loc["CurrentStepHintProducts"].Value,
            _ => Loc["CurrentStepHintReview"].Value
        };

        public string FooterStatusText => currentStep switch
        {
            0 when SelectedItemCount == 0 => Loc["NoItemsSelectedYetStartCatalogLeft"].Value,
            0 => string.Format(Loc["SelectedItemsTotalQuantityFormat"], SelectedItemCount, Context.TotalQty),
            _ => SelectedItemCount == 0
                ? Loc["AddAtLeastOneItemBeforeSubmitting"].Value
                : string.Format(Loc["ReadyToSubmitItemsTotalQuantityFormat"], SelectedItemCount, Context.TotalQty)
        };

        public string PrimaryActionText => Context.IsAdditional
            ? Loc["SubmitForApproval"].Value
            : IsEdit
                ? Loc["UpdateOrder"].Value
                : Loc["CreateOrder"].Value;

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
                await PermissionState.EnsureLoadedAsync();
                if (PermissionState.IdentityClaims.Any())
                {
                    Claims = PermissionState.IdentityClaims;
                }

                if (!PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestOrder))
                {
                    NotificationService.Notify(new NotificationMessage()
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = Loc["AccessDenied"],
                        Detail = Loc["NoPermissionCreateOrEditOrders"],
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
            currentStep = step;
            Context.NotifyStateChanged();
            return Task.CompletedTask;
        }

        private Task GoNextStepAsync()
        {
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

        private bool ValidateBeforeSubmit()
        {
            // Additional orders require a description/reason
            if (Context.IsAdditional && string.IsNullOrWhiteSpace(Context.Description))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = Loc["Order"],
                    Detail = Loc["ProvideReasonBeforeContinue"],
                    Duration = 3500
                });
                return false;
            }
            return true;
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
                        Summary = Loc["Order"],
                        Detail = Loc["OrderToUpdateNotFound"],
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
                    Summary = Loc["Order"],
                    Detail = string.Format(Loc["LoadOrderFailedFormat"], ex.Message),
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
                        Summary = Loc["CopyPrevious"],
                        Detail = Loc["NoPreviousOrderFoundToCopy"],
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
                    Summary = Loc["CopyPrevious"],
                    Detail = string.Format(Loc["CopiedItemsFromPreviousOrderFormat"], Context.SelectedItems.Count),
                    Duration = 4000
                });
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["CopyPrevious"],
                    Detail = string.Format(Loc["FailedToLoadPreviousOrderFormat"], ex.Message),
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
                        Summary = Loc["OrderDraft"],
                        Detail = Loc["DraftSavedInBrowserStorage"],
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
            if (!ValidateBeforeSubmit())
            {
                return;
            }

            if (Context.SelectedItems.Count == 0)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = Loc["Order"],
                    Detail = Loc["PleaseSelectAtLeastOneProduct"],
                    Duration = 3000
                });
                return;
            }

            if (Context.SelectedItems.Any(x => x.VPPId == Guid.Empty || x.Qty <= 0))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = Loc["Order"],
                    Detail = Loc["InvalidProductOrQuantity"],
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
                        Summary = Loc["Order"],
                        Detail = Loc["CouldNotDetermineCurrentPeriodPleaseReload"],
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
                    Summary = Loc["Order"],
                    Detail = IsEdit ? Loc["OrderUpdatedSuccessfully"] : Loc["OrderCreatedSuccessfully"],
                    Duration = 3000
                });

                NavigationManager.NavigateTo("/dashboard?tab=0", true);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Order"],
                    Detail = string.Format(Loc["FailedToSaveOrderFormat"], ex.Message),
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
