# Verifica Milestone 04 — 6 novembre 2026

## Baseline e perimetro
AGENTS.md letto integralmente, README, requisiti, architettura, modello e verifica
M03 analizzati prima dell'implementazione. Baseline effettiva: 39 test backend su
PostgreSQL (10 unitari + 29 integrazione), build backend e frontend, lint e snapshot
migrazioni superati. Nessun progetto ricreato e nessun pacchetto aggiunto.

Conservate le due cancellazioni preesistenti UnitTest1.cs presenti nell'indice.
Nessun commit/push o modifica AGENTS.md. Nessun database di lavoro utilizzato,
cancellato o ricreato; solo progetti Compose temporanei univoci dell'harness.

## Verifiche effettive finali
Windows, SDK 10.0.403 / target net8.0, Node 24, PostgreSQL 16 Docker, Edge.

| Verifica / comando | Esito |
| --- | --- |
| dotnet build, tramite harness | OK, 0 warning / 0 errori |
| dotnet test con PostgreSQL isolato | 64 superati: 23 unitari + 41 integrazione, nessuno ignorato |
| npm install nella directory web | OK, audit 0 vulnerabilita |
| npm run build | OK, TypeScript e Vite |
| npm run lint | OK |
| dotnet ef database update --project src/backend/CondoManager.Infrastructure | Quattro migrazioni applicate su DB temporaneo |
| dotnet ef migrations has-pending-model-changes --project src/backend/CondoManager.Infrastructure | Nessuna modifica pendente |
| scripts/Verify-Postgres.ps1 -Browser | OK, 3 test browser reali, bootstrap CLI ripetuto |

Migrazione generata **20261106091836_AccountingCoreMilestone04**, con partial
Constraints per i trigger PostgreSQL. Up additivo, Down rifiutato. Lo snapshot
include il transaction ID generato dal DB. Verificato anche l'upgrade dei dati
sintetici M01 attraverso tutte le migrazioni senza cambiare UUID/storico.

## Copertura contabile aggiunta
- Piano predefinito atomico con la creazione del condominio, inizializzazione
  ripetibile, codici univoci/normalizzati e gerarchia senza cicli.
- Fornitore creato/modificato/disattivato/ricercato nello studio corretto.
- Fattura con IVA e IVA zero; importi incoerenti e precisione eccessiva rifiutati.
- Scritture bilanciate e immutabili; scritture sbilanciate rifiutate.
- Pagamento totale, parziale, multiplo, residuo esatto al centesimo, sovrapagamento
  negato, stessa chiave restituisce lo stesso UUID, payload diverso rifiutato.
- Pagamenti concorrenti serializzati: nessun eccesso, retry concorrente non duplica.
- Storno pagamento e documento preservano gli originali, doppio storno negato,
  pagamento attivo impedisce lo storno della fattura.
- Esercizio chiuso blocca nuove registrazioni; correzione in esercizio successivo
  aperto; retry di un pagamento gia registrato dopo la chiusura non duplica.
- Saldi positivi/negativi, apertura zero idempotente, incasso non attribuito,
  rettifica apertura con storno e nuova scrittura senza riscrivere lo storico.
- Collaborator non puo stornare o usare Saldi di apertura per rettifiche;
  Viewer non scrive; anonimo non legge; precondizione tenant obsoleta negata.
- ID e relazioni cross-tenant e cross-condominio negati, anche per pagamenti.
- SQL diretto: inserimento sbilanciato fallisce al commit con rollback; aggiunte
  tardive di righe, modifiche/cancellazioni del giornale e modifica saldo iniziale
  vengono rifiutate dai trigger.
- Duplicato conto finanziario: errore di unicita nel salvataggio annulla anche
  conto contabile, scrittura e audit della nuova operazione.
- Tutti i test M01/M02/M03 restano attivi con autenticazione reale e CSRF.

## Browser reale
Nuovo test accounting.spec.ts, senza API mock:
1. Login, creazione condominio ed esercizio aperto, piano predefinito visibile.
2. Creazione fornitore e banca con apertura 500 EUR.
3. Fattura 100 + 22 = 122, registrazione e due pagamenti 22,01 / 99,99.
4. Storico pagamenti, residuo zero e saldo 378 EUR.
5. Incasso generico 200, saldo 578 EUR e conto transitorio visibile.
6. Storno autorizzato incasso, saldo nuovamente 378 EUR, originali conservati.
7. Reload con persistenza; screenshot desktop/mobile e pagina senza overflow
   orizzontale a 390px. Screenshot ispezionati; tabelle scorrevoli sul mobile.

Restano superati i due test browser M03 su invito/Viewer/revoca/logout e su
anagrafiche/associazioni/quote/esercizi. HTTPS, cookie e CSRF effettivi.
Trace auth disabilitate. Nessuna credenziale o token incorporato nei test.

## Correzioni durante le verifiche
Il primo controllo di aggiunta righe usava xmin, incompatibile con i savepoint EF.
Sostituito con txid_current() persistito dal DB; il test continua a negare aggiunte
in transazioni successive. Nessuna protezione disattivata per far passare i test.
Corrette le etichette dei selettori nel nuovo test browser. Un tentativo di build
con il server del test ancora aperto ha incontrato il lock delle DLL Windows;
la verifica finale completa, a server fermo prima della build, passa senza avvisi.

## Limiti e prossimi passi
Nessun impedimento esterno per la verifica contabile. SMTP reale e deployment
produzione restano prerequisiti M03 e non vengono simulati o riconfigurati.
Nessuna paginazione del registro, riporto automatico, riconciliazione o liquidazione
IVA. Riferimenti liberi non univoci: per retry usare la stessa RequestId.
Proposta M05: legame categorie/tabelle, riparti decimal con arrotondamenti espliciti,
storico versioni approvate e conguagli. M05 non iniziata.

Avvisi npm min-release-age e Node NO_COLOR/FORCE_COLOR del runner sono di ambiente;
nessun warning del codice o vulnerabilita npm rilevati. Harness termina l'API e
rimuove soltanto container/rete/volume temporanei da esso creati, ripristinando
le variabili del proprio processo.

## Verifiche documentali finali
Script SQL da M03 a M04 generato con dotnet ef migrations script --no-build e
ispezionato: nessun DROP TABLE, TRUNCATE o DELETE FROM. Artefatto locale ignorato
in .artifacts/milestone-04.sql; lo schema versionato resta nelle migration EF.
Parser PowerShell: nessun errore. git diff --check: superato; avvisi locali sulla
normalizzazione LF/CRLF non sono errori di whitespace. Controllati i nuovi file:
nessuna password, token o connection string con credenziali introdotta.
Nessun listener dell'API di test rimasto sulla porta 7262 dopo l'harness.
