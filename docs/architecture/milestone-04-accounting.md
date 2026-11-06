# Modulo contabile — Milestone 04

## Struttura e relazioni
Il monolite e i confini Domain/Application/Infrastructure/Api di M03 rimangono.
AccountingModel contiene invarianti, Money e factory del piano predefinito;
IAccountingService/DTO sono in Application; AccountingService coordina EF e
transazioni in Infrastructure. AccountingEndpoints espone solo DTO.
Non vengono introdotti pacchetti o servizi esterni.

~~~mermaid
erDiagram
    Tenant ||--o{ Supplier : possiede
    Condominium ||--o{ Account : configura
    Account o|--o{ Account : padre
    Condominium ||--o{ FinancialAccount : possiede
    FinancialAccount ||--|| Account : conto_attivita
    AccountingPeriod ||--o{ ExpenseDocument : contiene
    Supplier ||--o{ ExpenseDocument : emette
    Account ||--o{ ExpenseDocument : categoria_spesa
    ExpenseDocument ||--o{ ExpensePayment : pagamenti
    FinancialAccount ||--o{ ExpensePayment : utilizza
    AccountingPeriod ||--o{ JournalEntry : contiene
    JournalEntry ||--|{ JournalEntryLine : righe
    Account ||--o{ JournalEntryLine : movimentato
    JournalEntry ||--o| JournalEntry : storno
    JournalEntry ||--o| ExpensePayment : contabilizza
~~~

Ogni nuova entita ha UUID, TenantId, CreatedAt/UpdatedAt UTC. Account, documenti,
conti finanziari, pagamenti e scritture appartengono anche a un condominio.
Le FK composite (TenantId, CondominiumId, Id) impediscono relazioni tra
condomini anche dello stesso studio. Supplier e condiviso solo nello studio.
L'esercizio riutilizza AccountingPeriod di M02; nessuna duplicazione del modello.

## Piano dei conti
Codice normalizzato maiuscolo, univoco per tenant/condominio. Gerarchia senza
cicli; padre nello stesso condominio, attivo e della stessa tipologia.
Tipo e funzione del conto sono immutabili. Codice, nome, descrizione, padre e
stato sono modificabili. Non esiste cancellazione fisica dei conti.
Disattivare prima figli/conto finanziario attivi. I conti di controllo restano attivi.

I nuovi condomini ricevono il piano predefinito nella stessa transazione della
creazione. Per i preesistenti usare Completa piano predefinito: non modifica dati
esistenti; se un codice di controllo e occupato da un conto diverso, occorre
liberarlo esplicitamente prima. Ripetere non duplica i conti.

AccountPurpose identifica Debiti fornitori (Liability), Incassi da attribuire
(Liability), Saldi di apertura (Equity). I nomi/codici sono modificabili senza
alterare il ruolo delle scritture automatiche. Gli altri conti sono General.
La creazione di ogni banca/cassa crea un conto Asset dedicato con codice scelto
dall'operatore: piu banche non confluiscono in un unico saldo indistinguibile.
Non esiste collegamento con tabelle millesimali in questa milestone.

## Importi e partita doppia
Solo EUR: decimal, PostgreSQL numeric(18,2), massimo 9999999999999999.99 per
singolo importo. Piu di due decimali vengono RIFIUTATI, mai arrotondati in
silenzio. Nessuna politica di arrotondamento dei riparti viene anticipata.
Il JSON emette decimal come stringhe invarianti. React conserva le stringhe;
le somme visuali delle righe usano BigInt in centesimi, senza float/double.

Una riga ha solo Dare positivo o solo Avere positivo; nessuna riga zero.
Ogni scrittura ha almeno due righe, somma Dare = somma Avere e viene
contabilizzata interamente nella stessa transazione. Nessuna bozza di giornale
persistita. Data inclusa negli estremi di un esercizio Open, mai Draft/Closed.

| Tipo conto | Saldo naturale positivo |
| --- | --- |
| Asset / Expense | Dare - Avere |
| Liability / Equity / Income | Avere - Dare |

Il riepilogo con esercizio selezionato mostra le VARIAZIONI del solo esercizio,
inclusi gli storni registrati li; non e un bilancio con riporto automatico.
Senza filtro mostra tutti i movimenti storici. La disponibilita attuale banca e
cassa e sempre complessiva, con etichetta esplicita nell'interfaccia.

## Documenti e pagamenti
Un documento in bozza e modificabile solo nell'esercizio aperto, con fornitore
attivo dello studio e conto Expense attivo del condominio. Numero/data/fornitore
sono univoci nello stesso condominio. Scadenza opzionale, non prima del documento.
NetAmount e VatAmount non negativi, GrossAmount positivo ed esattamente uguale
alla somma. IVA zero ammessa. Si contabilizza il LORDO come costo; nessuna
detraibilita o liquidazione IVA viene inferita.

Esempio sintetico 100 + 22 = 122:
- Registrazione: Dare spesa 122, Avere debiti fornitori 122.
- Pagamento parziale 22,01: Dare debiti fornitori 22,01, Avere banca 22,01.
- Secondo pagamento 99,99: stessa logica; residuo zero.

Lo stato e derivato: Draft, Registered, PartiallyPaid, Paid, Cancelled.
Pagato/residuo derivano dai pagamenti NON stornati, senza contatori mutabili.
Un documento contabilizzato non viene riscritto o eliminato. Per annullarlo:
stornare prima tutti i pagamenti attivi, poi la sua registrazione.
Annullare una bozza conserva comunque il record e l'audit.
Il fornitore puo essere disattivato senza perdere documenti o storico.
Pagamenti possono appartenere a un successivo esercizio aperto: non riaprono
quello della fattura; la data non puo precedere il documento.

## Banca, cassa e incassi
OpeningBalance e il saldo dichiarato ALLA CREAZIONE, immutabile. Se diverso da
zero genera una scrittura Asset/Saldi di apertura; ammesso anche saldo banca
negativo. Zero non genera righe artificiali. Il saldo corrente e soltanto la
somma Dare-Avere del conto Asset; OpeningBalance non viene sommato una seconda
volta. Creazione con saldo zero e anch'essa idempotente.

Correzione del saldo iniziale: StudioAdmin storna la scrittura di apertura e
registra una nuova scrittura manuale tra conto finanziario e Saldi di apertura.
Il valore originariamente dichiarato resta nello storico; il saldo corrente
riflette le nuove scritture. Nessun aggiornamento manuale del saldo aggregato.

Incasso generico 200: Dare banca/cassa 200, Avere Incassi da attribuire 200.
Non e automaticamente una rata pagata o una riduzione del debito di un residente.
Scritture manuali consentite sui conti ordinari e finanziari; i conti di controllo
fornitori/incassi sono riservati ai flussi dedicati. Saldi di apertura riservato
allo StudioAdmin per le correzioni. Nessuna integrazione bancaria.

## Concorrenza, retry e storni
Ogni mutazione contabile del condominio acquisisce FOR UPDATE sulla stessa riga
Condominium usata da RegistryService per esercizi e struttura. Due pagamenti,
storni o una chiusura non possono superarsi nella verifica dello stato.
Bozza, registrazione, righe, pagamento e audit vengono salvati atomicamente.
Una violazione anche al commit produce rollback dell'intera operazione.

Le mutazioni che producono scritture richiedono RequestId UUID del client.
Unique (tenant,condominio,RequestId) + hash SHA-256 del comando: stesso comando
restituisce la scrittura originaria; stessa chiave con dati diversi risponde 409.
Lettura del retry prima del controllo esercizio: una risposta persa puo essere
recuperata anche dopo la chiusura, senza creare una nuova scrittura.
Creazione banca/cassa usa una chiave persistita anche per saldo iniziale zero.
La SPA conserva la chiave durante i retry della medesima operazione aperta;
non usa localStorage. Se si chiude il modulo e si reinserisce come nuova operazione,
si genera una nuova chiave: verificare il registro prima di reinserire un pagamento
con esito di rete incerto. Il riferimento bancario non e globalmente univoco.

Storno solo StudioAdmin, in esercizio aperto alla data indicata, non precedente
all'originale. Nuova scrittura con Dare/Avere invertiti, FK all'originale e
unicita dello storno. Originale immutabile; stato Stornato derivato dalla FK.
Nessuno storno dello storno e nessuna riapertura automatica. Per ripristinare un
fatto economico creare una nuova operazione con riferimento all'originale.

## Difese PostgreSQL e sicurezza
Migrazione 20261106091836_AccountingCoreMilestone04: nuove tabelle, indici, FK,
check e sola chiave alternativa aggiuntiva agli esercizi esistenti. Up non
cancella o riscrive righe esistenti, Down rifiutato. Nessun aggiornamento DB
all'avvio; applicazione esplicita. Nessun seed di fornitori o importi reali.

Oltre ai filtri/guardie M03 e alle FK:
- Trigger impediscono UPDATE/DELETE di testate, righe e pagamenti.
- Inserimento testata verifica esercizio Open, data e condominio attivo.
- Le righe si aggiungono solo nella transazione che crea la testata.
  CreationTransactionId usa txid_current(), compatibile con savepoint EF;
  non xmin, che puo essere l'ID di una sottotransazione.
- Constraint trigger DEFERRABLE verifica equilibrio e numero righe al commit,
  fonti dei movimenti automatici, corrispondenza pagamenti e limite al residuo.
- Un documento registrato richiede la propria scrittura; annullamento richiede
  storno e assenza di pagamenti attivi. Unica contabilizzazione per documento.
- Storno deve invertire esattamente i totali per conto dell'originale.
- Tipologia/funzione del conto e saldo/link originari di banca/cassa sono protetti.

Tutte le route /api/accounting richiedono membership attiva; letture anche Viewer,
mutazioni StudioAdmin/Collaborator, storni e correzioni Saldi di apertura solo
StudioAdmin. TenantId non e mai input. Stesso cookie HTTPS, CSRF, policy e
X-Expected-Tenant di M03. PlatformAdmin senza membership non ha accesso.
Audit nella medesima transazione: soli codici evento e UUID, mai note, IBAN,
password, token o payload del documento. Nessuna nuova credenziale richiesta.
SQL privilegiato non e un perimetro di autorizzazione tenant: nessun RLS;
separare ruoli runtime/migrazione e proteggere backup come in M03.

## Limiti espliciti
Nessun riparto, conguaglio, rata, bilancio certificato, riconciliazione bancaria,
OCR, allegato o liquidazione fiscale. Nessun riporto/chiusura automatica dei saldi.
Il registro e le liste attualmente non paginano: valutare paginazione e query
aggregate per volumi maggiori. La banca dati di produzione non e stata toccata;
verifiche con dati sintetici e PostgreSQL isolato nell'harness.
