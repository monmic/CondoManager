# Architettura — Milestone 02
~~~mermaid
flowchart LR
    React[React / TypeScript] -->|/api via Vite| Api[ASP.NET Core net8.0]
    Api --> Application[Application: contratti e DTO]
    Api --> Infrastructure[Infrastructure: RegistryService e AppDbContext]
    Infrastructure --> Application
    Application --> Domain[Domain: entita e invarianti]
    Infrastructure --> Domain
    Infrastructure --> PostgreSQL[(PostgreSQL 16)]
~~~

Rimangono gli stessi progetti della Milestone 01. Domain non dipende da
framework o persistenza. Application espone ITenantContext, IRegistryService,
input/output DTO ed errori applicativi. RegistryService in Infrastructure
implementa questi use case con transazioni EF e richiama le regole del dominio:
non e stato aggiunto un repository generico o EF ad Application.
Api compone DI, converte gli errori in ProblemDetails e mappa esclusivamente DTO.
Le entita persistite non vengono serializzate direttamente.

AppDbContext applica filtri tenant per istanza e controlla tutte le scritture;
TenantId e concurrency token per UPDATE/DELETE anche di entita scollegate.
FK composite includono tenant e, dove necessario, condominio.
Migrazioni manuali: nessuna creazione automatica di schema o dati all'avvio.

Il tenant di sviluppo e un'eccezione esplicita all'attesa dell'autenticazione:
viene letto dalla configurazione server dopo la composizione dell'host,
mai dalla richiesta. API dati registrate solo in Development con UUID valido.
Endpoint filter verifica anche l'esistenza dello studio. In Production/Staging
rimangono solo health e session (metadati di disponibilita, nessun dato tenant).
Non e un modello di autorizzazione per un deployment esposto.

Per mutazioni delle strutture, quote, legami ed esercizi, il servizio prende un
lock transazionale sulla riga del condominio, filtrato anche per TenantId.
Questo serializza modifiche concorrenti dello stesso condominio. PostgreSQL
impone inoltre esclusione degli intervalli per esercizi e legami dello stesso
soggetto/unita/ruolo, e trigger di immutabilita per esercizi chiusi e tabelle attive.

Il frontend usa API reali, stati caricamento/errore/vuoto, form riutilizzabili,
conferme per attivazione/chiusura e una navigazione adattabile al mobile.
Fetch annulla le richieste obsolete e non mantiene i dati di una selezione diversa.
Nessun dato mock permanente. Decimal trasferiti come stringhe JSON per conservarne
le cifre; totali e validazione sono calcolati sul server.

Vedere [modello dati](milestone-02-data-model.md),
[API](api.md), [ADR tenant](../decisions/0002-development-tenant.md).
Il resoconto della Milestone 01 resta una fotografia storica, non descrive
le API operative introdotte in questa milestone.