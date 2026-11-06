# M07.2 — Verifica del 6 novembre 2026

## Baseline e ambiente

Prima dell'incremento: `./scripts/Verify-Postgres.ps1` ha confermato 172 test backend
(80 unitari, 92 integrazione), nove migration e snapshot EF allineato.
Windows, SDK .NET con target net8.0, PostgreSQL 16 Docker, Node/npm e Edge Playwright.
Dati esclusivamente sintetici. Nessun commit/push o aggiornamento del database di lavoro.
Le modifiche M07.1 già presenti e le due voci Git preesistenti `AD .../UnitTest1.cs`
sono state conservate.

## Risultati backend e schema

- `dotnet build`: zero warning/errori.
- `dotnet test` nel harness PostgreSQL isolato: **209 superati**, 101 unitari e
  108 integrazione, nessun ignorato.
- Nuovi casi: 21 casi unitari in VotingEngineTests e 16 integrazione in VotingTests.
  I 46 scenari funzionali sono raggruppati; non si dichiarano 46 nuovi test indipendenti.
- Dieci migration applicate al volume isolato, inclusa
  `20261106210126_VotingMilestone072`.
- `dotnet ef migrations has-pending-model-changes`: nessuna modifica pendente.
- SQL tra M07.1 e M07.2 generato in `.artifacts/milestone-072.sql`: nessun DROP TABLE,
  TRUNCATE, DELETE FROM o UPDATE dei dati esistenti. Dieci tabelle additive, FK,
  indici e controlli. Down bloccato per evitare eliminazione dello storico.
- `npm run build --prefix src/frontend/web`: TypeScript/Vite riusciti.
- `npm run lint --prefix src/frontend/web`: nessun errore o warning oxlint.
- `git diff --check`: nessun errore whitespace. Il sistema segnala soltanto LF/CRLF.

Credenziali Docker e bootstrap sono generate in memoria. Il harness crea un progetto
Compose casuale con volume separato e rimuove solo le proprie risorse. Gli avvisi
ambientali npm `min-release-age` e Node sui colori non cambiano gli esiti dei test.
Nessun pacchetto aggiunto.

## Copertura

| Scenari richiesti | Prove |
|---|---|
| 1–4, quorum prima/seconda | ConstitutionalRulesAreIndependentPerCall; UnsatisfiedOrUndeterminedQuorumCannotOpenVote; SecondCallMustBuildItsOwnQuorumAndElectorate |
| 5–10, indeterminazione e soglie | NumericalMajorityDoesNotReplaceMillesimalMajority; MillesimalMajorityDoesNotReplaceHeadcount; ExactBoundaryHasNoFloatingPointRounding; OneThirdIsNotRoundedToSixDecimals; AnomalousRepresentationIsUndetermined; preparazione comproprietà M07.1 |
| 11–18, presenze/deleghe/snapshot | WrittenProxyRepresentsOneHeadAndCanBeRevokedBeforeOpening; ElectorateFreezesExitsAndDoesNotAdmitLateArrivals; SameOwnerOfTwoUnitsIsOneHeadWithCombinedValue; test M07.1 di doppia rappresentanza e snapshot |
| 19–23, scelte e denominatori | UnanimousAndAdverseVotes; AbstentionsDependOnDenominator; PresentNonVotersAreDistinctFromAbstentionsAndAbsentees |
| 24–28, unicità/modifica/concorrenza | BallotsAreUniqueVersionedAndCannotChangeAfterClosing; DuplicateBallotOrForeignRightCannotApprove; duplicate unit nel motore; blocco termine sessione con voto aperto |
| 29–34, delibere | VoteResolutionAndCompletionAreImmutableIdempotentAndBalanced; RejectedAndUndeterminedResolutionsCannotApplyAndOpenVoteBlocksCompletion; update SQL snapshot/delibera rifiutati e rettifica testuale con catena |
| 35–40, M06 | ApplyExactBudgetVersionExplicitlyWithoutDuplicatingAccounting; ApplyFinalStatementUsesExistingApprovalAndCreatesNoJournal; AlreadyApprovedBudgetGetsHistoricalLinkWithoutChangingDate; ChangedTargetVersionContentCannotBeApprovedByOldVote; scope M07.1/M07.2 e test di rollback |
| 41–46, conclusione/sicurezza | Checklist con voti aperti/presenze attive; conclusione e replay; FailedQuorumCanFinishOnlyWithAllAgendaExplicitlyDeferred; RulesAreVersionedAndScopeRoleAndCsrfAreEnforced; Viewer senza recapiti dello snapshot conclusivo |

## Atomicità M06

Il test M06ApprovalAndResolutionLinkRollbackTogetherWhenPersistenceFails aggiunge
un vincolo temporaneo che rifiuta soltanto il UUID del target sintetico. L'approvazione
M06 viene eseguita dal vero servizio, poi il collegamento fallisce: preventivo nuovo
rimane Proposed, precedente rimane Approved, versione assemblea e richiesta non
vengono commesse. Rimosso il vincolo di test, la stessa richiesta riesce.
Il vincolo viene rimosso in finally e non viene mai creato sul database di lavoro.
Questo prova la transazione condivisa, senza disabilitare controlli di sicurezza.

## Browser e risultati numerici

`e2e/voting.spec.ts` prepara tramite API reali anagrafiche, tabella, preventivo
Proposed e assemblea M07.1. Dalla UI crea/attiva il criterio deliberativo, verifica
il quorum, congela la platea, registra i tre voti, chiude, consolida la delibera,
applica M06, rinvia un altro punto, verifica il blocco con presenze attive, registra
le uscite e conclude. Nessun mock e nessun invio esterno.

Tre teste con valori 500/300/200: A e C favorevoli, B contrario.
Teste: 2 > 3×1/2. Valore: 700 >= 1000×1/2. Approved secondo il criterio configurato.
Il target specifico da 106.733,48 € diventa Approved solo con il comando esplicito.
I test PostgreSQL verificano assenza di nuove rate o giornali e retry senza duplicazioni.

Caso A della richiesta: 4 favorevoli su 6 ma valore favorevole 280 su nominale 1000;
il requisito numerico passa, quello di metà valore fallisce: Rejected.
Caso astensioni: 5 favorevoli, 3 contrari, 2 astenuti non bastano con maggioranza
stretta dei 10 presenti; con base espressi 8 e l'altro requisito soddisfatto cambia
l'esito. Non esiste una gestione universale dell'astensione.

Screenshot desktop/mobile in `.artifacts/voting-desktop.png` e `voting-mobile.png`,
ispezionati visivamente. Il browser verifica assenza di overflow della pagina a
390 px. Le tabelle mantengono lo scorrimento interno. Le prove di selettore e una
race della checklist sono state corrette prima della verifica conclusiva.

## Riproduzione

```powershell
.\scripts\Verify-Postgres.ps1 -Browser
.\scripts\Verify-Postgres.ps1 -TestFilter 'FullyQualifiedName~Voting' -Browser -BrowserFilter voting.spec.ts
```

Vedere [fonti effettivamente consultate e limiti](milestone-072-legal-sources.md):
nessuna dichiarazione di conformità legale generale. Applicazioni successive alla
conclusione, rettifiche sostanziali, PDF e archivio non sono implementati in M07.2.
M07.3 non iniziata.

## Esito conclusivo

Ultima esecuzione integrale `./scripts/Verify-Postgres.ps1 -Browser` riuscita:
**209 test backend superati (101 unitari, 108 PostgreSQL), zero ignorati; 9 flussi
Edge superati insieme**. Dieci migration applicate, modello EF allineato, build
backend zero warning/errori, TypeScript/Vite e oxlint riusciti. La rete, il container
e il volume del progetto Docker isolato sono stati rimossi. Database di lavoro intatto.
