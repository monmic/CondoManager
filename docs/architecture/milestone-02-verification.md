# Verifica Milestone 02
Eseguita su Windows il 5 novembre 2026, dopo analisi del codice/documentazione
della Milestone 01 e verifica iniziale di build e test.
AGENTS.md non era presente. Git era inizializzato; le modifiche all'indice gia
presenti all'inizio sono state conservate. Nessun commit automatico.

Ambiente: SDK 10.0.403, target net8.0, runtime ASP.NET Core 8,
Node 24.13.0, npm 11.6.2, Docker Engine 29.4.0, PostgreSQL 16,
Playwright 1.65.0 e Microsoft Edge.

## Esito finale effettivo
| Verifica | Risultato |
| --- | --- |
| dotnet build | OK, 0 avvisi e 0 errori |
| dotnet test via harness PostgreSQL | OK: 8 unitari + 16 integrazione, nessuno ignorato |
| npm install | OK, 32 pacchetti verificati, audit 0 vulnerabilita |
| npm run build | OK |
| npm run lint | OK, nessun warning del codice |
| docker compose up -d --wait su progetto univoco | PostgreSQL healthy |
| dotnet ef database update | Applicate InitialFoundation e RegistryMilestone02 |
| dotnet ef migrations has-pending-model-changes | Nessuna modifica pendente |
| scripts/Verify-Postgres.ps1 -Browser | OK, incluso test browser reale |
| Playwright: anagrafiche, associazioni, millesimi ed esercizi | 1 test end-to-end passato |
| git diff --check | OK |

La verifica finale del browser attende esplicitamente il badge di tabella
attiva e l'assenza dei campi modificabili, non solo il clic sul pulsante.
Conferma poi apertura/chiusura dell'esercizio, dati conservati dopo reload e
assenza di overflow del body al viewport mobile 390px.

## Copertura
- Isolamento delle query per contesto, scritture senza tenant o per altro tenant.
- Tentativi di accesso via header e TenantId aggiunto al body respinti/ignorati:
  i dati rimangono nello studio server.
- API dati assenti in Production/Staging anche con tenant configurato.
- Development senza tenant: API dati assenti; UUID vuoto: startup rifiutato.
- Tenant UUID configurato ma non inizializzato: 503.
- CRUD condomini, audit, disattivazione logica, unicita dei codici nello studio.
- FK composite tra tenant; edifici, tre scale, unita diretta, gerarchia coerente.
- Unicita delle unita nel condominio, collegamenti ad altro condominio respinti.
- Associazioni storicizzate, fine inclusiva, nuova associazione senza perdita
  della precedente, rifiuto sovrapposizioni, ruoli senza quota proprietaria.
- Quote decimal: precisione, round trip PostgreSQL/HTTP, totale esatto,
  valori negativi o precisione eccessiva respinti, tabella incompleta non attivata.
- Attivazione valida, immutabilita della tabella e trigger sulle quote attive.
- Esercizi non solari, sovrapposizioni respinte anche dal vincolo SQL,
  richieste concorrenti con una sola vincitrice, chiusura definitiva.
- Trigger SQL impedisce anche modifiche dirette di esercizi chiusi.
- Migrazione da database Milestone 01 popolato: UUID, legami e quote conservati,
  campi legacy non inventati, persone Unspecified da completare.
- Browser senza mock: creazione condominio, due unita, soggetto,
  associazione, due quote, attivazione, creazione/apertura/chiusura esercizio.
- Screenshot desktop/mobile prodotti e ispezionati; artefatti in
  src/frontend/web/test-results, ignorati da Git.

## Sicurezza e risorse
Controllo dei riferimenti password/connection string nei sorgenti e script:
nessuna credenziale hardcoded; valori da ambiente o generati in memoria.
Nessun database di lavoro e stato usato o cancellato. Tutte le prove PostgreSQL
hanno utilizzato un progetto Compose temporaneo univoco; inclusa una seconda
base temporanea per il test di upgrade, rimossa con il relativo container.
Container/network/volume di verifica rimossi anche dopo gli errori intermedi.
API/Vite del browser terminati dal runner; non rimangono servizi di test avviati.

Avvisi dell'ambiente senza effetti sui risultati:
npm Unknown env config "min-release-age"; Node FORCE_COLOR/NO_COLOR del runner.
Il test browser usa msedge installato sul sistema, come documentato nel README.

## Limiti aperti
- Nessuna autenticazione: API operative esclusivamente Development/localhost.
- Nessun PostgreSQL RLS; migrazioni/test e lock SQL parametrizzati sono
  percorsi privilegiati fuori dalla barriera dei filtri EF.
- btree_gist richiede un ruolo autorizzato alla creazione dell'estensione.
- Dati legacy incompatibili con i vincoli di intervallo richiedono decisioni
  di correzione; la migrazione fallisce transazionalmente senza cancellarli.
- La validazione corrente di tabelle attive puo segnalare nuove unita mancanti:
  le vecchie quote non vengono riscritte; creare una nuova versione di tabella.
- Nessun bilancio, riparto, motore contabile, riapertura esercizi o deploy SaaS.

Milestone 03 proposta: identita verificata, membership e ruoli degli studi,
provisioning controllato e autorizzazione end-to-end. Non avviata.