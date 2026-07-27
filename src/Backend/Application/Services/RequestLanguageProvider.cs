using System.Globalization;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_be.Service.Services;

public interface IRequestLanguageProvider
{
    string LanguageCode { get; }
}

public sealed class RequestLanguageProvider : IRequestLanguageProvider
{
    public string LanguageCode => BusinessLanguages.Normalize(
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
}
