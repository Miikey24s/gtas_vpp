[CmdletBinding()]
param(
    [ValidateSet('text', 'json')]
    [string]$OutputFormat = 'text'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$results = [Collections.Generic.List[object]]::new()

function Add-Check([string]$Name, [bool]$Passed, [string]$Detail) {
    $results.Add([pscustomobject]@{
        Check = $Name
        Result = if ($Passed) { 'PASS' } else { 'FAIL' }
        Detail = $Detail
    })
}

function Get-RepoPath([string]$RelativePath) {
    Join-Path $RepoRoot $RelativePath
}

$requiredFiles = @(
    'AGENTS.md',
    'src/Frontend/Blazor/AGENTS.md',
    'src/Backend/AGENTS.md',
    'tests/AGENTS.md',
    'LVTN/AGENTS.md',
    'docs/ai/AI-AGENT-OPERATING-MODEL.md',
    'docs/ai/AI-AGENT-EVALS.md',
    'docs/ai/CODE-REVIEW.md',
    '.github/copilot-instructions.md',
    '.github/instructions/frontend.instructions.md',
    '.github/instructions/backend.instructions.md',
    '.github/instructions/tests.instructions.md',
    '.github/instructions/thesis.instructions.md',
    '.github/pull_request_template.md',
    '.github/workflows/ci.yml',
    '.editorconfig',
    'CLAUDE.md',
    'GEMINI.md'
)

foreach ($file in $requiredFiles) {
    Add-Check "required:$file" (Test-Path -LiteralPath (Get-RepoPath $file)) 'Required AI context surface exists.'
}

$rootAgents = Get-Item -LiteralPath (Get-RepoPath 'AGENTS.md')
Add-Check 'root-agents-budget' ($rootAgents.Length -le 16384) "Root AGENTS.md uses $($rootAgents.Length) of the 16384-byte project budget."

$adapterLimits = @{
    'CLAUDE.md' = 20
    'GEMINI.md' = 15
    '.github/copilot-instructions.md' = 30
    '.codexrules' = 10
}
foreach ($entry in $adapterLimits.GetEnumerator()) {
    $path = Get-RepoPath $entry.Key
    $lineCount = if (Test-Path -LiteralPath $path) { (Get-Content -LiteralPath $path).Count } else { 0 }
    Add-Check "adapter-budget:$($entry.Key)" ($lineCount -gt 0 -and $lineCount -le $entry.Value) "$lineCount lines; maximum $($entry.Value)."
}

$skillRoot = Get-RepoPath '.agents/skills'
$skills = @(Get-ChildItem -LiteralPath $skillRoot -Directory -ErrorAction SilentlyContinue)
Add-Check 'repo-skill-count' ($skills.Count -ge 3) "$($skills.Count) repo skills discovered."

$skillNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($skill in $skills) {
    $skillFile = Join-Path $skill.FullName 'SKILL.md'
    $metadataFile = Join-Path $skill.FullName 'agents/openai.yaml'
    $content = if (Test-Path -LiteralPath $skillFile) { Get-Content -LiteralPath $skillFile -Raw } else { '' }
    $nameMatch = [regex]::Match($content, '(?m)^name:\s*([a-z0-9-]+)\s*$')
    $name = if ($nameMatch.Success) { $nameMatch.Groups[1].Value } else { $skill.Name }

    Add-Check "skill-file:$($skill.Name)" (Test-Path -LiteralPath $skillFile) 'SKILL.md exists.'
    Add-Check "skill-frontmatter:$($skill.Name)" $nameMatch.Success "Declared name: $name."
    Add-Check "skill-name:$($skill.Name)" ($name -eq $skill.Name) 'Folder and frontmatter names match.'
    Add-Check "skill-unique:$($skill.Name)" ($skillNames.Add($name)) 'Skill name is unique.'
    Add-Check "skill-no-placeholder:$($skill.Name)" ($content -notmatch '\[TODO|TODO:') 'No scaffold placeholder remains.'
    Add-Check "skill-metadata:$($skill.Name)" (Test-Path -LiteralPath $metadataFile) 'agents/openai.yaml exists.'

    if (Test-Path -LiteralPath $metadataFile) {
        $metadata = Get-Content -LiteralPath $metadataFile -Raw
        Add-Check "skill-default-prompt:$($skill.Name)" ($metadata.Contains("`$$name")) "Default prompt mentions `$$name."
    }
}

$rootContent = Get-Content -LiteralPath (Get-RepoPath 'AGENTS.md') -Raw
Add-Check 'no-snapshot-test-count' ($rootContent -notmatch '\b\d+\s*/\s*\d+\b') 'Root guidance contains no stale test-count snapshot.'

$legacyRules = Get-Content -LiteralPath (Get-RepoPath '.codexrules') -Raw
Add-Check 'legacy-codexrules-adapter' ($legacyRules -match 'Legacy UI instruction adapter' -and $legacyRules -match 'Không thêm quy tắc') 'Legacy file only redirects to canonical UI authority.'

$failed = @($results | Where-Object Result -eq 'FAIL')
if ($OutputFormat -eq 'json') {
    [pscustomobject]@{
        Result = if ($failed.Count -eq 0) { 'PASS' } else { 'FAIL' }
        Passed = $results.Count - $failed.Count
        Failed = $failed.Count
        Checks = $results
    } | ConvertTo-Json -Depth 5
}
else {
    $results | Format-Table -AutoSize
    Write-Host "Agent setup: $($results.Count - $failed.Count) passed, $($failed.Count) failed."
}

if ($failed.Count -gt 0) {
    exit 1
}
