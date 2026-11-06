# ADR 0006 — Riparti preventivi attraverso il motore esistente

Stato: adottato per l'incremento preventivi, non per l'intero ciclo M06.

BudgetLine è una fonte matematica distinta da ExpenseDocument. Il medesimo
ResolveCriteria risolve regole e dati autorevoli per entrambe. Il motore puro
rimane privo di database e non genera fatture o movimenti artificiali.
La discriminante SourceKind distingue i due tipi; i nomi legacy del contratto
sono conservati per leggere gli snapshot M05 già salvati.

L'approvazione congela il calcolo ma non rileva un credito. Tale rilevazione dovrà
avvenire con il piano rateale e con scritture bilanciate nel giornale M04, senza
un secondo saldo aggiornabile. La scelta dei conti di controllo, gli storni dei
piani e la riconciliazione degli incassi vanno implementati e verificati insieme
prima di rendere operativa la richiesta di rate.

Scadenze e quote individuali usano l'algoritmo razionale esatto già usato da M05;
non viene introdotto un secondo algoritmo di arrotondamento. Le formule dei
conguagli restano esplicitamente separate dai saldi finanziari.

Implementazione successiva: [ADR 0007](0007-resident-receivables.md) e
[modello M06.2](../architecture/milestone-062-finance.md) definiscono crediti,
incassi, storni delle attribuzioni e blocco delle compensazioni non definite.
