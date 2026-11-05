param([int]$Port = 55439)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$project = 'cm-m01-check-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$names = @('POSTGRES_USER','POSTGRES_PASSWORD','POSTGRES_PORT','ConnectionStrings__CondoManager','CONDOMANAGER_TEST_POSTGRES')
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
}
finally {
    # Only resources created by this unique verification project are removed.
    docker compose -p $project down -v
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    Pop-Location
}