# Verifica Milestone 03 — 6 novembre 2026
## Stato iniziale e istruzioni
README, documentazione, codice e Git verificati prima delle modifiche.
Baseline M01/M02: 24 test backend su PostgreSQL, migrazioni e snapshot allineati,
frontend build/lint superati. AGENTS.md, aggiunto durante il lavoro, e stato
letto subito e applicato prima di proseguire. Non e stato modificato.
File gia staged e cancellazioni gia presenti nell'indice sono stati conservati.
Nessun commit, push, reset o pulizia Git.

## Esito finale effettivo
Ambiente Windows: SDK 10.0.403, target net8.0, Node 24.13.0,
PostgreSQL 16 in Docker, Edge e certificato HTTPS di sviluppo gia disponibile.
Non e stata modificata la fiducia del certificato nel sistema operativo.

| Comando / verifica | Esito |
| --- | --- |
| dotnet build | OK, 0 avvisi / 0 errori |
| dotnet test nel progetto Compose isolato | 39 superati: 10 unitari + 29 integrazione; nessuno ignorato |
| npm install | OK, audit 0 vulnerabilita |
| npm run build | OK |
| npm run lint | OK, nessun warning del codice |
| dotnet ef database update --project src/backend/CondoManager.Infrastructure | Applicate tutte e tre le migrazioni |
| dotnet ef migrations has-pending-model-changes --project src/backend/CondoManager.Infrastructure | Nessuna modifica pendente |
| scripts/Verify-Postgres.ps1 -Browser | OK, incluse CLI/bootstrap, API HTTPS, SPA compilata e 2 test browser |
| Bootstrap CLI ripetuto due volte | OK, account/ruolo/membership univoci, password non reimpostata |
| Parsing PowerShell degli script | OK |
| git diff --check | OK |

Migration generata: 20261105233533_IdentitySecurityMilestone03.
Up contiene solo creazione di tabelle, indici e FK della sicurezza; nessuna
eliminazione o alterazione delle tabelle condominiali. Down rifiutato.
Il test di upgrade con dati M01 attraversa M02/M03 e conserva UUID/storico.
Nessun database di lavoro e stato usato o ricreato.

## Copertura di sicurezza
- Login valido, password non valida e lockout Identity.
- Password debole rifiutata; account/email normalizzati tramite Identity.
- Accesso anonimo rifiutato in Development/Staging/Production.
- Impostazione legacy DevelopmentTenant__Id incapace di autorizzare richieste.
- Viewer puo leggere ma non mutare o gestire utenti.
- Un account con due membership sceglie studio e ruolo lato server.
- Cambio tenant non autorizzato rifiutato; header TenantId ignorati.
- Precondizione X-Expected-Tenant obsoleta produce 409 senza selezionare tenant.
- Lettura, modifica e relazioni cross-tenant negate.
- Modifiche e revoche membership rilevate alla richiesta successiva.
- Ultimo StudioAdmin protetto anche da declassamenti concorrenti.
- Inviti scaduti, riutilizzati, revocati o emessi da admin poi revocato respinti.
- Invito ad account esistente richiede login e non duplica account.
- Reset monouso/scaduto, cambio password e invalidazione delle sessioni.
- Cookie Secure/HttpOnly/Lax; CSRF richiesto anche su login/logout.
- Logout revoca il record di sessione; replay del vecchio cookie negato.
- Durata assoluta sessione verificata.
- PlatformAdmin senza membership non legge dati dello studio.
- Disable globale revoca le sessioni e protegge l'ultimo admin.
- Nessun inbox Development in produzione; SMTP assente produce 503 uniforme.
- Rate limit auth: 429.
- Ruolo omesso in un invito rifiutato, nessun default accidentale ad amministratore.
- Invarianti/decimal/esercizi e CRUD M02 rimangono verificati.

## Browser reale, senza mock o bypass auth
1. Invito da StudioAdmin, accettazione da nuovo account, login Viewer,
   pulsanti di modifica assenti, POST diretto con CSRF valido rifiutato dal SERVER,
   revoca membership, stato senza accessi, logout e /auth/me 401.
2. Login reale, creazione condominio/unita/soggetto, associazione storica,
   quote decimal e attivazione tabella, apertura/chiusura esercizio, reload e
   persistenza, controllo mobile a 390px.

Entrambi usano HTTPS, cookie e CSRF effettivi. L'unica eccezione TLS e
l'accettazione del certificato locale self-signed da parte del runner Edge,
non una disattivazione delle policy o della validazione delle credenziali.
Trace auth disabilitate; screenshot del flusso operativo prodotti e ispezionati.
Nessun JWT/localStorage, account o password reali nei test.

## Risorse e limiti
Solo progetti Compose temporanei univoci; container, rete e volumi rimossi
nel finally dopo le verifiche. Variabili del processo ripristinate.
L'harness avvia e termina l'API di test; non lascia servizi attivi.
Le migrazioni non vengono applicate automaticamente dall'applicazione.

SMTP verso provider esterno non eseguito: mancano credenziali esterne.
Il flusso di grant e testato col provider Development in memoria.
Il percorso browser verificato e la SPA compilata servita dall'API; Vite con
certificato esportato/hot reload e documentato come alternativa opzionale.

Per produzione occorrono DNS/AllowedHosts, certificati HTTPS, key ring Data
Protection persistente/protetto, SMTP e ruoli DB limitati.
Rate limiter per processo, da affiancare a un ingresso condiviso in multi-istanza.
Nessun PostgreSQL RLS o impersonazione/supporto automatico introdotti.
Revoche proteggono le nuove richieste, senza annullare transazioni gia autorizzate.

Avvisi ambiente: npm min-release-age e Node NO_COLOR/FORCE_COLOR del runner,
senza impatto su installazione, compilazione o test.

Milestone 04 proposta: piano dei conti, registrazione spese e movimenti.
Non e stata iniziata.