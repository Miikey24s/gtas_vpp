param(
    [switch]$Apply,
    [switch]$IncludeThesisIntermediates
)

$ErrorActionPreference = "Stop"

$workspace = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path.TrimEnd("\")
$git = (Get-Command git -ErrorAction Stop).Source
$candidateBytes = [int64]0
$candidateTargets = 0
$failures = [System.Collections.Generic.List[string]]::new()

function Remove-WorkspaceTarget {
    param([Parameter(Mandatory)][string]$Target)

    if (-not (Test-Path -LiteralPath $Target)) {
        return
    }

    $resolved = (Resolve-Path -LiteralPath $Target).Path
    if ($resolved -eq $workspace -or
        -not $resolved.StartsWith($workspace + "\", [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing unsafe cleanup target: $resolved"
    }

    $item = Get-Item -LiteralPath $resolved -Force
    $bytes = if ($item.PSIsContainer) {
        [int64]((Get-ChildItem -LiteralPath $resolved -Recurse -File -Force -ErrorAction SilentlyContinue |
            Measure-Object Length -Sum).Sum)
    }
    else {
        [int64]$item.Length
    }

    $script:candidateBytes += $bytes
    $script:candidateTargets++

    if (-not $Apply) {
        Write-Output ("WOULD_REMOVE`t{0:N2} MB`t{1}" -f ($bytes / 1MB), $resolved)
        return
    }

    try {
        Remove-Item -LiteralPath $resolved -Recurse -Force -ErrorAction Stop
        Write-Output ("REMOVED`t{0:N2} MB`t{1}" -f ($bytes / 1MB), $resolved)
    }
    catch {
        $script:failures.Add("$resolved :: $($_.Exception.Message)")
    }
}

$fixedTargets = @(
    ".vs",
    ".playwright-mcp",
    "gtas_vpp_fe_react\node_modules",
    "gtas_vpp_fe_react\dist",
    "gtas_vpp_fe_react\test-results",
    "LVTN\render",
    ".tmp",
    "src\Backend\Api\logs"
)

if ($IncludeThesisIntermediates) {
    $fixedTargets += "LVTN\_render_tmp"
}

foreach ($relativePath in $fixedTargets) {
    Remove-WorkspaceTarget -Target (Join-Path $workspace $relativePath)
}

$generatedDirectories = Get-ChildItem -LiteralPath $workspace -Recurse -Directory -Force -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -in @("bin", "obj", "__pycache__", "TestResults", "test-results") -and
        $_.FullName -notlike "*\.git\*" -and
        $_.FullName -notlike "*\node_modules\*"
    } |
    Sort-Object { $_.FullName.Length } -Descending

foreach ($directory in $generatedDirectories) {
    Remove-WorkspaceTarget -Target $directory.FullName
}

if ($IncludeThesisIntermediates) {
    $checkpointDirectory = Join-Path $workspace "LVTN\checkpoints"
    $checkpointFiles = Get-ChildItem -LiteralPath $checkpointDirectory -File -Force -ErrorAction SilentlyContinue

    foreach ($file in $checkpointFiles) {
        & $git check-ignore -q -- $file.FullName
        if ($LASTEXITCODE -eq 0) {
            Remove-WorkspaceTarget -Target $file.FullName
        }
    }

}

$mode = if ($Apply) { "APPLIED" } else { "PREVIEW" }
Write-Output ("SUMMARY`t{0}`t{1:N2} MB`t{2} targets`t{3} failures" -f
    $mode, ($candidateBytes / 1MB), $candidateTargets, $failures.Count)
$failures | ForEach-Object { Write-Output ("FAILED`t{0}" -f $_) }

if ($failures.Count -gt 0) {
    exit 1
}
