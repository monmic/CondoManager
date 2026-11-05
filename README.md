# CondoManager — Milestone 02
Gestionale condominiale modular monolith: progetti .NET 8, EF Core/Npgsql 8,
PostgreSQL 16 e React/TypeScript/Vite. Anagrafiche, associazioni storicizzate,
tabelle millesimali ed esercizi persistono realmente su PostgreSQL.

## Prerequisiti Windows
- Windows PowerShell 5.1 o PowerShell 7.
- SDK con supporto .slnx (9.0.200+; verificato con 10.0.403) e runtime ASP.NET Core 8.
  Tutti i progetti compilano per net8.0.
- Node.js 24 e npm.
- Docker Desktop avviato, con container Linux.
- Microsoft Edge per il test browser opzionale (Playwright, canale msedge).

## Database e studio di sviluppo
Dalla radice del repository, in PowerShell:
~~~powershell
dotnet tool restore
$env:POSTGRES_USER = 'condomanager'
$credential = Get-Credential -UserName $env:POSTGRES_USER -Message 'Password locale PostgreSQL'
$env:POSTGRES_PASSWORD = $credential.GetNetworkCredential().Password
$env:ConnectionStrings__CondoManager = "Host=127.0.0.1;Port=5432;Database=condomanager;Username=$env:POSTGRES_USER;Password=$env:POSTGRES_PASSWORD"
docker compose up -d --wait
dotnet ef database update --project src/backend/CondoManager.Infrastructure

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DevelopmentTenant__Id = [Guid]::NewGuid().ToString()
.scriptsInitialize-DevelopmentTenant.ps1
~~~
Conservare il valore di DevelopmentTenant__Id per accedere allo stesso studio
nelle sessioni successive: e un identificativo, non una credenziale.
Non rigenerarlo a ogni avvio se si vuole riaprire lo studio esistente.
Lo script crea solo il Tenant indicato, senza modificare record esistenti.
Non carica condomini, persone o altri dati dimostrativi.

POSTGRES_PORT cambia la porta host (default 5432); aggiornare anche la connection
string. La porta PostgreSQL e pubblicata solo su loopback. Il volume conserva
i dati: cambiare POSTGRES_PASSWORD nell'ambiente non aggiorna utenti gia creati.
Arresto non distruttivo: docker compose stop.
Non eseguire down -v sul database di lavoro.

Segreti solo nell'ambiente o nei user secrets configurati localmente.
Non salvare password in appsettings, compose, script, file .env o Git.

## Backend e frontend
Nello stesso terminale con le variabili appena configurate:
~~~powershell
dotnet run --project src/backend/CondoManager.Api --no-launch-profile --urls http://127.0.0.1:5080
~~~
In un secondo terminale:
~~~powershell
Set-Location src/frontend/web
npm install
npm run dev
~~~
Aprire http://127.0.0.1:5173. Vite inoltra /api al backend sulla porta 5080.
Swagger: http://127.0.0.1:5080/swagger, solo Development.
GET /health restituisce {"status":"healthy"}: e una verifica del processo,
non della disponibilita del database.
Nessuna migrazione o creazione di studio automatica all'avvio.

Le API dei dati esistono **solo** in Development e con DevelopmentTenant__Id
valido e non vuoto configurato sul server. In Production/Staging non vengono
registrate, anche se la variabile e presente. GET /api/session espone solo lo
stato della configurazione; nessun dato dello studio.
Header, URL e body del client non possono selezionare il tenant.
Questa modalita e destinata a dati sintetici su localhost: non c'e autenticazione.
Tenant non inizializzato o database indisponibile vengono segnalati nell'interfaccia.

## Uso
1. Condomini: crea e modifica l'anagrafica, poi Apri per gestire strutture e unita.
   Edificio e scala sono facoltativi; le scale possono appartenere a un edificio
   oppure direttamente al condominio. La disattivazione conserva i dati.
2. Anagrafiche: crea persone fisiche/giuridiche. Dal dettaglio condominio associa
   soggetti alle unita con ruolo, validita e quota frazionaria per i proprietari.
   Per un passaggio di proprieta, concludi il vecchio legame e creane uno nuovo.
   Le date finali sono inclusive: il nuovo legame dello stesso soggetto/ruolo
   inizia dal giorno seguente per non sovrapporsi.
3. Tabelle millesimali: seleziona un condominio, crea una bozza, inserisci e salva
   le quote. Il server mostra totale e unita mancanti. Zero e un valore esplicito.
   Attiva solo dopo aver raggiunto il denominatore esatto e coperto ogni unita
   attiva. Attivazione rende configurazione e quote immutabili.
4. Esercizi: crea un intervallo libero anche non solare, modifica la bozza,
   apri e infine chiudi. Date e denominazione si modificano solo in Draft.
   La chiusura e definitiva; non ci sono riapertura, bilancio o riparti.

Codici univoci nello studio o nel condominio, case-sensitive e senza spazi agli
estremi. Codici fiscali normalizzati maiuscoli e univoci nello studio quando
presenti. Nessuna verifica fiscale esterna o algoritmo nazionale implicito.
Un codice disattivato rimane riservato per conservare identita e storico.

## Build e test
~~~powershell
dotnet build
dotnet test
Push-Location src/frontend/web
npm install
npm run build
npm run lint
Pop-Location
~~~
I test PostgreSQL vengono esplicitamente ignorati senza
CONDOMANAGER_TEST_POSTGRES. Per eseguirli tutti su un database temporaneo isolato:
~~~powershell
.scriptsVerify-Postgres.ps1
# Aggiunge il flusso browser reale con Microsoft Edge:
.scriptsVerify-Postgres.ps1 -Browser
~~~
Richiede tool restore e npm install gia eseguiti. Porta test default 55439,
modificabile con -Port. Lo script genera credenziali casuali solo in memoria,
usa un progetto Compose univoco, applica le migrazioni e controlla lo snapshot EF.
Esegue test di dominio/API/PostgreSQL, inclusa la migrazione di dati Milestone 01,
e opzionalmente crea un tenant isolato per il browser. API e Vite vengono avviati
dal runner browser su 5080/5173: queste porte devono essere libere.
Rimuove solo container/network/volume del proprio progetto temporaneo, anche
in caso di errore, e ripristina l'ambiente del processo chiamante.
Non puntare CONDOMANAGER_TEST_POSTGRES a database di lavoro: i test inseriscono
dati sintetici e creano un database aggiuntivo per verificare l'upgrade.
Trace Playwright di eventuali errori in src/frontend/web/test-results (ignorato da Git).

## Migrazioni e compatibilita
- 20261105220532_InitialFoundation (Milestone 01, invariata).
- 20261105222055_RegistryMilestone02: nuovi campi e scale, ruoli storici,
  stati/denominatore, indici, vincoli e trigger PostgreSQL.
  Dettagli SQL in RegistryMilestone02.Constraints.cs.

La nuova migrazione conserva tabelle, UUID e relazioni esistenti; richiede
l'estensione standard PostgreSQL btree_gist e un ruolo autorizzato a crearla.
E forward-only: il downgrade e esplicitamente vietato per evitare perdita dati.
Se dati storici hanno intervalli sovrapposti, la migrazione fallisce con rollback:
occorre concordarne la correzione, non vengono cancellati o riscritti.
Le anagrafiche legacy con campi non disponibili rimangono da completare; non sono
inventati indirizzi, codici fiscali o tipo di soggetto.

## Documentazione e limiti
[Architettura](docs/architecture/overview.md),
[modello e decisioni](docs/architecture/milestone-02-data-model.md),
[ADR tenant](docs/decisions/0002-development-tenant.md),
[glossario](docs/domain/glossary.md),
[API](docs/architecture/api.md),
[esiti delle verifiche](docs/architecture/milestone-02-verification.md).

Nessun motore contabile, calcolo fiscale, bilancio o riparto.
L'isolamento applicativo EF non sostituisce RLS; SQL e operazioni bulk sono
riservati alle migrazioni, ai test e al lock parametrizzato dei servizi.
Autenticazione, membership e distribuzione SaaS restano da implementare.
Nessun commit automatico e nessuna modifica all'indice Git.

Milestone 03 consigliata: autenticazione verificata, ruoli/membership degli studi,
provisioning controllato e autorizzazione end-to-end prima di esporre le API
fuori da Development.