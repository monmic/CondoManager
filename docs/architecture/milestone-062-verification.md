# Verifica M06.2 — 6 novembre 2026

## Baseline effettiva
AGENTS.md letto integralmente. README, documentazione e implementazioni M04/M05/M06.1
analizzate. La baseline isolata ha verificato 110 test backend: 52 unitari e 58
integrazione, zero ignorati, build zero warning/errori, sei migration applicate e
modello EF allineato. Quindi il totale effettivo superava di uno quello riportato
nella prima suite completa M06.1 (109), coerentemente con il successivo test mirato.
Nessuna supposizione sul database di lavoro: l'harness usa solo risorse temporanee.

## Esecuzioni finali
- `dotnet build --no-restore`: superato, zero warning/errori.
- `.\scripts\Verify-Postgres.ps1 -Browser`: superato con 59 unitari + 72 integrazione
  (131 totali, zero ignorati), tutte e sette le migration applicate, nessuna modifica
  modello pendente, sei flussi Edge superati.
- `npm run build --prefix src/frontend/web`: superato.
- `npm run lint --prefix src/frontend/web`: superato.
- Script SQL generato da AdministrativeCycleMilestone06 a InstallmentsReceiptsMilestone062
  in .artifacts/milestone-062.sql. Nessun DROP TABLE, TRUNCATE, DELETE FROM o aggiornamento
  delle scritture/conti preesistenti. Si sostituiscono soltanto i check da ampliare
  per i nuovi enum, mantenendo i valori precedenti validi.
- `git diff --check`: superato; soli avvisi di conversione LF/CRLF dell'ambiente Git.

Il browser crea fixture sintetiche attraverso API autentiche, poi opera dalla UI:
quattro rate uguali da un preventivo 6000; emissione; rata 500, incasso/attribuzione
200, residuo 300; acquisizione RecordOnly 250 e match/reconcile di movimento M04;
saldi emesso 6000/attribuito 200/residuo 5800, transitorio 250, banca totale 450.
Nessuna chiamata mock. Screenshot desktop e mobile 390 px in .artifacts,
visivamente ispezionati; nessun overflow orizzontale della pagina.
Lo storno delle attribuzioni è verificato in integrazione PostgreSQL.

## Matrice scenari richiesti
I nomi sotto identificano test in ResidentFinanceTests o ResidentFinanceModelTests;
più scenari sono verificati nello stesso flusso, senza test che rispecchiano soltanto
l'implementazione. Si riusano anche i test matematici M06.1.

| # | Scenario | Evidenza |
| --- | --- | --- |
| 1 | Quattro rate uguali, Scenario A | ReferenceAPlanPreviewDraftApprovalAndLedgerAreExactlyReconciled |
| 2 | Percentuali differenti | PercentageAndExactAmountPlansVersionWithoutCreditsUntilApproval |
| 3 | Centesimi, importi esatti per scadenza | ExactAmountsPreserveEveryDebtorAndEveryDeadlineEvenForTinyAmounts |
| 4 | Date non valide/duplicate/disordinate | InvalidDatesPercentagesScopeAndPermissionsAreRejected; InvalidUnorderedAndDuplicateDeadlinesRejected |
| 5 | Approvazione | ReferenceAPlanPreviewDraftApprovalAndLedgerAreExactlyReconciled |
| 6 | Approvazione idempotente | ReferenceAPlanPreviewDraftApprovalAndLedgerAreExactlyReconciled |
| 7 | Rilevazione credito | ReferenceAPlanPreviewDraftApprovalAndLedgerAreExactlyReconciled |
| 8 | Dare/Avere | ReferenceAPlanPreviewDraftApprovalAndLedgerAreExactlyReconciled |
| 9 | Nessun duplicato | ReferenceAPlanPreviewDraftApprovalAndLedgerAreExactlyReconciled; OneReceiptAcrossRatesUnallocatedExcessAndReceiptIdempotence |
| 10 | Pagamento totale | PartialMultipleAndFullPaymentsKeepPayerDistinctAndPreventOverpayment |
| 11 | Parziale 500/200/300, Scenario B | PartialMultipleAndFullPaymentsKeepPayerDistinctAndPreventOverpayment; browser |
| 12 | Più pagamenti/rata | PartialMultipleAndFullPaymentsKeepPayerDistinctAndPreventOverpayment |
| 13 | 700 su due rate 500/200, Scenario C | OneReceiptAcrossRatesUnallocatedExcessAndReceiptIdempotence |
| 14 | Non attribuito 250, Scenario D | OneReceiptAcrossRatesUnallocatedExcessAndReceiptIdempotence; browser |
| 15 | Pagante distinto | PartialMultipleAndFullPaymentsKeepPayerDistinctAndPreventOverpayment; browser |
| 16 | Oltre residuo | PartialMultipleAndFullPaymentsKeepPayerDistinctAndPreventOverpayment |
| 17 | Oltre incasso | OneReceiptAcrossRatesUnallocatedExcessAndReceiptIdempotence |
| 18 | Incassi duplicati | OneReceiptAcrossRatesUnallocatedExcessAndReceiptIdempotence |
| 19 | Concorrenza | ConcurrentAllocationsCannotExceedReceiptOrRateAndFailuresAreAtomic; CompetingReceiptsCannotOverpaySameInstallment |
| 20 | Riconciliazione | ExistingBankMovementManualMatchingDoesNotDuplicateBankAndCannotMatchTwice; browser |
| 21 | Doppia riconciliazione | ExistingBankMovementManualMatchingDoesNotDuplicateBankAndCannotMatchTwice |
| 22 | Proprietà cambia, Scenario E | OwnershipChangeKeepsOriginalDebtorAndBalancesAreReconciledToLedger |
| 23 | Scaduto/non scaduto | OwnershipChangeKeepsOriginalDebtorAndBalancesAreReconciledToLedger |
| 24 | Saldi individuali = controllo contabile | OwnershipChangeKeepsOriginalDebtorAndBalancesAreReconciledToLedger |
| 25 | Cross-tenant | InvalidDatesPercentagesScopeAndPermissionsAreRejected |
| 26 | Cross-condominio stesso studio | SameTenantOtherCondominiumCannotUsePlansReceiptsOrCredits |
| 27 | Ruoli e CSRF | InvalidDatesPercentagesScopeAndPermissionsAreRejected; UnmatchedReceiptCannotPayAndCollaboratorCannotIssueOrReverse |
| 28 | Rettifica tracciata | AllocationReversalIsImmutableAuditedAndRestoresBothResiduals |

Aggiunti inoltre incasso anticipato e collegamento diretto a movimento esistente,
blocco di attribuzioni antecedenti all'emissione, bozze senza credito, piani emessi
non sostituibili automaticamente, immutabilità SQL di rate/attribuzioni.

## Dati e ambiente
Migration nuova 20261106134439_InstallmentsReceiptsMilestone062, sette tabelle nuove
più vincoli PostgreSQL. Nessuna migration applicata al database di lavoro.
Solo database/container/volume Compose univoci dell'harness sono stati rimossi.
Nessuna nuova credenziale, dipendenza, commit o push; modifiche precedenti preservate.
Warning ambientali npm min-release-age e Node NO_COLOR/FORCE_COLOR non bloccanti.

## Limiti dichiarati
Sostituzione di un piano già emesso bloccata con differenze consultabili, nessuna
politica automatica di compensazione. Rettifica attribuzioni tramite storno intero
batch; nessun annullamento automatico di incassi/associazioni bancarie.
Riconciliazione esatta uno a uno dei soli incassi generici M04. Liste non paginate;
asOf regola lo scadenziario, non ricostruisce saldi storici dei pagamenti.
Consuntivi, conguagli definitivi e chiusura amministrativa restano nella M06.3.

Verifica mirata successiva: `.\scripts\Verify-Postgres.ps1 -TestFilter 'FullyQualifiedName~ResidentFinanceTests'`
completata con 15/15 test PostgreSQL superati. Include il nuovo test
FailedMultiRateBatchLeavesNoPartialAttributionsOrJournal: prima riga valida,
seconda rata inesistente; nessuna attribuzione o scrittura parziale persiste.
I test backend presenti/verificati sono ora 132 distinti (59 unitari + 73 integrazione),
eseguiti come suite completa da 131 più suite mirata finale dei 15 test del modulo.
Il codice applicativo non è cambiato dopo la suite completa/browser: solo il nuovo
test e il titolo descrittivo del test browser, ora correttamente riferito ai saldi.
