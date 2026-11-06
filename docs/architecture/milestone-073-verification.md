# M07.3 — Verifica effettiva

Verifiche eseguite tra il 6 e il 7 novembre 2026. I risultati M07.2 sono stati
ricontrollati attraverso la suite reale; nessun risultato storico viene trattato
come prova della nuova implementazione.

## Ambiente e isolamento

Windows, .NET SDK 10.0.403 / target net8.0, PostgreSQL 16 in Docker Desktop, Node 24,
Vite, Playwright con Edge. Verify-Postgres.ps1 crea un progetto Compose con UUID,
database e credenziali casuali, porta loopback 55439. Applica le migration solo
su quel database. Lo storage dei test è separato sotto .artifacts per fixture.
Al termine rimuove esclusivamente container/volume temporaneo creati dalla stessa
esecuzione. Nessun database di lavoro, volume preesistente o file utente modificato.
Nessun commit/push. Conservate le modifiche preesistenti del repository, incluse le
due vecchie fixture UnitTest1.cs già in stato AD prima dell'intervento.

## Comandi e risultati

- `dotnet build CondoManager.slnx`: compilazione riuscita senza warning/errori.
- `scripts/Verify-Postgres.ps1 -Browser -BrowserFilter voting.spec.ts`:
  suite completa iniziale estesa: 104 unitari + 122 integrazione = 226 superati,
  nessuno ignorato; flusso Edge con verbale e rettifica superato.
- `scripts/Verify-Postgres.ps1 -Browser -TestFilter 'FullyQualifiedName~Document'`:
  regression test mirati, incluse concorrenza e deleghe: 3 unitari + 17 integrazione
  superati; tutti i 9 flussi Edge superati insieme.
- Dopo la correzione della larghezza del renderer:
  `scripts/Verify-Postgres.ps1 -Browser -TestFilter 'FullyQualifiedName~Document' -BrowserFilter voting.spec.ts`:
  3 unitari + 18 integrazione superati, Edge documentale superato.
- `npm install --ignore-scripts`: completato, audit 0 vulnerabilità.
- `npm run build`: tsc e Vite riusciti; `npm run lint`: nessun rilievo.
- `docker build -t condomanager:milestone073 .`: riuscito su immagine Linux.
- Container effimero dell'immagine: `/health` HTTP 200; rimosso soltanto quel
  container. Il compose applicativo richiede invece HTTPS e certificato configurato.
- Script SQL delta da VotingMilestone072: nessun DROP TABLE, TRUNCATE, DELETE FROM
  o UPDATE dati. Tre nuove tabelle, FK, indici e trigger; downgrade bloccato.
- `has-pending-model-changes`: nessuna differenza. Undici migration applicate sul
  PostgreSQL isolato, inclusa `20261106223138_DocumentsMilestone073`.

Il warning npm sulla configurazione ambientale min-release-age e quello Playwright
su NO_COLOR/FORCE_COLOR non indicano errori applicativi.

## Regressione conclusiva

`./scripts/Verify-Postgres.ps1 -Browser` ripetuto sul codice definitivo:
**104 unitari + 125 integrazione = 229 test backend superati**, nessuno ignorato.
**9 flussi browser Edge superati insieme** nella medesima esecuzione conclusiva.
Build con zero warning/errori; undici migration applicate su PostgreSQL isolato e
snapshot EF allineato. Il test di larghezza PDF aggiunto verifica anche l'assenza
 di glifi fuori margine e l'uso corretto della larghezza disponibile.

L'immagine Docker aggiornata include Apache-2.0 e attribuzione PdfPig in Licenses,
OFL in Fonts. Verificati UID runtime 1654 (non root), directory documentale
scrivibile e licenze leggibili nel container. Compose con override validato tramite
`docker compose ... config --quiet`, usando valori effimeri di verifica.

## Copertura e prove

DocumentDomainTests verifica revisioni, immutabilità e motivazione rettifiche.
DocumentFileTests verifica byte PDF, testo italiano, accenti, valore EUR sintetico,
testo lungo multipagina, numerazione, margini e larghezza utile, formato/limiti upload,
staging, rename, hash, separazione tenant, traversal, cleanup e file riconosciuti.
DocumentTests usa PostgreSQL/API reali per bozze, finalizzazione/retry, rettifiche,
originale conservato, delibere invariate, ricerca, quattro tipi di prospetto,
autenticazione/ruoli/CSRF, cross-tenant/condominio, upload, nomi sanitizzati, audit,
file mancanti/corrotti, trigger immutabili, errori storage e DB dopo storage,
finalizzazioni concorrenti, revisione superata e deleghe verificate nello snapshot.

Le fault injection sono limitate al DB e ai file sintetici dei test. La verifica
di recupero ordinario dimostra che un rollback non lascia un documento definitivo
apparentemente riuscito. Il recupero dopo crash/commit ambiguo è un comando esplicito
con marker e controllo DB: non è stato simulato un arresto fisico del sistema operativo.

Il flusso Edge voting.spec.ts usa tre unità sintetiche (500/300/200 millesimi) e percorre: quorum, voto,
approvazione preventivo M06 da **106.733,48 EUR**, conclusione, bozza, note,
anteprima, checklist, finalizzazione, download, SHA-256 calcolato indipendentemente
dal browser, rettifica v2, conservazione download v1, prospetto presenze e upload.
Nel caso verificato: quorum costitutivo 3/3 teste, 1000/1000 millesimi;
voto favorevole 2 teste / 700 millesimi, contrario 1 testa / 300 millesimi,
esito Approved secondo il criterio congelato. PDF con importo 106.733,48 EUR,
SHA-256 corrispondente ai byte e v1 ancora scaricabile dopo v2.
La rettifica non cambia la delibera o genera rate/crediti. Verificati desktop e
viewport mobile 390 px senza overflow. Artefatti locali ignorati da Git in .artifacts.

PDF definitivo campione controllato anche con pdfinfo e rendering pdftoppm:
595 × 842 pt (A4), due pagine dopo il corretto wrapping. Ispezionata l'immagine
della prima pagina; nessun ritaglio e piè di pagina separato dal contenuto.
Non sono dati reali o documenti firmati.

## Limiti e stato complessivo

Il ciclo M01–M07 è utilizzabile e verificato con dati sintetici in ambiente isolato.
Non è una certificazione di prontezza produttiva, conformità legale, firma o
conservazione a norma. Restano: antivirus reale, gestione firme/ruoli assembleari,
layout editoriale avanzato, adapter storage remoto, collaudo restore completo,
monitoraggio e politiche di retention. L'archivio iniziale è operativo-only, ricerca
limitata ai primi 250 risultati ordinati; la storia per ID rimane consultabile.
Bilanci e deleghe possono essere archiviati come allegati con collegamenti validati;
non si genera automaticamente un PDF completo del bilancio M06.

Prossime attività proposte, senza implementarle: hardening/backup-restore e scanner;
firma e gestione documentale; invio tracciato tramite provider email/PEC; ottimizzazione
ricerca/paginazione e infrastruttura S3; successivi workflow amministrativi concordati.


