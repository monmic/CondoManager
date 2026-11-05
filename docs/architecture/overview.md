# Architettura
~~~mermaid
flowchart LR
  Web[React / TypeScript] --> Api[ASP.NET Core API]
  Api --> Application
  Api --> Infrastructure
  Infrastructure --> Application
  Application --> Domain
  Infrastructure --> Domain
  Infrastructure --> DB[(PostgreSQL)]
~~~
Domain è indipendente da framework e persistenza; Application definisce
ITenantContext; Infrastructure contiene AppDbContext, Fluent mapping e migrazioni.
Api compone DI. Nessun repository generico.

Il frontend è al momento indipendente dai dati: navigazione locale e stati vuoti.
L'unica route applicativa pubblica è GET /health: indica vitalità del processo,
non disponibilità di PostgreSQL. L'API può partire senza connessione configurata;
risolvere AppDbContext richiede ConnectionStrings__CondoManager.
Swagger e relativo JSON sono registrati solo in Development. Nessuna migrazione
automatica all'avvio: applicarla esplicitamente con gli strumenti EF.

Modello: Tenant (studio), Condominium, BuildingOrStaircase, PropertyUnit, Person,
OwnershipPeriod, MillesimalTable, MillesimalShare, AccountingPeriod.
UUID, audit UTC, cancellazioni Restrict, codici univoci per studio o condominio.
FK composite includono TenantId; scala/unità e tabella/quota condividono anche
CondominiumId. Persona e unità sono entità distinte.

DateOnly per validità con estremi inclusivi; fine nulla significa validità aperta.
Quota proprietà: decimal (0,1], numeric(18,10); millesimi: non negativi,
numeric(18,6). Futuri importi: decimal, convenzione numeric(18,2).
Non sono ancora imposte somme a 1000, somma quote proprietarie, esclusioni di
sovrapposizioni o regole fiscali/contabili: richiedono requisiti concordati.

SaveChanges applica audit e valida il tenant anche per entità scollegate.
Filtri di query dipendono dall'istanza del contesto (non da un valore memorizzato
globalmente). Vedere ADR 0001 per limiti e confini di sicurezza.

Test unitari sulle invarianti; integrazione API in memoria e isolamento EF;
verifica PostgreSQL reale tramite scripts/Verify-Postgres.ps1, con database
temporaneo e credenziali generate in memoria. Nessun dato reale.
TenantId è anche concurrency token: UPDATE/DELETE includono il tenant nella
clausola WHERE, impedendo di colpire ID appartenenti a un altro studio.
