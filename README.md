# CondoManager — M07.3: verbali PDF e archivio documentale
Modular monolith net8.0, ASP.NET Core Identity, EF Core/Npgsql 8, PostgreSQL 16,
React/TypeScript/Vite. Supporta account condivisi tra piu studi tramite membership.
Anagrafiche, Identity e contabilita M04 rimangono operative. M05 aggiunge regole, consumi, simulazione e conferma dei riparti con snapshot immutabili.

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

## Ripartizioni — Milestone 05
Migrazione **20261106103327_ExpenseAllocationMilestone05**: nove nuove tabelle e
vincoli/trigger additivi. Nessun reset, dato ricreato o modifica ai movimenti M04.
Le configurazioni ambiente/HTTPS e l'account del bootstrap restano quelli sopra.
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
Aprire https://localhost:7262, accedere, scegliere Ripartizioni e il condominio.

### Verifica del motore dall'interfaccia
Usare esclusivamente dati sintetici in un condominio di prova:
1. Creare unita A, B, C ed esercizio Open 2026-01-01 / 2026-12-31.
2. In Tabelle millesimali creare una tabella con decorrenza 2026-01-01, nominale
   1000 e quote A=500, B=300, C=200; attivarla.
3. In Contabilita creare e registrare un documento di 1000 EUR, IVA zero, nello
   stesso esercizio. Il residuo da pagare non limita il costo da ripartire.
4. In Ripartizioni selezionare l'esercizio. Gruppi > Nuovo gruppo permette, per
   esempio, A e B: non serve che il gruppo coincida con una scala.
5. Regole > Nuova regola, tipo Parti uguali, peso 100%; salvare e attivare.
6. Regole > Nuova regola, tipo Mista: componente 1 Millesimi 30%, tabella attiva;
   componente 2 Consumi 70%, codice COLDWATER e misura M3. Salvare e attivare.
   Percentuali, tabelle, gruppi ed eventuale normalizzazione sono configurabili.
7. Consumi > Nuova lettura: per ogni unita inserire iniziale 100 al 2026-01-01;
   finali al 2026-12-31 A=110, B=120, C=140. Codice contatore distinto per unita.
   Verificare consumi 10/20/40. Mancanti, stime e rettifiche sono evidenziati.
8. Simulazione > Aggiungi quota di spesa: scegliere il documento, importo 100 e
   Parti uguali. Simulare: 33,34 / 33,33 / 33,33, differenza zero. Non confermare.
9. Nella stessa quota scegliere importo 1000 e regola Mista: simulare e verificare
   A=250,00 / B=290,00 / C=460,00. Espandere il dettaglio dei centesimi/denominatori.
10. Confermare dopo aver letto l'anteprima: lo storico conserva quote, autore,
    data, versione motore, regola, gruppi, tabelle e letture usate. Ricaricare la
    pagina e aprire Storico ripartizioni > Dettaglio riparto: risultati persistiti.
11. StudioAdmin puo preparare una sostituzione esplicita dall'ultima versione,
    risimulare e confermare. Stessi documenti/totali, nuovo UUID e versione +1;
    l'originale resta consultabile e non consuma due volte il budget.

Regole > Associa regola configura default di categoria e deroga sul documento.
La scelta sulla singola quota ha precedenza su entrambi. Si possono creare piu
quote dello stesso documento con criteri diversi; il totale non puo superare il
lordo ancora da ripartire. Le modifiche alle regole non riscrivono la contabilita.

Per gruppi/esclusioni millesimali con totale ammesso diverso dal nominale,
NormalizeAdmitted deve essere esplicito nel componente, prima dell'attivazione:
nessuna normalizzazione silenziosa. Gli addebiti diretti indicano unita/importi
e opzionalmente soggetto, con associazione storica valida per l'intera competenza.
Non viene scelto automaticamente il proprietario attuale.

Le letture sono append-only: Rettifica lettura richiede motivo e conserva la
precedente. Sostituzioni di contatore: codici distinti, finale del vecchio e
iniziale del nuovo nella stessa data. Nessuna stima automatica o consumo inventato.

Preview non scrive dati. Conferma ricarica le fonti e ne confronta l'impronta;
fonti cambiate richiedono una nuova simulazione. RequestId impedisce conferme
ripetute; stessa richiesta puo recuperare il risultato anche dopo la chiusura.
Viewer legge e simula, ruoli operativi configurano/confermano, sostituzioni solo
StudioAdmin. Cookie, CSRF, membership e isolamento M03 rimangono obbligatori.
Nessuna nuova credenziale o dipendenza esterna introdotta.

Verifica completa su PostgreSQL temporaneo e browser:
~~~powershell
.\scripts\Verify-Postgres.ps1 -Browser
~~~
Il database di lavoro non viene usato dall'harness; vengono rimosse solo le sue
risorse Compose temporanee. Per backend senza browser omettere -Browser.

[Modello e formule](docs/architecture/milestone-05-allocations.md),
[ADR arrotondamenti/snapshot](docs/decisions/0005-deterministic-allocations.md),
[glossario riparti](docs/domain/allocation-glossary.md),
[esito verifiche M05](docs/architecture/milestone-05-verification.md).

Limiti: nessun prorata temporale o algoritmo normativo automatico; una coppia
iniziale/finale corrente per contatore/tipo/esercizio, senza interpolazione.
Le fonti devono coprire l'intera competenza in un esercizio aperto. Liste non
paginate; limiti di dimensione espliciti nel modello. Nessun credito, rata,
preventivo, consuntivo o conguaglio generato da M05.
Proposta storica successiva a M05: preventivi/consuntivi, piano rate e rilevazione dei crediti
sulla base di riparti approvati e fonti ancora valide. Per lo stato corrente vedere M06.1 e M06.2 sotto.
Se una fonte viene annullata in contabilita dopo la conferma, lo storico la
segnala senza cambiare le quote originarie. Nessun credito o rata viene generato.

## Milestone 06.1 — incremento preventivi (riepilogo storico)
Preventivi versionati, proposte, simulazione e approvazione amministrativa con
snapshot M05, audit, autorizzazione e protezione PostgreSQL. Nuova sezione Preventivi.
Sono presenti inoltre calcolatori puri e testati per rate individuali e conguagli,
Il collegamento persistente di rate, crediti e incassi è aggiunto nella M06.2 sotto; consuntivi e chiusura restano M06.3.
Non usare il preventivo approvato come rilevazione di un credito.

Migration additiva: `20261106112924_AdministrativeCycleMilestone06`.
Applicazione manuale con `dotnet ef database update --project src/backend/CondoManager.Infrastructure`;
mai automatica all'avvio. Non eseguire reset del database di lavoro.

Procedura UI, API e limiti: [incremento M06](docs/architecture/milestone-06-administration.md).
Verifica isolata completa: `.\scripts\Verify-Postgres.ps1 -Browser`.
Per un controllo mirato: `-TestFilter 'FullyQualifiedName~BudgetApiTests'`.
Il filtro limita i test e non equivale alla verifica completa della milestone.

## M06.2 — Rate, crediti e incassi
Piani persistenti da preventivi approvati, debitori espliciti, emissione contabile,
incassi/attribuzioni, riconciliazione manuale e saldi individuali. Nessun saldo
aggiornabile manualmente e nessun movimento bancario duplicato in riconciliazione.

Migration: **20261106134439_InstallmentsReceiptsMilestone062**. Aggiunge sette tabelle,
vincoli/indici/trigger e amplia i check dei tipi contabili senza modificare le righe
preesistenti. Verificata solo sul database Docker temporaneo; nessuna applicazione
al database di lavoro da parte dell'agente. Per l'installazione l'operatore applica
esplicitamente le migration con la procedura già descritta, senza reset.

Avvio con ambiente e connection string già configurati:
~~~powershell
docker compose up -d --wait
# L'operatore applica esplicitamente le migration quando decide di aggiornare:
# dotnet ef database update --project src/backend/CondoManager.Infrastructure
npm run build --prefix src/frontend/web
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/backend/CondoManager.Api --no-launch-profile --urls https://localhost:7262
~~~

### Ciclo verificabile dalla UI
1. Preparare condominio, unità, persone con associazioni storiche ed esercizio Open.
2. In Preventivi creare/proporre/simulare/approvare il preventivo M06.1.
3. In **Rate e incassi > Piani rateali** creare il piano: preventivo corrente,
   data delle designazioni, scadenze ordinate e debitori espliciti (quote totali 1
   per unità). Scegliere rate uguali, percentuali o importi per scadenza.
4. Simulare, verificare quote e salvare Draft. Nessun credito è ancora emesso.
5. StudioAdmin: scegliere data di emissione ed **Approva ed emetti crediti**.
   In Rate e crediti verificare importi, scadenze, debitore originario e residui.
6. Incassi > Nuovo incasso: scegliere conto, esercizio/data, importo, eventuale
   debitore e pagante distinto. Senza RecordOnly viene creato un solo movimento.
7. Nel dettaglio indicare importi su una o più rate e confermare l'attribuzione.
   Esempio: rata 500, incasso/attribuzione 200 → pagato 200, residuo 300.
8. Per un incasso generico M04 già presente: registrare lo stesso importo/data/conto
   con **Da riconciliare con movimento esistente**; Riconciliazione > selezionare
   il movimento > Associa > Conferma riconciliazione. La banca non aumenta di nuovo.
9. Saldi mostra posizioni unità/debitore, riepiloghi separati, scaduto e fondi non
   attribuiti. Gli incassi non identificati restano distinti dai debiti individuali.
10. StudioAdmin può stornare un gruppo di attribuzioni con data/motivo e poi
    attribuire nuovamente, conservando scritture e dettagli originali.

Una nuova versione del piano mostra differenze; se esistono crediti già emessi,
l'emissione successiva è bloccata con messaggio esplicito. Non si modificano rate
pagate né si compensano automaticamente soggetti. I collegamenti bancari errati
non possono essere riassegnati silenziosamente. Riconciliazione solo esatta uno a uno.

[Modello e regole M06.2](docs/architecture/milestone-062-finance.md),
[convenzione contabile](docs/decisions/0007-resident-receivables.md),
[glossario](docs/domain/resident-finance-glossary.md).
Limite storico M06.2: consuntivi/conguagli/chiusura sono ora disponibili nella M06.3 descritta sotto; M07 non implementata.

Esiti effettivi: [rapporto M06.2](docs/architecture/milestone-062-verification.md).
## M06.3 — Consuntivi e chiusura amministrativa

Consuntivi versionati, confronto con il preventivo approvato, conguagli individuali,
aperture verificate e riporti idempotenti sono integrati nel giornale M04.
Le sezioni M06.1/M06.2 precedenti riportano anche i limiti storici di quegli incrementi;
lo stato attuale è descritto nel [modello M06.3](docs/architecture/milestone-063-final-statements.md).

La migration aggiunta è `20261106145113_FinalStatementsMilestone063`.
Non viene applicata automaticamente al database di lavoro. Il comando
`./scripts/Verify-Postgres.ps1 -Browser` verifica tutte le migration e i flussi
su un progetto Docker isolato, rimosso al termine. Richiede Docker Desktop e Edge.

Per verificare il ciclo dall'interfaccia, avviare database, bootstrap, API HTTPS e
frontend con i comandi di avvio documentati sopra, quindi:

1. Creare un esercizio aperto, unità e associazioni storiche dei soggetti.
2. Approvare un preventivo con riparto, emettere il piano rateale e attribuire gli incassi.
3. Registrare le spese effettive e confermare le relative ripartizioni nella sezione Ripartizioni.
4. In **Consuntivi → Saldi iniziali**, attestare esplicitamente zero, importare un saldo
   documentato oppure riportare la chiusura precedente. Nessun saldo assente è assunto zero.
5. Simulare il consuntivo, controllare fonti e anomalie, conservare la bozza, proporla
   e approvarla come StudioAdmin. Il confronto espone anche categorie presenti in un solo bilancio.
6. In **Conguagli**, eseguire anteprima, conferma e contabilizzazione. La scrittura
   registra solo la differenza rispetto ai crediti già emessi.
7. In **Chiusura esercizio**, aggiornare la checklist e confermare la chiusura autorizzata.
   Gli insoluti correttamente riconciliati restano crediti e non bloccano la chiusura.

Esempio sintetico verificato: preventivo 1.000 €, consuntivo 1.100 €, quote A/B/C
500/300/200 e 550/330/220 €. Conguagli +50/+30/+20 €. Con 400 € attribuiti ad A,
il saldo di A è 150 €; il saldo complessivo è 700 €. La nuova scrittura è di 100 €,
senza emettere di nuovo i 1.000 €. Un saldo negativo indica credito del soggetto.

Per le rettifiche non definite si applica un blocco esplicito: niente riaperture,
trasferimenti automatici a nuovi proprietari o riscritture dei documenti chiusi.
Le nuove differenze finali non generano automaticamente un secondo piano di riscossione.
Vedere [formule e limiti](docs/architecture/milestone-063-final-statements.md),
[ADR](docs/decisions/0008-final-statements-and-carry.md) e
[verifiche](docs/architecture/milestone-063-verification.md).

## M07.1 — Assemblee, convocazioni e presenze

La sezione **Assemblee** gestisce prima/seconda convocazione, ordine del giorno,
versioni M06 collegate, destinatari storicizzati, invii dichiarati manualmente,
deleghe verificate e presenze per sessione. Non invia email/PEC e non calcola quorum.

Migration aggiunta: `20261106162359_MeetingsMilestone071`. Nessun aggiornamento
automatico del database. Verifica riproducibile, solo su PostgreSQL isolato:

```powershell
.\scripts\Verify-Postgres.ps1 -Browser
```

Avvio API/frontend e configurazione Docker/Identity rimangono quelli documentati
sopra. Per verificare un'assemblea dall'interfaccia con dati sintetici:

1. Preparare unità, soggetti e associazioni storiche, più una tabella millesimale attiva.
2. Aprire **Assemblee**, selezionare il condominio e creare una nuova assemblea,
   indicando luogo e date ISO con fuso (es. `2026-12-10T07:00:00+01:00`).
   La seconda convocazione è facoltativa e distinta.
3. Inserire i punti ODG; collegare facoltativamente la versione esatta di un
   preventivo/consuntivo o un esercizio. Modificare o riordinare prima della convocazione.
4. In **Aventi diritto**, scegliere data di riferimento e tabella, ricostruire
   l'elenco e controllare anomalie e rappresentanti unitari. Per comproprietà scegliere
   esplicitamente il rappresentante; consolidare lo snapshot con un motivo.
5. In **Convocazioni**, verificare i recapiti e preparare un record per ogni destinatario.
   Ready/Sent/Delivered sono eventi manuali dichiarati, senza prova verificata implicita.
6. In **Assemblea**, programmare e consolidare la convocazione. In **Deleghe**,
   selezionare delegante, delegato e unità; presentare la delega e verificarla come
   StudioAdmin. Non si può usare una delega non verificata.
7. Registrare l'esito della prima sessione (avviata/terminata o non tenuta).
   Avviare esplicitamente la sessione interessata, quindi registrare presenze dirette,
   remote o tramite delega, eventuali assenze e uscite. Le quote sono informative.
8. Consultare **Storico**: snapshot precedenti e revisioni non vengono sovrascritti.
   Dopo un cambio titolarità, ricostruire l'anteprima e riconciliare esplicitamente
   prima dell'avvio, revocando prima le deleghe attive.

Esempio browser: unità A con 120 millesimi, titolare X rappresentato dal delegato Y;
prima convocazione non tenuta, presenza tramite delega verificata nella seconda.
Risultato: un presente effettivo, una unità e 120 millesimi informativi, senza duplicazioni.

[Modello e limiti](docs/architecture/milestone-071-meetings.md),
[ADR rappresentanza](docs/decisions/0009-meeting-representation.md),
[glossario](docs/domain/meetings-glossary.md),
[verifiche](docs/architecture/milestone-071-verification.md).
Limite storico M07.1: M07.2 e M07.3 sono ora descritte nelle sezioni successive.

## M07.2 — Quorum, votazioni e delibere

La pagina **Assemblee → Quorum e votazioni** gestisce criteri versionati,
quorum costitutivo per sessione, platee congelate, voti e loro revisioni,
delibere immutabili, rettifiche testuali, applicazione esplicita M06 e conclusione.
La maggioranza delle teste e quella del valore sono controlli separati, con
frazioni esatte. Approved significa soddisfazione del criterio selezionato,
non certificazione legale: leggere le [fonti e limitazioni](docs/architecture/milestone-072-legal-sources.md).

Migration: `20261106210126_VotingMilestone072`, dieci tabelle additive.
Nessuna migration automatica all'avvio o sul database di lavoro. Verifica isolata:

```powershell
.\scripts\Verify-Postgres.ps1 -Browser
# Solo il nuovo flusso, sempre con PostgreSQL isolato:
.\scripts\Verify-Postgres.ps1 -TestFilter 'FullyQualifiedName~Voting' -Browser -BrowserFilter voting.spec.ts
```

I comandi di avvio Docker, API HTTPS, bootstrap Identity e frontend restano quelli
documentati sopra. Procedura UI con dati sintetici:

1. Preparare l'assemblea M07.1: ODG con UUID della versione esatta del preventivo
   o consuntivo, snapshot anagrafico/millesimale, destinatari e dichiarazioni manuali
   di invio. Verificare deleghe scritte e registrare le presenze della sessione.
2. In **Criteri**, StudioAdmin crea una versione da modello numerico o configurazione
   esplicita; sceglie finalità, chiamata, materia, frazioni, denominatori e validità.
   La attiva con una nota di revisione dell'ambito. I template non sono attivati da soli.
3. In **Quorum**, scegliere sessione e criterio costitutivo, motivare verifica delle
   convocazioni, titolarità e deleghe. Eventuali esclusioni/scelte per materia vanno
   motivate. Conservare la verifica e risolvere le anomalie prima di deliberare.
4. In **Votazioni**, scegliere ODG, criterio e verifica costitutiva della stessa
   sessione. Aprire: la platea e le fonti M06 sono congelate. Registrare InFavor,
   Against o Abstained per testa rappresentata; modificare solo prima della chiusura.
   Assenti e presenti non votanti restano distinti.
5. Chiudere il voto. In **Delibere**, StudioAdmin consolida titolo/testo con l'esito
   calcolato. Una rettifica testuale crea una nuova versione con motivo, senza
   cambiare l'esito o i voti precedenti.
6. Per un bilancio Proposed applicare esplicitamente **Applica approvazione M06**.
   Per un bilancio già Approved scegliere il solo collegamento storico. Non vengono
   generate rate, crediti o scritture; completare questo passo prima della conclusione.
7. In **Conclusione**, classificare punti senza delibera come DiscussedWithoutVote
   o Deferred. Registrare le uscite in Presenze, aggiornare la checklist e confermare
   come StudioAdmin. Un voto aperto o una rappresentanza non determinabile blocca.

Caso Edge verificato: tre teste, valori 500/300/200 su 1000; favorevoli A e C,
contrario B. Con criterio prima ordinaria configurato, 2 > 3×1/2 e 700 >= 1000×1/2:
esito Approved. Il preventivo collegato da **106.733,48 €** viene approvato soltanto
con il comando M06, quindi l'assemblea viene conclusa. Il caso matematico con
4 favorevoli su 6 ma soltanto 280 millesimi non passa il requisito di metà del valore.

[Modello tecnico](docs/architecture/milestone-072-voting.md),
[ADR](docs/decisions/0010-voting-snapshots.md),
[glossario](docs/domain/voting-glossary.md),
[rapporto verifiche](docs/architecture/milestone-072-verification.md).
Limite storico M07.2, superato dalla M07.3 descritta sotto.

## M07.3 — Verbali PDF e archivio privato

La sezione **Assemblee → Verbale e documenti** prepara il verbale da uno snapshot
M07.2 concluso. **Documenti** apre l'archivio del condominio senza selezionare
un'assemblea. Accesso riservato a StudioAdmin/Collaborator; finalizzazione solo
StudioAdmin. Viewer non accede a file contenenti recapiti e altri dati personali.

1. Completare presenze, quorum, votazioni e delibere; applicare esplicitamente M06
   quando necessario, quindi concludere l'assemblea.
2. Aprire Verbale e documenti, creare una bozza e modificare introduzione,
   discussione per punti e chiusura.
3. Scaricare l'anteprima BOZZA e controllare i dati consolidati.
4. Verificare la checklist; StudioAdmin conferma Finalizza verbale.
5. Scaricare il PDF definitivo; l'archivio mostra dimensione, SHA-256 e collegamenti.
6. Per rettificare, indicare una motivazione e creare una nuova versione. Il PDF
   originale resta disponibile; voti, delibere e approvazioni M06 non cambiano.
7. Generare prospetti presenze/votazioni o un estratto usando l'ID della delibera;
   la convocazione PDF richiede l'ID della convocazione, non della persona.
8. Caricare allegati PDF/JPEG/PNG, con collegamenti opzionali validati dal backend.

Il PDF non è automaticamente firmato. L'archivio non è conservazione digitale a
norma. Non è attivo uno scanner antivirus; i controlli formato non lo sostituiscono.

Configurazione locale (directory privata ignorata da Git):

```powershell
$env:Documents__RootPath = Join-Path $PWD '.local\documents'
$env:Documents__MaxUploadBytes = '8388608'
dotnet run --project src/backend/CondoManager.Api
```

Migration additiva: `20261106223138_DocumentsMilestone073`.
L'applicazione al proprio database resta un'operazione esplicita dopo backup;
non viene eseguita all'avvio. Lo storage deve essere esterno alla directory SPA.

### Docker con volume documentale

Il compose predefinito resta PostgreSQL. L'override aggiunge API e SPA, storage e
key ring persistenti; richiede un certificato HTTPS PFX di cui l'utente controlli
l'accesso. Non esporre password nella cronologia: impostarle da prompt protetto o
secret manager. Configurare POSTGRES_USER/POSTGRES_PASSWORD come sopra, più:

```powershell
$env:HTTPS_CERT_DIRECTORY = Join-Path $PWD '.certs' # contiene https.pfx
$certCredential = Get-Credential -Message 'Password del certificato HTTPS PFX'
$env:HTTPS_CERT_PASSWORD = $certCredential.GetNetworkCredential().Password
docker compose -f compose.yaml -f compose.documents.yaml up -d --build
```

L'API risponde su https://localhost:7262. I volumi document_files e protection_keys
non hanno URL pubblici. Il bootstrap Identity è sempre esplicito; il container non
crea utenti o applica migration automaticamente. In produzione usare un certificato
attendibile e permessi minimi; il PFX protegge anche il key ring Data Protection.
Per aggiornare il database usare il comando EF esplicito della sezione iniziale
con la connection string locale corretta, senza reset. Per fermare: `docker compose
-f compose.yaml -f compose.documents.yaml stop` (senza eliminare volumi).

### Backup, ripristino e recupero

Arrestare le istanze API prima del backup consistente. Salvare insieme:
PostgreSQL (pg_dump formato custom), contenuto del volume document_files, key ring,
certificato necessario a decifrarlo e configurazione. Proteggere/cifrare i backup e
verificarne periodicamente il ripristino su ambiente isolato. Per la copia di un
volume usare un mount in sola lettura e una destinazione di backup protetta;
non usare redirezione binaria PowerShell 5.1 per pg_dump.

Ripristinare prima schema/dati e file della stessa generazione, poi key ring e
configurazione; verificare hash e disponibilità tramite download autorizzato prima
di riaprire l'accesso. Non ricreare il database di lavoro per una verifica.

Crash o commit DB ambiguo possono lasciare marker pendenti. In manutenzione, con
API ferme e configurazione storage/DB corretta:

```powershell
dotnet run --project src/backend/CondoManager.Api --no-launch-profile -- --document-storage-recover
```

Il comando tratta solo marker più vecchi di 24 ore, controlla il DB e non elimina
file definitivi referenziati. DB irraggiungibile o integrità errata interrompono il
recupero. Non è necessario eseguirlo normalmente dopo un upload riuscito.

Dettagli: [architettura documentale](docs/architecture/milestone-073-documents.md),
[glossario](docs/domain/documents.md), [ADR 0011](docs/decisions/0011-document-storage-pdf.md),
[verifica M07.3](docs/architecture/milestone-073-verification.md).
