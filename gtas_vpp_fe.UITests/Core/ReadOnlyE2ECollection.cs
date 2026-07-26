namespace gtas_vpp_fe.UITests.Core;

/// <summary>
/// xUnit collection for browser tests that never mutate business data: read-only
/// authenticated tests plus the anonymous account-shell tests. Members share one
/// <see cref="SharedE2EAppFixture"/> (app + QA database). A class may only join
/// after its "no business POST/PUT/DELETE" audit line is ticked in review
/// (REFACTOR-001-R0-DESIGN section 2.2); mutating tests must implement
/// <see cref="IMutatingUiTest"/> and stay out of this collection.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ReadOnlyE2ECollection : ICollectionFixture<SharedE2EAppFixture>
{
    public const string Name = "e2e-readonly";
}
