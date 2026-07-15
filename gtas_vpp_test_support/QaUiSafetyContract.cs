namespace gtas_vpp_test_support;

public static class QaUiSafetyContract
{
    public const string IsolatedRunEnvironmentVariable = "GTAS_E2E_ISOLATED";
    public const string MutationOptInEnvironmentVariable = "GTAS_E2E_MUTATION_OPT_IN";
    public const string RequiredMutationOptIn = "I_UNDERSTAND_THIS_MUTATES_QA_DATA";

    public static void EnsureRunAllowed(
        bool requiresAuthenticatedFixture,
        bool mutatesServerState,
        string? isolatedRunOptIn,
        string? mutationOptIn)
    {
        if (requiresAuthenticatedFixture
            && !string.Equals(isolatedRunOptIn, "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Authenticated UI tests require {IsolatedRunEnvironmentVariable}=1 and a harness-owned QA fixture.");
        }

        if (mutatesServerState
            && !string.Equals(mutationOptIn, RequiredMutationOptIn, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Mutating UI tests require the exact {MutationOptInEnvironmentVariable} acknowledgement.");
        }
    }

    public static void EnsureLoopbackUrl(Uri uri, string label)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri
            || !uri.IsLoopback
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"{label} must be an absolute HTTP(S) loopback URL.");
        }
    }
}
