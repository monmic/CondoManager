# ADR 0008 — Conguagli come variazioni e riporti senza doppio credito

Stato: adottato per M06.3.

I costi provengono dai documenti M04 assegnati all'esercizio e sono riconciliati
con i conti Expense, non con i pagamenti. Le quote definitive provengono dai run M05
immutabili; criteri modificati vengono segnalati, mai riscritti in silenzio.
Il preventivo Approved determina il confronto economico; i crediti realmente emessi
nel giornale determinano invece la variazione da contabilizzare.

Il conguaglio usa ResidentReceivables contro BudgetContributions Equity. Nessun
ricavo/costo duplicato, nessuna nuova tabella di saldi. Conferma e Post sono distinti,
con fingerprint economico e finanziario. Le posizioni con segno restano distinte
anche quando il totale netto è zero. Un Post interamente zero non inventa giornali.

Il primo saldo iniziale è attestato esplicitamente o importato con scrittura;
il riporto da una chiusura è analitico, non una seconda rilevazione contabile.
Le attribuzioni successive su vecchie rate sono distinte mediante le scritture
incluse nello snapshot precedente. Nessun trasferimento al nuovo proprietario.

La chiusura richiede il nuovo workflow e un validatore PostgreSQL: il vecchio
endpoint diretto non può aggirarlo. Insoluti e transitorio identificato non sono
un blocco automatico; fonti ambigue, disallineamenti e operazioni future lo sono.
Rettifiche non definite sono bloccate, non compensate o riaperte automaticamente.
