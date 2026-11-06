param(
    [Parameter(Mandatory=$true)][Guid]$TenantId,
    [Parameter(Mandatory=$true)][string]$Email,
    [Parameter(Mandatory=$true)][string]$TenantCode,
    [Parameter(Mandatory=$true)][string]$TenantName
)
$ErrorActionPreference = 'Stop'
if (!$env:ConnectionStrings__CondoManager) { throw 'Impostare ConnectionStrings__CondoManager prima del bootstrap.' }
$names=@('Bootstrap__TenantId','Bootstrap__Email','Bootstrap__TenantCode','Bootstrap__TenantName','Bootstrap__Password')
$previous=@{}
foreach($name in $names) { $previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process') }
Push-Location (Split-Path $PSScriptRoot)
try {
    $secure = Read-Host 'Password iniziale (12-128 caratteri, maiuscole, minuscole, numero, simbolo)' -AsSecureString
    $credential = New-Object System.Management.Automation.PSCredential($Email,$secure)
    $env:Bootstrap__TenantId=$TenantId.ToString()
    $env:Bootstrap__Email=$Email
    $env:Bootstrap__TenantCode=$TenantCode
    $env:Bootstrap__TenantName=$TenantName
    $env:Bootstrap__Password=$credential.GetNetworkCredential().Password
    dotnet run --project src/backend/CondoManager.Api --no-launch-profile -- --bootstrap
    if($LASTEXITCODE) { throw 'Bootstrap non completato.' }
} finally {
    foreach($name in $names) { [Environment]::SetEnvironmentVariable($name,$previous[$name],'Process') }
    $credential=$null; $secure=$null
    Pop-Location
}