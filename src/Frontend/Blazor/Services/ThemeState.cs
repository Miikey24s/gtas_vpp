namespace gtas_vpp_fe.Services;

public sealed class ThemeState
{
    public string CurrentTheme { get; private set; } = "material3";

    public bool IsDark => CurrentTheme.Contains("dark", StringComparison.OrdinalIgnoreCase);

    public event Action? Changed;

    public void SetTheme(string? theme)
    {
        var nextTheme = string.IsNullOrWhiteSpace(theme) ? "material3" : theme;
        if (string.Equals(CurrentTheme, nextTheme, StringComparison.Ordinal))
        {
            return;
        }

        CurrentTheme = nextTheme;
        Changed?.Invoke();
    }
}
