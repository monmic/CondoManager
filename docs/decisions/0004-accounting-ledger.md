# ADR 0004 — Partita doppia immutabile e contabilizzazione atomica
Stato: adottato nella Milestone 04.

## Decisione
Riutilizzare AccountingPeriod e l'isolamento M03. Registrare ogni fatto con una
testata e almeno due righe Dare/Avere bilanciate. Importi decimal/numeric(18,2),
nessun arrotondamento implicito. Costi al lordo IVA; IVA descrittiva senza inferire
trattamenti fiscali. Tipo e ruolo dei conti immutabili; gerarchia configurabile.

Il saldo e una query sulle righe: nessun saldo corrente aggiornabile separatamente.
Ogni banca/cassa ha un Asset dedicato; aperture non nulle hanno contropartita
Equity. Incassi non attribuiti hanno contropartita Liability e non saldano rate.
Riepilogo per esercizio come variazioni, senza riporto automatico non concordato.

Documenti registrati, giornale e pagamenti non sono riscrivibili. Correzione con
storno in esercizio Open e nuova registrazione; mai riaprire quello chiuso.
Storno collegato/unico, inversione esatta dei conti, riservato a StudioAdmin.

Una transazione salva fatti, righe, pagamento e audit. Lock per condominio
condiviso con apertura/chiusura esercizi. Retry con UUID e fingerprint del comando;
nessun pagamento duplicato per il medesimo comando. Non deduplicare arbitrariamente
sul solo riferimento libero, che puo essere condiviso da operazioni legittime.

## Difesa in profondita
Controlli Domain/Application, ICurrentTenant, FK composite tenant/condominio,
unicita e trigger PostgreSQL. Verifica differita del giornale al commit;
immutabilita dopo inserimento e righe ammesse solo nella transazione di creazione.
Si conserva il top-level transaction ID: xmin non e adatto con i savepoint EF.
Migrazione solo additiva e forward-only, nessun reset o migrazione automatica.

## Conseguenze
Serializzare le mutazioni di un condominio semplifica integrita e concorrenza,
a costo di throughput per singolo condominio. I condomini restano indipendenti.
Il frontend mantiene importi come stringhe e usa BigInt per i totali delle righe.
I controlli fiscali, riparti, crediti individuali, competenza pluriennale e riporto
saldi richiederanno decisioni esplicite future; non sono simulati in questa fase.
Vedere il [modello contabile](../architecture/milestone-04-accounting.md).
