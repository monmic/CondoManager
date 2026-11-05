# Requisiti — Milestone 01
Il repository iniziale non conteneva AGENTS.md, README.md o questo documento.
Fonte: richiesta della milestone fornita dal committente nella sessione del 5 novembre 2026.

Fondazioni: modular monolith .NET 8, PostgreSQL/EF Core, React/TypeScript, test,
audit, UUID e isolamento multi-tenant. Solo GET /health pubblico; Swagger solo
Development. Nessuna API di lettura o scrittura dei dati degli studi.
Frontend con navigazione e stato vuoto, senza dati dimostrativi spacciati per reali.
Segreti esclusivamente da ambiente o user secrets.

Esclusi: autenticazione, provisioning studi, regole di riparto, movimenti contabili,
saldi, chiusura esercizi, gestione assemblee e documentale.
Accettazione: build e test .NET, install e build npm, smoke API, migrazione
iniziale applicata a PostgreSQL quando disponibile. Vedere README per i comandi.