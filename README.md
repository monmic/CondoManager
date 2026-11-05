# CondoManager — Milestone 01
Fondazioni modular monolith: ASP.NET Core **.NET 8**, EF Core/Npgsql 8,
PostgreSQL 16 e React/TypeScript. Nessun dato reale e nessuna API dei dati tenant.

## Requisiti Windows
Windows PowerShell 5.1 o PowerShell 7, SDK .NET 8+
(con supporto solution .slnx: SDK 9.0.200+; qui verificato con SDK 10),
runtime ASP.NET Core 8, Node.js 24 e npm, Docker Desktop avviato.
I progetti restano tutti net8.0. Git non era inizializzato nello scheletro.

## Build e test
Dalla radice, in PowerShell:
~~~powershell
dotnet tool restore
dotnet build
dotnet test
Push-Location src/frontend/web
npm install
npm run build
npm run lint
Pop-Location
~~~
Il test PostgreSQL è esplicitamente ignorato quando manca
CONDOMANAGER_TEST_POSTGRES. Tutti gli altri test non richiedono database.

## PostgreSQL e migrazioni
Segreti solo in variabili d'ambiente della sessione; non salvarli nei file.
~~~powershell
$env:POSTGRES_USER = 'condomanager'
$credential = Get-Credential -UserName $env:POSTGRES_USER -Message 'Password locale PostgreSQL'
$env:POSTGRES_PASSWORD = $credential.GetNetworkCredential().Password
$env:ConnectionStrings__CondoManager = "Host=127.0.0.1;Port=5432;Database=condomanager;Username=$env:POSTGRES_USER;Password=$env:POSTGRES_PASSWORD"
docker compose up -d --wait
dotnet ef database update --project src/backend/CondoManager.Infrastructure
~~~
POSTGRES_PORT può cambiare la porta host (predefinita 5432); adeguare anche
la connection string. PostgreSQL è esposto solo su loopback. Il volume conserva
i dati; modificare la password nell'ambiente non modifica un utente già creato.
Arresto non distruttivo: docker compose stop.

Per una verifica ripetibile su database isolato:
~~~powershell
.\scripts\Verify-Postgres.ps1
~~~
Usa porta 55439 (parametro -Port), progetto Compose univoco, password casuale in
memoria, applica la migrazione ed esegue tutti i test. Rimuove esclusivamente
container e volume temporanei creati dalla verifica, anche in caso di errore.

## Avvio API e frontend
~~~powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/backend/CondoManager.Api --no-launch-profile --urls http://localhost:5080
~~~
In un secondo terminale:
~~~powershell
Invoke-RestMethod http://localhost:5080/health
Set-Location src/frontend/web
npm run dev -- --host 127.0.0.1
~~~
Frontend su URL stampato da Vite (normalmente http://localhost:5173).
Swagger su http://localhost:5080/swagger solo Development.
GET /health restituisce {"status":"healthy"} senza interrogare il database e
funziona anche senza connection string. Nessuna migrazione automatica all'avvio.

## Limiti e seguito
L'autenticazione non è implementata: il contesto tenant nega ogni accesso ai dati.
Nessun header TenantId viene accettato. Non introdurre API dati prima di avere
autenticazione verificata e autorizzazione allo studio.
Le protezioni EF non sostituiscono RLS: SQL diretto e operazioni bulk richiedono
controlli espliciti. Nessuna funzionalità contabile è ancora implementata.
Prossima milestone: identità, membership degli studi, provisioning controllato e
primi endpoint condominio con test end-to-end di isolamento e autorizzazione.

Documenti: [architettura](docs/architecture/overview.md),
[ADR multi-tenant](docs/decisions/0001-multi-tenant.md),
[glossario](docs/domain/glossary.md), [requisiti](docs/requirements/README.md).