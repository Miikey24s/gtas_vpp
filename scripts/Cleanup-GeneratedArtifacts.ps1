param(
    [switch]$IncludeThesisIntermediates,
    [switch]$IncludeSupersededThesisDeliverables
)

$ErrorActionPreference = "Stop"

$workspace = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path.TrimEnd("\")
$git = (Get-Command git -ErrorAction Stop).Source
$deletedBytes = [int64]0
$deletedTargets = 0
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

    try {
        Remove-Item -LiteralPath $resolved -Recurse -Force -ErrorAction Stop
        $script:deletedBytes += $bytes
        $script:deletedTargets++
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
    "gtas_vpp_be\gtas_vpp_be\logs"
)

foreach ($relativePath in $fixedTargets) {
    Remove-WorkspaceTarget -Target (Join-Path $workspace $relativePath)
}

$generatedDirectories = Get-ChildItem -LiteralPath $workspace -Recurse -Directory -Force -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -in @("bin", "obj", "__pycache__") -and
        $_.FullName -notlike "*\.git\*" -and
        $_.FullName -notlike "*\node_modules\*"
    } |
    Sort-Object { $_.FullName.Length } -Descending

foreach ($directory in $generatedDirectories) {
    Remove-WorkspaceTarget -Target $directory.FullName
}

if ($IncludeThesisIntermediates) {
    $checkpointDirectory = Join-Path $workspace "LVTN\checkpoints"
    $patterns = @(
        "*before_fields*.docx",
        "*_nav.docx",
        "*_linked.docx",
        "*_repaired.docx",
        "*followup*.docx",
        "*followup*.pdf",
        "*pre-*.docx",
        "*pre-*.pdf",
        "content_review_ch4_ch5.docx",
        "*final_candidate_v2.pdf",
        "*final_candidate_v3.pdf"
    )

    $checkpointFiles = foreach ($pattern in $patterns) {
        Get-ChildItem -LiteralPath $checkpointDirectory -File -Force -Filter $pattern -ErrorAction SilentlyContinue
    }

    foreach ($file in ($checkpointFiles | Sort-Object FullName -Unique)) {
        & $git check-ignore -q -- $file.FullName
        if ($LASTEXITCODE -eq 0) {
            Remove-WorkspaceTarget -Target $file.FullName
        }
    }

    if (-not (Get-Process WINWORD -ErrorAction SilentlyContinue)) {
        Get-ChildItem -LiteralPath $checkpointDirectory -File -Force -Filter "~`$*.docx" -ErrorAction SilentlyContinue |
            ForEach-Object { Remove-WorkspaceTarget -Target $_.FullName }
    }
}

if ($IncludeSupersededThesisDeliverables) {
    $supersededDeliverables = @(
        "LVTN\checkpoints\NguyenAnNam_DH52201078_final_candidate.docx",
        "LVTN\checkpoints\NguyenAnNam_DH52201078_final_candidate.pdf",
        "LVTN\checkpoints\NguyenAnNam_DH52201078_final_candidate_final.pdf",
        "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v2.docx",
        "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v2.pdf",
        "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v2_navigation.docx",
        "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v3.pdf"
    )

    foreach ($relativePath in $supersededDeliverables) {
        Remove-WorkspaceTarget -Target (Join-Path $workspace $relativePath)
    }
}

Write-Output ("SUMMARY`t{0:N2} MB`t{1} targets removed`t{2} failures" -f
    ($deletedBytes / 1MB), $deletedTargets, $failures.Count)
$failures | ForEach-Object { Write-Output ("FAILED`t{0}" -f $_) }

if ($failures.Count -gt 0) {
    exit 1
}
