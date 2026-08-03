using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Features.Requests.Drafts;
using gtas_vpp_fe.Features.Requests.Submission;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest
{
    public partial class Page_OrderCreate : IDisposable
    {
        [Inject] public RequestsQueryClient Requests { get; set; } = default!;
        [Inject] public RequestsCommandClient Commands { get; set; } = default!;
        [Inject] public OrderDraftStore Drafts { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public AuthHelper AuthHelper { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public IToastService Toast { get; set; } = default!;

        [SupplyParameterFromQuery] public Guid? OrderId { get; set; }
        [SupplyParameterFromQuery(Name = "isAdditional")] public string? IsAdditionalParam { get; set; }
        [SupplyParameterFromQuery(Name = "copyFrom")] public string? CopyFromParam { get; set; }
        [SupplyParameterFromQuery(Name = "mode")] public string? ModeParam { get; set; }

        private bool _isAdditionalOverride;
        private bool _hasLoadedOrder;
        private bool _editingAllowed;
        private readonly string _submissionIdempotencyKey = Guid.NewGuid().ToString("N");

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
        public bool IsRecreate => OrderId.HasValue
            && string.Equals(ModeParam, "recreate", StringComparison.OrdinalIgnoreCase);
        public bool IsEdit => OrderId.HasValue && !IsRecreate;
        public bool IsCopyFromPrevious => !string.IsNullOrWhiteSpace(CopyFromParam) && CopyFromParam.Equals("previous", StringComparison.OrdinalIgnoreCase);
        public DateTime? LastDraftSavedAt { get; set; }
        public bool DraftRecovered { get; set; }
        public int currentStep { get; set; }
        public int StepCount => 2;

        private IReadOnlyList<VppWorkflowStep> OrderWorkflowSteps =>
        [
            new("products", 1, Loc["SelectProducts"], currentStep == 0 ? VppWorkflowStepState.Active : VppWorkflowStepState.Complete),
            new("review", 2, Loc["ReviewSubmit"], currentStep == 1 ? VppWorkflowStepState.Active : VppWorkflowStepState.Pending)
        ];

        public OrderCreateContext Context { get; set; } = new();
        public IEnumerable<Claim> Claims { get; set; } = new List<Claim>();
        private bool _showSupplementReasonForm;
        private bool _supplementReasonTouched;
        private string? _supplementReasonDraft;

        private bool CanSaveSupplementReason
            => !string.IsNullOrWhiteSpace(_supplementReasonDraft)
                && _supplementReasonDraft.Trim().Length is >= 5 and <= 500;


        // P1: BE sở hữu dữ liệu kỳ có thẩm quyền; FE không suy Year/Month từ DateTime.Now.
        public VppPeriodInfoResDTO? PeriodInfo { get; set; }

        private PeriodicTimer? _draftAutoSaveTimer;
        private CancellationTokenSource? _draftAutoSaveCts;
        private volatile bool _draftDirty;

        private string? CurrentUserId
            => Claims.FirstOrDefault(x => x.Type == "UserID")?.Value;

        private string? DraftStorageKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CurrentUserId)
                    || PeriodInfo?.PeriodId is not Guid periodId
                    || periodId == Guid.Empty)
                {
                    return null;
                }

                return OrderDraftStoragePolicy.BuildStorageKey(
                    CurrentUserId,
                    periodId,
                    IsAdditional,
                    OrderId);
            }
        }

        public int SelectedItemCount => Context.SelectedItems.Count;

        public string ItemsStatusText => string.Format(Loc["WizardItemsBadgeFormat"].Value, SelectedItemCount);
        public string QtyStatusText => string.Format(
            System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase)
                ? Loc["WizardTotalQtyBadgeFormat"].Value
                : Loc["WizardQtyBadgeFormat"].Value,
            Context.TotalQty);
        public string DraftRecoveredText => Loc["DraftRestored"].Value.ToLower();

        public string OrderModeTitle => IsRecreate
            ? Loc["RecreateOrder"].Value
            : IsEdit
            ? Loc["EditOrder"].Value
            : IsCopyFromPrevious
                ? Loc["CopyOrder"].Value
                : Context.IsAdditional
                    ? Loc["AdditionalOrder"].Value
                    : Loc["CreateOrder"].Value;

        public string OrderModeBadgeText => IsRecreate
            ? Loc["WizardRecreatingCancelledRequest"].Value
            : IsEdit
            ? Loc["WizardEditingExistingRequest"].Value
            : IsCopyFromPrevious
                ? Loc["WizardCopiedFromPreviousOrder"].Value
                : Context.IsAdditional
                    ? Loc["WizardModeAdditionalFlow"].Value
                    : Loc["WizardRegularRequest"].Value;

        public string OrderModeSummary => IsRecreate
            ? Loc["WizardOrderModeSummaryRecreate"].Value
            : IsEdit
            ? Loc["WizardOrderModeSummaryEdit"].Value
            : IsCopyFromPrevious
                ? Loc["WizardOrderModeSummaryCopy"].Value
                : Context.IsAdditional
                    ? Loc["WizardOrderModeSummaryAdditional"].Value
                    : Loc["WizardOrderModeSummaryCreate"].Value;

        public DateTime TargetPeriodDate => PeriodInfo is { } p
            ? new DateTime(p.CurrentPeriodYear, p.CurrentPeriodMonth, 1)
            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public DateTime TargetPeriodStartDate => PeriodInfo?.StartDate
            ?? new DateTime(TargetPeriodDate.Year, TargetPeriodDate.Month, 1).AddDays(4);
        public DateTime TargetPeriodEndDate => PeriodInfo?.DeadlineDate.AddTicks(-1)
            ?? new DateTime(TargetPeriodDate.Year, TargetPeriodDate.Month, 1).AddMonths(1).AddDays(3);
        public string TargetPeriodText => DateFormatter.Format(TargetPeriodDate, DateFormatter.MonthYear);
        public string TargetWindowText => $"{DateFormatter.Format(TargetPeriodStartDate, DateFormatter.ShortDate)} - {DateFormatter.Format(TargetPeriodEndDate, DateFormatter.ShortDate)}";

        public bool CanSubmitForPeriod => IsEdit || IsRecreate
            ? _editingAllowed
            : PeriodInfo is not null
              && (Context.IsAdditional ? PeriodInfo.CanCreateAdditional : PeriodInfo.CanCreateOrder);

        public string PeriodActionReason => IsEdit || IsRecreate
            ? Loc["OrderNoLongerEditable"].Value
            : Context.IsAdditional
                ? PeriodInfo?.CanCreateAdditionalReason ?? Loc["SupplementUnavailable"].Value
                : PeriodInfo?.CanCreateOrderReason ?? Loc["RegularRequestUnavailable"].Value;

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

        public string PrimaryActionText => IsRecreate
                ? Loc["RecreateOrder"].Value
            : Context.IsAdditional
                ? Loc["SubmitForApproval"].Value
            : IsEdit
                ? Loc["UpdateOrder"].Value
                : Loc["CreateOrder"].Value;

        protected override async Task OnInitializedAsync()
        {
            if (!RendererInfo.IsInteractive)
            {
                return;
            }

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

                var requiredPermission = IsEdit || IsRecreate
                    ? Permissions.RequestUpdateOwn
                    : Permissions.RequestCreate;
                if (!PermissionState.HasPermission(requiredPermission))
                {
                    Toast.Notify(new NotificationMessage()
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = Loc["AccessDenied"],
                        Detail = Loc["NoPermissionCreateOrEditOrders"],
                        Duration = 5000
                    });
                    NavigationManager.NavigateTo("/dashboard?tab=0", true);
                    return;
                }

                // P1: Lấy dữ liệu kỳ có thẩm quyền từ BE trước mọi thao tác nhạy với kỳ.
                await LoadPeriodInfoAsync();

                // Khởi tạo context.
                Context.Mode = IsRecreate
                    ? "recreate"
                    : IsEdit
                        ? "edit"
                        : (IsCopyFromPrevious ? "copy" : (IsAdditional ? "additional" : "new"));
                Context.EditOrderId = OrderId;
                Context.IsAdditional = IsAdditional;
                Context.BaseRequestId = PeriodInfo?.BaseRequestId;
                Context.BaseRequestCode = PeriodInfo?.BaseRequestCode;

                if ((IsEdit || IsRecreate) && OrderId.HasValue)
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
                if (!string.IsNullOrWhiteSpace(CurrentUserId)
                    && PeriodInfo?.PeriodId is Guid periodId
                    && periodId != Guid.Empty)
                {
                    await Drafts.ClearOtherPeriodsAsync(
                        CurrentUserId,
                        periodId);
                }
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

        private void GoToStep(int step)
        {
            if (step < 0 || step >= StepCount)
            {
                return;
            }

            currentStep = step;
            Context.NotifyStateChanged();
        }

        private void OnOrderWorkflowStepSelected(string stepKey)
            => GoToStep(string.Equals(stepKey, "review", StringComparison.Ordinal) ? 1 : 0);

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
            // Đơn bổ sung bắt buộc có mô tả/lý do.
            if (Context.IsAdditional
                && (string.IsNullOrWhiteSpace(Context.SupplementReason)
                    || Context.SupplementReason.Trim().Length < 5
                    || Context.SupplementReason.Trim().Length > 500))
            {
                OpenSupplementReasonForm();
                return false;
            }
            return true;
        }

        private void OpenSupplementReasonForm()
        {
            _supplementReasonDraft = Context.SupplementReason;
            _supplementReasonTouched = false;
            _showSupplementReasonForm = true;
        }

        private void CloseSupplementReasonForm()
        {
            _showSupplementReasonForm = false;
            _supplementReasonTouched = false;
        }

        private void UpdateSupplementReasonDraft(string? value)
        {
            _supplementReasonDraft = value;
            _supplementReasonTouched = true;
        }

        private void SaveSupplementReason()
        {
            _supplementReasonTouched = true;
            if (!CanSaveSupplementReason)
            {
                return;
            }

            Context.SupplementReason = _supplementReasonDraft!.Trim();
            Context.NotifyStateChanged();
            CloseSupplementReasonForm();
        }

        private async Task LoadPeriodInfoAsync()
        {
            try
            {
                PeriodInfo = await Requests.GetPeriodInfoAsync();
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Period"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc),
                    Duration = 5000
                });
            }
        }

        private async Task LoadOrderForEditAsync()
        {
            if (!OrderId.HasValue) return;

            try
            {
                var editingOrder = await Requests.GetOrderAsync(OrderId.Value);
                if (editingOrder == null)
                {
                    Toast.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = Loc["Order"],
                        Detail = Loc["OrderToUpdateNotFound"],
                        Duration = 4000
                    });
                    GoBack();
                    return;
                }

                Context.RowVersion = editingOrder.RowVersion;
                _editingAllowed = IsRecreate
                    ? editingOrder.CanRecreate
                    : editingOrder.CanEdit;
                _isAdditionalOverride = editingOrder.IsAdditionalOrder;
                _hasLoadedOrder = true;
                Context.IsAdditional = _isAdditionalOverride;
                Context.BaseRequestId = editingOrder.BaseRequestId;

                if (!IsRecreate)
                {
                    Context.Description = editingOrder.Description;
                    Context.SupplementReason = editingOrder.SupplementReason;
                    Context.BaseRequestId = editingOrder.BaseRequestId;
                    Context.SelectedItems = (editingOrder.Items ?? new())
                        .Select(x => new OrderCreateContext.SelectedItem
                        {
                            VppId = x.VppId,
                            VppCode = x.VppCode,
                            VppName = x.VppName,
                            UomCode = x.UomCode,
                            UomName = x.UomName,
                            Qty = x.Qty,
                            Description = x.Description
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Order"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc, "LoadOrderFailed"),
                    Duration = 5000
                });
            }
        }

        private async Task LoadPreviousOrderItemsAsync()
        {
            try
            {
                var previousOrder = await Requests.GetPreviousOrderItemsAsync();
                if (previousOrder == null)
                {
                    Toast.Notify(new NotificationMessage
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
                        VppId = x.VppId,
                        VppCode = x.VppCode,
                        VppName = x.VppName,
                        UomCode = x.UomCode,
                        UomName = x.UomName,
                        Qty = x.Qty,
                        Description = x.Description
                    })
                    .ToList();

                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = Loc["CopyPrevious"],
                    Detail = string.Format(Loc["CopiedItemsFromPreviousOrderFormat"], Context.SelectedItems.Count),
                    Duration = 4000
                });
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["CopyPrevious"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc, "FailedToLoadPreviousOrder"),
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
            var storageKey = DraftStorageKey;
            if (IsEdit
                || string.IsNullOrWhiteSpace(storageKey)
                || string.IsNullOrWhiteSpace(CurrentUserId)
                || PeriodInfo?.PeriodId is not Guid periodId)
            {
                return;
            }

            try
            {
                var draft = new OrderDraftSnapshot
                {
                    UserId = CurrentUserId,
                    PeriodId = periodId,
                    Description = Context.Description,
                    SupplementReason = Context.SupplementReason,
                    Items = Context.SelectedItems.Select(item => new OrderDraftItemSnapshot
                    {
                        VppId = item.VppId,
                        VppCode = item.VppCode,
                        VppName = item.VppName,
                        UomCode = item.UomCode,
                        UomName = item.UomName,
                        Qty = item.Qty,
                        Description = item.Description
                    }).ToList(),
                    SavedAtUtc = DateTime.UtcNow
                };

                await Drafts.SaveAsync(storageKey, draft);
                LastDraftSavedAt = draft.SavedAtUtc.ToLocalTime();
                _draftDirty = false;

                if (showMessage)
                {
                    Toast.Notify(new NotificationMessage
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
            var storageKey = DraftStorageKey;
            if (string.IsNullOrWhiteSpace(storageKey)
                || string.IsNullOrWhiteSpace(CurrentUserId)
                || PeriodInfo?.PeriodId is not Guid periodId)
            {
                return;
            }

            try
            {
                var draft = await Drafts.RestoreAsync(
                    storageKey,
                    CurrentUserId,
                    periodId,
                    DateTime.UtcNow);
                if (draft is null) return;

                Context.Description = draft.Description;
                Context.SupplementReason = draft.SupplementReason;
                Context.SelectedItems = draft.Items.Select(item => new OrderCreateContext.SelectedItem
                {
                    VppId = item.VppId,
                    VppCode = item.VppCode,
                    VppName = item.VppName,
                    UomCode = item.UomCode,
                    UomName = item.UomName,
                    Qty = item.Qty,
                    Description = item.Description
                }).ToList();
                LastDraftSavedAt = draft.SavedAtUtc.ToLocalTime();
                DraftRecovered = Context.SelectedItems.Count > 0
                    || !string.IsNullOrWhiteSpace(Context.Description)
                    || !string.IsNullOrWhiteSpace(Context.SupplementReason);
                Context.DraftRecovered = DraftRecovered;
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
            if (IsSaving || !CanSubmitForPeriod)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = Loc["Order"],
                    Detail = PeriodActionReason,
                    Duration = 4500
                });
                return;
            }

            if (!ValidateBeforeSubmit())
            {
                return;
            }

            if (Context.SelectedItems.Count == 0)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = Loc["Order"],
                    Detail = Loc["PleaseSelectAtLeastOneProduct"],
                    Duration = 3000
                });
                return;
            }

            if (Context.SelectedItems.Any(x => x.VppId == Guid.Empty || x.Qty <= 0))
            {
                Toast.Notify(new NotificationMessage
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
                // P1: Lấy Year/Month từ PeriodInfo có thẩm quyền của BE, đã tải trong OnInitializedAsync.
                // Fallback bằng đúng một lần fetch mới nếu wizard mở xuyên qua biên deadline;
                // BE vẫn kiểm tra lại bằng PeriodCalculator khi submit.
                if (PeriodInfo is null)
                {
                    await LoadPeriodInfoAsync();
                }
                if (PeriodInfo is null)
                {
                    Toast.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = Loc["Order"],
                        Detail = Loc["CouldNotDetermineCurrentPeriodPleaseReload"],
                        Duration = 5000
                    });
                    return;
                }

                var period = new DateTime(PeriodInfo.CurrentPeriodYear, PeriodInfo.CurrentPeriodMonth, 1);

                var submission = new OrderSubmissionSnapshot(
                    Context.Description,
                    Context.SupplementReason,
                    _submissionIdempotencyKey,
                    Context.SelectedItems.Select(item => new OrderSubmissionItem(
                        item.VppId,
                        item.Qty,
                        item.Description)).ToArray());

                if (IsRecreate)
                {
                    await Commands.RecreateAsync(
                        OrderId!.Value,
                        OrderSubmissionRequestFactory.BuildRecreateRequest(
                            submission,
                            Context.IsAdditional,
                            Context.RowVersion));
                }
                else if (IsEdit)
                {
                    await Commands.UpdateAsync(
                        OrderId!.Value,
                        OrderSubmissionRequestFactory.BuildUpdateRequest(
                            submission,
                            OrderId.Value,
                            IsAdditional,
                            Context.RowVersion));
                }
                else
                {
                    await Commands.CreateAsync(OrderSubmissionRequestFactory.BuildCreateRequest(
                        submission,
                        period.Year,
                        period.Month,
                        IsAdditional,
                        Context.IsAdditional,
                        PeriodInfo.BaseRequestId));
                    if (!string.IsNullOrWhiteSpace(DraftStorageKey))
                    {
                        await Drafts.RemoveAsync(DraftStorageKey);
                    }
                }

                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = Loc["Order"],
                    Detail = IsRecreate
                        ? Loc["OrderRecreatedSuccess"]
                        : IsEdit
                            ? Loc["OrderUpdatedSuccessfully"]
                            : Loc["OrderCreatedSuccessfully"],
                    Duration = 3000
                });

                NavigationManager.NavigateTo("/dashboard?tab=0", true);
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Order"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc, "FailedToSaveOrder"),
                    Duration = 6000
                });

                await JS.InvokeVoidAsync("console.error", "Order submission error", UiErrorMapper.GetErrorCode(ex));
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
