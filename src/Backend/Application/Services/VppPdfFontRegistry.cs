using System.Reflection;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace gtas_vpp_be.Service.Services;

internal static class VppPdfFontRegistry
{
    public const string FontFamily = "Poppins";

    private static readonly object InitLock = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (InitLock)
        {
            if (_initialized)
            {
                return;
            }

            QuestPDF.Settings.License = LicenseType.Community;
            RegisterFont("gtas_vpp_be.Service.Fonts.Poppins-SemiBold.ttf");
            RegisterFont("gtas_vpp_be.Service.Fonts.Poppins-Bold.ttf");
            _initialized = true;
        }
    }

    private static void RegisterFont(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded font resource '{resourceName}' was not found.");
        FontManager.RegisterFont(stream);
    }
}
