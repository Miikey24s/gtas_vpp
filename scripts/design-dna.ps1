[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('help', 'run', 'test', 'reset', 'data-path')]
    [string]$Command = 'help',

    [string]$DataRoot,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$StudioRoot = Join-Path $RepoRoot 'design_dna_studio'
$Solution = Join-Path $StudioRoot 'DesignDnaStudio.slnx'
$WebProject = Join-Path $StudioRoot 'src/DesignDnaStudio.Web/DesignDnaStudio.Web.csproj'
$TestProject = Join-Path $StudioRoot 'tests/DesignDnaStudio.Tests/DesignDnaStudio.Tests.csproj'
$DataMarkerName = '.design-dna-studio-local'

function Resolve-StudioDataRoot {
    if ([string]::IsNullOrWhiteSpace($DataRoot)) {
        return [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'GTAS/DesignDNAStudio'))
    }

    return [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($DataRoot))
}

function Assert-LocalDataRoot([string]$Root) {
    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ($resolvedRoot.StartsWith('\\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing a UNC/network data directory: $resolvedRoot"
    }

    $repoRoot = [IO.Path]::GetFullPath($RepoRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $repoPrefix = $repoRoot + [IO.Path]::DirectorySeparatorChar
    if ($resolvedRoot -eq $repoRoot -or $resolvedRoot.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing a repository-local data directory: $resolvedRoot"
    }
}

function Assert-StudioFilesExist {
    foreach ($path in @($Solution, $WebProject, $TestProject)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required DesignDNA Studio file was not found: $path"
        }
    }
}

function Initialize-DataRoot([string]$Root) {
    $marker = Join-Path $Root $DataMarkerName
    $defaultRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'GTAS/DesignDNAStudio')).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)

    if (Test-Path -LiteralPath $Root -PathType Container) {
        $hasMarker = Test-Path -LiteralPath $marker -PathType Leaf
        $hasExistingContent = @(Get-ChildItem -LiteralPath $Root -Force).Count -gt 0
        if ($resolvedRoot -ne $defaultRoot -and -not $hasMarker -and $hasExistingContent) {
            throw "Refusing to use a non-empty, unmarked custom data directory: $resolvedRoot"
        }
    }
    else {
        New-Item -ItemType Directory -Path $Root -Force | Out-Null
    }

    if (-not (Test-Path -LiteralPath $marker -PathType Leaf)) {
        New-Item -ItemType File -Path $marker -Force | Out-Null
    }
}

function Assert-SafeResetRoot([string]$Root) {
    $resolvedRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $driveRoot = [IO.Path]::GetPathRoot($resolvedRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ($resolvedRoot -eq $driveRoot) {
        throw 'Refusing to reset a drive root.'
    }

    $defaultRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'GTAS/DesignDNAStudio')).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ($resolvedRoot -ne $defaultRoot) {
        $marker = Join-Path $resolvedRoot $DataMarkerName
        if (-not (Test-Path -LiteralPath $marker -PathType Leaf)) {
            throw "Refusing to reset an unmarked custom directory. Run the Studio once through this script first: $resolvedRoot"
        }
    }
}

function Invoke-WithDataRoot([string]$Root, [scriptblock]$Action) {
    $previous = [Environment]::GetEnvironmentVariable('Studio__DataRoot', 'Process')
    try {
        [Environment]::SetEnvironmentVariable('Studio__DataRoot', $Root, 'Process')
        & $Action
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        [Environment]::SetEnvironmentVariable('Studio__DataRoot', $previous, 'Process')
    }
}

function Show-Help {
    @'
DesignDNA Studio local manager

Usage:
  .\scripts\design-dna.cmd <command> [-DataRoot <path>] [-Force]

Commands:
  help       Show this catalog.
  run        Start the Blazor app with dotnet watch and Hot Reload.
  test       Build the Studio solution and run its tests in Release mode.
  data-path  Print the local data directory used by run/reset.
  reset      Delete only the Studio SQLite database files.

Examples:
  .\scripts\design-dna.cmd run
  .\scripts\design-dna.cmd test
  .\scripts\design-dna.cmd data-path
  .\scripts\design-dna.cmd reset
  .\scripts\design-dna.cmd reset -Force

Default data directory:
  %LOCALAPPDATA%\GTAS\DesignDNAStudio

reset asks you to type RESET. -Force is intended for deliberate automation.
A custom -DataRoot must stay on a local drive outside the repository. It is marked on first run and reset refuses unmarked paths.
'@
}

Assert-StudioFilesExist
$resolvedDataRoot = Resolve-StudioDataRoot
Assert-LocalDataRoot $resolvedDataRoot

switch ($Command) {
    'help' {
        Show-Help
    }
    'data-path' {
        Write-Output $resolvedDataRoot
    }
    'run' {
        Initialize-DataRoot $resolvedDataRoot
        Write-Host "DesignDNA Studio data: $resolvedDataRoot"
        Invoke-WithDataRoot $resolvedDataRoot {
            & dotnet watch --project $WebProject --launch-profile http --non-interactive
        }
    }
    'test' {
        & dotnet build $Solution -c Release
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE."
        }

        & dotnet test $TestProject -c Release --no-build
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed with exit code $LASTEXITCODE."
        }

        $previousSkipInitialization = [Environment]::GetEnvironmentVariable('Studio__SkipStartupInitialization', 'Process')
        Push-Location $RepoRoot
        try {
            & dotnet tool restore --tool-manifest (Join-Path $RepoRoot 'dotnet-tools.json')
            if ($LASTEXITCODE -ne 0) {
                throw "Local .NET tool restore failed with exit code $LASTEXITCODE."
            }

            [Environment]::SetEnvironmentVariable('Studio__SkipStartupInitialization', 'true', 'Process')
            & dotnet ef migrations has-pending-model-changes `
                --project $WebProject `
                --startup-project $WebProject `
                --configuration Release `
                --no-build
            if ($LASTEXITCODE -ne 0) {
                throw "EF migration drift check failed with exit code $LASTEXITCODE."
            }
        }
        finally {
            Pop-Location
            [Environment]::SetEnvironmentVariable('Studio__SkipStartupInitialization', $previousSkipInitialization, 'Process')
        }
    }
    'reset' {
        Assert-SafeResetRoot $resolvedDataRoot

        $targets = @(
            @(
                (Join-Path $resolvedDataRoot 'design-dna.db'),
                (Join-Path $resolvedDataRoot 'design-dna.db-journal'),
                (Join-Path $resolvedDataRoot 'design-dna.db-shm'),
                (Join-Path $resolvedDataRoot 'design-dna.db-wal')
            ) | Where-Object { Test-Path -LiteralPath $_ }
        )

        if ($targets.Count -eq 0) {
            Write-Host "Nothing to reset at $resolvedDataRoot"
            break
        }

        Write-Warning "This will permanently delete DesignDNA Studio local data at: $resolvedDataRoot"
        if (-not $Force) {
            $confirmation = Read-Host 'Type RESET to continue'
            if ($confirmation -cne 'RESET') {
                Write-Host 'Reset cancelled.'
                break
            }
        }

        try {
            foreach ($target in $targets) {
                Remove-Item -LiteralPath $target -Recurse -Force
            }
        }
        catch {
            throw "Reset failed. Stop DesignDNA Studio, then try again. $($_.Exception.Message)"
        }

        Write-Host 'DesignDNA Studio local data was reset. The next run will migrate and seed a fresh database.'
    }
}
