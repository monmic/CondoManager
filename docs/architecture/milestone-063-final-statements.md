# M06.3 — Consuntivi, conguagli e chiusura amministrativa

## Modello e workflow
Si riusano giornale M04, riparti immutabili M05, preventivi M06.1 e crediti/incassi
M06.2. AccountingService ha due partial dedicate a raccolta/calcolo e operazioni
finanziarie, con gli stessi helper transazionali e di audit. Non esistono nuovi saldi
editabili o fatture artificiali. IFinalStatementService espone solo DTO.

~~~mermaid
flowchart LR
 Spese[Documenti contabilizzati M04] --> Riparti[Snapshot confermati M05]
 Riparti --> Consuntivo[Draft / Proposed / Approved / Superseded]
 Preventivo[Preventivo approvato corrente] --> Consuntivo
 Rate[Piano emesso e debitori originari] --> Consuntivo
 Incassi[Attribuzioni e storni M06.2] --> Conguagli
 Consuntivo --> Conguagli[Preview / Confirm / Post]
 Apertura[Zero verificato / Import / Riporto] --> Conguagli
 Conguagli --> Giornale[Giornale M04: sola variazione]
 Giornale --> Checklist[Controlli e snapshot di chiusura]
 Checklist --> Chiusura[Esercizio Closed]
 Chiusura --> Riporto[Riporto analitico senza secondo credito]
~~~

FinalStatement conserva versione, input, snapshot, fingerprint, autore/data,
riferimento facoltativo alla delibera e stato. Una sola versione Approved corrente
per esercizio; nuove versioni collegate all'ultima. Bozze e proposte sono snapshot:
per correggere parametri/fonti si crea una nuova versione, senza sovrascritture.
Approvazione applicativa riservata a StudioAdmin, distinta da una delibera assembleare.

StatementSettlement conserva la conferma della contabilizzazione e le righe
SettlementPosition firmate per unità/soggetto. ResidentOpening/OpeningPosition
attestano i saldi iniziali; AdministrativeClosure conserva checklist, fonti e saldi.
Tutte le entità hanno UUID, tenant, condominio, audit UTC; FK composite, importi
numeric(18,2), snapshot jsonb e chiavi di idempotenza. Stato EF con token di concorrenza.

## Competenza e origine dei costi
La competenza è l'AccountingPeriodId assegnato al documento M04. Non deriva dalle
scadenze o dal pagamento. Si selezionano una sola volta documenti IsRegistered e
non IsCancelled dell'esercizio. Il costo è il lordo contabilizzato secondo M04;
IVA zero è supportata. Nessun OCR, rateo/prorata automatico o criterio normativo nuovo.

Il dettaglio distingue data documento, data contabile Journal.Date, istante UTC di
registrazione Journal.CreatedAt (visualizzato nel fuso del browser), periodo assegnato
e date dei pagamenti, inclusi storni. M04 attualmente richiede che la data contabile
della fattura coincida con la data documento; questa coincidenza non viene estesa
alla data effettiva di registrazione né alle date di pagamento.

Pagamenti fornitori, incassi, emissioni rate, giroconti e conguagli non sono costi.
La somma dei documenti selezionati deve coincidere con Dare-Avere dei conti Expense
nell'esercizio. Una scrittura manuale che produce un costo non classificabile genera
UnclassifiedCost bloccante: non viene distribuita arbitrariamente.
Documenti Draft non annullati bloccano la chiusura.

Storni di fatture entro un esercizio aperto escludono esplicitamente il costo annullato;
il vecchio riparto resta storico e viene segnalato. Per una rettifica si usano storno
e nuovo documento/riparto. M04 non ha note di credito negative native: non vengono
simulate. Scritture manuali di credito Expense non classificabili restano bloccanti.
Una fattura di esercizio amministrativamente chiuso non può essere annullata da una
semplice registrazione in un esercizio successivo; occorre un futuro workflow controllato.

## Riparti definitivi e fingerprint
Si usano solo run confermati correnti (foglie della catena M05), del medesimo
esercizio/condominio, e le quote salvate. Per documento: somma fonti = lordo;
per fonte e complessivamente: somma quote identica al costo al centesimo.
Si verificano inoltre scope, stato attivo delle regole e criteri correnti tramite
ResolveCriteria M05. Si confrontano i criteri con quelli congelati, senza ricalcolare
e riscrivere le quote confermate. Gruppi, unità, tabelle o letture cambiate richiedono
una nuova operazione esplicita M05. Le anomalie rendono non proponibile/approvabile
il consuntivo; le bozze possono conservarle per la revisione.

Il fingerprint economico include versione del preventivo, documenti/cancellazioni,
run e snapshot, saldo iniziale, designazioni e quote/crediti originari. Nuovi incassi
non riscrivono un consuntivo approvato: rimangono operazioni finanziarie esplicite.
Conferma/post dei conguagli e chiusura acquisiscono ulteriori fingerprint finanziari,
ricontrollati prima del commit. I dettagli delle versioni sono fotografie alla
preparazione, non saldi live; checklist e riepilogo M06.2 usano la situazione aggiornata.

## Debitori e confronto
Per quote M05 già personali si conserva il soggetto esplicito. Per quote ancora
all'unità, l'operatore conferma esplicitamente l'uso dei debitori storici del piano
emesso; le proporzioni sono le quote originali di quel piano. Non si consulta il
proprietario attuale come debitore automatico. In alternativa si designano persone,
data e quote esplicite, coerenti con associazioni/frazioni storiche. Soggetto ambiguo
produce DebtorUndetermined bloccante per la contabilizzazione.
La suddivisione fra soggetti riusa AllocationEngine.SplitCents, senza nuova matematica.

Il confronto usa il preventivo Approved corrente, anche se il piano fu emesso su
un'altra versione. Differenze economiche e variazioni da contabilizzare restano
quindi concetti distinti. Se manca un preventivo, serve un motivo esplicito per
trattare la previsione come zero; nessuna assenza è interpretata silenziosamente.
Categorie presenti in un solo documento restano visibili con zero nell'altro.

- Differenza categoria = actual - budget.
- Percentuale = differenza / budget * 100; null se budget è zero.
- Percentuali decimal arrotondate a sei decimali; UI conserva le stringhe.
- Riepiloghi per categoria, unità e soggetto, oltre al dettaglio delle posizioni.

## Conguaglio economico e saldo finanziario
Positivo = maggiore debito del soggetto; negativo = credito a suo favore.

Conguaglio di competenza = quota effettiva - quota preventiva + rettifiche di competenza.
Gli incassi NON entrano in questa formula. FinalCalculations riusa SettlementAmounts.
In M06.3 non sono accettate rettifiche economiche manuali prive di una fonte contabile:
si rettificano documenti e riparti, poi si crea una nuova versione. Il campo di
rettifica autonoma resta zero, senza introdurre addebiti arbitrari.

Delta da contabilizzare = quota effettiva - crediti già emessi del piano corrente.
Non si somma di nuovo la quota preventiva e non si sottraggono gli incassi dal delta.

Saldo finanziario = apertura + crediti emessi + delta definitivo - attribuzioni nette
- pagamenti successivi alla precedente chiusura su posizioni riportate.
Le attribuzioni nette includono gli storni. Incassi non attribuiti sono ancora nel
transitorio, distinti dal saldo del debitore, senza compensazioni automatiche.

| Operazione | Dare | Avere |
| --- | --- | --- |
| Conguaglio positivo | ResidentReceivables | BudgetContributions (Equity) |
| Conguaglio negativo | BudgetContributions | ResidentReceivables |
| Importo iniziale debitore | ResidentReceivables | OpeningEquity |
| Importo iniziale creditore | OpeningEquity | ResidentReceivables |

Nessuna nuova spesa o ricavo. Ogni posizione non zero genera le proprie righe:
+50 per A e -50 per B conservano debito/credito distinti anche se il netto è zero.
Se tutte le variazioni sono zero, Post registra l'avvenuto consolidamento senza
scritture artificiali a zero. Nessuna doppia contabilizzazione al retry.

Preview non scrive; Confirm salva snapshot e posizioni senza giornale; Post rilegge
le fonti e contabilizza atomicamente. Incassi attribuiti dopo Confirm richiedono una
nuova conferma. Conguagli non nulli possono essere stornati da StudioAdmin nello
stesso esercizio aperto, con motivo e nuova scrittura opposta. Poi si può approvare
una nuova versione e contabilizzare la nuova variazione. Storni generici M04 sui
nuovi controlli sono bloccati. Un consolidamento a zero, privo di giornale da stornare,
non ha ancora un workflow di revoca: la sostituzione è bloccata conservativamente.

Le rate M06.2 mantengono importi, pagamenti e residui nominali originari. I saldi
M06.2 integrano aperture importate e conguagli netti; scaduto/non scaduto resta
esplicitamente riferito alle rate, senza inventare scadenze normative dei conguagli.
La M06.3 non genera un ulteriore piano esigibile per la variazione: incassi attribuibili
alle rate continuano nel workflow esistente; rimborso o riscossione specifica dei nuovi
saldi richiede una procedura amministrativa successiva, non una compensazione implicita.

## Aperture e riporti
- VerifiedZero: primo esercizio, attestazione amministrativa esplicita con motivo.
- Import: primo esercizio, posizioni firmate per soggetto/unità con storico valido,
  motivo e unica scrittura patrimoniale. Nessun saldo libero sovrascrivibile.
- CarryForward: dalla chiusura dell'ultimo esercizio precedente; posizioni e origine
  congelate, senza seconda scrittura perché i crediti sono già nel giornale.

Un'apertura per esercizio, una destinazione per chiusura e UUID/hash impediscono il
secondo riporto. Il soggetto originario è preservato. Il riporto usa identificativi
delle scritture già comprese nella chiusura per distinguere pagamenti successivi su
vecchie rate. Snapshot di riporto compatti: posizioni e fingerprint/provenienza,
senza duplicare ricorsivamente gli interi snapshot degli esercizi precedenti.

Saldi assenti = non determinati, non zero. Attività patrimoniali General non finanziarie
con saldi preesistenti bloccano una nuova attestazione/importazione: vanno classificate
o riclassificate esplicitamente per evitare di duplicare crediti legacy.
Esercizi già chiusi prima di M06.3 senza snapshot amministrativo non ricevono una
chiusura retroattiva automatica; il riporto richiede un workflow di attestazione storico
ancora non automatizzato. Nessuna riapertura o conversione del vecchio conto 100.

## Checklist e chiusura
Si conservano gli stati Open/Closed M02; ClosingReview è la verifica esplicita,
non un nuovo stato che modifica le scritture. La checklist controlla consuntivo
approvato, fingerprint, riparti, documenti Draft, costi/quote, conguagli Post,
saldi individuali/globali contro il controllo ResidentReceivables, Dare/Avere e
assenza di operazioni di esercizi futuri inglobate. Chiusura degli esercizi in ordine.

Insoluti validi sono avvisi e crediti riportabili, non un blocco. Segnalazioni di
incasso o movimenti bancari non riconciliati sono avvisi, conservati nei rispettivi
stati/transitorio; non vengono considerati rate pagate né bloccano indiscriminatamente.

StudioAdmin verifica e conferma fingerprint/UUID. Il lock del condominio serializza
chiusura e scritture concorrenti; eventuali nuove fonti rendono obsoleta la checklist.
Commit atomico di snapshot e AccountingPeriod.Closed, audit e retry idempotente.
Il vecchio endpoint anagrafico /close rifiuta l'operazione; UI rimanda a Consuntivi.
Non si azzerano automaticamente i conti economici/fondi e non si riapre l'esercizio.

PostgreSQL aggiunge check, FK/indici, snapshot immutabili, limiti di transizione,
validatori differiti di giornali firmati, riporti identici alla fonte e chiusura
riconciliata per singola posizione. La vista cm_resident_positions deriva soltanto
da movimenti/righe immutabili; non è una tabella di saldi. Due colonne tecniche
CreationTransactionId sono gestite solo dai trigger per impedire append tardivi.
La migration aggiunge sei tabelle e amplia il check dei tipi di giornale, conservando
le protezioni M04/M06.2. Nessuna modifica ai dati storici o migration automatica.

## Sicurezza e limiti
Cookie, CSRF, TenantMember, ICurrentTenant, FK composite e X-Expected-Tenant rimangono
obbligatori. Viewer legge/simula; Collaborator prepara/propone; StudioAdmin approva,
verifica aperture, conferma/Post/storna conguagli e chiude. PlatformAdmin non aggira
la membership. Eventi di audit contengono ID/attore/data, non segreti.

Liste non paginate, snapshot con limiti di dimensione, nessun prorata/solidarietà
automatico, nessun trasferimento di debiti, nessuna compensazione fra persone.
Le rettifiche dopo chiusura, le note di credito native e la revoca dei consolidamenti
zero richiedono workflow ulteriori. Assemblee, verbali e documenti M07 non implementati.
