# Modello dati e decisioni — Milestone 02
~~~mermaid
erDiagram
    Tenant ||--o{ Condominium : amministra
    Tenant ||--o{ Person : registra
    Condominium ||--o{ Building : contiene
    Condominium ||--o{ Staircase : contiene
    Building o|--o{ Staircase : raggruppa
    Condominium ||--o{ PropertyUnit : contiene
    Building o|--o{ PropertyUnit : raggruppa
    Staircase o|--o{ PropertyUnit : raggruppa
    Person ||--o{ OwnershipPeriod : partecipa
    PropertyUnit ||--o{ OwnershipPeriod : associa
    Condominium ||--o{ AllocationTable : definisce
    AllocationTable ||--o{ AllocationTableEntry : contiene
    PropertyUnit ||--o{ AllocationTableEntry : riceve
    Condominium ||--o{ AccountingPeriod : apre
~~~

## Identita e integrita
UUID per tutte le entita; CreatedAt/UpdatedAt UTC aggiornati dalla persistenza.
TenantId obbligatorio per dati dello studio. Codici case-sensitive, normalizzati
solo con Trim, univoci per TenantId (condomini e soggetti), o TenantId e
CondominiumId (edifici, scale, unita, tabelle, esercizi). Codici fiscali maiuscoli,
univoci nello studio; null consentito per condomini e record soggetto legacy.
FK Restrict e chiavi alternative composite impediscono riferimenti tra studi
o condomini differenti. Il trigger delle unita impedisce anche discordanza
tra scala e relativo edificio.

Una scala puo riferirsi direttamente al condominio; una unita puo avere nessun
edificio/scala. Se la scala ha un edificio, il servizio lo eredita nell'unita
e rifiuta un diverso edificio esplicito. L'edificio di una scala esistente non
si cambia: si crea la struttura corretta, si ricollegano le unita, si disattiva
la precedente. Edifici/scale con dipendenze attive non si disattivano.

Condomini, edifici, scale, unita e soggetti si disattivano tramite IsActive;
restano visibili con stato e conservano codici/relazioni. Per il condominio,
DELETE e una disattivazione logica. Dati figli di un condominio inattivo non
sono modificabili fino a riattivazione. Nessuna cancellazione fisica nelle API.

## Persone e storico
Person distingue NaturalPerson e LegalEntity, con nome, cognome/ragione sociale,
codice fiscale, partita IVA e recapiti. Email validata sintatticamente.
Nessun dato fiscale viene validato tramite servizi esterni.
OwnershipPeriod e evoluto in associazione generica unita/soggetto, conservando
la tabella e gli UUID della Milestone 01.
Ruoli Owner, CoOwner, Tenant, Usufructuary, Other. Fraction obbligatoria solo
per Owner/CoOwner: decimal numeric(18,10), nell'intervallo (0,1].
Per altri ruoli e null. Date inclusive; ValidTo null indica fine aperta.

Si crea un nuovo record per un nuovo legame. L'unica modifica esposta e la
conclusione di un legame ancora aperto, non una riscrittura del soggetto o
della quota. Nessuna modifica o eliminazione dello storico e esposta.
Intervalli del medesimo soggetto/unita/ruolo non possono sovrapporsi, anche
in richieste concorrenti. Persone/ruoli diversi possono coesistere.
La somma delle quote di comproprieta non e vincolata in questa milestone:
richiede regole funzionali concordate.

## Millesimi
AllocationTable ha codice, nome, denominatore positivo numeric(18,6),
decorrenza e stato Draft/Active. AllocationTableEntry ha quota numeric(18,6)
non negativa, unita e validita. Tutti i calcoli usano decimal e gli input
con piu di 6 decimali sono rifiutati, non arrotondati silenziosamente.
I futuri importi monetari mantengono la convenzione numeric(18,2).

La validazione usa le quote efficaci alla decorrenza della tabella e tutte
le unita attualmente attive del condominio. Richiede una quota per ciascuna
unita, nessun duplicato o unita inattesa, e somma uguale al denominatore.
Zero e consentito ed esplicito; quota mancante non equivale a zero.
Le bozze incomplete possono essere salvate, ma non attivate.
La decorrenza non cambia dopo aver inserito quote.

Attivazione rende immutabili tabella e quote, anche tramite trigger PostgreSQL.
Per modifiche successive creare una nuova tabella/codice versione, mantenendo
la precedente. Una bozza puo essere archiviata; tabelle attive non sono
eliminabili nemmeno in assenza di contabile. Nessuna API elimina fisicamente
tabelle: quando nasceranno riferimenti contabili, le FK Restrict dovranno
essere mantenute e i controlli d'archiviazione estesi.
Non e implementato il motore di riparto.

La validazione riportata da una tabella attiva e ricalcolata rispetto alle
unita correnti: l'inserimento o disattivazione di unita puo segnalarla incompleta
senza riscrivere il suo storico. Occorre una nuova tabella per la nuova
configurazione. Nessuna riattivazione o reset automatico a Draft.

## Esercizi
AccountingPeriod: codice, denominazione, inizio/fine inclusivi, stato
Draft/Open/Closed, CreatedAt e ClosedAt.
Intervalli anche non solari, mai sovrapposti nello stesso condominio, in tutti
gli stati. Un esercizio successivo inizia almeno il giorno dopo la fine.
Vincolo EXCLUDE GiST con btree_gist per integrita anche sotto concorrenza.

Solo Draft modifica codice/nome/date; apertura solo da Draft; chiusura solo da
Open. Chiusura registra timestamp UTC. Nessuna riapertura o eliminazione nelle
API. Trigger PostgreSQL vieta UPDATE/DELETE di una riga gia Closed.
Non vengono calcolati saldi o bilanci, e non sono inventati requisiti di
quadratura da verificare alla chiusura.

## Upgrade non distruttivo
La migrazione 20261105222055_RegistryMilestone02 mantiene:
- Buildings come tabella di Building (prima BuildingOrStaircase).
- PropertyUnits.BuildingOrStaircaseId come colonna mappata a BuildingId.
- MillesimalTables/MillesimalShares come tabelle di AllocationTable/Entry.
- MillesimalTableId come colonna mappata ad AllocationTableId.
- OwnershipPeriods e relativi identificativi/date/quote.

Nuovi campi mancanti restano vuoti/null, da completare dall'utente; persone
legacy restano Kind=Unspecified invece di inventare la loro natura giuridica.
Denominatore legacy 1000, tabelle Draft, decorrenza dalla prima quota se presente,
altrimenti anno 0001 (da completare prima dell'uso). I nuovi flag IsActive
sono true per non nascondere record esistenti.
I vecchi BuildingOrStaircase restano edifici: la precedente ambiguita non viene
risolta inventando una classificazione automatica. Si possono aggiungere scale
e ricollegare le unita senza cancellare i record originari.

Il downgrade e vietato: perderebbe dati nuovi/storico.
Sovrapposizioni legacy incompatibili con i nuovi vincoli fanno fallire la
migrazione transazionale, lasciando lo schema precedente intatto.
La correzione richiede una decisione sul dato: nessuna eliminazione automatica.
Le unita legacy senza tipologia ricevono Other, senza presumere che siano appartamenti.
