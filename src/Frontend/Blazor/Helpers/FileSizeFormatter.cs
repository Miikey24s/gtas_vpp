using System.Globalization;

namespace gtas_vpp_fe.Helpers;

public static class FileSizeFormatter
{
    public static string Format(long bytes, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (bytes < 1024)
        {
            return $"{bytes.ToString("N0", culture)} B";
        }

        var kilobytes = bytes / 1024d;
        if (kilobytes < 1024)
        {
            return $"{kilobytes.ToString(kilobytes < 10 ? "N1" : "N0", culture)} KB";
        }

        var megabytes = kilobytes / 1024d;
        return $"{megabytes.ToString(megabytes < 10 ? "N1" : "N0", culture)} MB";
    }
}
