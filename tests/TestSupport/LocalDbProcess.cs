using System.Diagnostics;

namespace gtas_vpp_test_support;

internal sealed class LocalDbProcess
{
    private readonly string _executablePath;

    private LocalDbProcess(string executablePath)
    {
        _executablePath = executablePath;
    }

    public static LocalDbProcess Create()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "QA-001 local fixture requires Windows SQL Server LocalDB. Use the documented CI SQL alternative elsewhere.");
        }

        var executable = FindExecutable();
        if (executable is null)
        {
            throw new FileNotFoundException(
                "SqlLocalDB.exe was not found. Install SQL Server Express LocalDB or use the documented CI SQL alternative.");
        }

        return new LocalDbProcess(executable);
    }

    public async Task<bool> InstanceExistsAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["info"], cancellationToken);
        return result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(name => string.Equals(name, instanceName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task CreateAndStartAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        if (await InstanceExistsAsync(instanceName, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Refusing to reuse existing LocalDB instance '{instanceName}'.");
        }

        await RunAsync(["create", instanceName, "-s"], cancellationToken);
    }

    public async Task StopAndDeleteAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        if (!await InstanceExistsAsync(instanceName, cancellationToken))
        {
            return;
        }

        await RunAsync(["stop", instanceName, "-k"], cancellationToken, throwOnFailure: false);
        await RunAsync(["delete", instanceName], cancellationToken);
    }

    private async Task<ProcessResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        bool throwOnFailure = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("SqlLocalDB.exe could not be started.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var result = new ProcessResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);

        if (throwOnFailure && result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"SqlLocalDB.exe failed with exit code {result.ExitCode}: {result.StandardError.Trim()}");
        }

        return result;
    }

    private static string? FindExecutable()
    {
        var configured = Environment.GetEnvironmentVariable("GTAS_SQLLOCALDB_EXECUTABLE");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var fullPath = Path.GetFullPath(configured);
            if (!string.Equals(Path.GetFileName(fullPath), "SqlLocalDB.exe", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(fullPath))
            {
                throw new InvalidOperationException(
                    "GTAS_SQLLOCALDB_EXECUTABLE must point to an existing SqlLocalDB.exe.");
            }

            return fullPath;
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        foreach (var version in new[] { "170", "160", "150", "140", "130" })
        {
            var candidate = Path.Combine(
                programFiles,
                "Microsoft SQL Server",
                version,
                "Tools",
                "Binn",
                "SqlLocalDB.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var pathEntries = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return pathEntries
            .Select(entry => Path.Combine(entry, "SqlLocalDB.exe"))
            .FirstOrDefault(File.Exists);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
