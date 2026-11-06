# Architettura — Milestone 04
~~~mermaid
flowchart LR
    Web[React / TypeScript] -->|HTTPS same-origin + cookie + CSRF| Api[ASP.NET Core API]
    Api --> Application[Application: contratti, DTO, regole membership]
    Api --> Infrastructure[Infrastructure: Identity, EF, servizi, SMTP]
    Application --> Domain[Domain: modello e invarianti]
    Infrastructure --> Application
    Infrastructure --> Domain
    Infrastructure --> DB[(PostgreSQL)]
~~~

Si riutilizzano i progetti M01/M02. Domain e indipendente da Identity e da EF.
Application contiene ICurrentTenant, contratti e regole; Infrastructure implementa
gli adapter di persistenza/Identity/email. API compone DI e policy HTTP.
Nessuna entita EF viene esposta direttamente.

AppDbContext ora estende IdentityDbContext, conservando le tabelle condominiali
e aggiungendo solo schema di sicurezza. Non si duplicano tenant o anagrafiche.
TenantMembership lega un account a piu studi con ruolo/stato per studio.
Il tenant di sviluppo e rimosso come autorita: valgono cookie validato e
membership riletta dal database, anche in produzione.

API protette per default. Policy, guardie dei servizi e SaveChanges affiancano
filtri EF e FK composite; PlatformAdmin non aggira queste verifiche.
Il frontend mostra i menu secondo i permessi, gestisce 401/403, sessione scaduta,
selezione studio, cambio password e inviti, senza memorizzare token persistenti.
La SPA compilata viene servita dall'API sulla stessa origine HTTPS.

I vincoli M02 rimangono: storia soggetti/unita, quote decimal, bozze/attivazione,
esercizi non sovrapposti e chiusi immutabili. Migrazioni manuali, nessun reset,
seed o bootstrap automatico all'avvio.

Vedere [Identity e sicurezza](identity-security.md),
[ADR cookie](../decisions/0003-identity-cookies.md),
[API](api.md) e [modello M02](milestone-02-data-model.md).
I resoconti M01/M02 sono fotografie storiche, non lo stato delle API attuali.
## Modulo contabile M04
Nuove entita Account, Supplier, ExpenseDocument, FinancialAccount, JournalEntry,
JournalEntryLine ed ExpensePayment, con FK composite tenant/condominio, audit,
precisione monetaria e scritture immutabili. AccountingService riutilizza il lock
del condominio per serializzare contabilizzazioni, pagamenti, storni e chiusure.
Cookie, CSRF, membership e policy M03 continuano a proteggere tutte le route.
Vedere il [modello contabile](milestone-04-accounting.md),
[ADR 0004](../decisions/0004-accounting-ledger.md) e
[verifiche M04](milestone-04-verification.md).
