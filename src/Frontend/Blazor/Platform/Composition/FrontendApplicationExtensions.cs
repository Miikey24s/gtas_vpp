using gtas_vpp_fe.Components;
using gtas_vpp_fe.Endpoints;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace gtas_vpp_fe.Platform.Composition;

public static class FrontendApplicationExtensions
{
    public static WebApplication UseFrontendPipeline(
        this WebApplication app,
        FrontendRuntimeSettings settings)
    {
        app.Logger.LogInformation("Configured API base URL: {ApiBaseUrl}", settings.ApiBaseUri);
        if (settings.BypassApiServerCertificateValidation)
        {
            app.Logger.LogWarning(
                "Ignoring HTTPS certificate validation for local development API endpoint {ApiBaseUrl}.",
                settings.ApiBaseUri);
        }

        // Forwarded scheme phải có trước SignalR để Blazor tạo đúng kết nối wss phía sau reverse proxy.
        app.UseForwardedHeaders();

        var supportedCultures = new[] { new CultureInfo("vi"), new CultureInfo("en") };
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("vi"),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // Nginx sở hữu HTTPS redirect và HSTS trong môi trường triển khai hiện tại.
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        // Static framework assets phải được phục vụ trước authentication.
        app.UseStaticFiles();
        app.MapStaticAssets();

        app.UseCookiePolicy();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        return app;
    }

    public static WebApplication MapFrontendEndpoints(this WebApplication app)
    {
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapLoginEndpoints();
        app.MapHealthChecks("/health").AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet(
                "/debug/endpoints",
                (IEnumerable<EndpointDataSource> endpointSources) =>
                {
                    var endpoints = endpointSources
                        .SelectMany(source => source.Endpoints)
                        .Select(endpoint => new
                        {
                            endpoint.DisplayName,
                            RoutePattern = (endpoint as RouteEndpoint)?.RoutePattern.RawText
                        });
                    return endpoints;
                });
        }

        return app;
    }
}
