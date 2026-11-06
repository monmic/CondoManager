# Ripartizione delle spese — Milestone 05

## Moduli e dati
Si riutilizzano Condominium, PropertyUnit, Person/OwnershipPeriod, AllocationTable,
AccountingPeriod, Account ed ExpenseDocument. Nessuna tabella contabile M04 viene
riscritta dal riparto e nessuna conferma genera nuovi JournalEntry o crediti.
Domain contiene regole/invarianti e nuove entita; Application contiene DTO,
IAllocationEngine e AllocationEngine puro, indipendente da EF, HTTP e UI.
Infrastructure risolve gli input autorizzati, applica transazioni e salva gli
snapshot. API espone DTO; React conserva decimal come stringhe.

~~~mermaid
erDiagram
    Condominium ||--o{ AllocationRule : configura
    AllocationRule ||--|{ AllocationRuleComponent : componenti
    AllocationRule o|--o| AllocationRule : versione_precedente
    AllocationGroup ||--|{ AllocationGroupMember : membri
    PropertyUnit ||--o{ AllocationGroupMember : appartiene
    AllocationRuleComponent }o--o| AllocationGroup : ammette
    AllocationRuleComponent }o--o| AllocationTable : usa
    PropertyUnit ||--o{ ConsumptionReading : letture
    AccountingPeriod ||--o{ ConsumptionReading : riferimento
    ConsumptionReading o|--o| ConsumptionReading : rettifica
    Account o|--o| AllocationAssignment : default_categoria
    ExpenseDocument o|--o| AllocationAssignment : deroga_documento
    AllocationRule ||--o{ AllocationAssignment : seleziona
    AllocationRun ||--|{ AllocationRunSource : fonti
    ExpenseDocument ||--o{ AllocationRunSource : quota_di_spesa
    AllocationRule ||--o{ AllocationRunSource : criterio_approvato
    AllocationRunSource ||--|{ AllocationRunLine : quote
    PropertyUnit ||--o{ AllocationRunLine : destinataria
    Person o|--o{ AllocationRunLine : soggetto_esplicito
    AllocationRun o|--o| AllocationRun : sostituzione
~~~

Tutte le nuove entita hanno UUID, TenantId e audit UTC; tutte hanno CondominiumId.
FK composite tenant/condominio impediscono riferimenti ad altri studi o condomini.
Person e studio-wide: FK tenant/persona e controllo applicativo dello storico
soggetto/unita per l'intera competenza richiesta.
Importi numeric(18,2), pesi/percentuali/letture numeric(18,6); dettagli teorici e
rettifiche di arrotondamento numeric(28,12). Snapshot/input JSONB immutabili.

## Regole e gruppi
Tipi: Millesimal, EqualShares, Consumption, Mixed, DirectCharge.
Da 1 a 20 componenti, percentuali positive con massimo 6 decimali, somma ESATTA
100%. Mixed richiede almeno due componenti elementari; gli addebiti diretti usano
una regola dedicata 100%. Per combinare un addebito personale con una spesa comune,
creare due quote dello stesso documento con regole distinte.

La bozza e modificabile e deve essere strutturalmente valida. L'attivazione
verifica anche tabelle e gruppi referenziati. Una regola attiva e immutabile:
PUT crea una nuova bozza con UUID diverso, PreviousVersionId e versione +1.
Una sola versione successiva per nodo, nessuna biforcazione accidentale.
Attivare una versione nuova non cambia i default o i documenti gia associati:
la nuova scelta deve essere esplicita. Versioni precedenti restano consultabili.

Un gruppo e una lista esplicita di unita, indipendente da edificio/scala; non deve
essere vuoto. Membri correnti modificabili, snapshot dei membri configurati e
ammessi conservato per ciascun riparto. Unita inattive nel gruppo richiedono una
rimozione esplicita; non vengono escluse silenziosamente dal criterio.
Le esclusioni per singola quota si intersecano con il gruppo del componente.

## Priorita delle associazioni e budget
1. RuleId esplicito sulla quota di spesa.
2. Deroga AllocationAssignment per ExpenseDocument.
3. Default AllocationAssignment per categoria Account Expense.

Associazioni in tabella separata: nessun cambiamento ai DTO o alle scritture M04.
Una conferma salva ogni quota in AllocationRunSource, con importo/regola scelti.
Sono ammesse piu quote dello stesso documento nello stesso calcolo; la somma con
quelle gia confermate correnti non puo superare il lordo del documento.
Il residuo da ripartire e distinto dal residuo DA PAGARE: i pagamenti non cambiano
il costo da ripartire. Solo documenti registrati e non annullati dell'esercizio
scelto; esercizio Open e competenza compresa negli estremi inclusivi.

## Formule e denominatori
Millesimi: importo componente × quota unita / totale quote ammesse.
La tabella deve essere attiva, non archiviata, valida dall'inizio della competenza;
le quote devono coprire l'intera competenza, tutte le unita attive e il totale
nominale. Tabelle parziali, duplicate o con valori mancanti sono rifiutate.

Denominatore nominale ed effettivo sono esposti separatamente. Con gruppi/esclusioni,
se il totale ammesso differisce dal nominale il calcolo e rifiutato, salvo
NormalizeAdmitted esplicitamente configurato nel componente prima dell'attivazione.
Questa scelta produce un avviso e viene congelata nello snapshot. Esempio:
500 + 300 ammessi su tabella nominale 1000: con normalizzazione esplicita il
denominatore effettivo e 800; senza normalizzazione nessuna quota viene confermata.

Parti uguali: peso 1 per ogni unita ammessa. Nessun proprietario scelto automaticamente.
Consumi: peso pari alla differenza finale-iniziale, sommata sui segmenti dei contatori.
Totale pesi nullo rifiutato; nessun fallback automatico a parti uguali.
Mixed: prima ripartire il budget fra componenti, poi ogni budget fra destinatari.
30/70 e un esempio configurabile, non una regola normativa o universale.

## Quadratura al centesimo
Algoritmo **largest-remainder-v1**, applicato a componenti e poi destinatari:
1. Convertire l'importo in centesimi interi e i pesi decimal in interi con scala 6.
2. Calcolare i quozienti interi inferiori e i resti ESATTI delle frazioni.
3. Distribuire i centesimi ancora disponibili ai resti maggiori, uno per destinatario.
4. Parita: componenti per posizione; destinatari per codice unita maiuscolo Ordinal,
   poi UUID unita in formato D, poi UUID persona. Nessun ordinamento casuale.

Gli input sono decimal; BigInteger evita overflow e perdita di precisione nei
prodotti e nel confronto dei resti razionali. Mai float/double, neanche nel client.
React usa BigInt per i totali percentuali, budget e quote visualizzati.
UnroundedAmount e solo un dettaglio teorico TRONCATO a 12 decimali; non guida la
scelta dei centesimi. BaseAmount e il troncamento al centesimo; Amount e il risultato
finale; RoundingAdjustment = Amount - UnroundedAmount. Si conserva anche la
rettifica del budget fra componenti e l'indicatore del centesimo residuo.

Casi sintetici verificati:
- 1000 EUR, pesi 500/300/200: 500 / 300 / 200.
- 100 EUR, tre parti uguali A/B/C: 33,34 / 33,33 / 33,33.
- 1000 EUR, 30% millesimi 500/300/200 + 70% consumi 10/20/40: 250 / 290 / 460.
- 0,01 EUR, tre parti uguali: 0,01 / 0 / 0. Nessun centesimo perso o negativo.
Sono verificate anche percentuali e millesimi frazionari e importi massimi EUR.

## Letture, sostituzioni e stime
ConsumptionReading contiene esercizio, unita, tipo consumo estensibile (codice
normalizzato, ad esempio COLDWATER/HOTWATER/HEATING), unita di misura, codice
contatore opzionale (UNSPECIFIED se assente), data, estremo Initial/Final e valore.
Per ogni contatore/tipo/unita/esercizio e prevista una coppia iniziale/finale
corrente. Valori e consumi non negativi, data finale successiva all'iniziale.
Non e un archivio di teleletture periodiche o un algoritmo di termoregolazione.

Per sostituire un contatore: finale del vecchio e iniziale del nuovo con codice
distinto alla stessa data; i segmenti devono coprire esattamente la competenza,
senza sovrapposizioni o vuoti. Nessuna interpolazione o estrapolazione implicita.
Letture mancanti producono un'anomalia e bloccano la ripartizione; il pannello
Consumi le mostra anche senza un calcolo di spesa.

IsEstimated e motivo esplicito per le stime: nessun valore inventato dal motore.
Rettifiche append-only con ReplacesReadingId verso l'ultima lettura corrente dello
stesso contesto/estremo; motivo obbligatorio. La lettura precedente resta integra.
Il consumo usa soltanto l'ultima versione, ma lo snapshot conserva letture e
rettifiche usate. Modifiche dopo un preview cambiano l'impronta delle fonti.

## Addebiti diretti e soggetti
Ogni addebito indica unita, importo e opzionalmente PersonId. Gli importi devono
sommare esattamente la quota di spesa. Senza persona la quota resta dell'unita.
Con persona, il server verifica un'associazione storica che copra l'intera
competenza. Se cambia soggetto nel periodo, suddividere esplicitamente le quote
per competenze omogenee: nessuna assegnazione al proprietario attuale e nessun
prorata temporale inventato. Nome/persona/associazioni sono congelati nel risultato.
Non viene rilevato automaticamente un credito o una rata.

## Preview, conferma e versioni
Preview usa una transazione RepeatableRead di sola lettura: nessuna riga salvata,
nessun audit di conferma o scrittura contabile. Il risultato comprende quote,
percentuali, denominatori, rettifiche, avvisi, errori e differenza di quadratura.
SHA-256 identifica versione motore, chiamante, eventuale riparto sostituito e
snapshot risolto. L'hash e una precondizione di coerenza, non un'autorizzazione.

Confirm acquisisce il lock FOR UPDATE del condominio condiviso con M02/M04,
ricarica e ricalcola tutte le fonti. Per i soggetti espliciti mantiene anche un
lock FOR SHARE sulle anagrafiche globali dello studio, che possono essere aggiornate
fuori dal lock del singolo condominio. Impronta diversa: 409, ripetere il preview.
Esercizio chiuso, documento annullato o budget esaurito bloccano la conferma.

RequestId UUID + hash del comando impediscono duplicazioni. Stessa richiesta
restituisce lo stesso riparto anche dopo chiusura esercizio; stessa chiave con
payload diverso e rifiutata. Due conferme diverse non consumano due volte il budget.
Snapshot, righe, fonti e audit sono salvati nella medesima transazione.

AllocationRun conserva autore, data, versione, motore, InputJson e SnapshotJson
con input risolti E risultato. Lo storico NON ricalcola con il motore corrente.
Gruppi, nomi, tabelle o regole successive non cambiano le quote precedenti.

Solo StudioAdmin puo confermare una sostituzione: PreviousRunId verso l'ultima
versione, stessa serie di documenti e stessi totali per documento, nuovi criteri/
quote/competenza espliciti. Originale e righe immutabili; nuovo UUID/versione +1.
Unica versione successiva, fonti delle versioni sostituite escluse dal consumo
corrente di budget. Nessuna riapertura automatica di esercizi chiusi.

## Sicurezza e vincoli
Tutte le route /api/allocations richiedono membership server-side. TenantId non e
input e PlatformAdmin non bypassa lo studio. Viewer puo leggere e simulare;
Collaborator/StudioAdmin configurano e confermano; sostituzioni solo StudioAdmin.
POST preview richiede CSRF anche se e privo di effetti collaterali. X-Expected-Tenant
protegge anche queste route contro schede con selezione studio obsoleta.

FK composite, check, indici e trigger PostgreSQL affiancano i controlli applicativi:
immutabilita di calcoli/righe/fonti/letture e regole attive; righe ammesse solo nella
transazione di creazione della testata (txid_current, compatibile con savepoint EF).
Constraint trigger differiti verificano quadratura generale/per fonte, budget
documenti, versioni, stato dei riferimenti e corrispondenza dei totali sostituiti.
Le mutazioni applicative usano ReadCommitted con lock del condominio; SQL privilegiato
rimane un confine amministrativo, senza RLS, come nelle milestone precedenti.
Audit: codici evento e UUID, niente snapshot, note, token o credenziali nei log.

Migrazione 20261106103327_ExpenseAllocationMilestone05: nove nuove tabelle,
indici/FK/check/trigger, nessuna modifica retroattiva ai dati o al giornale M04.
Down vietato. Nessuna migrazione automatica all'avvio.

## Limiti della milestone
Unita ammesse secondo lo stato attivo corrente; nessun modello di occupazione
storica delle unita o riparto temporale implicito. Tabelle e letture devono coprire
interamente la competenza richiesta. Un solo esercizio per run, senza riparti su
esercizi chiusi. Al massimo 100 quote, 20 componenti/regola, 2000 unita e 20000 righe
per run. Liste e storico non paginati, come M04: ottimizzazione futura per grandi volumi.
Nessun algoritmo normativo automatico, preventivo, consuntivo, rata, conguaglio,
incasso attribuito o credito generato. Sono argomenti successivi, non simulati qui.

## Documenti annullati dopo un riparto
Uno storno M04 successivo non riscrive il riparto gia confermato. La lettura dello
storico restituisce sourceWarnings e la UI evidenzia Fonte annullata, conservando
il risultato originario. Il documento non e piu ammesso a nuovi calcoli. Non vengono
generati o modificati crediti: una futura rilevazione M06 dovra verificare anche
la validita corrente delle fonti, oltre al risultato matematico storico.
