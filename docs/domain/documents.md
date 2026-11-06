# Glossario documentale

- **Documento**: metadata immutabili e riferimento a byte privati, identificati da
  SHA-256 e dimensione. L'ID pubblico è distinto dalla chiave storage.
- **Bozza verbale**: testo redazionale modificabile con revisione ottimistica;
  l'anteprima è sempre marcata BOZZA e non è un documento definitivo.
- **Fonte assembleare**: snapshot M07.2 alla conclusione, comprensivo di presenze,
  deleghe, quorum, platee, voti, delibere e applicazioni M06.
- **Consolidamento**: generazione, verifica file e commit dei metadata; riservato a
  StudioAdmin. Non firma il documento e non cambia gli esiti delle delibere.
- **Rettifica**: nuova versione collegata al predecessore, con motivazione;
  conserva il PDF originario e non rettifica una votazione.
- **Superseded**: esiste una versione definitiva successiva; stato di consultazione
  derivato senza riscrivere i metadata storici.
- **Collegamento**: riferimento validato a un'entità dello stesso tenant/condominio.
- **Archivio applicativo**: deposito privato con audit e integrità; non conservazione
  digitale a norma e non prova automatica dell'invio di una convocazione.
- **Marker pendente**: protocollo di recupero per file non ancora riconosciuti da un
  commit DB certo; non autorizza la cancellazione di documenti definitivi.
