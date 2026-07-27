[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

try {
    $inputText = [Console]::In.ReadToEnd()
    $payload = if ([string]::IsNullOrWhiteSpace($inputText)) { $null } else { $inputText | ConvertFrom-Json }
    $workingDirectory = if ($payload -and $payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $repoRoot = (& git -C $workingDirectory rev-parse --show-toplevel 2>$null | Select-Object -First 1)

    if ([string]::IsNullOrWhiteSpace($repoRoot)) {
        exit 0
    }

    $repoRoot = $repoRoot.Trim()
    $branch = (& git -C $repoRoot branch --show-current 2>$null | Select-Object -First 1)
    $statusEntries = @(& git -C $repoRoot status --short 2>$null)
    $lastCommit = (& git -C $repoRoot log -1 --pretty=format:'%h %s' 2>$null | Select-Object -First 1)
    $dirtySummary = if ($statusEntries.Count -eq 0) { 'clean' } else { "$($statusEntries.Count) changed entries" }
    $message = "GTAS VPP resume preflight required. Branch: $branch. Working tree: $dirtySummary. Latest commit: $lastCommit. Before new work, inspect the active goal, unfinished plan, latest user request, Git diff, and background processes; continue the unfinished task unless the owner replaced it."

    [pscustomobject]@{
        continue = $true
        systemMessage = $message
    } | ConvertTo-Json -Compress
}
catch {
    [pscustomobject]@{
        continue = $true
        systemMessage = 'GTAS VPP resume preflight could not read repository state; inspect the active goal, unfinished plan, Git status, and background processes manually before continuing.'
    } | ConvertTo-Json -Compress
}
