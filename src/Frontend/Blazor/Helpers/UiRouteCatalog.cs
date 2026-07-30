namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Typed visual/composition profile for each logical route. This is metadata only;
/// route API, permission and business state remain in the route itself.
/// </summary>
public static class UiRouteCatalog
{
    public enum WorkspacePattern
    {
        SystemState,
        Process,
        Account,
        Collection,
        ListDetail,
        SplitEditor,
        Operation,
        Analytics
    }

    public enum DataSourceMode
    {
        None,
        Static,
        ServerPaging,
        ClientSnapshotPaged,
        ClientSnapshotVirtualized,
        Mixed
    }

    public enum Density
    {
        None,
        Compact,
        RichTwoLine,
        Mixed
    }

    public enum ToolbarMotif
    {
        None,
        Filter,
        FilterWithColumnPicker,
        Decision,
        Workflow,
        Report,
        Account
    }

    public enum FooterMotif
    {
        None,
        Summary,
        Paged,
        VirtualSummary,
        ActionBar,
        Mixed
    }

    public enum ResponsiveStrategy
    {
        None,
        Shell,
        AccountCentered,
        BoundedSurface,
        StackPanes,
        OverlayDetail,
        AdaptiveDialog
    }

    public sealed record Profile(
        string RouteKey,
        WorkspacePattern Workspace,
        DataSourceMode DataSource,
        Density RowDensity,
        ToolbarMotif Toolbar,
        FooterMotif Footer,
        ResponsiveStrategy Responsive,
        string[] Motifs,
        string[] States,
        string? Notes = null);

    public static IReadOnlyDictionary<string, Profile> Profiles { get; } = BuildProfiles();

    public static Profile Get(string routeKey) =>
        Profiles.TryGetValue(routeKey, out var profile)
            ? profile
            : throw new KeyNotFoundException($"No UI profile registered for route '{routeKey}'.");

    private static IReadOnlyDictionary<string, Profile> BuildProfiles()
    {
        var profiles = new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase);

        Add(profiles, [
            "account.change-password",
            "account.change-password.required",
            "account.forgot-password",
            "account.reset-password",
            "account.register",
            "account.confirm-email"
        ], WorkspacePattern.Account, DataSourceMode.None, Density.None, ToolbarMotif.Account, FooterMotif.None, ResponsiveStrategy.AccountCentered, ["ACCOUNT", "FEEDBACK"], ["loading", "error", "success", "disabled"]);

        Add(profiles, ["login", "login-process", "logout-process"], WorkspacePattern.Process, DataSourceMode.None, Density.None, ToolbarMotif.Account, FooterMotif.None, ResponsiveStrategy.AccountCentered, ["ACCOUNT", "FEEDBACK"], ["loading", "error", "success"]);
        Add(profiles, ["home", "not-found", "error"], WorkspacePattern.SystemState, DataSourceMode.None, Density.None, ToolbarMotif.None, FooterMotif.None, ResponsiveStrategy.Shell, ["CONTENT-STATE", "SHELL-NAV"], ["loading", "error", "denied"]);

        Add(profiles, ["dashboard.my-orders", "dashboard.my-orders.current", "dashboard.my-orders.supplement", "dashboard.my-orders.previous"], WorkspacePattern.Analytics, DataSourceMode.Mixed, Density.Mixed, ToolbarMotif.Filter, FooterMotif.Mixed, ResponsiveStrategy.OverlayDetail, ["ANALYTICS", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-FOOTER", "CELL-VALUE", "CONTENT-STATE", "TRANSIENT"], ["loading", "empty", "filtered-empty", "error", "disabled"]);
        Add(profiles, ["dashboard.history"], WorkspacePattern.Analytics, DataSourceMode.Mixed, Density.Mixed, ToolbarMotif.Filter, FooterMotif.Mixed, ResponsiveStrategy.OverlayDetail, ["ANALYTICS", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-FOOTER", "CELL-VALUE", "CONTENT-STATE", "TRANSIENT"], ["loading", "empty", "filtered-empty", "error", "disabled"]);
        Add(profiles, ["dashboard.catalog"], WorkspacePattern.Collection, DataSourceMode.ServerPaging, Density.RichTwoLine, ToolbarMotif.Filter, FooterMotif.Paged, ResponsiveStrategy.BoundedSurface, ["COLLECTION", "DATA-FRAME", "FILTER-TOOLBAR", "DATA-COLUMN", "DATA-FOOTER", "CELL-VALUE", "CONTENT-STATE"], ["loading", "empty", "filtered-empty", "error", "disabled"]);
        Add(profiles, ["dashboard.management.department"], WorkspacePattern.Analytics, DataSourceMode.Mixed, Density.Mixed, ToolbarMotif.Filter, FooterMotif.Mixed, ResponsiveStrategy.OverlayDetail, ["ANALYTICS", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-FOOTER", "CELL-VALUE", "CONTENT-STATE"], ["loading", "empty", "filtered-empty", "error", "disabled"]);
        Add(profiles, ["dashboard.management.all"], WorkspacePattern.Operation, DataSourceMode.Static, Density.Compact, ToolbarMotif.None, FooterMotif.None, ResponsiveStrategy.BoundedSurface, ["OPERATION", "CONTENT-STATE"], ["disabled"], "Legacy redirect contract only; no standalone page component.");
        Add(profiles, ["dashboard.period-operations", "dashboard.period.pending-approval", "dashboard.period.review", "dashboard.period.demand", "dashboard.period.supply", "dashboard.period.settle"], WorkspacePattern.Operation, DataSourceMode.Mixed, Density.Mixed, ToolbarMotif.Decision, FooterMotif.Mixed, ResponsiveStrategy.BoundedSurface, ["OPERATION", "SELECTOR-DECISION", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-FOOTER", "DIALOG-EDITOR", "CONTENT-STATE", "TRANSIENT"], ["loading", "empty", "filtered-empty", "error", "denied", "disabled"], "Legacy period query values resolve to the unified period workspace.");
        Add(profiles, ["dashboard.order-create.new", "dashboard.order-create.additional", "dashboard.order-create.copy-previous", "dashboard.order-create.edit", "dashboard.order-create.recreate"], WorkspacePattern.SplitEditor, DataSourceMode.Mixed, Density.RichTwoLine, ToolbarMotif.Workflow, FooterMotif.ActionBar, ResponsiveStrategy.StackPanes, ["SPLIT-EDITOR", "OPERATION", "FILTER-TOOLBAR", "SELECTOR-DECISION", "DATA-FRAME", "DATA-FOOTER", "CELL-VALUE", "CONTENT-STATE", "TRANSIENT"], ["loading", "empty", "filtered-empty", "error", "disabled", "success"]);

        Add(profiles, ["library.classes"], WorkspacePattern.SplitEditor, DataSourceMode.ServerPaging, Density.Compact, ToolbarMotif.FilterWithColumnPicker, FooterMotif.Paged, ResponsiveStrategy.StackPanes, ["SPLIT-EDITOR", "COLLECTION-HEADER", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-COLUMN", "DATA-FOOTER", "DIALOG-EDITOR", "CONTENT-STATE"], ["loading", "empty", "filtered-empty", "error", "disabled"]);
        Add(profiles, ["library.categories", "library.items", "library.suppliers", "library.departments", "library.pricing.price-lists", "library.pricing.prices", "library.pricing.prices.selected-list"], WorkspacePattern.Collection, DataSourceMode.ServerPaging, Density.Compact, ToolbarMotif.FilterWithColumnPicker, FooterMotif.Paged, ResponsiveStrategy.BoundedSurface, ["COLLECTION", "COLLECTION-HEADER", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-COLUMN", "DATA-FOOTER", "DIALOG-EDITOR", "CONTENT-STATE"], ["loading", "empty", "filtered-empty", "error", "denied", "disabled"]);

        Add(profiles, ["permission.user"], WorkspacePattern.Collection, DataSourceMode.ServerPaging, Density.Compact, ToolbarMotif.FilterWithColumnPicker, FooterMotif.Paged, ResponsiveStrategy.BoundedSurface, ["COLLECTION", "COLLECTION-HEADER", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-COLUMN", "DATA-FOOTER", "DIALOG-EDITOR", "CONTENT-STATE"], ["loading", "empty", "filtered-empty", "error", "denied", "disabled"]);
        Add(profiles, ["permission.component"], WorkspacePattern.ListDetail, DataSourceMode.ServerPaging, Density.Compact, ToolbarMotif.FilterWithColumnPicker, FooterMotif.Mixed, ResponsiveStrategy.StackPanes, ["LIST-DETAIL", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-COLUMN", "DATA-FOOTER", "DIALOG-EDITOR", "CONTENT-STATE"], ["loading", "empty", "filtered-empty", "error", "denied", "disabled"]);
        Add(profiles, ["permission.security-audit"], WorkspacePattern.Collection, DataSourceMode.ServerPaging, Density.Compact, ToolbarMotif.FilterWithColumnPicker, FooterMotif.Paged, ResponsiveStrategy.BoundedSurface, ["COLLECTION", "FILTER-TOOLBAR", "DATA-FRAME", "DATA-COLUMN", "DATA-FOOTER", "CONTENT-STATE"], ["loading", "empty", "filtered-empty", "error", "denied"]);
        Add(profiles, ["report"], WorkspacePattern.Analytics, DataSourceMode.Static, Density.Compact, ToolbarMotif.Report, FooterMotif.Summary, ResponsiveStrategy.BoundedSurface, ["ANALYTICS", "METRIC-CARD", "DATA-FRAME", "CONTENT-STATE", "FEEDBACK"], ["loading", "empty", "filtered-empty", "error", "disabled"]);

        return profiles;
    }

    private static void Add(
        IDictionary<string, Profile> profiles,
        IEnumerable<string> keys,
        WorkspacePattern workspace,
        DataSourceMode dataSource,
        Density density,
        ToolbarMotif toolbar,
        FooterMotif footer,
        ResponsiveStrategy responsive,
        string[] motifs,
        string[] states,
        string? notes = null)
    {
        foreach (var key in keys)
        {
            profiles.Add(key, new Profile(key, workspace, dataSource, density, toolbar, footer, responsive, motifs, states, notes));
        }
    }
}
