# ADR 0005 — Riparti deterministici, normalizzazione esplicita e snapshot
Stato: adottato in Milestone 05.

## Decisione
Motore IAllocationEngine puro in Application, entita e invarianti in Domain,
risoluzione autorizzata/persistenza in Infrastructure. Non collegare il calcolo
a EF o alla UI e non creare movimenti contabili durante preview/conferma.

Usare decimal e maggiori resti: prima budget dei componenti, poi quote dei
destinatari. Resti razionali confrontati con BigInteger, senza float/double.
Spareggio per posizione componente o codice unita maiuscolo Ordinal + UUID unita
+ UUID soggetto. Quote teoriche a 12 decimali solo descrittive. Ogni totale finale
rimane esattamente uguale all'importo in centesimi.

Distinguere denominatore nominale ed effettivo. Gruppi/esclusioni che cambiano il
totale richiedono NormalizeAdmitted esplicito in una regola approvata, con avviso
congelato nel calcolo. Nessuna normalizzazione o stima silenziosa.

Regole attive immutabili con nuove versioni; letture rettificate per aggiunta.
Snapshot di gruppi, quote, letture, soggetti, input e risultati. Lo storico legge
il risultato congelato invece di ricalcolare con una versione futura del motore.

Preview di sola lettura e fingerprint SHA-256 delle fonti. Conferma serializzata
sul condominio, fonti ricaricate e ricontrollate, idempotenza per RequestId.
Una sostituzione completa mantiene documenti/totali finanziari originali, indica
il predecessore ed e riservata a StudioAdmin. Le quote del predecessore restano
storiche senza consumare due volte il budget. Vincoli e trigger PostgreSQL
verificano immutabilita, unicita e quadratura anche al commit.

## Conseguenze
Configurazione e arrotondamenti sono espliciti e riproducibili. Nessuna assegnazione
implicita al proprietario attuale: addebiti personali richiedono associazione
storica valida per l'intera competenza. Per cambi di soggetto dividere le competenze.
Maggiore spazio per snapshot, ma piena tracciabilita. Lock per condominio privilegia
integrita rispetto al throughput del singolo condominio. Budget calcolato sui run
correnti, senza contatori aggregati modificabili manualmente.

Prorata temporali, normativa termica, conguagli e generazione crediti/rate richiedono
decisioni e verifiche distinte. Non sono anticipati da questo motore.
Vedere [formule, modello e limiti](../architecture/milestone-05-allocations.md).
