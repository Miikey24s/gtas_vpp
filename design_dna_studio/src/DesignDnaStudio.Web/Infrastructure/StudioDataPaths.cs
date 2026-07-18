namespace DesignDnaStudio.Web.Infrastructure;

public sealed record StudioDataPaths(string Root, string DatabasePath)
{
    public static StudioDataPaths Resolve(IConfiguration configuration, string? contentRootPath = null)
    {
        var configuredRoot = configuration["Studio:DataRoot"];
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GTAS",
                "DesignDNAStudio")
            : Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredRoot));

        EnsureLocalDataRoot(root, contentRootPath);
        Directory.CreateDirectory(root);

        return new StudioDataPaths(root, Path.Combine(root, "design-dna.db"));
    }

    private static void EnsureLocalDataRoot(string root, string? contentRootPath)
    {
        if (root.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DesignDNA Studio cannot use a UNC/network data directory.");
        }

        if (OperatingSystem.IsWindows())
        {
            var driveRoot = Path.GetPathRoot(root);
            if (!string.IsNullOrWhiteSpace(driveRoot) && new DriveInfo(driveRoot).DriveType == DriveType.Network)
            {
                throw new InvalidOperationException("DesignDNA Studio cannot use a mapped network data directory.");
            }
        }

        var repositoryRoot = FindRepositoryRoot(contentRootPath);
        if (repositoryRoot is not null && IsWithin(root, repositoryRoot))
        {
            throw new InvalidOperationException(
                "DesignDNA Studio data must stay outside the repository so local research data cannot be committed accidentally.");
        }
    }

    private static string? FindRepositoryRoot(string? contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(contentRootPath))
        {
            return null;
        }

        for (var directory = new DirectoryInfo(Path.GetFullPath(contentRootPath)); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
        }

        return null;
    }

    private static bool IsWithin(string candidate, string parent)
    {
        var normalizedCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar);
        var normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar);
        var parentPrefix = normalizedParent + Path.DirectorySeparatorChar;
        return string.Equals(normalizedCandidate, normalizedParent, StringComparison.OrdinalIgnoreCase) ||
               normalizedCandidate.StartsWith(parentPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
