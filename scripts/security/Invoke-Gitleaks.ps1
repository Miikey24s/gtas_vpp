[CmdletBinding()]
param(
    [ValidateSet("Current", "History")]
    [string]$Mode = "Current",

    [switch]$PostRevocation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($Mode -eq "History" -and -not $PostRevocation) {
    throw "History scanning is intentionally gated. Revoke affected credentials first, then rerun with -PostRevocation."
}

$gitleaksVersion = "8.30.1"
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$tempRoot = [IO.Path]::GetFullPath((Join-Path $tempBase ("gtas-gitleaks-" + [Guid]::NewGuid().ToString("N"))))
$tempPrefix = $tempBase.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar

if (-not $tempRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -or
    -not ([IO.Path]::GetFileName($tempRoot)).StartsWith("gtas-gitleaks-", [StringComparison]::Ordinal)) {
    throw "Refusing to use an unexpected temporary directory."
}

$isWindows = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [Runtime.InteropServices.OSPlatform]::Windows)
$isLinux = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [Runtime.InteropServices.OSPlatform]::Linux)
$architecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()

if ($architecture -ne "X64") {
    throw "The pinned scanner supports x64 only; detected $architecture."
}

if ($isWindows) {
    $archiveName = "gitleaks_${gitleaksVersion}_windows_x64.zip"
    $archiveSha256 = "d29144deff3a68aa93ced33dddf84b7fdc26070add4aa0f4513094c8332afc4e"
    $binaryName = "gitleaks.exe"
}
elseif ($isLinux) {
    $archiveName = "gitleaks_${gitleaksVersion}_linux_x64.tar.gz"
    $archiveSha256 = "551f6fc83ea457d62a0d98237cbad105af8d557003051f41f3e7ca7b3f2470eb"
    $binaryName = "gitleaks"
}
else {
    throw "The pinned scanner currently supports Windows x64 and Linux x64 only."
}

$downloadUrl = "https://github.com/gitleaks/gitleaks/releases/download/v${gitleaksVersion}/${archiveName}"
$archivePath = Join-Path $tempRoot $archiveName
$toolRoot = Join-Path $tempRoot "tool"
$scanRoot = Join-Path $tempRoot "tracked-tree"

try {
    New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null

    if ($isWindows) {
        [Net.ServicePointManager]::SecurityProtocol =
            [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    }

    Write-Host "Downloading pinned Gitleaks v$gitleaksVersion from the official release."
    Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath -UseBasicParsing

    $actualSha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualSha256 -ne $archiveSha256) {
        throw "Gitleaks archive checksum mismatch."
    }

    if ($isWindows) {
        Expand-Archive -LiteralPath $archivePath -DestinationPath $toolRoot -Force
    }
    else {
        & tar -xzf $archivePath -C $toolRoot
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to extract the verified Gitleaks archive."
        }
    }

    $gitleaks = Join-Path $toolRoot $binaryName
    if (-not (Test-Path -LiteralPath $gitleaks -PathType Leaf)) {
        throw "The verified archive did not contain the expected Gitleaks binary."
    }

    $reportedVersion = ((& $gitleaks version 2>&1) | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $reportedVersion -ne $gitleaksVersion) {
        throw "Unexpected Gitleaks binary version."
    }

    $commonArguments = @(
        "--redact=100",
        "--no-banner",
        "--no-color",
        "--ignore-gitleaks-allow",
        "--max-archive-depth=1",
        "--exit-code=1"
    )

    if ($Mode -eq "Current") {
        New-Item -ItemType Directory -Path $scanRoot -Force | Out-Null

        $candidateFiles = @(& git -c core.quotepath=false -C $repoRoot `
                ls-files --cached --others --exclude-standard)
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to enumerate repository files for the current-tree scan."
        }

        $repoPrefix = $repoRoot.TrimEnd(
            [IO.Path]::DirectorySeparatorChar,
            [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $scanPrefix = [IO.Path]::GetFullPath($scanRoot).TrimEnd(
            [IO.Path]::DirectorySeparatorChar,
            [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar

        foreach ($relativePath in $candidateFiles) {
            if ([String]::IsNullOrWhiteSpace($relativePath)) {
                continue
            }

            $platformPath = $relativePath.Replace('/', [IO.Path]::DirectorySeparatorChar)
            $sourcePath = [IO.Path]::GetFullPath((Join-Path $repoRoot $platformPath))
            $destinationPath = [IO.Path]::GetFullPath((Join-Path $scanRoot $platformPath))

            if (-not $sourcePath.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase) -or
                -not $destinationPath.StartsWith($scanPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Refusing to scan a repository path outside the expected roots."
            }

            if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
                continue
            }

            $destinationDirectory = Split-Path -Parent $destinationPath
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
            Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
        }

        Write-Host "Scanning tracked and not-ignored candidate files with fully redacted output."
        $scanArguments = @("dir", $scanRoot) + $commonArguments
    }
    else {
        Write-Host "Running the explicit post-revocation all-ref history gate with fully redacted output."
        $scanArguments = @("git", $repoRoot, "--log-opts=--all") + $commonArguments
    }

    & $gitleaks @scanArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Gitleaks detected a candidate secret or could not complete the scan."
    }
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        $resolvedTempRoot = [IO.Path]::GetFullPath($tempRoot)
        if ($resolvedTempRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
            ([IO.Path]::GetFileName($resolvedTempRoot)).StartsWith("gtas-gitleaks-", [StringComparison]::Ordinal)) {
            Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force
        }
    }
}
