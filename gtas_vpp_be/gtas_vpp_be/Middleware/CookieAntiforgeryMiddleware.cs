using System.Text.Json;
using gtas_vpp_be.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace gtas_vpp_be.Middleware;

/// <summary>
/// Cookie authentication is automatic in the browser, so every unsafe request
/// must also prove that it originated from the GTAS frontend. Bearer clients
/// remain supported for the Blazor rollback path and aren't subject to this
/// cookie-specific boundary.
/// </summary>
public sealed class CookieAntiforgeryMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    };

    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var usesCookieSession = context.User.Identity?.IsAuthenticated == true
            && context.Request.Cookies.ContainsKey(AppAuthenticationSchemes.SessionCookieName);
        var allowAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        if (!usesCookieSession
            || allowAnonymous
            || SafeMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context);
            await _next(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                new
                {
                    type = "https://httpstatuses.com/400",
                    title = "Antiforgery validation failed.",
                    status = StatusCodes.Status400BadRequest,
                    code = "ANTIFORGERY_VALIDATION_FAILED"
                },
                cancellationToken: context.RequestAborted);
        }
    }
}
