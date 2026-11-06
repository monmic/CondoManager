# Verifica incremento M06 — 6 novembre 2026

M06 NON completata. Questo rapporto riguarda esclusivamente l'incremento preventivi.

## Baseline
AGENTS.md letto integralmente, README e documentazione architetturale analizzati.
Baseline su PostgreSQL Docker isolato: 42 unitari + 54 integrazione = 96 superati,
zero ignorati; build senza warning/errori, cinque migration applicate, modello EF
allineato. Modifiche M05 e stato Git preesistente conservati, nessun commit/push.

## Verifica finale dell'incremento
- `dotnet build`: superato, zero warning/errori.
- `dotnet test` nel database isolato: 52 unitari e 57 integrazione superati,
  zero ignorati (109 totali), prima dell'aggiunta dell'ultimo test mirato di rollback.
- `.\scripts\Verify-Postgres.ps1 -Browser`: superato; sei migration applicate,
  nessuna modifica modello pendente; cinque test Edge superati.
- `npm run build --prefix src/frontend/web`: superato.
- `npm run lint --prefix src/frontend/web`: superato.
- `dotnet ef migrations script ExpenseAllocationMilestone05 AdministrativeCycleMilestone06 ...`:
  script generato in .artifacts/milestone-06.sql, senza DROP TABLE/TRUNCATE/DELETE FROM
  e senza modifiche alle scritture M04.
- Parsing PowerShell: zero errori. `git diff --check`: superato.

Test aggiunti: workflow e immutabilità del preventivo; versioni con importi dello
scenario D; differenza +11.131,67; approvazione idempotente; concorrenza;
autorizzazione anonimo/Viewer/Collaborator; cross-tenant e cross-condominio;
fonti cambiate tra simulazione e approvazione; nessuna fattura/scrittura artificiale;
calcolo rate Scenario A e centesimi 100/3 e 0,01/3; percentuali; conguagli positivi,
negativi e zero; distinzione Scenario C tra +50 competenza e +150 finanziario.

Il test browser crea le fixture anagrafiche tramite API reali e percorre dalla UI
bozza, proposta, simulazione 100/3, approvazione e nuova versione a 110,00.
Verifica l'assenza di overflow orizzontale a 390 px. Screenshot desktop/mobile
in .artifacts, fuori dal versionamento; mobile ispezionato visivamente.

Errori intermedi corretti: alias SQL `old` ambiguo nel trigger e selezione
Playwright esatta del label contenente una select. Nessuna validazione indebolita.
Warning ambientali npm min-release-age e Node NO_COLOR/FORCE_COLOR non bloccanti.

## Ambiente e dati
Database Docker unico per ogni esecuzione; soltanto tali container/volumi sono stati
rimossi dal finally dello script. Nessuna migration sul database di lavoro.
Credenziali casuali in memoria e soli dati sintetici; nessun segreto aggiunto.

## Rimane da implementare per completare M06
Piani rateali persistenti e versionati; designazione storica dei debitori;
crediti nel giornale M04 e storni dei piani; incassi attribuiti e riconciliazione
senza duplicare banca; consuntivi/snapshot; rettifiche e saldi pregressi persistenti;
conguagli riconciliati; controlli di chiusura e relativi endpoint/UI/test completi.
Le funzioni pure per rate e formule non sostituiscono tali workflow.

Verifica mirata successiva: `.\scripts\Verify-Postgres.ps1 -TestFilter 'FullyQualifiedName~BudgetApiTests'`
completata con 4/4 test PostgreSQL superati, inclusa l'ultima verifica aggiunta:
modifica bozza, rollback dopo categoria non valida, esercizio chiuso.
Il totale dei test backend presenti è ora 110; eseguiti come suite completa da
109 più controllo mirato dei quattro test del modulo (tre già presenti e uno nuovo).
Nessun cambiamento al codice applicativo dopo la suite completa e i cinque browser.
