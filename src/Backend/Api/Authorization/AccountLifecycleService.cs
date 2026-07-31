using System.Diagnostics;
using System.Security.Claims;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Authorization;

public sealed record AccountLifecycleResult(
    int StatusCode,
    string Code,
    string Message,
    AccountLifecycleResDTO? Account = null)
{
    public bool Succeeded => StatusCode is >= 200 and < 300;

    public static AccountLifecycleResult Success(
        string code,
        string message,
        AccountLifecycleResDTO? account = null,
        int statusCode = StatusCodes.Status200OK) =>
        new(statusCode, code, message, account);

    public static AccountLifecycleResult Accepted(string message) =>
        new(StatusCodes.Status202Accepted, "REGISTRATION_ACCEPTED", message,
            new AccountLifecycleResDTO
            {
                AccountStatus = nameof(AppAccountStatus.PendingApproval),
                Message = message
            });

    public static AccountLifecycleResult Accepted(string code, string message) =>
        new(StatusCodes.Status202Accepted, code, message,
            new AccountLifecycleResDTO { Message = message });

    public static AccountLifecycleResult BadRequest(string code, string message) =>
        new(StatusCodes.Status400BadRequest, code, message);

    public static AccountLifecycleResult Unauthorized(string code, string message) =>
        new(StatusCodes.Status401Unauthorized, code, message);

    public static AccountLifecycleResult NotFound(string code, string message) =>
        new(StatusCodes.Status404NotFound, code, message);

    public static AccountLifecycleResult Conflict(string code, string message) =>
        new(StatusCodes.Status409Conflict, code, message);
}

public interface IAccountLifecycleService
{
    Task<AccountLifecycleResult> RegisterAsync(
        AccountRegistrationReqDTO request,
        CancellationToken cancellationToken = default);

    Task<AccountLifecycleResult> ConfirmEmailAsync(
        EmailConfirmationReqDTO request,
        CancellationToken cancellationToken = default);

    Task<AccountLifecycleResult> ResendEmailConfirmationAsync(
        EmailConfirmationResendReqDTO request,
        CancellationToken cancellationToken = default);

    Task<AccountLifecycleResult> RequestPasswordResetAsync(
        PasswordRecoveryReqDTO request,
        CancellationToken cancellationToken = default);

    Task<AccountLifecycleResult> ResetPasswordAsync(
        PasswordResetReqDTO request,
        CancellationToken cancellationToken = default);

    Task<AccountLifecycleResult> ChangePasswordAsync(
        int accountId,
        PasswordChangeReqDTO request,
        CancellationToken cancellationToken = default);

    Task<AccountLifecycleResult> AdminResetPasswordAsync(
        int actorAccountId,
        AdminPasswordResetReqDTO request,
        CancellationToken cancellationToken = default);

    Task<AccountLifecycleResult> AdminInviteAsync(
        int actorAccountId,
        AdminAccountInvitationReqDTO request,
        CancellationToken cancellationToken = default);

    Task<AccountLifecycleResult> AdminSendPasswordResetLinkAsync(
        int actorAccountId,
        AdminPasswordResetLinkReqDTO request,
        CancellationToken cancellationToken = default);

    AccountAdministrationCapabilitiesResDTO GetAdministrationCapabilities();

    Task<MembershipAdministrationResult> ActivateAsync(
        int actorAccountId,
        AdminAccountActivationReqDTO request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Boundary duy nhất do ứng dụng sở hữu cho đăng ký và vòng đời mật khẩu.
/// Public response chủ động chống dò tài khoản, đồng thời ghi audit trail và
/// notification bền vững cho admin/user để hỗ trợ vận hành.
/// </summary>
public sealed class AccountLifecycleService(
    UserManager<AppUser> userManager,
    VPPContext context,
    IMembershipAdministrationService membershipAdministrationService,
    IAppNotificationService notificationService,
    IAccountEmailSender emailSender,
    IOptions<AccountEmailOptions> emailOptions,
    ILogger<AccountLifecycleService> logger) : IAccountLifecycleService
{
    private const string GenericRegistrationMessage =
        "Nếu thông tin hợp lệ, yêu cầu đăng ký đã được tiếp nhận và đang chờ quản trị viên phê duyệt.";
    private const string GenericRecoveryMessage =
        "Nếu tài khoản tồn tại, hướng dẫn khôi phục đã được gửi tới email đã đăng ký.";
    private const string GenericConfirmationResendMessage =
        "Nếu tài khoản hợp lệ và chưa xác nhận email, yêu cầu gửi lại đã được tiếp nhận.";

    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly VPPContext _context = context;
    private readonly IMembershipAdministrationService _membershipAdministrationService = membershipAdministrationService;
    private readonly IAppNotificationService _notificationService = notificationService;
    private readonly IAccountEmailSender _emailSender = emailSender;
    private readonly AccountEmailOptions _emailOptions = emailOptions.Value;
    private readonly ILogger<AccountLifecycleService> _logger = logger;

    public async Task<AccountLifecycleResult> RegisterAsync(
        AccountRegistrationReqDTO request,
        CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();
        var employeeCode = NormalizeOptional(request.EmployeeCode);

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return AccountLifecycleResult.BadRequest(
                "PASSWORD_CONFIRMATION_MISMATCH",
                "The password confirmation does not match.");
        }

        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(fullName))
        {
            return AccountLifecycleResult.BadRequest(
                "REGISTRATION_FIELDS_REQUIRED",
                "Username, email and full name are required.");
        }

        var validationAccount = new AppUser
        {
            UserName = username,
            Email = email,
            FullName = fullName
        };
        if (!await IsPasswordValidAsync(validationAccount, request.Password))
        {
            return AccountLifecycleResult.BadRequest(
                "REGISTRATION_INVALID",
                "The registration data does not satisfy the account policy.");
        }

        var normalizedUsername = _userManager.NormalizeName(username);
        var normalizedEmail = _userManager.NormalizeEmail(email);
        var duplicate = await _context.Users.AsNoTracking().AnyAsync(
            account => (normalizedUsername != null && account.NormalizedUserName == normalizedUsername)
                || (normalizedEmail != null && account.NormalizedEmail == normalizedEmail)
                || (employeeCode != null && account.EmployeeCode == employeeCode),
            cancellationToken);
        if (duplicate)
        {
            await RecordAuditAsync(
                actorUserId: null,
                targetUserId: null,
                action: "ACCOUNT_REGISTRATION_REJECTED",
                outcome: "Duplicate",
                summary: "A registration request matched an existing application identity.",
                reason: null,
                cancellationToken);
            return AccountLifecycleResult.Accepted(GenericRegistrationMessage);
        }

        var now = DateTime.UtcNow;
        var account = new AppUser
        {
            UserName = username,
            Email = email,
            FullName = fullName,
            EmployeeCode = employeeCode,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            AccountStatus = AppAccountStatus.PendingApproval,
            EmailConfirmed = false,
            LockoutEnabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SessionVersion = 1
        };

        IdentityResult createResult;
        try
        {
            createResult = await _userManager.CreateAsync(account, request.Password);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.Entry(account).State = EntityState.Detached;
            await RecordAuditAsync(
                actorUserId: null,
                targetUserId: null,
                action: "ACCOUNT_REGISTRATION_REJECTED",
                outcome: "Duplicate",
                summary: "A concurrent registration collided with an existing application identity.",
                reason: null,
                cancellationToken);
            return AccountLifecycleResult.Accepted(GenericRegistrationMessage);
        }
        if (!createResult.Succeeded)
        {
            var duplicateIdentity = createResult.Errors.Any(error =>
                error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)
                || error.Code.Contains("UserName", StringComparison.OrdinalIgnoreCase)
                || error.Code.Contains("Email", StringComparison.OrdinalIgnoreCase));
            await RecordAuditAsync(
                actorUserId: null,
                targetUserId: null,
                action: "ACCOUNT_REGISTRATION_REJECTED",
                outcome: duplicateIdentity ? "Duplicate" : "Invalid",
                summary: duplicateIdentity
                    ? "A concurrent registration collided with an existing identity."
                    : "A registration request failed Identity validation.",
                reason: null,
                cancellationToken);

            return duplicateIdentity
                ? AccountLifecycleResult.Accepted(GenericRegistrationMessage)
                : AccountLifecycleResult.BadRequest(
                    "REGISTRATION_INVALID",
                    "The registration data does not satisfy the account policy.");
        }

        await RecordAuditAsync(
            actorUserId: null,
            targetUserId: account.Id,
            action: "ACCOUNT_REGISTERED",
            outcome: "Succeeded",
            summary: "A new application account entered PendingApproval with no membership.",
            reason: null,
            cancellationToken);

        await TrySendConfirmationAsync(account, cancellationToken);
        await TryNotifyAdministratorsAsync(
            account,
            "ACCOUNT_REGISTRATION_PENDING",
            "New account registration pending",
            $"A new account request for {account.FullName} is waiting for approval.",
            "/permission?tab=0",
            cancellationToken);

        return AccountLifecycleResult.Accepted(GenericRegistrationMessage);
    }

    public async Task<AccountLifecycleResult> ConfirmEmailAsync(
        EmailConfirmationReqDTO request,
        CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (account is null || string.IsNullOrWhiteSpace(request.Token))
        {
            return AccountLifecycleResult.BadRequest(
                "EMAIL_CONFIRMATION_INVALID",
                "The confirmation link is invalid or expired.");
        }

        var result = await _userManager.ConfirmEmailAsync(account, request.Token);
        if (!result.Succeeded)
        {
            await RecordAuditAsync(
                null,
                account.Id,
                "ACCOUNT_EMAIL_CONFIRMATION_FAILED",
                "Rejected",
                "An email confirmation token was rejected.",
                null,
                cancellationToken);
            return AccountLifecycleResult.BadRequest(
                "EMAIL_CONFIRMATION_INVALID",
                "The confirmation link is invalid or expired.");
        }

        account.UpdatedAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(account);
        await RecordAuditAsync(
            null,
            account.Id,
            "ACCOUNT_EMAIL_CONFIRMED",
            "Succeeded",
            "The application account email was confirmed.",
            null,
            cancellationToken);
        return AccountLifecycleResult.Success(
            "EMAIL_CONFIRMED",
            "Email confirmed.",
            MapAccount(account));
    }

    public async Task<AccountLifecycleResult> ResendEmailConfirmationAsync(
        EmailConfirmationResendReqDTO request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var normalizedEmail = _userManager.NormalizeEmail(email);
        var account = string.IsNullOrWhiteSpace(normalizedEmail)
            ? null
            : await _context.Users.SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail,
                cancellationToken);

        var canResend = account is not null
            && account.AccountStatus != AppAccountStatus.Disabled
            && !account.EmailConfirmed
            && !string.IsNullOrWhiteSpace(account.Email);

        if (canResend)
        {
            await TrySendConfirmationAsync(account!, cancellationToken);
        }

        await RecordAuditAsync(
            actorUserId: null,
            targetUserId: account?.Id,
            action: "ACCOUNT_EMAIL_CONFIRMATION_RESEND_REQUESTED",
            outcome: canResend
                ? (_emailOptions.Enabled ? "Queued" : "EmailDisabled")
                : "Ignored",
            summary: "An email confirmation resend request was handled without exposing account existence.",
            reason: null,
            cancellationToken);

        return AccountLifecycleResult.Accepted(
            "EMAIL_CONFIRMATION_RESEND_ACCEPTED",
            GenericConfirmationResendMessage);
    }

    public async Task<AccountLifecycleResult> RequestPasswordResetAsync(
        PasswordRecoveryReqDTO request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var normalizedEmail = _userManager.NormalizeEmail(email);
        var account = string.IsNullOrWhiteSpace(normalizedEmail)
            ? null
            : await _context.Users.SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (account is not null
            && account.AccountStatus != AppAccountStatus.Disabled
            && !string.IsNullOrWhiteSpace(account.Email))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(account);
            var resetUrl = BuildUrl(
                "/Account/ResetPassword",
                $"userId={account.Id}&token={Uri.EscapeDataString(token)}");
            try
            {
                await _emailSender.SendAsync(
                    new AccountEmailMessage(
                        account.Email,
                        "GTAS VPP - Password recovery",
                        $"Use this link to reset your GTAS VPP password: {resetUrl}\n\nIf you did not request this, ignore this message."),
                    cancellationToken);
                await RecordAuditAsync(
                    null,
                    account.Id,
                    "ACCOUNT_PASSWORD_RESET_REQUESTED",
                    "Succeeded",
                    "A password recovery message was queued.",
                    null,
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Password recovery email delivery failed for AccountId={AccountId}.", account.Id);
                await RecordAuditAsync(
                    null,
                    account.Id,
                    "ACCOUNT_PASSWORD_RESET_REQUESTED",
                    "EmailUnavailable",
                    "Password recovery was requested but the email adapter was unavailable.",
                    null,
                    cancellationToken);
            }
        }
        else
        {
            await RecordAuditAsync(
                null,
                null,
                "ACCOUNT_PASSWORD_RESET_REQUESTED",
                "Accepted",
                "A password recovery request did not disclose account existence.",
                null,
                cancellationToken);
        }

        return AccountLifecycleResult.Success(
            "PASSWORD_RESET_REQUEST_ACCEPTED",
            GenericRecoveryMessage,
            statusCode: StatusCodes.Status202Accepted);
    }

    public async Task<AccountLifecycleResult> ResetPasswordAsync(
        PasswordResetReqDTO request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return AccountLifecycleResult.BadRequest(
                "PASSWORD_CONFIRMATION_MISMATCH",
                "The password confirmation does not match.");
        }

        var account = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (account is null || string.IsNullOrWhiteSpace(request.Token))
        {
            return AccountLifecycleResult.BadRequest(
                "PASSWORD_RESET_INVALID",
                "The password reset link is invalid or expired.");
        }

        var acceptsInvitation = string.IsNullOrWhiteSpace(account.PasswordHash);
        var result = await _userManager.ResetPasswordAsync(account, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            await RecordAuditAsync(
                null,
                account.Id,
                "ACCOUNT_PASSWORD_RESET_FAILED",
                "Rejected",
                "A password reset token was rejected.",
                null,
                cancellationToken);
            return AccountLifecycleResult.BadRequest(
                "PASSWORD_RESET_INVALID",
                "The password reset link is invalid or expired.");
        }

        account.MustChangePassword = false;
        if (acceptsInvitation)
        {
            account.EmailConfirmed = true;
        }
        account.UpdatedAtUtc = DateTime.UtcNow;
        InvalidateSessions(account);
        await _userManager.UpdateAsync(account);
        await RecordAuditAsync(
            null,
            account.Id,
            acceptsInvitation ? "ACCOUNT_INVITATION_ACCEPTED" : "ACCOUNT_PASSWORD_RESET",
            "Succeeded",
            acceptsInvitation
                ? "The invited account accepted its one-time link and created its first password."
                : "The account password was reset through a valid recovery token.",
            null,
            cancellationToken);
        return AccountLifecycleResult.Success(
            "PASSWORD_RESET",
            "Password reset successfully. Please sign in again.",
            MapAccount(account));
    }

    public async Task<AccountLifecycleResult> ChangePasswordAsync(
        int accountId,
        PasswordChangeReqDTO request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return AccountLifecycleResult.BadRequest(
                "PASSWORD_CONFIRMATION_MISMATCH",
                "The password confirmation does not match.");
        }

        var account = await _userManager.FindByIdAsync(accountId.ToString());
        if (account is null || account.AccountStatus != AppAccountStatus.Active)
        {
            return AccountLifecycleResult.Unauthorized(
                "ACCOUNT_UNAVAILABLE",
                "The account is not available.");
        }

        var result = await _userManager.ChangePasswordAsync(
            account,
            request.CurrentPassword,
            request.NewPassword);
        if (!result.Succeeded)
        {
            await RecordAuditAsync(
                accountId,
                account.Id,
                "ACCOUNT_PASSWORD_CHANGE_FAILED",
                "Rejected",
                "The current password was rejected or the new password violated policy.",
                null,
                cancellationToken);
            return AccountLifecycleResult.BadRequest(
                "PASSWORD_CHANGE_INVALID",
                "The current password is incorrect or the new password does not satisfy policy.");
        }

        account.MustChangePassword = false;
        account.UpdatedAtUtc = DateTime.UtcNow;
        InvalidateSessions(account);
        await _userManager.UpdateAsync(account);
        await RecordAuditAsync(
            accountId,
            account.Id,
            "ACCOUNT_PASSWORD_CHANGED",
            "Succeeded",
            "The account password was changed; existing sessions require re-authentication.",
            null,
            cancellationToken);
        return AccountLifecycleResult.Success(
            "PASSWORD_CHANGED",
            "Password changed successfully. Please sign in again.",
            MapAccount(account));
    }

    public async Task<AccountLifecycleResult> AdminResetPasswordAsync(
        int actorAccountId,
        AdminPasswordResetReqDTO request,
        CancellationToken cancellationToken = default)
    {
        if (actorAccountId <= 0 || actorAccountId == request.AccountId)
        {
            return AccountLifecycleResult.Conflict(
                "SELF_PASSWORD_RESET",
                "An administrator cannot reset their own password through the fallback flow.");
        }

        if (!string.Equals(request.TemporaryPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return AccountLifecycleResult.BadRequest(
                "PASSWORD_CONFIRMATION_MISMATCH",
                "The password confirmation does not match.");
        }

        var account = await _userManager.FindByIdAsync(request.AccountId.ToString());
        if (account is null)
        {
            return AccountLifecycleResult.NotFound(
                "ACCOUNT_NOT_FOUND",
                "The application account was not found.");
        }

        if (account.AccountStatus != AppAccountStatus.Active)
        {
            return AccountLifecycleResult.Conflict(
                "ACCOUNT_NOT_ACTIVE",
                "Only an active application account can use the administrator password-reset fallback.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(account);
        var result = await _userManager.ResetPasswordAsync(account, token, request.TemporaryPassword);
        if (!result.Succeeded)
        {
            return AccountLifecycleResult.BadRequest(
                "PASSWORD_RESET_INVALID",
                "The temporary password does not satisfy the account policy.");
        }

        account.MustChangePassword = true;
        account.UpdatedAtUtc = DateTime.UtcNow;
        InvalidateSessions(account);
        await _userManager.UpdateAsync(account);
        await RecordAuditAsync(
            actorAccountId,
            account.Id,
            "ACCOUNT_ADMIN_PASSWORD_RESET",
            "Succeeded",
            "An administrator reset the account password; the account must change it at next login.",
            request.Reason,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(account.Email))
        {
            await TrySendAsync(
                new AccountEmailMessage(
                    account.Email,
                    "GTAS VPP - Password reset by administrator",
                    "Your GTAS VPP password was reset by an administrator. Sign in with the temporary password supplied through the approved internal channel and change it immediately."),
                account.Id,
                "ACCOUNT_ADMIN_PASSWORD_RESET_EMAIL_FAILED",
                cancellationToken);
        }

        return AccountLifecycleResult.Success(
            "PASSWORD_RESET",
            "The temporary password was set. The account must change it at next login.",
            MapAccount(account));
    }

    public AccountAdministrationCapabilitiesResDTO GetAdministrationCapabilities() => new()
    {
        InvitationEnabled = _emailOptions.Enabled,
        DeliveryMode = _emailOptions.Enabled ? "EmailOutbox" : "Disabled",
        Message = _emailOptions.Enabled
            ? "Invitation and password-reset links are delivered through the configured email outbox."
            : "Email delivery is disabled; invitations cannot be created until an email adapter is enabled."
    };

    public async Task<AccountLifecycleResult> AdminInviteAsync(
        int actorAccountId,
        AdminAccountInvitationReqDTO request,
        CancellationToken cancellationToken = default)
    {
        if (actorAccountId <= 0)
        {
            return AccountLifecycleResult.Unauthorized("ACTOR_REQUIRED", "An authenticated administrator is required.");
        }

        if (!_emailOptions.Enabled)
        {
            return AccountLifecycleResult.Conflict(
                "INVITATION_EMAIL_DISABLED",
                "Email delivery must be enabled before inviting an account.");
        }

        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();
        var employeeCode = NormalizeOptional(request.EmployeeCode);
        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(fullName)
            || request.GroupId == Guid.Empty
            || request.PrimaryDepartmentId == Guid.Empty)
        {
            return AccountLifecycleResult.BadRequest(
                "INVITATION_FIELDS_REQUIRED",
                "Username, email, full name, group and primary department are required.");
        }

        var normalizedUsername = _userManager.NormalizeName(username);
        var normalizedEmail = _userManager.NormalizeEmail(email);
        var duplicate = await _context.Users.AsNoTracking().AnyAsync(
            account => (normalizedUsername != null && account.NormalizedUserName == normalizedUsername)
                || (normalizedEmail != null && account.NormalizedEmail == normalizedEmail)
                || (employeeCode != null && account.EmployeeCode == employeeCode),
            cancellationToken);
        if (duplicate)
        {
            return AccountLifecycleResult.Conflict(
                "INVITATION_DUPLICATE_ACCOUNT",
                "Username, email or employee code is already assigned to another account.");
        }

        var now = DateTime.UtcNow;
        var account = new AppUser
        {
            UserName = username,
            Email = email,
            FullName = fullName,
            EmployeeCode = employeeCode,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            AccountStatus = AppAccountStatus.PendingApproval,
            MustChangePassword = true,
            EmailConfirmed = false,
            LockoutEnabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SessionVersion = 1
        };

        IdentityResult createResult;
        try
        {
            createResult = await _userManager.CreateAsync(account);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            _context.Entry(account).State = EntityState.Detached;
            return AccountLifecycleResult.Conflict(
                "INVITATION_DUPLICATE_ACCOUNT",
                "Username, email or employee code is already assigned to another account.");
        }

        if (!createResult.Succeeded)
        {
            return AccountLifecycleResult.BadRequest(
                "INVITATION_INVALID",
                "The invitation data does not satisfy the account policy.");
        }

        var membership = await _membershipAdministrationService.ActivateAndUpsertAsync(
            actorAccountId,
            new MembershipUpsertReqDTO
            {
                AccountId = account.Id,
                GroupId = request.GroupId,
                PrimaryDepartmentId = request.PrimaryDepartmentId,
                Reason = request.Reason
            },
            cancellationToken);
        if (!membership.Succeeded)
        {
            await _userManager.DeleteAsync(account);
            return new AccountLifecycleResult(membership.StatusCode, membership.Code, membership.Message);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(account);
        var invitationUrl = BuildUrl(
            "/Account/ResetPassword",
            $"userId={account.Id}&token={Uri.EscapeDataString(token)}");
        await TrySendAsync(
            new AccountEmailMessage(
                email,
                "GTAS VPP - Account invitation",
                $"You were invited to GTAS VPP. Create your password using this one-time link: {invitationUrl}"),
            account.Id,
            "ACCOUNT_INVITATION_EMAIL_FAILED",
            cancellationToken);
        await RecordAuditAsync(
            actorAccountId,
            account.Id,
            "ACCOUNT_INVITED",
            "Succeeded",
            "An administrator created an account without a password and sent a one-time setup link.",
            request.Reason,
            cancellationToken);

        var refreshed = await _userManager.FindByIdAsync(account.Id.ToString()) ?? account;
        return AccountLifecycleResult.Success(
            "ACCOUNT_INVITED",
            "The account was created and a one-time password setup link was queued.",
            MapAccount(refreshed),
            StatusCodes.Status201Created);
    }

    public async Task<AccountLifecycleResult> AdminSendPasswordResetLinkAsync(
        int actorAccountId,
        AdminPasswordResetLinkReqDTO request,
        CancellationToken cancellationToken = default)
    {
        if (!_emailOptions.Enabled)
        {
            return AccountLifecycleResult.Conflict(
                "PASSWORD_RESET_EMAIL_DISABLED",
                "Email delivery must be enabled before sending a password-reset link.");
        }

        var account = await _userManager.FindByIdAsync(request.AccountId.ToString());
        if (account is null || string.IsNullOrWhiteSpace(account.Email))
        {
            return AccountLifecycleResult.NotFound("ACCOUNT_EMAIL_NOT_FOUND", "The account email was not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(account);
        var resetUrl = BuildUrl(
            "/Account/ResetPassword",
            $"userId={account.Id}&token={Uri.EscapeDataString(token)}");
        await TrySendAsync(
            new AccountEmailMessage(
                account.Email,
                "GTAS VPP - Password setup link",
                $"Create or reset your GTAS VPP password using this one-time link: {resetUrl}"),
            account.Id,
            "ACCOUNT_ADMIN_RESET_LINK_EMAIL_FAILED",
            cancellationToken);
        await RecordAuditAsync(
            actorAccountId,
            account.Id,
            "ACCOUNT_ADMIN_RESET_LINK_SENT",
            "Succeeded",
            "An administrator queued a one-time password setup/reset link without seeing a password.",
            request.Reason,
            cancellationToken);

        return AccountLifecycleResult.Success(
            "PASSWORD_RESET_LINK_SENT",
            "A one-time password setup/reset link was queued.",
            MapAccount(account));
    }

    public async Task<MembershipAdministrationResult> ActivateAsync(
        int actorAccountId,
        AdminAccountActivationReqDTO request,
        CancellationToken cancellationToken = default)
    {
        if (_emailOptions.Enabled
            && _emailOptions.RequireConfirmationWhenEnabled)
        {
            var account = await _userManager.FindByIdAsync(request.AccountId.ToString());
            if (account is null)
            {
                return MembershipAdministrationResult.NotFound(
                    "ACCOUNT_NOT_FOUND",
                    "The application account was not found.");
            }

            if (!account.EmailConfirmed)
            {
                return MembershipAdministrationResult.Conflict(
                    "EMAIL_CONFIRMATION_REQUIRED",
                    "The account email must be confirmed before activation.");
            }
        }

        var result = await _membershipAdministrationService.ActivateAndUpsertAsync(
            actorAccountId,
            new MembershipUpsertReqDTO
            {
                AccountId = request.AccountId,
                GroupId = request.GroupId,
                PrimaryDepartmentId = request.PrimaryDepartmentId,
                Reason = request.Reason
            },
            cancellationToken);

        if (result.Succeeded && result.Membership is not null)
        {
            var account = await _userManager.FindByIdAsync(request.AccountId.ToString());
            if (account is not null)
            {
                await TryNotifyUserAsync(
                    account,
                    "ACCOUNT_ACTIVATED",
                    "GTAS VPP account activated",
                    "Your GTAS VPP account has been activated. You can now sign in.",
                    "/Account/Login",
                    cancellationToken);
                if (!string.IsNullOrWhiteSpace(account.Email))
                {
                    await TrySendAsync(
                        new AccountEmailMessage(
                            account.Email,
                            "GTAS VPP - Account activated",
                            "Your GTAS VPP account has been activated. You can now sign in to the internal application."),
                        account.Id,
                        "ACCOUNT_ACTIVATION_EMAIL_FAILED",
                        cancellationToken);
                }
            }
        }

        return result;
    }

    private async Task TrySendConfirmationAsync(
        AppUser account,
        CancellationToken cancellationToken)
    {
        if (!_emailOptions.Enabled || string.IsNullOrWhiteSpace(account.Email))
        {
            return;
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(account);
        var url = BuildUrl(
            "/Account/ConfirmEmail",
            $"userId={account.Id}&token={Uri.EscapeDataString(token)}");
        await TrySendAsync(
            new AccountEmailMessage(
                account.Email,
                "GTAS VPP - Confirm your email",
                $"Confirm your GTAS VPP email by opening this link: {url}"),
            account.Id,
            "ACCOUNT_CONFIRMATION_EMAIL_FAILED",
            cancellationToken);
    }

    private async Task TrySendAsync(
        AccountEmailMessage message,
        int accountId,
        string failureAction,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Account email delivery failed for AccountId={AccountId}.", accountId);
            await RecordAuditAsync(
                null,
                accountId,
                failureAction,
                "EmailUnavailable",
                "The account email adapter was unavailable; in-app fallback remains available.",
                null,
                cancellationToken);
        }
    }

    private async Task TryNotifyAdministratorsAsync(
        AppUser account,
        string type,
        string title,
        string message,
        string route,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await _notificationService.GetRecipientsWithPermissionAsync(
                account.MemberCompanyCode.ToString(),
                Permissions.PermissionManage,
                cancellationToken);
            await _notificationService.PublishAsync(
                recipients,
                account.MemberCompanyCode.ToString(),
                type,
                title,
                message,
                route,
                Activity.Current?.TraceId.ToString(),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not publish account-admin notification for AccountId={AccountId}.", account.Id);
        }
    }

    private async Task TryNotifyUserAsync(
        AppUser account,
        string type,
        string title,
        string message,
        string route,
        CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.PublishAsync(
                [account.Id],
                account.MemberCompanyCode.ToString(),
                type,
                title,
                message,
                route,
                Activity.Current?.TraceId.ToString(),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Could not publish account notification for AccountId={AccountId}.", account.Id);
        }
    }

    private async Task RecordAuditAsync(
        int? actorUserId,
        int? targetUserId,
        string action,
        string outcome,
        string summary,
        string? reason,
        CancellationToken cancellationToken)
    {
        _context.SecurityAudits.Add(new SecurityAudit
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            Action = action,
            ResourceType = "AppUser",
            ResourceId = targetUserId?.ToString(),
            Outcome = outcome,
            Summary = summary,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CorrelationId = Activity.Current?.TraceId.ToString(),
            OccurredAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private string BuildUrl(string path, string query) =>
        $"{_emailOptions.PublicBaseUrl.TrimEnd('/')}{path}?{query}";

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<bool> IsPasswordValidAsync(AppUser account, string password)
    {
        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, account, password);
            if (!result.Succeeded)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    private static void InvalidateSessions(AppUser account)
    {
        account.SessionVersion = checked(account.SessionVersion + 1);
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    private static AccountLifecycleResDTO MapAccount(AppUser account) => new()
    {
        AccountId = account.Id,
        AccountStatus = account.AccountStatus.ToString(),
        MustChangePassword = account.MustChangePassword,
        EmailConfirmed = account.EmailConfirmed,
        Message = "Account lifecycle operation completed."
    };
}
