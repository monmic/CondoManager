# Requisiti e perimetro
Fonte: richieste del committente Milestone 01 e Milestone 02 nella sessione.

Milestone 01 ha introdotto modular monolith net8.0, EF/PostgreSQL, React,
audit/UUID e fondazioni tenant. Il suo resoconto resta in architecture.

Milestone 02 rende operative le anagrafiche:
- Tenant server configurato esclusivamente in Development in assenza di auth.
- CRUD condomini con indirizzo, fiscale opzionale e stato.
- Edifici, scale facoltative e unita con tipologia, catasto e note.
- Soggetti fisici/giuridici, recapiti e legami storici con unita/ruoli/date/quote.
- Tabelle millesimali decimal con bozze, validazione totale/completezza e attivazione.
- Esercizi non solari con divieto sovrapposizione e ciclo Draft/Open/Closed.
- DTO, API REST reali, errori coerenti, interfaccia operativa, test automatici.
- Migrazione senza eliminare dati o reinterpretare arbitrariamente anagrafiche legacy.

Restano esclusi autenticazione, membership SaaS, motore contabile/riparti,
bilanci, gestione assemblee e documenti.
La modalita Development e una deroga esplicita al blocco API dati della
Milestone 01, documentata in ADR 0002. Nessun TenantId client e attendibile.

AGENTS.md continua a non essere presente; non e stato inventato un file di
istruzioni. Nessun commit automatico o modifica intenzionale dell'indice Git.