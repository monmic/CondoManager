# Requisiti e perimetro
Fonte: richieste del committente e AGENTS.md.

M01: fondazioni net8.0, EF/PostgreSQL, React, UUID/audit, isolamento tenant.
M02: anagrafiche, unita, persone/storico, tabelle millesimali, esercizi.
M03: Identity, cookie/CSRF, membership multi-studio, ruoli, inviti/reset,
bootstrap CLI, audit e test di autorizzazione.

L'eccezione tenant Development di M02 e sostituita da autenticazione verificata
in TUTTI gli ambienti (ADR 0003). Nessun header o DTO autorizza da solo un tenant.
Account globale e membership studio sono distinti. PlatformAdmin non bypassa
le membership per i dati condominiali.

Nessuna registrazione pubblica degli studi, impersonazione, reset database o
seed automatico. Migrazione M03 additiva e bootstrap manuale/protetto.
Provider email astratto con SMTP TLS e casella Development autorizzata, mai
token pubblici o in log di produzione.

M04: piano dei conti, fornitori, documenti, pagamenti, banca/cassa e movimenti a partita doppia. Vedere docs/architecture/milestone-04-accounting.md e il resoconto M04. Restano esclusi riparti, conguagli, rate, bilanci certificati, assemblee e documenti. I resoconti precedenti rimangono fotografie storiche.