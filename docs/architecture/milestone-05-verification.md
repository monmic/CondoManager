# Verifica Milestone 05 — 6 novembre 2026

## Baseline e conservazione
AGENTS.md letto integralmente; README, architettura, modello M04, DbContext,
servizi/API, frontend e test analizzati. Baseline eseguita su PostgreSQL 16
isolato: 64 test superati (23 unitari + 41 integrazione), build backend senza
warning/errori, snapshot migrazioni allineato, build/lint frontend superati.

Conservate le due cancellazioni UnitTest1.cs gia presenti nell'indice. AGENTS.md
non modificato; nessun commit/push/reset. Nessun database di lavoro usato,
cancellato o ricreato. Nessuna dipendenza aggiunta o progetto duplicato.

## Esiti effettivi
Ambiente Windows, SDK 10.0.403 / target net8.0, Node 24, PostgreSQL 16 Docker,
Edge con certificato HTTPS locale gia disponibile. Nessuna modifica alla fiducia
nel certificato di sistema.

| Comando / verifica | Esito |
| --- | --- |
| dotnet build | OK, 0 warning / 0 errori |
| dotnet test nell'harness PostgreSQL isolato | 96 superati: 42 unitari + 54 integrazione; 0 ignorati |
| dotnet test tests/CondoManager.UnitTests --no-build | 42 superati anche dopo l'ultimo controllo sugli avvisi indipendenti dalla cultura |
| npm install in src/frontend/web | OK, audit 0 vulnerabilita |
| npm run build | OK, TypeScript/Vite |
| npm run lint | OK |
| dotnet ef database update --project src/backend/CondoManager.Infrastructure | Applicate tutte le cinque migrazioni nel database temporaneo |
| dotnet ef migrations has-pending-model-changes --project src/backend/CondoManager.Infrastructure | Nessuna modifica pendente |
| scripts/Verify-Postgres.ps1 -Browser | OK: 4 test browser reali, bootstrap CLI ripetuto |
| dotnet ef migrations script M04 M05 --no-build | SQL generato e ispezionato: nessun DROP TABLE, TRUNCATE o DELETE FROM |
| Parser PowerShell di Verify-Postgres.ps1 | 0 errori |
| git diff --check | OK; soli avvisi locali LF/CRLF |

Migrazione: **20261106103327_ExpenseAllocationMilestone05**, con partial
ExpenseAllocationMilestone05.Constraints per i vincoli PostgreSQL. Nove tabelle
nuove, FK/indici/check/trigger, nessuna alterazione delle registrazioni M04.
Down rifiutato. Script locale ignorato: .artifacts/milestone-05.sql.
L'upgrade con dati sintetici M01 attraversa anche M05 conservando UUID e storico.
Dopo l'ultimo affinamento del testo degli avvisi per la cultura, rieseguiti build
e tutti i test unitari; nessun cambiamento a schema, API o formule monetarie.

## Scenari verificati
1. Caso A: 1000 EUR, millesimi 500/300/200 -> 500/300/200.
2. Gruppo delle sole unita della scala A (due unita su tre).
3. Esclusione esplicita e normalizzazione ammessa soltanto se approvata.
4. Caso B: parti uguali 100 -> 33,34/33,33/33,33.
5. Maggiori resti deterministici; input riordinato e cultura fr-FR/en-US.
6. Caso D: 0,01 EUR tra tre unita, nessun importo negativo o centesimo perso.
7. Caso C: 30/70 e consumi 10/20/40 -> 250/290/460.
8. Percentuali 99/101/0 rifiutate.
9. Consumi da iniziale/finale e pesi usati nello snapshot.
10. Letture mancanti: nessun consumo inventato, anomalia esplicita.
11. Letture incoerenti, consumo negativo e contesti misti rifiutati.
12. Sostituzione contatore con due segmenti contigui.
13. Addebito diretto a unita.
14. Addebito al soggetto storico corretto, proprietario attuale non scelto.
15. Risultati e dati storicizzati, regole attive e righe immutabili.
16. Stessi input danno lo stesso risultato; retry di conferma stesso UUID.
17. Doppia conferma negata; richieste concorrenti non duplicano il budget.
18. Modifica gruppo/lettura tra preview e conferma: richiesta rifiutata.
19. Esercizio chiuso blocca nuovi calcoli/letture, ma non lettura o retry storico.
20. Accessi/relazioni cross-tenant negati; anonimo/Viewer/CSRF verificati.
21. Accessi e associazioni cross-condominio dello stesso studio negati.
22. Quadratura esatta per importi decimali, millesimi non interi, percentuali
    frazionarie, budget dei componenti e importo EUR massimo.

Ulteriori verifiche:
- Priorita categoria/documento/quota e porzioni multiple dello stesso documento.
- Fonte gia completamente allocata rifiutata, nessun nuovo movimento contabile.
- Correzione lettura append-only e stima evidenziata con motivo.
- Sostituzione v2 conserva v1 e non consuma due volte le fonti.
- Modifica regola attiva crea una bozza nuova, senza riscrivere gli snapshot.
- PostgreSQL rifiuta UPDATE/DELETE di storico e letture; testata incompleta
  fallisce al commit e viene interamente annullata.
- Annullamento contabile successivo segnala Fonte annullata nello storico,
  conservando le quote originarie senza generare crediti.
- Tutti i test precedenti restano operativi con Identity e CSRF reali.

## Browser, senza API mock
Nuovo allocations.spec.ts: login, condominio/unita/esercizio, tabella attiva,
documento M04 registrato, gruppo, due regole approvate, sei letture iniziali/finali,
preview di 100 EUR a parti uguali, preview misto 1000 EUR con quote 250/290/460,
conferma e storico persistito dopo reload. Differenza zero verificata dalla UI.

Restano superati accounting.spec.ts, identity.spec.ts e registry.spec.ts.
HTTPS/cookie/CSRF reali; trace auth disabilitate. Screenshot desktop/mobile M05
prodotti e ispezionati: tabelle scorrevoli e nessun overflow della pagina a 390px.
Durante lo sviluppo corretti i selettori del nuovo test per etichette con help
text e quote millesimali mostrate a sei decimali, senza indebolire le verifiche.

## Risorse e limiti
Harness con progetto Compose univoco cm-m05-check-*, password casuale solo in
memoria, risorse rimosse nel finally e ambiente del processo ripristinato.
Nessun listener API di test rimasto sulla porta 7262. Nessuna credenziale inserita
nei nuovi file o nei test, soltanto dati sintetici.

Nessun impedimento esterno per le verifiche M05. SMTP/deployment produzione restano
prerequisiti M03. Nessun calcolo normativo, prorata automatico, telelettura multipla,
credito o rata introdotto. Coppia iniziale/finale corrente per contatore/tipo/
esercizio; fonti valide sull'intera competenza e soltanto esercizi aperti.
Liste non paginate e dimensioni limitate esplicitamente nel modello.

Proposta M06: preventivi/consuntivi, piano rate e rilevazione crediti da riparti
approvati, ricontrollando fonti e stato delle sostituzioni. M06 non iniziata.
Avvisi npm min-release-age e Node NO_COLOR/FORCE_COLOR sono di ambiente,
senza errori di build, warning del codice o vulnerabilita npm rilevate.
