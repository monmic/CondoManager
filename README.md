# CondoManager — Milestone 04
Modular monolith net8.0, ASP.NET Core Identity, EF Core/Npgsql 8, PostgreSQL 16,
React/TypeScript/Vite. Supporta account condivisi tra piu studi tramite membership.
Anagrafiche e Identity rimangono operative; M04 aggiunge contabilita a partita doppia, documenti, pagamenti e saldi.

## Prerequisiti Windows
Windows PowerShell 5.1 o PowerShell 7, SDK con supporto .slnx (9.0.200+;
verificato con 10.0.403), runtime ASP.NET Core 8, Node 24/npm, Docker Desktop.
Microsoft Edge per il test browser opzionale. Tutti i progetti restano net8.0.
Leggere AGENTS.md prima di contribuire; nessun commit automatico.

## Database e migrazioni
Dalla radice:
~~~powershell
dotnet tool restore
$env:POSTGRES_USER = 'condomanager'
$credential = Get-Credential -UserName $env:POSTGRES_USER -Message 'Password PostgreSQL locale'
$env:POSTGRES_PASSWORD = $credential.GetNetworkCredential().Password
$env:ConnectionStrings__CondoManager = "Host=127.0.0.1;Port=5432;Database=condomanager;Username=$env:POSTGRES_USER;Password=$env:POSTGRES_PASSWORD"
docker compose up -d --wait
dotnet ef database update --project src/backend/CondoManager.Infrastructure
~~~
Porta PostgreSQL pubblicata solo su loopback; POSTGRES_PORT puo modificarla.
Aggiornare anche la connection string. La password di un utente PostgreSQL
gia creato non cambia semplicemente modificando l'ambiente.
Arresto conservativo: docker compose stop. Non usare down -v sul database di lavoro.

Migrazioni:
- 20261105220532_InitialFoundation.
- 20261105222055_RegistryMilestone02, con btree_gist e trigger contabili.
- 20261105233533_IdentitySecurityMilestone03: solo nuove tabelle/indici/FK Identity,
  membership, sessioni, grant e audit. Non altera o cancella dati delle anagrafiche.

Applicazione delle migrazioni sempre esplicita; nessun reset o seed all'avvio.
M02/M03 sono forward-only: downgrade vietato per evitare perdita di dati.
Usare un ruolo di migrazione separato dal ruolo runtime in produzione.

## Primo amministratore e primo studio
Il bootstrap e un comando locale esplicito: l'API HTTP non viene avviata.
Dopo la migrazione, scegliere l'UUID del primo studio. Per dati preesistenti,
utilizzare il TenantId e il Code gia esistenti senza ricreare il database.
~~~powershell
$studioId = [Guid]::NewGuid() # solo per un NUOVO studio; conservare questo UUID
.\scripts\Bootstrap-Identity.ps1 -TenantId $studioId -Email 'amministratore@example.invalid' -TenantCode 'STUDIO' -TenantName 'Il mio studio'
~~~
Sostituire l'email di esempio con quella controllata dall'amministratore.
Lo script chiede la password con prompt protetto: 12-128 caratteri, maiuscole,
minuscole, numero e simbolo. Non passare password in argomenti di shell.
Le variabili Bootstrap__* esistono solo durante il comando e sono ripristinate.
L'account riceve PlatformAdmin globale e una membership StudioAdmin ESPLICITA
nel solo studio indicato. Il ruolo globale da solo non autorizza dati degli studi.

Ripetere con gli stessi UUID/codice/email non duplica account o membership e non
reimposta password esistenti. Account disabilitati o membership esistenti diverse
non vengono sovrascritti automaticamente. Ogni esecuzione e registrata in audit.
Il bootstrap non offre un endpoint pubblico e non e eseguito all'avvio normale.

DevelopmentTenant__Id non ha piu alcun effetto, in qualsiasi ambiente.
Initialize-DevelopmentTenant.ps1 e disabilitato e rimanda al bootstrap Identity.

## Avvio e verifica login
Compilare la SPA e avviare l'API HTTPS nello stesso terminale configurato:
~~~powershell
Push-Location src/frontend/web
npm install
npm run build
Pop-Location
dotnet dev-certs https --check
# Solo se necessario, l'operatore configura/considera attendibile il certificato locale:
# dotnet dev-certs https --trust
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/backend/CondoManager.Api --no-launch-profile --urls https://localhost:7262
~~~
Aprire https://localhost:7262 e accedere con l'account del bootstrap.
In Development la SPA compilata viene servita dalla directory dist del frontend.
Frontend__RootPath puo indicare un percorso assoluto alternativo; in produzione
deve essere configurato esplicitamente. Non servire la pagina di login via HTTP.

Verifiche manuali: login, studio attivo nell'intestazione, consultazione condomini,
Account/cambio password, Utenti studio/inviti e logout. Con piu membership viene
richiesta la selezione di uno studio. Viewer vede solo azioni di consultazione;
StudioAdmin gestisce gli accessi del proprio studio. PlatformAdmin gestisce lo
stato globale degli account, senza accesso implicito ai loro dati condominiali.

Le API /api rifiutano HTTP. GET /health rimane pubblico e verifica il processo,
non PostgreSQL. Swagger solo Development e autenticato.
Non ci sono CORS permissivi: SPA e API operano sulla stessa origine.

### Vite con hot reload (facoltativo)
Il percorso principale verificato e la SPA compilata servita dall'API.
Per Vite HTTPS, esportare il proprio certificato locale in una cartella ignorata:
~~~powershell
New-Item -ItemType Directory -Force .artifacts | Out-Null
# Impostare DEV_HTTPS_PFX_PASSWORD nell'ambiente con un valore locale segreto.
dotnet dev-certs https --export-path .artifacts/devcert.pfx --password $env:DEV_HTTPS_PFX_PASSWORD
$env:DEV_HTTPS_PFX_PATH = (Resolve-Path .artifacts/devcert.pfx).Path
Push-Location src/frontend/web
npm run dev
Pop-Location
~~~
Vite richiede certificato/password e inoltra /api a https://localhost:7262.
La verifica del certificato del solo proxy locale Vite e disabilitata per il
certificato self-signed; questo non e un'impostazione di produzione.
Non versionare certificati privati o file .env. La build non richiede certificati.

## Inviti, recupero password ed email
Inviti: token casuale monouso, 48 ore; reset: token casuale monouso, 30 minuti.
Nel database sono conservati solo hash SHA-256. I link usano frammenti URL,
rimossi subito dal frontend, senza token in query string o localStorage.
Un account esistente deve autenticarsi con la stessa email per accettare
un invito; l'invito non puo sostituire o reimpostare la sua password.

Configurazione SMTP tramite ambiente (user secrets richiede prima l'inizializzazione locale del progetto):
Smtp__Host, Smtp__Port (default 587), Smtp__From,
Smtp__Username e Smtp__Password se necessari.
TLS obbligatorio; Security__PublicWebUrl deve essere l'URL HTTPS attendibile
dell'app, senza query o frammenti. Non viene costruito dall'header Host.
SMTP richiede credenziali esterne: la consegna verso un provider reale non e
verificabile senza la relativa configurazione.

Senza SMTP, solo Development usa una casella IN MEMORIA:
- StudioAdmin vede, in Utenti studio, gli inviti emessi DA LUI nello studio attivo.
- Ogni utente vede in Account solo i link di reset destinati alla PROPRIA email.
- Un utente disconnesso non puo leggere la casella: per recupero reale usare SMTP.
- Il riavvio perde i messaggi, non i grant nel DB; richiedere un nuovo link.
- Nessun endpoint della casella esiste in Production/Staging.
- Nessun token viene scritto nei log o restituito dall'API normale degli inviti.

In produzione senza SMTP, richieste di invito/reset rispondono 503 in modo
uniforme; non viene pubblicato un token o simulata una consegna email.

## Sessioni, autorizzazione e produzione
Cookie __Host-CondoManager: Secure, HttpOnly, SameSite=Lax, Path=/, senza Domain.
Scadenza assoluta di 8 ore, senza rinnovo automatico e senza remember-me.
Ogni richiesta verifica utente attivo, sessione DB non revocata/scaduta,
security stamp e membership attiva. Ruolo/tenant non sono autorita nel cookie.
Logout revoca la sessione anche contro replay del vecchio cookie.
Cambio/reset password e disabilitazione globale revocano tutte le sessioni.

Le mutazioni richiedono X-CSRF-TOKEN ottenuto da GET /api/auth/csrf.
Il cookie antiforgery e Secure/HttpOnly/SameSite=Strict; il token richiesta
rimane solo in memoria. Non sono usati JWT o token persistenti in localStorage.
Cambio ruolo/revoca membership valgono dalla richiesta successiva; operazioni
gia autorizzate e in corso non vengono annullate retroattivamente.

Prima di un deployment:
- Configurare AllowedHosts con i DNS effettivi (default localhost;127.0.0.1).
- Usare HTTPS end-to-end o configurare esplicitamente proxy conosciuti;
  non sono accettati automaticamente X-Forwarded-* non attendibili.
- Conservare il key ring Data Protection in modo persistente e protetto,
  condiviso dalle istanze dello stesso ambiente: DataProtection__KeyPath.
  Windows: DPAPI. Altri sistemi: DataProtection__CertificatePath e
  DataProtection__CertificatePassword; un key path non protetto e rifiutato
  fuori da Development su sistemi non Windows.
- Separare e limitare i ruoli DB runtime/migrazione, proteggere backup e SMTP.
- Il rate limiter IP e per processo (20 richieste auth/minuto); aggiungere
  limitazione condivisa nel punto di ingresso in un deployment multi-istanza.

Le verifiche applicative e le FK composite si aggiungono ai filtri EF.
Non e implementato PostgreSQL RLS: SQL diretto/bulk e key ring sono confini
privilegiati. Nessuna modalita di impersonazione/supporto automatico.
Account/MFA e policy organizzative aggiuntive restano possibili estensioni,
non sono parte del motore contabile della roadmap.

## Build e test
~~~powershell
dotnet build
dotnet test
Push-Location src/frontend/web
npm install
npm run build
npm run lint
Pop-Location
.\scripts\Verify-Postgres.ps1
.\scripts\Verify-Postgres.ps1 -Browser
~~~
Senza CONDOMANAGER_TEST_POSTGRES i test PostgreSQL sono esplicitamente ignorati.
L'harness crea un progetto Compose univoco, password solo in memoria, migra
lo schema e verifica lo snapshot. Non usare questa variabile su un database
di lavoro: i test inseriscono dati sintetici e creano un database di upgrade.
Con -Browser usa Edge e il certificato HTTPS di sviluppo gia installato, esegue
due volte il bootstrap, compila la SPA e avvia l'API su localhost:7262.
Porta DB test default 55439, modificabile con -Port; le porte devono essere libere.
Solo container/network/volume TEMPORANEI creati dall'harness vengono rimossi.
Ambiente ripristinato; trace di autenticazione disabilitate per non catturare segreti.

## Documentazione
[Architettura](docs/architecture/overview.md),
[Identity e sicurezza](docs/architecture/identity-security.md),
[API](docs/architecture/api.md),
[ADR autenticazione](docs/decisions/0003-identity-cookies.md),
[modello anagrafiche](docs/architecture/milestone-02-data-model.md),
[glossario](docs/domain/glossary.md),
[esito verifiche M03](docs/architecture/milestone-03-verification.md).


## Contabilita — Milestone 04
Migrazione nuova: **20261106091836_AccountingCoreMilestone04**. Solo nuove tabelle,
indici/trigger e una chiave alternativa sugli esercizi: nessuna perdita di dati.
Down vietato. Non serve ricreare il database o ripetere il bootstrap M03.
Dopo aver impostato la connection string come sopra:
~~~powershell
docker compose up -d --wait
dotnet ef database update --project src/backend/CondoManager.Infrastructure
Push-Location src/frontend/web
npm install
npm run build
Pop-Location
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/backend/CondoManager.Api --no-launch-profile --urls https://localhost:7262
~~~
Aprire https://localhost:7262 con un account dello studio. Il frontend compilato
viene servito dall'API; per Vite vedere la procedura HTTPS precedente.
Non applicare migrazioni mentre e in corso una contabilizzazione.

### Verifica dall'interfaccia
Usare un condominio di prova e dati sintetici:
1. Creare un condominio: il piano predefinito nasce nella stessa transazione.
   Per uno gia esistente: Contabilita > Piano dei conti > Completa piano predefinito.
2. In Esercizi contabili creare/aprire un intervallo che includa le date di prova.
3. In Contabilita selezionare il condominio; Fornitori > Nuovo fornitore.
4. Conti finanziari > Nuovo conto finanziario: codice contabile univoco, nome,
   esercizio/data e saldo iniziale, ad esempio 500 EUR sintetici. Il saldo genera
   una scrittura contro Saldi di apertura; non viene sommato di nuovo al registro.
5. Documenti di spesa > Nuova spesa: scegliere esercizio, fornitore e categoria,
   numero/data, imponibile 100, IVA 22, totale 122. Salvare la bozza, poi Registra.
6. Paga: scegliere conto, data e 22,01; verificare residuo 99,99. Registrare poi
   99,99 e verificare Pagato, residuo zero e disponibilita 378,00.
7. Aprire il dettaglio documento per storico pagamenti; Movimenti per Dare/Avere,
   dettaglio e filtri esercizio/conto/date/causale. Riepilogo mostra totali e saldi.
8. Conti finanziari > Registra incasso generico: 200 EUR aumentano banca/cassa e
   Incassi da attribuire; non costituiscono una rata pagata da un residente.
9. StudioAdmin: Storna dal registro, scegliendo data in un esercizio aperto e
   motivo. L'originale resta leggibile. Per annullare una fattura prima stornare
   tutti i pagamenti e poi la fattura. Per correzioni creare una nuova operazione.
10. Movimenti > Nuova scrittura manuale permette righe multiple con totali esatti;
    conti di controllo fornitori/incassi riservati ai flussi dedicati. Correzioni
    Saldi di apertura e storni richiedono StudioAdmin.

Con un esercizio selezionato saldi/debiti sono variazioni di QUEL periodo, senza
riporti automatici; disponibilita banca/cassa e sempre complessiva. OpeningBalance
e una dichiarazione storica, non un saldo modificabile. Tutti gli importi EUR
richiedono al massimo due decimali, senza arrotondamenti silenziosi.

Viewer consulta; StudioAdmin e Collaborator registrano operazioni; storni e
correzioni sul conto Saldi di apertura solo StudioAdmin. Ogni mutazione richiede
membership, cookie HTTPS e CSRF anche per /api/accounting. Il tenant non e input.
Nessuna nuova credenziale e nessuna dipendenza aggiunta per M04.

Operazioni contabili atomiche con chiavi di idempotenza, vincoli e trigger PostgreSQL.
In caso di rete incerta usare il retry della stessa operazione; prima di chiudere
il modulo e reinserire un pagamento verificare il registro: una nuova operazione
riceve una nuova chiave. Riferimenti bancari liberi non sono chiavi univoche.

[Modello e regole contabili](docs/architecture/milestone-04-accounting.md),
[ADR partita doppia](docs/decisions/0004-accounting-ledger.md),
[glossario contabile](docs/domain/accounting-glossary.md),
[verifiche M04](docs/architecture/milestone-04-verification.md).

Limiti: liste/registro non paginati, nessun riporto automatico, liquidazione IVA,
riconciliazione bancaria, OCR o allegati. Nessun riparto, conguaglio o rata.
Prossima milestone proposta: collegamento categorie/tabelle, riparti decimal,
criteri di arrotondamento e conguagli con versioni approvate; M05 non iniziata.