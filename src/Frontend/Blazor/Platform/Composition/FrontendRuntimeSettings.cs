using gtas_vpp_fe.Helpers;

namespace gtas_vpp_fe.Platform.Composition;

public sealed record FrontendRuntimeSettings(
    Uri ApiBaseUri,
    TimeSpan ApiConnectTimeout,
    TimeSpan ApiRequestTimeout,
    string DataProtectionKeysPath,
    CookieSecurePolicy AuthCookieSecurePolicy,
    bool BypassApiServerCertificateValidation,
    bool EnableMultiSupplierSettlement)
{
    public static FrontendRuntimeSettings Resolve(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var apiBaseUri = ApiBaseUrlResolver.Resolve(configuration["ApiSettings:BaseUrl"]);
        var dataProtectionKeysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
        {
            dataProtectionKeysPath = environment.IsProduction()
                ? "/app/keys"
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GTAS_VPP",
                    "DataProtection-Keys");
        }

        return new FrontendRuntimeSettings(
            apiBaseUri,
            TimeSpan.FromSeconds(configuration.GetValue("ApiSettings:ConnectTimeoutSeconds", 5)),
            TimeSpan.FromSeconds(configuration.GetValue("ApiSettings:RequestTimeoutSeconds", 30)),
            dataProtectionKeysPath,
            environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always,
            ApiBaseUrlResolver.ShouldBypassServerCertificateValidation(
                environment.IsDevelopment(),
                apiBaseUri),
            configuration.GetValue("Features:Settlement:MultiSupplierEnabled", false));
    }
}
