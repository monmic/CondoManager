# ADR 0001 — Database condiviso e tenant esplicito
Stato: accettato per Milestone 01.

Ogni dato dello studio ha TenantId; Tenant identifica lo studio stesso.
ITenantContext deve derivare da autenticazione verificata e appartenenza
autorizzata allo studio. Header, query string e body non sono fonti attendibili.
In questa milestone l'implementazione restituisce sempre null: non esistono
route dei dati né provisioning, e nessun header può sbloccarle.

EF applica filtri a tutte le entità, inclusi gli studi: nessun contesto significa
nessuna riga. SaveChanges sincrono/asincrono rifiuta assenza di contesto, tenant
diverso e cambi di tenant; il provisioning Tenant è vietato. Le FK composite
impediscono riferimenti tra tenant; i codici sono univoci entro il relativo ambito.
Non viene usato il pooling dei DbContext.

Conseguenze: questi controlli sono una barriera applicativa, non PostgreSQL RLS.
IgnoreQueryFilters, SQL diretto, ExecuteUpdate/ExecuteDelete e accesso diretto al
database possono aggirarli: non usarli per operazioni tenant in percorsi runtime.
SQL diretto compare solo nei test di vincoli, con dati sintetici e rollback.
Migrazioni hanno connessione amministrativa separata dal contesto runtime.

Prima delle API dati: autenticazione, validazione membership, contesto scoped
immutabile per richiesta e test end-to-end per accessi negati e tenant multipli.
Valutare RLS e ruoli PostgreSQL separati prima della produzione.
Questo ADR non autorizza a fidarsi di un TenantId fornito dal client.