using System.Reflection;
using System.Xml.Linq;
using gtas_vpp_shared.DTOs.Req;
using Xunit;

namespace gtas_vpp_be.Tests.Architecture;

public sealed class ProductionDependencyRulesTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static readonly IReadOnlyDictionary<string, string[]> ExpectedProjectReferences =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["src/Shared/gtas_vpp_shared.csproj"] = [],
            ["src/Backend/Domain/gtas_vpp_be.Model.csproj"] = [],
            ["src/Backend/Migrations/gtas_vpp_be.Migrations.csproj"] =
            [
                "src/backend/domain/gtas_vpp_be.model.csproj"
            ],
            ["src/Backend/Application/gtas_vpp_be.Service.csproj"] =
            [
                "src/backend/domain/gtas_vpp_be.model.csproj",
                "src/Shared/gtas_vpp_shared.csproj"
            ],
            ["src/Backend/Api/gtas_vpp_be.csproj"] =
            [
                "src/backend/migrations/gtas_vpp_be.migrations.csproj",
                "src/backend/domain/gtas_vpp_be.model.csproj",
                "src/backend/application/gtas_vpp_be.service.csproj",
                "src/Shared/gtas_vpp_shared.csproj"
            ],
            ["src/Frontend/Blazor/gtas_vpp_fe.csproj"] =
            [
                "src/Shared/gtas_vpp_shared.csproj"
            ],
            ["src/Hosting/AppHost/MyAspire.AppHost.csproj"] =
            [
                "src/Backend/Api/gtas_vpp_be.csproj",
                "src/Frontend/Blazor/gtas_vpp_fe.csproj"
            ],
            ["src/Hosting/ServiceDefaults/MyAspire.ServiceDefaults.csproj"] = []
        };

    [Fact]
    public void CoreProjects_FollowTheApprovedReferenceGraph()
    {
        foreach (var (project, expectedReferences) in ExpectedProjectReferences)
        {
            var projectPath = Path.Combine(RepositoryRoot, ToPlatformPath(project));
            var actualReferences = ReadProjectReferences(projectPath);

            Assert.Equal(
                expectedReferences.Order(StringComparer.OrdinalIgnoreCase),
                actualReferences.Order(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CanonicalSolution_ProductionProjectsMatchTheApprovedCatalog()
    {
        var expectedProjects = ExpectedProjectReferences.Keys
            .Select(NormalizeRepositoryPath)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var solution = XDocument.Load(Path.Combine(RepositoryRoot, "gtas_vpp.slnx"));
        var actualProjects = solution
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(path => path?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == true)
            .Select(path => NormalizeRepositoryPath(path!))
            .Where(IsProductionProject)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expectedProjects, actualProjects, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("gtas_vpp_be")]
    [InlineData("gtas_vpp_fe")]
    [InlineData("gtas_vpp_fe_react")]
    public void LegacyRootDirectories_AreNotReintroduced(string relativePath)
    {
        Assert.False(Directory.Exists(Path.Combine(RepositoryRoot, relativePath)));
    }

    [Fact]
    public void SharedBrowserTooling_IsNestedUnderScripts()
    {
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "scripts", "browser", "package.json")));
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "scripts", "browser", "package-lock.json")));
    }

    [Fact]
    public void SharedContracts_HaveNoPackageDependencies()
    {
        var project = Path.Combine(
            RepositoryRoot,
            ToPlatformPath("src/Shared/gtas_vpp_shared.csproj"));

        var packages = ReadPackageReferences(project);

        Assert.Empty(packages);
    }

    [Fact]
    public void Frontend_HasNoPersistenceOrObjectMappingPackage()
    {
        var project = Path.Combine(
            RepositoryRoot,
            ToPlatformPath("src/Frontend/Blazor/gtas_vpp_fe.csproj"));

        var packages = ReadPackageReferences(project);

        Assert.DoesNotContain(
            packages,
            package => package.Equals("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("Microsoft.EntityFrameworkCore.", StringComparison.OrdinalIgnoreCase)
                || package.Equals("Mapster", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("Mapster.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SharedAssembly_DoesNotReferencePersistenceMappingOrUiFrameworks()
    {
        Assembly sharedAssembly = typeof(AuthenticationLoginRequest).Assembly;
        var referencedAssemblies = sharedAssembly
            .GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(referencedAssemblies, IsForbiddenSharedAssembly);
    }

    [Fact]
    public void SharedAssembly_DoesNotExposeBackendOrFrontendOnlyTypes()
    {
        Assembly sharedAssembly = typeof(AuthenticationLoginRequest).Assembly;
        var forbiddenTypes = sharedAssembly
            .GetExportedTypes()
            .Where(type =>
                type.Name is "v_Users"
                    or "v_WFXCompany"
                    or "GlobalClass"
                    or "GlobalStorageModel"
                    or "DropdownModel"
                    or "GridColumnPropertyAttribute"
                || type.Namespace?.StartsWith("gtas_vpp_shared.UI", StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(forbiddenTypes);
    }

    [Fact]
    public void SolutionManifests_ReferenceExistingProjects()
    {
        var manifests = Directory
            .EnumerateFiles(RepositoryRoot, "*.slnx", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        foreach (var manifest in manifests)
        {
            var manifestDirectory = Path.GetDirectoryName(manifest)
                ?? throw new InvalidOperationException($"Cannot resolve solution directory: {manifest}");
            var projectPaths = XDocument.Load(manifest)
                .Descendants()
                .Where(element => element.Name.LocalName == "Project")
                .Select(element => element.Attribute("Path")?.Value)
                .Where(path => !string.IsNullOrWhiteSpace(path));

            foreach (var projectPath in projectPaths)
            {
                var absolutePath = Path.GetFullPath(
                    Path.Combine(manifestDirectory, ToPlatformPath(projectPath!)));
                Assert.True(
                    File.Exists(absolutePath),
                    $"{Path.GetRelativePath(RepositoryRoot, manifest)} references missing project {projectPath}.");
            }
        }
    }

    private static string[] ReadProjectReferences(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Cannot resolve project directory: {projectPath}");

        return LoadProject(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, ToPlatformPath(include!))))
            .Select(path => NormalizeRepositoryPath(Path.GetRelativePath(RepositoryRoot, path)))
            .ToArray();
    }

    private static string[] ReadPackageReferences(string projectPath) =>
        LoadProject(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!)
            .ToArray();

    private static XDocument LoadProject(string projectPath)
    {
        Assert.True(File.Exists(projectPath), $"Project not found: {projectPath}");
        return XDocument.Load(projectPath);
    }

    private static bool IsPersistenceOrUiPackage(string package) =>
        package.Equals("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
        || package.StartsWith("Microsoft.EntityFrameworkCore.", StringComparison.OrdinalIgnoreCase)
        || package.Equals("Mapster", StringComparison.OrdinalIgnoreCase)
        || package.StartsWith("Mapster.", StringComparison.OrdinalIgnoreCase)
        || package.Equals("Radzen.Blazor", StringComparison.OrdinalIgnoreCase);

    private static bool IsForbiddenSharedAssembly(string name) =>
        IsPersistenceOrUiPackage(name)
        || name.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("gtas_vpp_be", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("gtas_vpp_fe", StringComparison.OrdinalIgnoreCase);

    private static bool IsProductionProject(string normalizedPath)
    {
        var fileName = Path.GetFileName(normalizedPath);

        return !fileName.EndsWith(".Tests.csproj", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".IntegrationTests.csproj", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".UITests.csproj", StringComparison.OrdinalIgnoreCase)
            && !fileName.Equals("gtas_vpp_test_support.csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory);
             current is not null;
             current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "gtas_vpp.slnx")))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Cannot find gtas_vpp.slnx above {AppContext.BaseDirectory}.");
    }

    private static string ToPlatformPath(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

    private static string NormalizeRepositoryPath(string path) =>
        path.Replace('\\', '/').ToLowerInvariant();
}
