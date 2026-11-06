# ADR 0007 — Crediti rate e incassi nel giornale esistente

Stato: adottato per M06.2.

La rata emessa è l'origine analitica del credito; il conto ResidentReceivables è
il controllo patrimoniale nel giornale M04. Non viene introdotto un registro di
saldi editabili parallelo. La rilevazione avviene all'emissione del piano, con
scadenze che determinano l'esigibilità. Contropartita Equity BudgetContributions,
non Income: nessun costo/ricavo derivante artificialmente dalle previsioni.

Ogni incasso passa prima da UnallocatedReceipts. Le attribuzioni riclassificano
transitorio/crediti, senza muovere nuovamente banca. Un movimento già presente
può essere collegato uno a uno solo se è un incasso generico M04 esatto per
esercizio/data/conto/importo. RecordOnly resta una segnalazione senza banca finché
riconciliata. Le riconciliazioni parziali o aggregate non sono ammesse.

Lo storno di un'attribuzione crea la scrittura opposta e righe analitiche inverse,
conservando l'originale; lo storno generico M04 non può aggirare il workflow.
Sostituzioni di piani emessi e annullamenti di movimenti bancari collegati sono
bloccati invece di inventare politiche di compensazione o riscrivere lo storico.

Si riusano AccountingService (partial per responsabilità), Write/Period/Persist,
JournalEntry e il calcolatore puro esistente. Nessuna nuova dipendenza o servizio.
