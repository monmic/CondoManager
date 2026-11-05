# Frontend CondoManager
Dalla directory: npm install, npm run dev, npm run build, npm run lint.
API reale su http://127.0.0.1:5080, proxy Vite /api, frontend su porta 5173.
Vedere README alla radice per database, migrazioni e studio Development.

npm run test:e2e viene avviato da scripts/Verify-Postgres.ps1 -Browser dalla
radice. Richiede Microsoft Edge e database isolato; nessun mock permanente.
I decimal vengono mantenuti come stringhe, totali/validazioni dal server.