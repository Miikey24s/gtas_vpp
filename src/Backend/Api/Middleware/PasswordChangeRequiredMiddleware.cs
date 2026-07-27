using System.Security.Claims;
using System.Text.Json;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace gtas_vpp_be.Middleware;

/// <summary>
/// Mật khẩu quản trị tạm thời không được cấp quyền truy cập API nghiệp vụ.
/// Mỗi request, authentication bổ sung principal từ bản ghi tài khoản hiện tại;
/// boundary này chỉ cho phép đổi mật khẩu và đăng xuất cho đến khi cờ được xóa
/// và các session hiện có bị thu hồi.
/// </summary>
public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    private static readonly PathString ChangePasswordPath =
        new("/api/account/password/change");
    private static readonly PathString LogoutPath = new("/api/Auth/logout");
    private static readonly PathString CurrentUserPath = new("/api/Auth/me");
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var mustChangePassword = context.User.Identity?.IsAuthenticated == true
            && bool.TryParse(
                context.User.FindFirstValue(AppClaimTypes.MustChangePassword),
                out var required)
            && required;
        if (!mustChangePassword
            || IsAllowedPath(context.Request.Path)
            || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        context.Response.Headers["X-Auth-Reason"] = "password-change-required";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new
            {
                code = "PASSWORD_CHANGE_REQUIRED",
                message = "The temporary password must be changed before using this API."
            },
            cancellationToken: context.RequestAborted);
    }

    private static bool IsAllowedPath(PathString path) =>
        path.Equals(ChangePasswordPath, StringComparison.OrdinalIgnoreCase)
        || path.Equals(LogoutPath, StringComparison.OrdinalIgnoreCase)
        || path.Equals(CurrentUserPath, StringComparison.OrdinalIgnoreCase);
}
