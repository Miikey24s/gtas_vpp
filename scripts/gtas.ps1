[CmdletBinding()]
param(
    [ValidateSet('help', 'configure', 'status', 'init-db', 'bootstrap-admin', 'run', 'test')]
    [string]$Command = 'help',

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
$BackendProject = Join-Path $RepoRoot 'gtas_vpp_be/gtas_vpp_be/gtas_vpp_be.csproj'
$AppHostProject = Join-Path $RepoRoot 'MyAspire.AppHost/MyAspire.AppHost.csproj'

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
        & dotnet run --project $BackendProject --no-launch-profile
    }
}

function Show-Help {
    @'
GTAS local manager

Commands:
  help             Show this short catalog.
  configure        Store Aspire connection/JWT secrets in .NET user-secrets (not Git).
  status           Show branch and whether required Aspire secret keys exist.
  init-db          Migrate and seed a TEST/DEMO database; safe to run repeatedly.
  bootstrap-admin  Migrate, ensure one local department, and create the first System Admin once.
  run              Start Aspire in watch/Hot Reload mode for local development.
  test             Build and run backend/frontend unit tests.

DatabaseInitialization modes:
  None                 No migration or seed.
  Migrate              Apply EF migrations only.
  MigrateAndReference  Migrate + idempotent permission/reference data (recommended).
  MigrateAndDemo       Reference + non-sensitive demo catalog; TEST/DEMO only.

Configuration catalog:
  Connection string : ConnectionStrings__TestEnv / ConnectionStrings__LiveEnv
  TEST or LIVE      : DatabaseSettings__DefaultEnvironment = TestEnv | LiveEnv
  Migration mode   : DatabaseInitialization__Mode
  Admin bootstrap  : AuthBootstrap__* (one-shot RunOnly; this script supplies it)
  JWT key          : JwtSettings__Key
  SMTP password    : EmailNotifications__Password
  AI provider keys  : GROQ_API_KEY / GEMINI_API_KEY / OPENAI_API_KEY
  AI local provider : ReportInsights:Providers:Ollama:Enabled=true (Ollama, no key)
  AI provider order : ReportInsights:ProviderPriority:0..n

Examples:
  .\scripts\gtas.cmd configure
  .\scripts\gtas.cmd init-db -ConnectionString "Server=localhost;Database=GTAS_VPP_TEST_02;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
  .\scripts\gtas.cmd bootstrap-admin -ConnectionString "..." -DepartmentCode IT -DepartmentName "Information Technology"
  .\scripts\gtas.cmd run
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
        'init-db' {
            $ConnectionString = Read-RequiredValue 'TEST/DEMO database connection string' $ConnectionString
            $databaseName = Assert-LocalDatabase $ConnectionString
            Invoke-DatabaseRunOnly $ConnectionString $Mode
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
            & dotnet build gtas_vpp.sln -c Release
            if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
            & dotnet test gtas_vpp_be.Tests/gtas_vpp_be.Tests.csproj -c Release --no-build
            if ($LASTEXITCODE -ne 0) { throw 'Backend tests failed.' }
            & dotnet test gtas_vpp_fe.Tests/gtas_vpp_fe.Tests.csproj -c Release --no-build
            if ($LASTEXITCODE -ne 0) { throw 'Frontend tests failed.' }
        }
    }
}
finally {
    Pop-Location
}
