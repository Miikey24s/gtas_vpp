[CmdletBinding()]
param(
    [ValidateSet('help', 'configure', 'status', 'preflight', 'init-db', 'bootstrap-admin', 'run', 'test', 'test-backend', 'test-frontend', 'verify')]
    [string]$Command = 'help',

    [ValidateSet('all', 'frontend', 'backend', 'tests', 'thesis')]
    [string]$Scope = 'all',

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

function Show-AgentPreflight([string]$SelectedScope) {
    $commonDocuments = @(
        'AGENTS.md',
        'docs/ai/AI-AGENT-OPERATING-MODEL.md',
        'README.md'
    )
    $scopeDocuments = switch ($SelectedScope) {
        'frontend' {
            @(
                'src/Frontend/Blazor/AGENTS.md',
                '.codexrules',
                '.github/copilot-instructions.md',
                'docs/design/VPP-PULSE-BLAZOR-UI-RENOVATION-PLAN.md',
                'docs/design/VPP-PULSE-UI-UX-AI-TOOLCHAIN.md',
                'docs/execution/ATLAS-001.md'
            )
        }
        'backend' {
            @(
                'src/Backend/AGENTS.md',
                'docs/architecture/ARCH-001-MODULE-MAP.md'
            )
        }
        'tests' {
            @(
                'tests/AGENTS.md',
                'docs/testing/QA-001-ISOLATED-TESTING.md'
            )
        }
        'thesis' {
            @(
                'LVTN/AGENTS.md',
                'LVTN/README.md'
            )
        }
        default { @() }
    }

    $branch = (& git branch --show-current).Trim()
    $head = (& git rev-parse --short HEAD).Trim()
    $dotnetVersion = (& dotnet --version).Trim()
    $status = @(& git status --short)

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
    foreach ($document in @($commonDocuments + $scopeDocuments | Select-Object -Unique)) {
        $marker = if (Test-Path -LiteralPath (Join-Path $RepoRoot $document)) { '[OK]' } else { '[MISSING]' }
        Write-Host "  $marker $document"
    }

    Write-Host 'Next: read the listed context, inspect the relevant implementation and tests, then make one verifiable vertical slice.'
}

function Test-NuGetVulnerabilities {
    $auditJson = & dotnet list gtas_vpp.sln package --vulnerable --include-transitive --format json
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
  init-db          Migrate and seed a TEST/DEMO database; safe to run repeatedly.
  bootstrap-admin  Migrate, ensure one local department, and create the first System Admin once.
  run              Start Aspire in watch/Hot Reload mode for local development.
  test             Build and run backend/frontend unit tests.
  test-backend     Run backend unit tests.
  test-frontend    Run frontend unit tests.
  verify           Mirror the non-browser CI gates before a complete handoff.

DatabaseInitialization modes:
  None                 No migration or seed.
  Migrate              Apply EF migrations only.
  MigrateAndReference  Migrate + idempotent permission/reference data (recommended).
  MigrateAndDemo       Reference + normalized catalog, departments and orders; TEST/DEMO only.

Configuration catalog:
  Connection string : ConnectionStrings__TestEnv / ConnectionStrings__LiveEnv
  TEST or LIVE      : DatabaseSettings__DefaultEnvironment = TestEnv | LiveEnv
  Migration mode   : DatabaseInitialization__Mode
  Demo owner       : DatabaseInitialization__DemoOwnerUsername (active username; MigrateAndDemo only)
  Admin bootstrap  : AuthBootstrap__* (one-shot RunOnly; this script supplies it)
  JWT key          : JwtSettings__Key
  SMTP password    : EmailNotifications__Password
  AI provider keys  : GROQ_API_KEY / GEMINI_API_KEY / OPENAI_API_KEY
  AI local provider : ReportInsights:Providers:Ollama:Enabled=true (Ollama, no key)
  AI provider order : ReportInsights:ProviderPriority:0..n

Examples:
  .\scripts\gtas.cmd preflight -Scope frontend
  .\scripts\gtas.cmd configure
  .\scripts\gtas.cmd init-db -Mode MigrateAndDemo -Username "your-admin" -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_02;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
  .\scripts\gtas.cmd bootstrap-admin -ConnectionString "..." -DepartmentCode IT -DepartmentName "Information Technology"
  .\scripts\gtas.cmd run
  .\scripts\gtas.cmd test
  .\scripts\gtas.cmd verify
'@ | Write-Host
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
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
            Show-AgentPreflight $Scope
        }
        'init-db' {
            $ConnectionString = Read-RequiredValue 'TEST/DEMO database connection string' $ConnectionString
            $databaseName = Assert-LocalDatabase $ConnectionString
            $additionalVariables = @{}
            if ($Mode -eq 'MigrateAndDemo') {
                $Username = Read-RequiredValue 'Existing active account that owns the demo orders' $Username
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
                & dotnet build gtas_vpp.sln -c Release
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
            Invoke-CheckedCommand 'Solution restore failed.' {
                & dotnet restore gtas_vpp.sln
            }
            Invoke-CheckedCommand 'Local tool restore failed.' {
                & dotnet tool restore
            }
            Invoke-CheckedCommand 'Release build failed.' {
                & dotnet build gtas_vpp.sln -c Release --no-restore
            }
            Invoke-CheckedCommand 'Backend unit tests failed.' {
                & dotnet test tests/Backend.UnitTests/gtas_vpp_be.Tests.csproj -c Release --no-build
            }
            Invoke-CheckedCommand 'Frontend unit tests failed.' {
                & dotnet test tests/Frontend.UnitTests/gtas_vpp_fe.Tests.csproj -c Release --no-build
            }
            Invoke-CheckedCommand 'Backend integration tests failed.' {
                & dotnet test tests/Backend.IntegrationTests/gtas_vpp_be.IntegrationTests.csproj -c Release --no-build
            }
            Invoke-CheckedCommand 'UI configuration syntax smoke failed.' {
                & dotnet test tests/Frontend.UiTests/gtas_vpp_fe.UITests.csproj -c Release --no-build --filter 'FullyQualifiedName~ComposeConfigurationSyntaxTests'
            }
            Invoke-CheckedCommand 'EF pending-model check failed.' {
                & dotnet tool run dotnet-ef migrations has-pending-model-changes `
                    --project src/Backend/Migrations/gtas_vpp_be.Migrations.csproj `
                    --startup-project src/Backend/Api/gtas_vpp_be.csproj `
                    --context VPPMigrationDbContext `
                    --configuration Release `
                    --no-build
            }
            Test-NuGetVulnerabilities
            Invoke-CheckedCommand 'Thesis structure check failed.' {
                & python LVTN/tooling/check_thesis.py
            }
            & (Join-Path $RepoRoot 'scripts/security/Invoke-Gitleaks.ps1') -Mode Current
            Invoke-CheckedCommand 'Git whitespace check failed.' {
                & git diff --check
            }
            Write-Host 'GTAS repository verification passed. Authenticated route-real UI QA remains a separate gate for UI changes.'
        }
    }
}
finally {
    Pop-Location
}
