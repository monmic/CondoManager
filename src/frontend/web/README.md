# Frontend CondoManager — Milestone 03
npm install; npm run build; npm run lint.
Avvio principale: SPA dist servita dall'API HTTPS su https://localhost:7262.
Vedere README alla radice per database, migrazione e bootstrap amministratore.
Nessuna modalita tenant fittizia o autenticazione disattivata.

Vite opzionale richiede DEV_HTTPS_PFX_PATH e DEV_HTTPS_PFX_PASSWORD.
Il proxy locale inoltra a https://localhost:7262. Non usare HTTP per i cookie Secure.

Auth, selezione studio e permessi sono reali; header/controlli UI non concedono
autorizzazioni. Token CSRF e link di invito/reset restano in memoria, nessun
localStorage. Cookie HttpOnly gestito esclusivamente dal browser.

Test browser: scripts/Verify-Postgres.ps1 -Browser dalla radice, con Edge e
certificato locale installato. Database temporaneo, account e password casuali,
login reale e nessun endpoint che aggiri la sicurezza. Trace auth disabilitate.