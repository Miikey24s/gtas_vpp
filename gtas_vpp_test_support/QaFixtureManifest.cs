using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace gtas_vpp_test_support;

internal sealed record QaFixtureManifest(
    string Purpose,
    string FixtureVersion,
    string RunId,
    string InstanceName,
    string DatabaseName,
    int OwnerProcessId,
    DateTime OwnerProcessStartUtc,
    DateTime CreatedUtc)
{
    public static QaFixtureManifest Create(QaFixtureOptions options)
    {
        using var current = Process.GetCurrentProcess();
        return new QaFixtureManifest(
            gtas_vpp_be.Service.Helpers.QaFixtureIdentityContract.Purpose,
            gtas_vpp_be.Service.Helpers.QaFixtureIdentityContract.FixtureVersion,
            options.RunId,
            options.InstanceName,
            options.DatabaseName,
            current.Id,
            current.StartTime.ToUniversalTime(),
            DateTime.UtcNow);
    }

    public async Task WriteAsync(string path, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var created = false;
        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);
            created = true;
            await JsonSerializer.SerializeAsync(
                stream,
                this,
                new JsonSerializerOptions { WriteIndented = true },
                cancellationToken);
        }
        catch
        {
            if (created && File.Exists(path))
            {
                File.Delete(path);
            }

            throw;
        }
    }

    public static async Task<QaFixtureManifest> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<QaFixtureManifest>(json)
            ?? throw new InvalidDataException("QA fixture manifest is empty or invalid.");
    }

    public bool HasLiveOwner()
    {
        try
        {
            using var process = Process.GetProcessById(OwnerProcessId);
            return Math.Abs(
                (process.StartTime.ToUniversalTime() - OwnerProcessStartUtc).TotalSeconds) < 2;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            // If ownership cannot be inspected, preserve the fixture rather than deleting it.
            return true;
        }
    }
}
