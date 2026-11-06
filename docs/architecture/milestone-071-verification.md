# M07.1 — Verifiche del 6 novembre 2026

## Baseline verificato

`./scripts/Verify-Postgres.ps1`: 153 test backend (69 unitari, 84 PostgreSQL),
otto migration applicate, snapshot EF allineato, build zero warning/errori.
Le due voci preesistenti Git `AD .../UnitTest1.cs` non sono state modificate.
Nessun commit, push o modifica del database di lavoro.

## Comandi e risultati

| Comando | Risultato |
|---|---|
| `dotnet build` | Zero warning/errori |
| `dotnet test` nel harness isolato | 80 unitari + 92 integrazione PostgreSQL: 172 superati |
| `./scripts/Verify-Postgres.ps1 -TestFilter 'FullyQualifiedName~Meeting'` | 11 nuovi casi unitari e 8 integrazione superati |
| `dotnet ef database update --project src/backend/CondoManager.Infrastructure` nel harness | Nove migration applicate su volume isolato |
| `dotnet ef migrations has-pending-model-changes --project src/backend/CondoManager.Infrastructure` | Nessuna modifica pendente |
| `npm run build --prefix src/frontend/web` | TypeScript e Vite superati |
| `npm run lint --prefix src/frontend/web` | Nessun errore o warning oxlint |
| `git diff --check` | Nessun errore whitespace |

Migration finale: `20261106162359_MeetingsMilestone071`, dodici tabelle nuove,
FK e indici, vincoli e trigger di rappresentanza/storico. SQL generato tra M06.3
e M07.1 in `.artifacts/milestone-071.sql`: nessun DROP TABLE, TRUNCATE, DELETE
FROM o UPDATE dei dati esistenti. La rollback è esplicitamente bloccata per non
eliminare lo storico. Le precedenti migration non sono state cambiate.

Le prove usano PostgreSQL 16 Docker, .NET target net8.0 e Edge tramite Playwright.
Ogni harness crea un progetto Compose casuale e un volume proprio; solo queste
risorse sono rimosse al termine. Credenziali generate in memoria, non versionate.
L'avviso npm ambientale `min-release-age` e quello Node sui colori non incidono
su compilazione o test. Non sono stati aggiunti pacchetti.

## Copertura dei 34 scenari richiesti

Gli scenari sono raggruppati, non equivalgono a 34 nuovi test indipendenti.

| Scenari | Evidenza |
|---|---|
| 1–6: tipi, date, sessioni e stati | MeetingDomainTests e OrdinaryExtraordinaryDatesAndTwoDistinctCalls |
| 7–11: ODG, riordino, bilanci e scope | AgendaReordersAndKeepsBudgetAndFinalVersionReferences; versioni M06 non approvate automaticamente |
| 12–15: destinatari, convocazioni e storico | SnapshotOwnsOneUnitShareAndRemainsImmutableAfterRegistryChange, ManualNoticeEventsAreDeclarationsAndKeepContactSnapshot, browser |
| 16–18: proprietario, comproprietà e cambio | CoOwnershipNeedsExplicitUnitRepresentativeWithoutDuplicatingMillesimals, ricostruzione dopo cambio titolare |
| 19–22: delega, duplicazione, revoca e verifica | ProxyVerifiedAttendanceRejectsDuplicatesAndKeepsIndependentCalls, invarianti dominio |
| 23–27: presenze, deleghe, doppio conteggio, sessioni e uscita | ProxyVerifiedAttendanceRejectsDuplicatesAndKeepsIndependentCalls, DirectAttendanceAbsenceAndSecondCallNotAutomatic |
| 28–30: snapshot, concorrenza e audit | SQL UPDATE snapshot rifiutato, due richieste con stessa versione: una sola riuscita; revisione e audit verificati |
| 31–34: sicurezza | ConcurrentVersionAuditRolesCsrfAndCrossScope: altro tenant, altro condominio, Viewer, Collaborator non verificatore e CSRF mancante |

I test persistenti riutilizzano il fixture reale M06, senza duplicare il dominio
anagrafico o disabilitare autenticazione. Il test di collegamento ai consuntivi
verifica che il documento resti Draft. Le prove di errore mantengono i vincoli
attivi e possono produrre log tecnici di eccezioni attese con soli dati sintetici.

## Flusso browser

`e2e/meetings.spec.ts` crea unità, soggetti e tabella tramite le API reali,
poi esegue dall'interfaccia: nuova assemblea ordinaria con due sessioni,
ODG, anteprima/consolidamento partecipanti, preparazione destinatari,
dichiarazione manuale di invio, consolidamento convocazione, delega presentata
e verificata, prima convocazione non tenuta, seconda avviata, ingresso per
delega e uscita. Nessun mock e nessun invio esterno.

Esempio: A con 120 millesimi, X delegante e Y delegato. Una presenza per delega:
un presente effettivo, una sola unità rappresentata, 120 millesimi informativi.
Dopo l'uscita: zero presenze attive; l'ingresso storico rimane consultabile.
Le altre sette prove browser coprono contabilità, riparti, preventivi, consuntivi,
Identity, anagrafiche e rate/incassi.

## Limiti e prosecuzione

Vedere [modello M07.1](milestone-071-meetings.md): nessun quorum, voto, soglia
normativa automatica, invio reale o prova di consegna verificata. Completed è
riservato alla successiva gestione deliberativa. Le rettifiche dopo l'avvio non
riscrivono gli snapshot; uscite/rientri e revisioni conservano lo storico.
M07.2 e M07.3 non iniziate.

## Esiti browser e ultimo controllo

I sette flussi preesistenti Edge sono passati nella verifica completa. Dopo le
correzioni ai selettori del nuovo test (ricaricamento della rubrica condomini
creata via API e nome accessibile del campo con testo di aiuto), il flusso
Assemblee è passato su Edge. Il test è stato poi rieseguito anche con gli altri
flussi per la verifica conclusiva.

Il harness accetta ora anche un filtro browser, mantenendo database e bootstrap
isolati; utile per riprodurre solo il nuovo flusso:

```powershell
.\scripts\Verify-Postgres.ps1 -TestFilter 'FullyQualifiedName~Meeting' -Browser -BrowserFilter meetings.spec.ts
```

Le schermate `.artifacts/meeting-desktop.png` e `meeting-mobile.png` sono state
ispezionate visivamente. Il browser verifica inoltre l'assenza di overflow della
pagina a 390 px; le tabelle mantengono lo scorrimento interno. È stato aggiunto
un avviso per quote rappresentate mancanti e verificato il rifiuto di creazione
su condominio inattivo, oltre ai controlli di ruolo sulla verifica delle deleghe.

Un'ulteriore ripetizione ha individuato una race del test browser M04: dopo il
click su Apri esercizio navigava via prima della conferma della risposta, caricando
la contabilità con l'esercizio ancora Draft. `accounting.spec.ts` ora attende
esplicitamente la cella Open prima di cambiare pagina. Nessuna regola contabile
è stata modificata o disabilitata; il controllo rende verificabile il completamento
dell'operazione originaria.

## Risultato conclusivo

Ultima esecuzione integrale `./scripts/Verify-Postgres.ps1 -Browser` riuscita:
80 unitari + 92 integrazione PostgreSQL = **172 test backend superati**, zero
ignorati; **8 flussi Edge superati insieme** (45,8 secondi). Include gli ultimi
controlli su condomini inattivi e ruoli di verifica deleghe. Build backend zero
warning/errori, build TypeScript/Vite riuscita, nove migration applicate e modello
EF allineato. Volume, container e rete del progetto isolato sono stati rimossi.

`npm run lint --prefix src/frontend/web`: nessun errore o warning oxlint.
Nessun invio reale, commit, push o aggiornamento del database di lavoro.
