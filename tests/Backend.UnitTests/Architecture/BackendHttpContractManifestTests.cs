using System.Reflection;
using gtas_vpp_be.Controllers;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Sdk;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class BackendHttpContractManifestTests
{
    private const int ExpectedEndpointCount = 135;

    [Fact]
    public void PublicControllerContracts_MatchB0RManifest()
    {
        var endpoints = DiscoverContracts();
        var duplicateKeys = endpoints
            .GroupBy(endpoint => $"{endpoint.Verb} {endpoint.Route}", StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        var actualLines = endpoints
            .Select(contract => contract.ToString())
            .ToArray();
        var expectedLines = ExpectedManifest
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var namedPolicies = endpoints
            .SelectMany(endpoint => endpoint.Authorization.Split(" & ", StringSplitOptions.RemoveEmptyEntries))
            .Where(requirement => requirement.StartsWith("POLICY:", StringComparison.Ordinal))
            .Select(requirement => requirement["POLICY:".Length..])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedEndpointCount, endpoints.Count);
        Assert.Equal(ExpectedEndpointCount, expectedLines.Length);
        Assert.Empty(duplicateKeys);
        Assert.All(endpoints, endpoint => Assert.False(string.IsNullOrWhiteSpace(endpoint.Authorization)));
        Assert.All(namedPolicies, policy => Assert.Contains(policy, Permissions.All));
        Assert.Equal(expectedLines, actualLines);
    }

    private static IReadOnlyList<DiscoveredEndpoint> DiscoverContracts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddControllers()
            .AddApplicationPart(typeof(AccountController).Assembly);
        using var provider = services.BuildServiceProvider();

        var actions = provider
            .GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .ToArray();
        var contracts = new List<DiscoveredEndpoint>();

        foreach (var action in actions)
        {
            var route = action.AttributeRouteInfo?.Template;
            if (string.IsNullOrWhiteSpace(route))
            {
                throw new XunitException($"{Handler(action)} must declare an attribute route.");
            }

            var httpMethods = action.ActionConstraints?
                .OfType<HttpMethodActionConstraint>()
                .SelectMany(constraint => constraint.HttpMethods)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];
            if (httpMethods.Length == 0)
            {
                throw new XunitException($"{Handler(action)} must declare an HTTP method.");
            }

            var authorization = ResolveAuthorization(action);
            foreach (var httpMethod in httpMethods)
            {
                contracts.Add(new DiscoveredEndpoint(
                    httpMethod.ToUpperInvariant(),
                    $"/{route.TrimStart('/')}",
                    authorization,
                    Handler(action)));
            }
        }

        return contracts
            .OrderBy(contract => contract.Route, StringComparer.OrdinalIgnoreCase)
            .ThenBy(contract => contract.Verb, StringComparer.Ordinal)
            .ThenBy(contract => contract.Handler, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ResolveAuthorization(ControllerActionDescriptor action)
    {
        var controllerType = action.ControllerTypeInfo.AsType();
        var method = action.MethodInfo;
        if (controllerType.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null
            || method.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null)
        {
            return "ANONYMOUS";
        }

        var authorizeAttributes = controllerType
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Concat(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
            .ToArray();
        if (authorizeAttributes.Length == 0)
        {
            throw new XunitException($"{Handler(action)} must explicitly authorize or allow anonymous access.");
        }

        var requirements = authorizeAttributes
            .SelectMany(ToRequirements)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return requirements.Length == 0
            ? "AUTHENTICATED"
            : string.Join(" & ", requirements);
    }

    private static IEnumerable<string> ToRequirements(AuthorizeAttribute attribute)
    {
        if (!string.IsNullOrWhiteSpace(attribute.Policy))
        {
            yield return $"POLICY:{attribute.Policy}";
        }

        if (!string.IsNullOrWhiteSpace(attribute.Roles))
        {
            var roles = attribute.Roles
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Order(StringComparer.Ordinal);
            yield return $"ROLES:{string.Join(',', roles)}";
        }

        if (!string.IsNullOrWhiteSpace(attribute.AuthenticationSchemes))
        {
            var schemes = attribute.AuthenticationSchemes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Order(StringComparer.Ordinal);
            yield return $"SCHEMES:{string.Join(',', schemes)}";
        }
    }

    private static string Handler(ControllerActionDescriptor action) =>
        $"{action.ControllerTypeInfo.Name}.{action.MethodInfo.Name}";

    private sealed record DiscoveredEndpoint(
        string Verb,
        string Route,
        string Authorization,
        string Handler)
    {
        public override string ToString() => $"{Verb} | {Route} | {Authorization}";
    }

    // Đây là biên API public của B0R; handler nội bộ có thể đổi tên hoặc tách file khi refactor.
    private const string ExpectedManifest = """
        POST | /api/account/admin/activate | POLICY:PERMISSION_MANAGE
        GET | /api/account/admin/capabilities | POLICY:PERMISSION_MANAGE
        POST | /api/account/admin/invite | POLICY:PERMISSION_MANAGE
        POST | /api/account/admin/reset-password | POLICY:PERMISSION_MANAGE
        POST | /api/account/admin/send-password-reset-link | POLICY:PERMISSION_MANAGE
        GET | /api/account/confirm-email | ANONYMOUS
        POST | /api/account/confirm-email/resend | ANONYMOUS
        POST | /api/account/password/change | AUTHENTICATED
        POST | /api/account/password/recovery | ANONYMOUS
        POST | /api/account/password/reset | ANONYMOUS
        POST | /api/account/register | ANONYMOUS
        GET | /api/Auth/antiforgery | AUTHENTICATED
        POST | /api/Auth/login | ANONYMOUS
        POST | /api/Auth/logout | AUTHENTICATED
        GET | /api/Auth/me | AUTHENTICATED
        GET | /api/Auth/me/permissions | AUTHENTICATED
        GET | /api/catalog/items | POLICY:LIBRARY_VIEW
        POST | /api/catalog/items | POLICY:LIBRARY_MANAGE
        DELETE | /api/catalog/items/{id:guid} | POLICY:LIBRARY_MANAGE
        GET | /api/catalog/items/{id:guid} | POLICY:LIBRARY_VIEW
        PUT | /api/catalog/items/{id:guid} | POLICY:LIBRARY_MANAGE
        PATCH | /api/catalog/items/{id:guid}/status | POLICY:LIBRARY_MANAGE
        GET | /api/Library/{tableCode} | POLICY:LIBRARY_VIEW
        POST | /api/Library/{tableCode} | POLICY:LIBRARY_MANAGE
        PUT | /api/Library/{tableCode} | POLICY:LIBRARY_MANAGE
        DELETE | /api/Library/{tableCode}/{id:guid} | POLICY:LIBRARY_MANAGE
        GET | /api/Library/{tableCode}/{id:guid} | POLICY:LIBRARY_VIEW
        PATCH | /api/Library/{tableCode}/{id:guid} | POLICY:LIBRARY_MANAGE
        GET | /api/Library/{tableCode}/{id:guid}/dependency-impact | POLICY:LIBRARY_MANAGE
        GET | /api/notifications | AUTHENTICATED
        POST | /api/notifications/read-all | AUTHENTICATED
        POST | /api/notifications/{id:guid}/read | AUTHENTICATED
        GET | /api/order-periods | POLICY:PERIOD_SETTLE
        POST | /api/order-periods | POLICY:PERIOD_SETTLE
        POST | /api/order-periods/horizon-preview | POLICY:PERIOD_SETTLE
        GET | /api/order-periods/settings | POLICY:PERIOD_SETTINGS_MANAGE
        POST | /api/order-periods/settings | POLICY:PERIOD_SETTINGS_MANAGE
        GET | /api/order-periods/settings/current | POLICY:PERIOD_SETTLE
        GET | /api/order-periods/settings/history | POLICY:PERIOD_SETTINGS_MANAGE
        POST | /api/order-periods/top-up | POLICY:PERIOD_SETTLE
        PUT | /api/order-periods/{id:guid} | POLICY:PERIOD_SETTLE
        POST | /api/order-periods/{id:guid}/close-submissions | POLICY:PERIOD_SETTLE
        POST | /api/order-periods/{id:guid}/delete | POLICY:PERIOD_SETTLE
        POST | /api/order-periods/{id:guid}/extend-deadline | POLICY:PERIOD_SETTLE
        POST | /api/order-periods/{id:guid}/hard-delete | POLICY:PERIOD_SETTLE
        POST | /api/order-periods/{id:guid}/reopen-submissions | POLICY:PERIOD_SETTLE
        POST | /api/order-periods/{id:guid}/restore | POLICY:PERIOD_SETTLE
        GET | /api/PeriodSettlement | POLICY:PERIOD_SETTLE
        POST | /api/PeriodSettlement/confirm | POLICY:PERIOD_SETTLE
        GET | /api/PeriodSettlement/current/{y:int}/{m:int} | POLICY:PERIOD_SETTLE
        POST | /api/PeriodSettlement/preview | POLICY:PERIOD_SETTLE
        GET | /api/PeriodSettlement/revisions/{y:int}/{m:int} | POLICY:PERIOD_SETTLE
        POST | /api/PeriodSettlement/settle | POLICY:PERIOD_SETTLE
        POST | /api/PeriodSettlement/{settlementId:guid}/correct | POLICY:PERIOD_SETTLE
        GET | /api/PeriodSettlement/{settlementId:guid}/export.pdf | POLICY:PERIOD_SETTLE
        GET | /api/PeriodSettlement/{settlementId:guid}/export.xlsx | POLICY:PERIOD_SETTLE
        GET | /api/PeriodSettlement/{y:int}/{m:int} | POLICY:PERIOD_SETTLE
        PATCH | /api/Permission/component-mapping | POLICY:PERMISSION_MANAGE & POLICY:PERMISSION_VIEW
        PATCH | /api/Permission/component-mappings/batch | POLICY:PERMISSION_MANAGE & POLICY:PERMISSION_VIEW
        GET | /api/Permission/groups | POLICY:PERMISSION_VIEW
        GET | /api/Permission/groups/{id:guid} | POLICY:PERMISSION_VIEW
        PUT | /api/Permission/groups/{id:guid} | POLICY:PERMISSION_MANAGE & POLICY:PERMISSION_VIEW
        GET | /api/Permission/groups/{id:guid}/page-components | POLICY:PERMISSION_VIEW
        PUT | /api/Permission/memberships | POLICY:PERMISSION_MANAGE & POLICY:PERMISSION_VIEW
        POST | /api/Permission/memberships/deactivate | POLICY:PERMISSION_MANAGE & POLICY:PERMISSION_VIEW
        GET | /api/Permission/security-audits | POLICY:PERMISSION_MANAGE & POLICY:PERMISSION_VIEW
        GET | /api/Permission/security-audits/filter-options | POLICY:PERMISSION_MANAGE & POLICY:PERMISSION_VIEW
        GET | /api/Permission/user-groups | POLICY:PERMISSION_VIEW
        GET | /api/Permission/users | POLICY:PERMISSION_VIEW
        GET | /api/post-settlement-order-corrections | POLICY:PERIOD_SETTLE
        POST | /api/post-settlement-order-corrections | POLICY:PERIOD_SETTLE
        POST | /api/post-settlement-order-corrections/{id:guid}/confirm | POLICY:PERIOD_SETTLE
        POST | /api/post-settlement-order-corrections/{id:guid}/reject | POLICY:PERIOD_SETTLE
        GET | /api/reports/export | POLICY:REPORT_EXPORT
        GET | /api/reports/export.pdf | POLICY:REPORT_EXPORT
        GET | /api/reports/export.xlsx | POLICY:REPORT_EXPORT
        GET | /api/reports/insights | AUTHENTICATED
        GET | /api/reports/summary | AUTHENTICATED
        POST | /api/VPPPrice | POLICY:LIBRARY_MANAGE
        GET | /api/VPPPrice/by-supplier/{supplierId:guid} | POLICY:LIBRARY_VIEW
        GET | /api/VPPPrice/by-vpp/{vppId:guid} | POLICY:LIBRARY_VIEW
        GET | /api/VPPPrice/item-prices | POLICY:LIBRARY_VIEW
        POST | /api/VPPPrice/resolve | POLICY:LIBRARY_VIEW
        DELETE | /api/VPPPrice/{id:guid} | POLICY:LIBRARY_MANAGE
        PUT | /api/VPPPrice/{id:guid} | POLICY:LIBRARY_MANAGE
        PATCH | /api/VPPPrice/{id:guid}/deleted | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPrice/{id:guid}/set-default | POLICY:LIBRARY_MANAGE
        GET | /api/VPPPriceList | POLICY:LIBRARY_VIEW
        POST | /api/VPPPriceList | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPriceList/clone | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPriceList/compare | POLICY:LIBRARY_VIEW
        GET | /api/VPPPriceList/imports/template.xlsx | POLICY:LIBRARY_MANAGE
        DELETE | /api/VPPPriceList/{id:guid} | POLICY:LIBRARY_MANAGE
        GET | /api/VPPPriceList/{id:guid} | POLICY:LIBRARY_VIEW
        PUT | /api/VPPPriceList/{id:guid} | POLICY:LIBRARY_MANAGE
        PATCH | /api/VPPPriceList/{id:guid}/deleted | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPriceList/{id:guid}/expire | POLICY:LIBRARY_MANAGE
        GET | /api/VPPPriceList/{id:guid}/export.xlsx | POLICY:LIBRARY_VIEW
        DELETE | /api/VPPPriceList/{id:guid}/hard | POLICY:LIBRARY_MANAGE
        GET | /api/VPPPriceList/{id:guid}/imports | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPriceList/{id:guid}/imports/analyze | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPriceList/{id:guid}/imports/preview | POLICY:LIBRARY_MANAGE
        GET | /api/VPPPriceList/{id:guid}/imports/template.xlsx | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPriceList/{id:guid}/imports/{batchId:guid}/confirm | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPriceList/{id:guid}/publish | POLICY:LIBRARY_MANAGE
        POST | /api/VPPPriceList/{id:guid}/set-default | POLICY:LIBRARY_MANAGE
        GET | /api/VPPRequest/additional-orders/pending | AUTHENTICATED
        POST | /api/VPPRequest/additional-orders/{id:guid}/approve | POLICY:REQUEST_APPROVE
        POST | /api/VPPRequest/additional-orders/{id:guid}/reject | POLICY:REQUEST_REJECT
        GET | /api/VPPRequest/all-orders | POLICY:REQUEST_VIEW_ALL
        GET | /api/VPPRequest/categories | POLICY:REQUEST_CATALOG_VIEW
        GET | /api/VPPRequest/dashboard-charts | POLICY:REQUEST_VIEW_OWN
        GET | /api/VPPRequest/department-order-history | POLICY:REQUEST_VIEW_DEPARTMENT
        GET | /api/VPPRequest/department-order-history-summary | POLICY:REQUEST_VIEW_DEPARTMENT
        GET | /api/VPPRequest/department-orders | POLICY:REQUEST_VIEW_DEPARTMENT
        GET | /api/VPPRequest/my-order-history | POLICY:REQUEST_VIEW_OWN
        GET | /api/VPPRequest/my-order-history-summary | POLICY:REQUEST_VIEW_OWN
        GET | /api/VPPRequest/my-orders | POLICY:REQUEST_VIEW_OWN
        GET | /api/VPPRequest/my-orders-summary | POLICY:REQUEST_VIEW_OWN
        GET | /api/VPPRequest/order-filter-values | AUTHENTICATED
        POST | /api/VPPRequest/orders | POLICY:REQUEST_CREATE
        GET | /api/VPPRequest/orders/previous-items | POLICY:REQUEST_VIEW_OWN
        GET | /api/VPPRequest/orders/{id:guid} | AUTHENTICATED
        PUT | /api/VPPRequest/orders/{id:guid} | POLICY:REQUEST_UPDATE_OWN
        POST | /api/VPPRequest/orders/{id:guid}/cancel | POLICY:REQUEST_CANCEL_OWN
        GET | /api/VPPRequest/orders/{id:guid}/export.pdf | AUTHENTICATED
        GET | /api/VPPRequest/orders/{id:guid}/export.xlsx | AUTHENTICATED
        GET | /api/VPPRequest/orders/{id:guid}/history | AUTHENTICATED
        POST | /api/VPPRequest/orders/{id:guid}/manager-adjustment | POLICY:PERIOD_SETTLE
        POST | /api/VPPRequest/orders/{id:guid}/recreate | POLICY:REQUEST_UPDATE_OWN
        POST | /api/VPPRequest/orders/{id:guid}/restore | POLICY:REQUEST_UPDATE_OWN
        GET | /api/VPPRequest/period-demand | POLICY:PERIOD_SETTLE
        GET | /api/VPPRequest/period-info | POLICY:REQUEST_VIEW_OWN
        GET | /api/VPPRequest/products | POLICY:REQUEST_CATALOG_VIEW
        GET | /api/VPPRequest/products/lookup | POLICY:REQUEST_CATALOG_VIEW
        """;
}
