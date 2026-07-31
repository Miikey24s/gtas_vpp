[CmdletBinding()]
param(
    [ValidateSet('help', 'configure', 'status', 'preflight', 'doctor', 'agent-check', 'init-db', 'bootstrap-admin', 'run', 'test', 'test-backend', 'test-frontend', 'verify')]
    [string]$Command = 'help',

    [ValidateSet('all', 'frontend', 'backend', 'tests', 'thesis')]
    [string]$Scope = 'all',

    [ValidateSet('text', 'json')]
    [string]$OutputFormat = 'text',

    [string]$ConnectionString,

    [ValidateSet('None', 'Migrate', 'MigrateAndReference', 'MigrateAndDemo')]
    [string]$Mode = 'MigrateAndReference',

    [string]$Username,
    [string]$Email,
    [string]$FullName,
    [string]$DepartmentCode,
    [string]$DepartmentName,
    [string]$OperationKey
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
$BackendProject = Join-Path $RepoRoot 'src/Backend/Api/gtas_vpp_be.csproj'
$AppHostProject = Join-Path $RepoRoot 'src/Hosting/AppHost/MyAspire.AppHost.csproj'

function Invoke-CheckedCommand([string]$FailureMessage, [scriptblock]$Action) {
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage Exit code: $LASTEXITCODE."
    }
}

function Get-AgentScopeDocuments([string]$SelectedScope) {
    $commonDocuments = @(
        'AGENTS.md',
        'docs/ai/AI-AGENT-OPERATING-MODEL.md',
        'docs/ai/CODE-REVIEW.md',
        'README.md'
    )
    $documentsByScope = @{
        frontend = @(
                'src/Frontend/Blazor/AGENTS.md',
                '.github/copilot-instructions.md',
                'docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md',
                'docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md',
                'docs/execution/ATLAS-001.md',
                '.agents/skills/gtas-vpp-ui-system/SKILL.md'
        )
        backend = @(
                'src/Backend/AGENTS.md',
                'docs/architecture/ARCH-001-MODULE-MAP.md',
                '.agents/skills/gtas-vpp-db-safety/SKILL.md'
        )
        tests = @(
                'tests/AGENTS.md',
                'docs/testing/QA-001-ISOLATED-TESTING.md'
        )
        thesis = @(
                'LVTN/AGENTS.md',
                'LVTN/README.md',
                '.agents/skills/gtas-vpp-thesis-docx/SKILL.md'
        )
    }

    $scopeDocuments = if ($SelectedScope -eq 'all') {
        @($documentsByScope.Values | ForEach-Object { $_ })
    }
    else {
        @($documentsByScope[$SelectedScope])
    }

    @($commonDocuments + $scopeDocuments | Select-Object -Unique)
}

function Show-AgentPreflight([string]$SelectedScope, [string]$Format) {
    $branch = (& git branch --show-current).Trim()
    $head = (& git rev-parse --short HEAD).Trim()
    $dotnetVersion = (& dotnet --version).Trim()
    $status = @(& git status --short)
    $documents = @(Get-AgentScopeDocuments $SelectedScope | ForEach-Object {
        [pscustomobject]@{
            Path = $_
            Exists = Test-Path -LiteralPath (Join-Path $RepoRoot $_)
        }
    })

    $summary = [pscustomobject]@{
        Scope = $SelectedScope
        Branch = $branch
        Head = $head
        DotnetSdk = $dotnetVersion
        WorkingTreeClean = $status.Count -eq 0
        Changes = $status
        RequiredContext = $documents
    }

    if ($Format -eq 'json') {
        $summary | ConvertTo-Json -Depth 5
        return
    }

    Write-Host 'GTAS AI agent preflight'
    Write-Host "Scope: $SelectedScope"
    Write-Host "Branch/HEAD: $branch @ $head"
    Write-Host ".NET SDK: $dotnetVersion"
    Write-Host 'Working tree:'
    if ($status.Count -eq 0) {
        Write-Host '  clean'
    }
    else {
        $status | ForEach-Object { Write-Host "  $_" }
    }

    Write-Host 'Required context:'
    foreach ($document in $documents) {
        $marker = if ($document.Exists) { '[OK]' } else { '[MISSING]' }
        Write-Host "  $marker $($document.Path)"
    }

    Write-Host 'Next: read the listed context, inspect the relevant implementation and tests, then make one verifiable vertical slice.'
}

function Show-Doctor([string]$SelectedScope, [string]$Format) {
    $requiredCommands = @('git', 'dotnet', 'python')
    if ($SelectedScope -eq 'frontend' -or $SelectedScope -eq 'all') {
        $requiredCommands += @('node', 'npm')
    }

    $checks = [Collections.Generic.List[object]]::new()
    foreach ($commandName in @($requiredCommands | Select-Object -Unique)) {
        $command = Get-Command $commandName -ErrorAction SilentlyContinue
        $checks.Add([pscustomobject]@{
            Type = 'command'
            Name = $commandName
            Required = $true
            Available = $null -ne $command
            Detail = if ($command) { $command.Source } else { 'Not found on PATH.' }
        })
    }

    foreach ($document in Get-AgentScopeDocuments $SelectedScope) {
        $exists = Test-Path -LiteralPath (Join-Path $RepoRoot $document)
        $checks.Add([pscustomobject]@{
            Type = 'context'
            Name = $document
            Required = $true
            Available = $exists
            Detail = if ($exists) { 'Available.' } else { 'Missing required context.' }
        })
    }

    if ($SelectedScope -eq 'thesis' -or $SelectedScope -eq 'all') {
        $word = Get-Command winword -ErrorAction SilentlyContinue
        $wordComAvailable = $false
        if ($IsWindows -or $env:OS -eq 'Windows_NT') {
            $wordComAvailable = $null -ne [type]::GetTypeFromProgID('Word.Application')
        }
        $soffice = Get-Command soffice -ErrorAction SilentlyContinue
        $checks.Add([pscustomobject]@{
            Type = 'optional'
            Name = 'Word or LibreOffice'
            Required = $false
            Available = ($null -ne $word -or $wordComAvailable -or $null -ne $soffice)
            Detail = 'Required only when updating/rendering Word fields locally.'
        })
    }

    $failed = @($checks | Where-Object { $_.Required -and -not $_.Available })
    $result = [pscustomobject]@{
        Scope = $SelectedScope
        Result = if ($failed.Count -eq 0) { 'PASS' } else { 'FAIL' }
        Checks = $checks
    }

    if ($Format -eq 'json') {
        $result | ConvertTo-Json -Depth 5
    }
    else {
        $checks | Format-Table Type, Name, Required, Available, Detail -AutoSize
        Write-Host "Doctor result: $($result.Result)."
    }

    if ($failed.Count -gt 0) {
        exit 1
    }
}

function Test-NuGetVulnerabilities {
    $auditJson = & dotnet list gtas_vpp.slnx package --vulnerable --include-transitive --format json
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet vulnerability audit failed with exit code $LASTEXITCODE."
    }

    $audit = $auditJson | ConvertFrom-Json
    $packages = @(
        $audit.projects |
            ForEach-Object { $_.frameworks } |
            ForEach-Object { @($_.topLevelPackages) + @($_.transitivePackages) } |
            Where-Object { $_ }
    )
    if ($packages.Count -gt 0) {
        Write-Host ($packages | ConvertTo-Json -Depth 8)
        throw 'Vulnerable NuGet packages were found.'
    }

    Write-Host 'NuGet vulnerability audit passed.'
}

function New-RandomSecret {
    $bytes = New-Object byte[] 48
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        $rng.Dispose()
    }
}

function Read-RequiredValue([string]$Prompt, [string]$CurrentValue) {
    if (-not [string]::IsNullOrWhiteSpace($CurrentValue)) {
        return $CurrentValue.Trim()
    }

    $value = Read-Host $Prompt
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "$Prompt is required."
    }

    return $value.Trim()
}

function Read-PlainPassword {
    $secure = Read-Host 'Initial admin password' -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Get-AdminPasswordValidationErrors([string]$Password) {
    $characters = @($Password.ToCharArray())

    if ($Password.Length -lt 10) {
        'at least 10 characters'
    }
    if (-not ($characters | Where-Object { [char]::IsLower($_) })) {
        'one lowercase letter'
    }
    if (-not ($characters | Where-Object { [char]::IsUpper($_) })) {
        'one uppercase letter'
    }
    if (-not ($characters | Where-Object { [char]::IsDigit($_) })) {
        'one digit'
    }
    if (-not ($characters | Where-Object { -not [char]::IsLetterOrDigit($_) })) {
        'one special character'
    }
}

function Read-ValidAdminPassword {
    Write-Host 'Password requires: 10+ characters, lowercase, uppercase, digit, and special character.'

    while ($true) {
        $password = Read-PlainPassword
        $errors = @(Get-AdminPasswordValidationErrors $password)
        if ($errors.Count -eq 0) {
            return $password
        }

        Write-Warning ("Password is invalid; missing " + ($errors -join ', ') + '. Please try again.')
        $password = $null
    }
}

function Get-DatabaseName([string]$Value) {
    $builder = New-Object System.Data.Common.DbConnectionStringBuilder
    try {
        # PowerShell's IDictionary adapter treats property assignment as a
        # literal "ConnectionString" key; call the CLR setter explicitly.
        $builder.set_ConnectionString($Value)
    }
    catch {
        throw 'Connection string is invalid.'
    }

    foreach ($key in @('Initial Catalog', 'Database')) {
        if ($builder.ContainsKey($key) -and -not [string]::IsNullOrWhiteSpace([string]$builder[$key])) {
            return ([string]$builder[$key]).Trim()
        }
    }

    throw 'Connection string must contain Initial Catalog or Database.'
}

function Assert-LocalDatabase([string]$Value) {
    $databaseName = Get-DatabaseName $Value
    if ($databaseName -notmatch '(?i)(TEST|DEMO)') {
        throw "Local setup refuses database '$databaseName'. Its name must contain TEST or DEMO."
    }

    return $databaseName
}

function Invoke-WithEnvironment([hashtable]$Variables, [scriptblock]$Action) {
    $previous = @{}
    try {
        foreach ($entry in $Variables.GetEnumerator()) {
            $previous[$entry.Key] = [Environment]::GetEnvironmentVariable($entry.Key, 'Process')
            [Environment]::SetEnvironmentVariable($entry.Key, [string]$entry.Value, 'Process')
        }

        & $Action
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        foreach ($entry in $Variables.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($entry.Key, $previous[$entry.Key], 'Process')
        }
    }
}

function Invoke-DatabaseRunOnly(
    [string]$DatabaseConnection,
    [string]$InitializationMode,
    [hashtable]$AdditionalVariables = @{}) {
    $variables = @{
        'ASPNETCORE_ENVIRONMENT' = 'Development'
        'DatabaseSettings__DefaultEnvironment' = 'TestEnv'
        'ConnectionStrings__TestEnv' = $DatabaseConnection
        'DatabaseInitialization__Mode' = $InitializationMode
        'DatabaseInitialization__Environments__0' = 'TestEnv'
        'DatabaseInitialization__RunOnly' = 'true'
        'DatabaseInitialization__AllowDemoData' = ($InitializationMode -eq 'MigrateAndDemo').ToString().ToLowerInvariant()
        'JwtSettings__Key' = New-RandomSecret
        'JwtSettings__Audience' = 'gtas_vpp_test_clients'
    }

    foreach ($entry in $AdditionalVariables.GetEnumerator()) {
        $variables[$entry.Key] = $entry.Value
    }

    Invoke-WithEnvironment $variables {
        # Run one-shot database work from Release output so a developer-owned
        # Debug dotnet-watch process cannot lock the migration binaries.
        & dotnet run --project $BackendProject --configuration Release --no-launch-profile
    }
}

function Show-Help {
    @'
GTAS local manager

Commands:
  help             Show this short catalog.
  configure        Store Aspire connection/JWT secrets in .NET user-secrets (not Git).
  status           Show branch and whether required Aspire secret keys exist.
  preflight        Show branch, dirty files, SDK and required context for an AI task.
  doctor           Check local tools and required context without exposing secrets.
  agent-check      Lint the repository AI instruction and skill setup.
  init-db          Migrate and seed a TEST/DEMO database; safe to run repeatedly.
  bootstrap-admin  Migrate, ensure one local department, and create the first System Admin once.
  run              Start Aspire in watch/Hot Reload mode for local development.
  test             Build and run backend/frontend unit tests.
  test-backend     Run backend unit tests.
  test-frontend    Run frontend unit tests.
  verify           Run CI-style gates for -Scope all|frontend|backend|tests|thesis.

DatabaseInitialization modes:
  None                 No migration or seed.
  Migrate              Apply EF migrations only.
  MigrateAndReference  Migrate + idempotent permission/reference data (recommended).
  MigrateAndDemo       Reference + normalized catalog, departments and orders; TEST/DEMO only.

Configuration catalog:
  Connection string : ConnectionStrings__TestEnv / ConnectionStrings__LiveEnv
  TEST or LIVE      : DatabaseSettings__DefaultEnvironment = TestEnv | LiveEnv
  Migration mode   : DatabaseInitialization__Mode
  Demo owner       : DatabaseInitialization__DemoOwnerUsername (optional; otherwise exactly one active DEV is selected)
  Admin bootstrap  : AuthBootstrap__* (one-shot RunOnly; this script supplies it)
  JWT key          : JwtSettings__Key
  SMTP password    : EmailNotifications__Password
  AI provider keys  : GROQ_API_KEY / GEMINI_API_KEY / OPENAI_API_KEY
  AI local provider : ReportInsights:Providers:Ollama:Enabled=true (Ollama, no key)
  AI provider order : ReportInsights:ProviderPriority:0..n

Examples:
  .\scripts\gtas.cmd preflight -Scope frontend
  .\scripts\gtas.cmd preflight -Scope all -OutputFormat json
  .\scripts\gtas.cmd doctor -Scope frontend
  .\scripts\gtas.cmd agent-check
  .\scripts\gtas.cmd configure
  .\scripts\gtas.cmd init-db -Mode MigrateAndDemo -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_02;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
  .\scripts\gtas.cmd bootstrap-admin -ConnectionString "..." -DepartmentCode IT -DepartmentName "Information Technology"
  .\scripts\gtas.cmd run
  .\scripts\gtas.cmd test
  .\scripts\gtas.cmd verify -Scope all
'@ | Write-Host
}

$requiresDotnet = $Command -notin @('help', 'doctor', 'agent-check') -and -not ($Command -eq 'verify' -and $Scope -eq 'thesis')
if ($requiresDotnet -and -not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK is required.'
}

Push-Location $RepoRoot
try {
    switch ($Command) {
        'help' {
            Show-Help
        }
        'configure' {
            $ConnectionString = Read-RequiredValue 'TEST database connection string' $ConnectionString
            $databaseName = Assert-LocalDatabase $ConnectionString
            $jwtKey = New-RandomSecret

            & dotnet user-secrets set 'Parameters:test-database-connection-string' $ConnectionString --project $AppHostProject
            if ($LASTEXITCODE -ne 0) { throw 'Could not store the Aspire database secret.' }
            & dotnet user-secrets set 'Parameters:jwt-key' $jwtKey --project $AppHostProject
            if ($LASTEXITCODE -ne 0) { throw 'Could not store the Aspire JWT secret.' }

            Write-Host "Configured Aspire for $databaseName. Values are stored in local user-secrets, not the repository."
        }
        'status' {
            $branch = (& git branch --show-current).Trim()
            $secretLines = & dotnet user-secrets list --project $AppHostProject 2>$null
            $hasDatabase = [bool]($secretLines | Where-Object { $_ -like 'Parameters:test-database-connection-string =*' })
            $hasJwt = [bool]($secretLines | Where-Object { $_ -like 'Parameters:jwt-key =*' })
            $databaseStatus = if ($hasDatabase) { 'configured' } else { 'missing' }
            $jwtStatus = if ($hasJwt) { 'configured' } else { 'missing' }
            Write-Host "Branch: $branch"
            Write-Host "Aspire TEST connection: $databaseStatus"
            Write-Host "Aspire JWT key: $jwtStatus"
        }
        'preflight' {
            Show-AgentPreflight $Scope $OutputFormat
        }
        'doctor' {
            Show-Doctor $Scope $OutputFormat
        }
        'agent-check' {
            & (Join-Path $RepoRoot 'scripts/ai/Test-AgentSetup.ps1') -OutputFormat $OutputFormat
            if (-not $?) { throw 'Agent setup validation failed.' }
        }
        'init-db' {
            $ConnectionString = Read-RequiredValue 'TEST/DEMO database connection string' $ConnectionString
            $databaseName = Assert-LocalDatabase $ConnectionString
            $additionalVariables = @{}
            if ($Mode -eq 'MigrateAndDemo' -and -not [string]::IsNullOrWhiteSpace($Username)) {
                $additionalVariables['DatabaseInitialization__DemoOwnerUsername'] = $Username
            }

            Invoke-DatabaseRunOnly $ConnectionString $Mode $additionalVariables
            Write-Host "Database $databaseName initialized with mode $Mode. Re-running is idempotent."
        }
        'bootstrap-admin' {
            $ConnectionString = Read-RequiredValue 'TEST/DEMO database connection string' $ConnectionString
            $databaseName = Assert-LocalDatabase $ConnectionString
            $Username = Read-RequiredValue 'Admin username' $Username
            $Email = Read-RequiredValue 'Admin email' $Email
            $FullName = Read-RequiredValue 'Admin full name' $FullName
            $DepartmentCode = Read-RequiredValue 'Primary department code' $DepartmentCode
            $DepartmentName = Read-RequiredValue 'Primary department name' $DepartmentName
            $password = Read-ValidAdminPassword
            if ([string]::IsNullOrWhiteSpace($OperationKey)) {
                $safeKey = "$databaseName-$Username" -replace '[^A-Za-z0-9_.:-]', '-'
                $OperationKey = "local-owner-$safeKey"
            }

            try {
                Invoke-DatabaseRunOnly $ConnectionString 'MigrateAndReference' @{
                    'AuthBootstrap__Enabled' = 'true'
                    'AuthBootstrap__OperationKey' = $OperationKey
                    'AuthBootstrap__Username' = $Username
                    'AuthBootstrap__Email' = $Email
                    'AuthBootstrap__FullName' = $FullName
                    'AuthBootstrap__InitialPassword' = $password
                    'AuthBootstrap__PrimaryDepartmentCode' = $DepartmentCode
                    'AuthBootstrap__PrimaryDepartmentName' = $DepartmentName
                    'AuthBootstrap__CreatePrimaryDepartmentIfMissing' = 'true'
                }
            }
            finally {
                $password = $null
            }

            Write-Host "System Admin bootstrap completed for $databaseName. Use the same OperationKey to verify/re-run safely."
        }
        'run' {
            Write-Host 'Starting GTAS with dotnet watch (Hot Reload enabled).'
            Write-Host 'GTAS Login: https://localhost:7009/Account/Login'
            Write-Host 'The tokenized Aspire Login URL authenticates the infrastructure dashboard, not GTAS users.'
            Write-Host 'When frontend is Running, open Dashboard > frontend > GTAS Login.'
            & dotnet watch --project $AppHostProject --launch-profile https --non-interactive
            if ($LASTEXITCODE -ne 0) { throw "Aspire watch exited with code $LASTEXITCODE." }
        }
        'test' {
            Invoke-CheckedCommand 'Build failed.' {
                & dotnet build gtas_vpp.slnx -c Release
            }
            Invoke-CheckedCommand 'Backend tests failed.' {
                & dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release --no-build
            }
            Invoke-CheckedCommand 'Frontend tests failed.' {
                & dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release --no-build
            }
        }
        'test-backend' {
            Invoke-CheckedCommand 'Backend tests failed.' {
                & dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release
            }
        }
        'test-frontend' {
            Invoke-CheckedCommand 'Frontend tests failed.' {
                & dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release
            }
        }
        'verify' {
            & (Join-Path $RepoRoot 'scripts/ai/Test-AgentSetup.ps1')
            if (-not $?) { throw 'Agent setup validation failed.' }

            if ($Scope -ne 'thesis') {
                Invoke-CheckedCommand 'Solution restore failed.' {
                    & dotnet restore gtas_vpp.slnx
                }
                Invoke-CheckedCommand 'Release build failed.' {
                    & dotnet build gtas_vpp.slnx -c Release --no-restore
                }
            }

            if ($Scope -in @('all', 'backend', 'tests')) {
                Invoke-CheckedCommand 'Backend unit tests failed.' {
                    & dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release --no-build
                }
            }
            if ($Scope -in @('all', 'frontend', 'tests')) {
                Invoke-CheckedCommand 'Frontend unit tests failed.' {
                    & dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release --no-build
                }
                Invoke-CheckedCommand 'UI configuration syntax smoke failed.' {
                    & dotnet test tests/Frontend.UiTests/gtas_vpp_fe.UITests.csproj -c Release --no-build --filter 'FullyQualifiedName~ComposeConfigurationSyntaxTests'
                }
            }
            if ($Scope -in @('all', 'backend', 'tests')) {
                Invoke-CheckedCommand 'Backend integration tests failed.' {
                    & dotnet test tests/Backend.IntegrationTests/gtas_vpp_be.IntegrationTests.csproj -c Release --no-build
                }
            }
            if ($Scope -in @('all', 'backend')) {
                Invoke-CheckedCommand 'Local tool restore failed.' {
                    & dotnet tool restore
                }
                Invoke-CheckedCommand 'EF pending-model check failed.' {
                    & dotnet tool run dotnet-ef migrations has-pending-model-changes `
                        --project src/Backend/Migrations/gtas_vpp_be.Migrations.csproj `
                        --startup-project src/Backend/Api/gtas_vpp_be.csproj `
                        --context VPPMigrationDbContext `
                        --configuration Release `
                        --no-build
                }
            }
            if ($Scope -ne 'thesis') {
                Test-NuGetVulnerabilities
                Invoke-CheckedCommand 'Formatting verification failed.' {
                    & dotnet format gtas_vpp.slnx --verify-no-changes --no-restore
                }
            }
            if ($Scope -in @('all', 'thesis')) {
                Invoke-CheckedCommand 'Thesis structure check failed.' {
                    & python LVTN/tooling/check_thesis.py
                }
            }
            & (Join-Path $RepoRoot 'scripts/security/Invoke-Gitleaks.ps1') -Mode Current
            Invoke-CheckedCommand 'Git whitespace check failed.' {
                & git diff --check
            }
            Write-Host "GTAS verification passed for scope '$Scope'. Authenticated route-real UI QA remains a separate gate for UI changes."
        }
    }
}
finally {
    Pop-Location
}
