# M07.2 — Fonti normative e limiti del modello

Verifica documentale: **6 novembre 2026**. Fonti consultate direttamente:

- [Gazzetta Ufficiale, L.220/2012 art.14](https://www.gazzettaufficiale.it/atto/serie_generale/caricaArticolo?art.codiceRedazionale=012G0241&art.dataPubblicazioneGazzetta=2012-12-17&art.flagTipoArticolo=0&art.idArticolo=14&art.idGruppo=0&art.idSottoArticolo=1&art.idSottoArticolo1=10&art.progressivo=0&art.versione=1): testo sostitutivo dell'art.1136 c.c.
- [Gazzetta Ufficiale, L.220/2012 art.16](https://www.gazzettaufficiale.it/atto/serie_generale/caricaArticolo?art.codiceRedazionale=012G0241&art.dataPubblicazioneGazzetta=2012-12-17&art.flagTipoArticolo=0&art.idArticolo=16&art.idGruppo=0&art.idSottoArticolo=1&art.idSottoArticolo1=10&art.progressivo=0&art.versione=1): art.1138 c.c., con note di testo.
- [Gazzetta Ufficiale, L.220/2012 art.21](https://www.gazzettaufficiale.it/atto/serie_generale/caricaArticolo?art.codiceRedazionale=012G0241&art.dataPubblicazioneGazzetta=2012-12-17&art.flagTipoArticolo=0&art.idArticolo=21&art.idGruppo=0&art.idSottoArticolo=1&art.idSottoArticolo1=10&art.progressivo=0&art.versione=1): art.67 disp. att. c.c.
- [Portale istituzionale Giustizia Tributaria, art.1138](https://def.giustiziatributaria.gov.it/DocTribFrontend/executePrintArticolo.do?articolo=Articolo+1138&codiceOrdinamento=0000000000011380000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000&id=%7B9E93F1BE-06AE-4F24-8E9D-B838F7E0C2E6%7D): riscontro del testo con decorrenza indicata dal 18 giugno 2013.

## Modelli numerici ricavati dai riferimenti consultati

L'art.1136 riportato nell'art.14 distingue costituzione e deliberazione.
Il riferimento per la prima costituzione combina maggioranza dei partecipanti
con due terzi del valore; per la seconda, un terzo delle teste e del valore.
Per le deliberazioni ordinarie il riferimento numerico è maggioranza degli
intervenuti, insieme a metà del valore in prima o un terzo in seconda.
I commi 4 e 5 richiamano maggioranze speciali per le materie elencate: sono
presenti modelli con metà o due terzi del valore, senza classificatore automatico.
Il testo considera anche regolare convocazione e tempistica tra sessioni.

L'art.1138 richiama il secondo comma dell'art.1136 per il regolamento e pone
limiti alle deroghe. Un criterio personalizzato non rende derogabile la legge.
L'art.67 tratta delega scritta, limite di un quinto oltre venti condomini,
rappresentanza unitaria in comproprietà, divieto di delega all'amministratore,
regole del supercondominio e differenziazioni per usufrutto/materia.

## Cosa NON è stato certificato

Il caricamento della versione vigente degli artt.1136/1138 su Normattiva ha
restituito errori del portale tramite lo strumento di consultazione. Non è stata
quindi certificata la completezza del quadro normativo vigente al giorno della
verifica. Le fonti Gazzetta sopra sono testi pubblicati, non prova dell'assenza
successiva di modifiche o norme speciali. Non sono state ricavate soglie da
proposte di legge, articoli privati o risultati di ricerca non verificati.

I template sono **riferimenti matematici configurabili**, non un catalogo completo
di regole legalmente applicabili. Sono restituiti dall'API come valori iniziali:
nessun criterio viene inserito o attivato automaticamente nel database.
StudioAdmin deve scegliere finalità, convocazione, materia, validità, denominatori
e ambito; l'attivazione richiede una nota di revisione tracciata.

Non vengono dedotte maggioranze da categorie generiche come Maintenance o
ExtraordinaryExpense. Non sono implementate selezione automatica di norme
speciali (ad esempio energia, interventi agevolati o discipline derogatorie),
valutazioni di notevole entità, supercondominio o conformità del regolamento.
Non vengono verificate automaticamente modalità/termini legali di convocazione,
natura effettiva della PEC, identità dell'amministratore, limiti numerici complessivi
delle deleghe o validità formale del documento. L'operatore deve motivare la
verifica nell'ambito del criterio e nella platea. Il controllo di un amministratore
indicato esplicitamente nella platea impedisce l'utilizzo di deleghe a tale soggetto.

Il software verifica invece automaticamente aritmetica, scope, versioni,
rappresentanze duplicate, presenze all'istante scelto, deleghe operative verificate
con riferimento scritto, coerenza delle quote e invii dichiarati per gli inclusi.
Se la titolarità per materia è ambigua, occorre una scelta esplicita tra titolari
storici. Se non è possibile ricostruire correttamente le teste, l'esito resta
Undetermined: non si improvvisano raggruppamenti giuridici o quote.

Un risultato Approved indica che **i requisiti del criterio selezionato** sono
soddisfatti sui dati dichiarati e congelati; non è una certificazione di validità
legale della delibera. L'integrazione M06 resta un comando amministrativo esplicito.
