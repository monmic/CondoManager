# Verifica Milestone 01
Eseguita su Windows il 5 novembre 2026.
SDK .NET 10.0.403, target net8.0, Node 24.13.0, npm 11.6.2,
Docker Engine 29.4.0, PostgreSQL 16 tramite Compose.

| Comando / verifica | Esito |
| --- | --- |
| dotnet tool restore | OK, dotnet-ef 8.0.11 |
| dotnet build | OK, 0 warning / 0 errori |
| dotnet test, nello script PostgreSQL | OK, 3 unitari + 5 integrazione, nessuno ignorato |
| npm install | OK, audit: 0 vulnerabilita |
| npm run build | OK |
| npm run lint | OK |
| dotnet ef migrations add InitialFoundation --project src/backend/CondoManager.Infrastructure | OK |
| docker compose up -d --wait, progetto temporaneo | OK, PostgreSQL healthy |
| dotnet ef database update --project src/backend/CondoManager.Infrastructure | OK, applicata InitialFoundation |
| dotnet ef migrations has-pending-model-changes --project src/backend/CondoManager.Infrastructure | OK, modello allineato |
| Processo API reale in Production, GET http://127.0.0.1:5088/health | HTTP 200, {"status":"healthy"} |

Test: invarianti, precisione dei decimali, filtri tenant per contesto,
assenza di contesto, scritture non autorizzate, tenant immutabile,
FK composite, cancellazione di entita scollegata di altro tenant respinta,
route dati assenti e Swagger non pubblico in Production.
PostgreSQL: migrazione, lettura/scrittura reale, round trip decimali e FK.

La verifica completa e ripetibile con scripts/Verify-Postgres.ps1.
Il test PostgreSQL richiede CONDOMANAGER_TEST_POSTGRES; fuori dallo script,
in sua assenza viene esplicitamente ignorato.
Container, network e volume temporanei rimossi; processo API di smoke terminato.
Non e stata effettuata una verifica visiva in browser del layout responsive.

Nota ambiente: npm segnala Unknown env config "min-release-age", senza
impatti su installazione, build o lint.
Git non era inizializzato: nessun repository o commit creato.
AGENTS.md, README.md e docs/requirements/README.md erano assenti:
le istruzioni sono state cercate prima delle modifiche; README e requisiti
sono stati creati per documentare questa milestone.