# ADR 0011 — PDF da snapshot e storage privato append-only

Stato: adottato nella M07.3, 6 novembre 2026.

Il verbale deve restare fedele alla conclusione M07.2, indipendente dalle anagrafiche
successive e dal filesystem del container. Conserviamo una copia della fonte nella
bozza, metadata e collegamenti nel DB e byte in storage privato persistente.

IDocumentStorage separa il dominio dal filesystem e permette un futuro adapter
S3-compatible. Staging, rename, verifica, marker e recupero esplicito gestiscono
l'assenza di una transazione distribuita PostgreSQL/filesystem. In caso ambiguo
conserviamo il file e il marker; non eliminiamo un documento potenzialmente commesso.

PdfPig 0.1.16 (Apache-2.0) e Noto Sans (OFL) evitano dipendenze commerciali e font
del sistema operativo. Il layout iniziale è sobrio e testabile con estrazione testo;
impaginazione editoriale, firma, conservazione a norma e antivirus sono integrazioni
distinte. Si rinuncia alla modifica diretta dei documenti finali: le rettifiche sono
nuove versioni. Archivio e download inizialmente riservati ai ruoli operativi.
