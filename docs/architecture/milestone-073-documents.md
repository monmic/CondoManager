# M07.3 — Documenti e verbali

## Moduli e fonti

Domain definisce CondominiumDocument, DocumentLink e MeetingMinutes. Application
espone DTO, IDocumentService, IDocumentStorage, IDocumentPdfGenerator e il punto
di integrazione IDocumentScanner. Infrastructure implementa EF, storage locale e
PDF. API mantiene cookie Identity, membership attiva, CSRF e precondizione
X-Expected-Tenant. Nessun percorso storage è un identificatore pubblico.

Il verbale legge **MeetingCompletion.ContentJson**, consolidato dalla M07.2,
non ricostruisce voti o titolarità dalle anagrafiche correnti. Conserva una copia
integrale della fonte, fingerprint, identificativo e data della conclusione.
L'intestazione del condominio viene congelata alla prima bozza: il modello M07.2
non contiene lo snapshot del suo indirizzo. Le rettifiche mantengono questa fonte.
I dati nello snapshot conclusivo descrivono lo stato immediatamente precedente
alla transizione Completed; la data effettiva di conclusione deriva dal record
MeetingCompletion. L'orario di apertura non registrato non viene inventato.

## Workflow

Assemblea Completed → CreateDraft → Edit → Preview/Validate → Finalize.
Le modifiche richiedono Revision; tutte le scritture serializzano sul lock del
condominio, condiviso con i servizi precedenti. Finalize richiede StudioAdmin,
verifica nuovamente fonti e stato, genera byte, verifica storage, registra documento,
collegamenti e audit nella stessa transazione PostgreSQL. Il RequestId è univoco
nel condominio e confrontato con l'hash della richiesta. Ripetere la stessa richiesta
restituisce lo stesso documento. Una chiave riutilizzata con parametri diversi fallisce.

Le versioni definitive sono append-only, protette anche da trigger PostgreSQL.
Una rettifica deve indicare l'ultima versione e una motivazione; produce una nuova
bozza/PDF e collega il predecessore. La versione precedente resta scaricabile.
Lo stato Superseded nella consultazione è derivato dall'esistenza di un successore
definitivo; il record originale non viene riscritto. Nessuna API modifica le fonti
M07.2 o applica nuovamente approvazioni M06.

## PDF e licenze (verifica 6 novembre 2026)

PdfPig **0.1.16**, pacchetto NuGet con licenza Apache-2.0; generazione ed estrazione
del testo sono documentate dal progetto:

- <https://github.com/UglyToad/PdfPig>
- <https://uglytoad.github.io/PdfPig/>
- <https://www.nuget.org/packages/PdfPig/0.1.16>

Noto Sans Regular è incorporato come risorsa, con licenza SIL OFL 1.1 inclusa in
Infrastructure/Fonts/OFL.txt. Fonte:
<https://github.com/notofonts/noto-fonts/tree/main/hinted/ttf/NotoSans>.
Non dipende dai font installati su Windows e non scarica font a runtime.
PDFsharp corrente è stato escluso dopo aver verificato il passaggio ad
AGPL/commerciale descritto su <https://www.pdfsharp.com/>; non si assume che la
licenza delle vecchie versioni si applichi automaticamente alle nuove.

Il renderer usa A4, margini, intestazione, piè di pagina e numerazione totale.
I prospetti sono righe testuali che proseguono su più pagine: non è un impaginatore
editoriale con colonne complesse. Il testo lungo viene spezzato misurando i glifi,
anche in assenza di spazi. Noto Sans copre il testo italiano; non si dichiara supporto
universale per scritture complesse, emoji o glifi non presenti nel font.
Le date sono convertite in Europe/Rome con offset esplicito. Nessun importo
contabile è calcolato con floating point; le coordinate grafiche non sono importi.

## Storage e protocollo di recupero

Documents:RootPath configura la directory privata. Default locale:
AppContext.BaseDirectory/private-documents, ignorata da Git. In Docker:
/var/condomanager/documents, volume document_files dedicato. Non è wwwroot e non
viene montata in un server statico. Le directory sono separate per UUID tenant;
le chiavi sono UUID opachi generati dal server. Si rifiutano chiavi non valide e
directory/file con reparse point. Su Linux le directory hanno permessi 0700.

Save scrive un file .stage con CreateNew e un marker .pending; rilegge i byte e
calcola SHA-256 e dimensione. Commit promuove con rename nello stesso filesystem.
Prima del commit DB verifica nuovamente metadata e disponibilità. Nessuna risposta
di successo viene emessa prima del commit DB. A fine scope il servizio verifica il
record DB: se esiste, rimuove il marker; se non esiste, elimina soltanto il file
ancora marcato come non confermato. Un file riconosciuto dal DB non viene eliminato.

Un crash/DB irraggiungibile può lasciare un marker. Il recupero esplicito locale:

```powershell
dotnet run --project src/backend/CondoManager.Api --no-launch-profile -- --document-storage-recover
```

Richiede connection string e percorso corretti, non avvia HTTP, considera soltanto
marker più vecchi di 24 ore e interroga il DB prima di eliminare un orfano. Se il DB
non risponde o un file referenziato non supera l'integrità, si ferma. Eseguire in
manutenzione, con le istanze applicative arrestate: non è un garbage collector dei
documenti definitivi. Il comando non viene eseguito automaticamente né nei test sul
database di lavoro. DeleteDraft non elimina un file finale riconosciuto; il recupero
può rimuovere soltanto file con marker pendente e assenza verificata di metadata.

Download richiede lookup autorizzato per ID documento, rilegge dimensione/hash e
blocca file mancanti o corrotti; audit solo dopo controllo riuscito. StorageKey non
è restituita nei DTO. Backup e ripristino devono includere DB, file e key ring.

## Accessi, allegati e limiti

L'intero archivio è riservato a StudioAdmin/Collaborator: contiene recapiti e dati
personali. Viewer non può elencare o scaricare questi documenti. Finalize solo
StudioAdmin; nessun bypass PlatformAdmin. Audit conserva eventi e identificativi,
non contenuti, recapiti, token o nomi di file.

Upload: PDF, JPEG, PNG; limite predefinito 8 MiB (Documents:MaxUploadBytes).
Verifica estensione, MIME, signature e parser del formato; PDF deve essere leggibile,
PNG ha limite 25 milioni di pixel. Nome del client sanitizzato, mai usato come path.
Non sono accettati HTML o DOCX. Download attachment e nosniff; non esegue né rende
HTML/caricamenti nel DOM. IDocumentScanner è il punto di sostituzione per un antivirus:
il provider iniziale **non effettua scansione** e non certifica l'assenza di malware.
I parser e i magic byte non sostituiscono uno scanner o una sandbox.

DocumentLink ammette esercizio, assemblea, ODG, convocazione, delega, delibera,
versione preventivo/consuntivo. Ogni destinazione è validata per tenant/condominio
sia nel servizio sia nel trigger di inserimento. I collegamenti si dichiarano
all'archiviazione: quelli definitivi non sono modificabili in silenzio.

Prospetti presenze/votazioni derivano dall'intero dossier conclusivo (includono
anche il contesto assembleare). Estratto delibera conserva testo, risultato e fonte.
Convocazione deriva dai dati M07.1 e dichiara esplicitamente che il PDF non prova
invio o consegna; l'invio resta manuale. Non esiste servizio email documentale.

Il PDF **non equivale automaticamente a un documento firmato**. Presidente e
segretario non designati sono campi vuoti, senza nomi inventati. L'archiviazione
applicativa **non equivale alla conservazione digitale a norma**. Non si dichiara
conformità normativa complessiva, firma digitale, marcatura temporale o PDF/A.
Le delibere mantengono la propria fonte di verità nella M07.2.
