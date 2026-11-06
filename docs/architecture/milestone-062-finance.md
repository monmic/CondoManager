# M06.2 — Rate, crediti e incassi

## Confini e modello
M06.2 aggiunge il ciclo finanziario ai preventivi M06.1 senza duplicare contabilità,
persone, unità, conti o motore M05. Consuntivi, conguagli definitivi e chiusura
amministrativa restano M06.3, non implementata.

~~~mermaid
erDiagram
    Budget ||--o{ InstallmentPlan : versione_approvata
    InstallmentPlan ||--|{ PlanSchedule : scadenze
    InstallmentPlan ||--|{ InstallmentDebtor : quote_originali
    Person ||--o{ InstallmentDebtor : debitore
    PropertyUnit ||--o{ InstallmentDebtor : unita
    PlanSchedule ||--|{ Installment : rate
    InstallmentDebtor ||--|{ Installment : importi
    InstallmentPlan o|--o| JournalEntry : emissione
    ResidentReceipt ||--o| ReceiptBankLink : riconciliazione
    JournalEntry ||--o| ReceiptBankLink : incasso_bancario
    ResidentReceipt ||--o{ ReceiptAllocation : attribuzioni
    Installment ||--o{ ReceiptAllocation : credito
    JournalEntry ||--|{ ReceiptAllocation : attribuzione_o_storno
    ReceiptAllocation o|--o| ReceiptAllocation : rettifica
~~~

PlanSchedule è il nome persistente della scadenza, distinto dal DTO matematico
InstallmentSchedule esistente. InstallmentDebtor conserva persona, unità, nomi/codici
e quota originale. Installment è anche l'origine del credito: non c'è un secondo
registro di crediti con saldi aggiornabili manualmente.
Tutte le nuove entità hanno UUID, TenantId, CondominiumId, audit UTC e FK composite.
Gli importi sono decimal/numeric(18,2), pesi numeric(18,6), snapshot/input jsonb.

## Designazione dei debitori
Il riparto congelato del Budget resta autorevole. Per quote ancora solo dell'unità,
l'operatore seleziona esplicitamente i soggetti e una data entro l'esercizio: serve
un'associazione storica valida a quella data. Le quote selezionate sommano esattamente
1 per unità; se l'associazione ha una frazione di proprietà, deve coincidere con quella
selezionata. Precisione massima delle frazioni di calcolo: sei decimali; frazioni
storiche non rappresentabili richiedono una decisione/rettifica esplicita.
Gli addebiti già personali mantengono il soggetto approvato e richiedono un'associazione
alla data designata. Nessuna scelta automatica del proprietario attuale, solidarietà
giuridica o prorata. I nomi nel dettaglio delle rate emesse sono quelli dello snapshot.
Una nuova proprietà non trasferisce rate né incassi.

## Generazione e versioni
Il piano nasce Draft senza scritture. Simulazione e creazione usano
InstallmentCalculator e AllocationEngine.SplitCents: ciascuna quota individuale
viene divisa, non il totale condominiale indistintamente.
- Equal: pesi uguali, numero arbitrario da 1 a 120.
- Percentages: pesi positivi con somma esattamente 100.
- Amounts: totali monetari positivi per scadenza con somma uguale al preventivo.
Scadenze univoche, ordinate, entro l'esercizio. Quote zero sono conservate per
quadratura e mostrate come NotRequired; nessun credito positivo viene inventato.
Rate straordinarie sono scadenze/importi configurabili nello stesso piano.

L'arrotondamento per debitore riusa i resti maggiori razionali esatti di M05, con
spareggio per ordine cronologico della scadenza. Per Amounts, un bilanciamento
successivo trasferisce solo centesimi fra scadenze dello stesso debitore: prima
scadenza deficitaria, prima scadenza eccedentaria, unità UUID e persona UUID in
ordine stabile. Nessuna quota individuale cambia, nessun importo negativo, e
anche i totali richiesti per scadenza quadrano esattamente. Il metodo è puro,
deterministico e testato anche con tre quote da un centesimo.

Le bozze sono snapshot: per correggerle si crea una nuova versione collegata
all'ultima. Una sola versione può essere emessa per esercizio; le bozze precedenti
sono Superseded. I piani emessi sono immutabili.
Una nuova versione mostra differenze per unità/debitore, ma se un piano è già emesso
l'approvazione è bloccata: non si inventano compensazioni di crediti o rate pagate.
Le rettifiche amministrative del piano non sono automatizzate in M06.2.

## Emissione e convenzione contabile
StudioAdmin approva con data di registrazione in esercizio aperto. Il sistema
ricontrolla preventivo corrente e fonti/designazioni confrontando il fingerprint.
L'approvazione emette atomicamente l'intero credito; l'esigibilità resta la
scadenza della singola rata, distinta dalla data di rilevazione.

| Operazione | Dare | Avere |
| --- | --- | --- |
| Emissione piano | Crediti rate emesse (Asset) | Contributi preventivi emessi (Equity) |
| Incasso bancario/cassa | Conto finanziario (Asset) | Incassi da attribuire (Liability) |
| Attribuzione | Incassi da attribuire | Crediti rate emesse |
| Storno attribuzione | Crediti rate emesse | Incassi da attribuire |

L'emissione NON utilizza ricavi o costi. I contributi sono una posizione
patrimoniale preventiva, non una duplicazione delle fatture M04.
Al primo piano emesso vengono aggiunti due conti di controllo, se assenti:
110-RATE (ResidentReceivables) e 320-CONTRIBUTI (BudgetContributions).
Un codice occupato da un altro conto blocca l'operazione senza conversioni implicite.
Il vecchio conto General 100 resta invariato e non viene reinterpretato: eventuali
saldi pregressi richiedono una futura procedura esplicita. Codici/nomi modificabili,
funzione immutabile. Scritture manuali sui nuovi controlli vietate anche nel DB.

Idempotenza tramite UUID e hash del contenuto per creazione piano, emissione,
incasso, attribuzione e storno. Stessa chiave/stessi dati recupera il risultato;
stessa chiave/dati diversi produce conflitto. Riferimenti bancari liberi non sono
chiavi univoche; dopo una risposta incerta riusare la stessa operazione.

## Incassi e riconciliazione
DebtorId è facoltativo e diverso da PayerId. Il debitore identificato deve appartenere
al condominio tramite storico o piani; il pagante può essere un'altra persona dello
studio. Non è richiesto che coincida con il debitore.

Tre modalità:
1. Nuovo incasso: crea una sola scrittura bancaria M04 e un collegamento Reconciled.
2. ExistingJournalId: collega un incasso generico M04 esistente senza nuove righe bancarie.
3. RecordOnly: registra una segnalazione Unmatched, senza muovere banca o pagare rate.
   Match prenota un movimento (Matched); Reconcile conferma il collegamento.

Riconciliazione manuale ESATTA uno a uno: stesso condominio, esercizio, conto,
data e importo; solo JournalKind.Receipt non stornato, nato nel conto transitorio.
Il movimento e l'incasso hanno collegamenti univoci; niente parziali/aggregati,
importazione estratti conto o integrazioni esterne. I collegamenti confermati non
sono riassegnabili silenziosamente. Il modulo non annulla automaticamente incassi
bancari né collegamenti errati: tali rettifiche richiedono un workflow ulteriore.

Le segnalazioni non riconciliate sono distinte dai fondi effettivamente ricevuti e
non possono essere attribuite. Un incasso riconciliato senza attribuzioni rimane
nel transitorio. Un incasso anticipato può precedere l'emissione del piano;
l'attribuzione deve avvenire dopo incasso ed emissione del credito, in esercizio
aperto. Il credito può provenire da un esercizio precedente: il giornale della
nuova attribuzione conserva il proprio esercizio di registrazione.

## Attribuzioni e correzioni
Una singola operazione può distribuire l'incasso su più rate; una rata può ricevere
più operazioni. Somma attribuita <= incasso e <= residuo delle singole rate.
Se il debitore dell'incasso è identificato, tutte le rate devono essere di quel
soggetto. Gli importi non attribuiti non sono automaticamente compensati.

Ogni gruppo di attribuzioni genera una scrittura transitorio/crediti. Le righe
ReceiptAllocation sono immutabili. StudioAdmin può stornare l'intero gruppo con
motivo/data, creando scrittura opposta e righe che referenziano gli originali.
Poi può attribuire nuovamente con una nuova operazione. Nessun incasso o movimento
bancario è riscritto. Gli storni generici M04 di piani, incassi collegati o attribuzioni
sono bloccati; occorre usare il workflow dedicato per preservare i dettagli.

## Saldi
Residuo rata = importo emesso - somma attribuzioni + storni attribuzioni.
Scaduto = residui con scadenza precedente ad asOf (oggi per default).
Non scaduto = residui con scadenza >= asOf. asOf è la data dello scadenziario,
NON una ricostruzione storica dei pagamenti: i saldi includono tutte le operazioni
registrate. Non presentare questi dati come saldi di cassa a una data passata.

L'API espone posizioni unità/debitore; la UI distingue riepiloghi per persona e
per unità. Incassi riconciliati non attribuiti sono esposti separatamente, incluse
le eccedenze del debitore identificato; quelli non identificati restano nel totale.
Non esistono saldi editabili né trasferimenti automatici fra soggetti.
Il riepilogo completo usa una transazione RepeatableRead. Senza filtro, il totale
residuo è riconciliabile con il conto ResidentReceivables; i fondi non attribuiti
con il transitorio (che include anche gli incassi generici M04 non ancora acquisiti).
Con filtro esercizio, le quote sono selezionate per esercizio di origine del credito,
con tutte le attribuzioni, anche registrate successivamente; non coincidono per
forza con le sole variazioni di quell'esercizio del riepilogo M04.

## Difese e limiti
Cookie/CSRF/TenantMember M03, tenant server-side, X-Expected-Tenant, guardie dei
servizi e filtri EF. Viewer consulta/simula; Collaborator crea bozze e registra,
riconcilia/attribuisce incassi; StudioAdmin emette piani e storna attribuzioni.
Lock del condominio serializza emissioni, incassi, attribuzioni, storni e chiusure;
le persone delle designazioni sono bloccate in lettura durante creazione/emissione.
Audit per creazioni, emissioni, match, riconciliazioni, attribuzioni e storni.

PostgreSQL aggiunge FK composite, unicità delle operazioni/collegamenti, check,
trigger immutabili e controlli differiti di quadratura piano/debitore/giornale,
limiti di incasso/rata e corrispondenza delle riconciliazioni. Il filtro EF non è
l'unica difesa. SQL diretto/ruolo di migrazione restano confini privilegiati, senza RLS.

Liste non paginate; massimo 1000 attribuzioni per operazione. Nessun consuntivo,
conguaglio definitivo, compensazione automatica, rimborso di eccedenze, trasferimento
giuridico di debito o chiusura amministrativa M06.3 in questo incremento.
