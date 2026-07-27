using System.Net;
using System.Text.Json;
using gtas_vpp_fe.Components;
using gtas_vpp_fe.Services;
using Microsoft.Extensions.Localization;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Ánh xạ mã response vòng đời tài khoản công khai sang nội dung UI đã localization.
/// Message backend vẫn là output chẩn đoán/domain và không được render trực tiếp.
/// </summary>
public static class AccountLifecycleUiMapper
{
    public static string GetMessage(
        Exception exception,
        IStringLocalizer<App> localizer,
        string fallbackKey = "RequestInvalid")
    {
        if (exception is ApiRequestException apiException)
        {
            return GetMessage(
                apiException.ErrorCode,
                apiException.StatusCode ?? HttpStatusCode.BadRequest,
                localizer,
                fallbackKey);
        }

        return UiErrorMapper.GetMessage(exception, localizer, fallbackKey);
    }

    public static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response,
        IStringLocalizer<App> localizer,
        string fallbackKey = "RequestInvalid")
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(localizer);

        string? code = null;
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("code", out var codeNode))
            {
                code = codeNode.GetString();
            }
        }
        catch (JsonException)
        {
            // Status và fallback do caller cung cấp vẫn đủ dùng.
        }

        return GetMessage(code, response.StatusCode, localizer, fallbackKey);
    }

    public static string GetMessage(
        string? code,
        HttpStatusCode statusCode,
        IStringLocalizer<App> localizer,
        string fallbackKey = "RequestInvalid")
    {
        var resourceKey = code?.Trim().ToUpperInvariant() switch
        {
            "PASSWORD_CONFIRMATION_MISMATCH" => "PasswordConfirmationMismatch",
            "REGISTRATION_FIELDS_REQUIRED" => "RegistrationFieldsRequired",
            "REGISTRATION_INVALID" => "RegistrationInvalid",
            "EMAIL_CONFIRMATION_INVALID" => "ConfirmationLinkInvalid",
            "PASSWORD_RESET_INVALID" => "ResetLinkInvalid",
            "PASSWORD_CHANGE_INVALID" => "PasswordChangeInvalid",
            "ACCOUNT_UNAVAILABLE" => "AccountUnavailable",
            "ACCOUNT_NOT_FOUND" => "AccountNotFound",
            "ACCOUNT_NOT_ACTIVE" => "AccountNotActive",
            _ => statusCode switch
            {
                HttpStatusCode.Unauthorized => "Unauthorized",
                HttpStatusCode.Forbidden => "Forbidden",
                HttpStatusCode.NotFound => "NotFound",
                HttpStatusCode.Conflict => "Conflict",
                HttpStatusCode.TooManyRequests => "RateLimited",
                _ => fallbackKey
            }
        };

        var localized = localizer[resourceKey];
        return localized.ResourceNotFound
            ? localizer["RequestFailed"].Value
            : localized.Value;
    }
}
