param([int]$Port = 55439, [switch]$Browser)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$project = 'cm-m03-check-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$names = @('POSTGRES_USER','POSTGRES_PASSWORD','POSTGRES_PORT','ConnectionStrings__CondoManager','CONDOMANAGER_TEST_POSTGRES','ASPNETCORE_ENVIRONMENT','Frontend__RootPath','CONDOMANAGER_E2E','CONDOMANAGER_E2E_EMAIL','CONDOMANAGER_E2E_PASSWORD','Bootstrap__Email','Bootstrap__Password','Bootstrap__TenantId','Bootstrap__TenantCode','Bootstrap__TenantName')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
Push-Location $root
try {
    $env:POSTGRES_USER = 'milestone_check'
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    $bytes = New-Object byte[] 32
    try { $random.GetBytes($bytes) } finally { $random.Dispose() }
    $env:POSTGRES_PASSWORD = [BitConverter]::ToString($bytes).Replace('-', '')
    $env:POSTGRES_PORT = "$Port"
    $env:ConnectionStrings__CondoManager = "Host=127.0.0.1;Port=$Port;Database=condomanager;Username=$env:POSTGRES_USER;Password=$env:POSTGRES_PASSWORD"
    $env:CONDOMANAGER_TEST_POSTGRES = $env:ConnectionStrings__CondoManager
    docker compose -p $project up -d --wait --wait-timeout 90
    if ($LASTEXITCODE) { throw 'Avvio PostgreSQL fallito.' }
    dotnet ef database update --project src/backend/CondoManager.Infrastructure
    if ($LASTEXITCODE) { throw 'Migrazione fallita.' }
    dotnet ef migrations has-pending-model-changes --project src/backend/CondoManager.Infrastructure
    if ($LASTEXITCODE) { throw 'Snapshot migrazione non allineato.' }
    dotnet build
    if ($LASTEXITCODE) { throw 'Build fallita.' }
    dotnet test
    if ($LASTEXITCODE) { throw 'Test falliti.' }
    if ($Browser) {
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:Frontend__RootPath = Join-Path $root 'src/frontend/web/dist'
        $env:Bootstrap__TenantId = [Guid]::NewGuid().ToString()
        $env:Bootstrap__TenantCode = 'BROWSER'
        $env:Bootstrap__TenantName = 'Studio sintetico browser'
        $env:Bootstrap__Email = 'browser-' + [Guid]::NewGuid().ToString('N') + '@test.invalid'
        $random = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $random.GetBytes($bytes) } finally { $random.Dispose() }
        $env:Bootstrap__Password = [BitConverter]::ToString($bytes).Replace('-', '') + 'aA1!'
        $env:CONDOMANAGER_E2E = '1'
        $env:CONDOMANAGER_E2E_EMAIL = $env:Bootstrap__Email
        $env:CONDOMANAGER_E2E_PASSWORD = $env:Bootstrap__Password
        # Execute twice to check idempotence through the actual protected CLI.
        foreach($attempt in 1..2) {
            dotnet run --no-build --project src/backend/CondoManager.Api --no-launch-profile -- --bootstrap
            if ($LASTEXITCODE) { throw 'Bootstrap browser fallito.' }
        }
        Push-Location src/frontend/web
        try {
            npm run build
            if ($LASTEXITCODE) { throw 'Build frontend fallita.' }
            npm run test:e2e
            if ($LASTEXITCODE) { throw 'Test browser falliti.' }
        } finally { Pop-Location }
    }
} finally {
    # Only resources created by this unique verification project are removed.
    docker compose -p $project down -v
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    Pop-Location
}