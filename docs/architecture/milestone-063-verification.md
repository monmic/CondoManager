# M06.3 — Verifiche del 6 novembre 2026

## Ambiente e metodo

Windows, .NET SDK 10.0.403 con target net8.0, PostgreSQL 16 Docker,
Node/npm e Microsoft Edge tramite Playwright. Dati esclusivamente sintetici.
Nessun commit o push. Nessuna migration applicata al database di lavoro.

Il baseline verificato prima dell'incremento comprendeva 132 test backend.
Il comando riproducibile è:

```powershell
.\scripts\Verify-Postgres.ps1 -Browser
```

Il progetto Compose ha un nome casuale e un volume proprio; soltanto queste
risorse di verifica vengono rimosse. Le credenziali temporanee sono generate
in memoria, mai aggiunte ai file versionati.

## Controlli eseguiti

- Applicazione di tutte le otto migration, inclusa
  `20261106145113_FinalStatementsMilestone063`, su PostgreSQL vuoto isolato.
- `dotnet ef migrations has-pending-model-changes`: modello allineato.
- `dotnet build`: zero warning ed errori.
- `dotnet test`: 69 unitari e 84 integrazione PostgreSQL, 153 distinti.
- `npm run build`: TypeScript e Vite.
- `npm run lint`: oxlint.
- `npm run test:e2e`: sette flussi Edge, incluso il ciclo M06.3 fino alla chiusura.
- Generazione SQL tra M06.2 e M06.3 in `.artifacts/milestone-063.sql` e revisione:
  nessun DROP TABLE, TRUNCATE, DELETE o aggiornamento retroattivo dei dati contabili.
  L'estensione dei tipi contabili sostituisce check constraint conservando i dati.
- `git diff --check`: nessun errore whitespace; avvisi locali LF/CRLF.

Le prove di dettaglio risiedono in `FinalCalculationTests.cs`,
`FinalStatementTests.cs` e `e2e/final-statements.spec.ts`.
I test precedenti che chiudevano direttamente l'esercizio sono stati aggiornati
per usare il vero workflow tramite `AdministrativeTestFixture`, senza disattivare
vincoli o autenticazione. La vecchia route di chiusura è verificata come rifiutata.

## Copertura dei casi richiesti

| Scenari della richiesta | Evidenza |
|---|---|
| 1–5: creazione, versioni, approvazione, concorrenza e immutabilità | Integrazione: approvazioni concorrenti, sostituzione dopo storno, modifiche SQL vietate |
| 6–12: costi e riparti | Fattura registrata, pagamento escluso, incasso escluso, fonte variata, riparto mancante, quadratura esatta |
| 13–17: confronti | Calcolatori puri e integrazione categorie presenti in un solo bilancio, percentuale nulla con preventivo zero |
| 18–28: conguagli e finanza | Importi positivi/negativi/zero, parziale e non attribuito, importazione e riporto, retry, storno, proprietario storico e debitore ambiguo |
| 29–36: chiusura | Checklist bloccante, insoluti validi, chiusura ripetuta, race con nuova registrazione e controlli PostgreSQL |
| 37–40: sicurezza | Tenant diverso, altro condominio dello stesso tenant, Viewer/Collaborator insufficienti, CSRF assente |

Gli scenari sono raggruppati in dieci nuovi casi unitari e undici test
di integrazione; non corrispondono a quaranta test indipendenti.
La suite preesistente continua a verificare attribuzioni, riconciliazioni,
concorrenza sui pagamenti e isolamento del giornale.

## Esempio end-to-end

Preventivo 1.000 €, crediti rateali emessi 1.000 €, fattura registrata 1.100 €.
Millesimi A/B/C: 500/300/200. Incasso attribuito ad A: 400 €.

| Unità | Preventivo | Consuntivo | Conguaglio | Saldo finale |
|---|---:|---:|---:|---:|
| A | 500,00 | 550,00 | +50,00 | 150,00 |
| B | 300,00 | 330,00 | +30,00 | 330,00 |
| C | 200,00 | 220,00 | +20,00 | 220,00 |
| Totale | 1.000,00 | 1.100,00 | +100,00 | 700,00 |

La contabilizzazione finale genera soltanto 100 € aggiuntivi.
Conto di controllo e dettaglio individuale quadrano a 700 €; la chiusura conserva
i crediti insoluti. Il browser svolge preparazione e approvazione del consuntivo,
conferma e contabilizzazione del conguaglio, checklist e chiusura. Le fonti iniziali
sono create tramite le API reali autenticate; gli altri flussi UI hanno test propri.
Screenshot desktop/mobile in `.artifacts/final-desktop.png` e `final-mobile.png`,
ispezionati anche visivamente; tabelle scorrevoli su mobile senza overflow pagina.

Altri casi verificano B: preventivo 300 €, consuntivo 250 €, pagato 300 €,
saldo finale -50 €; e apertura importata +100 € con successivo riporto senza
seconda scrittura. Nessuna compensazione automatica tra soggetti.

## Limiti dichiarati

Vedere [modello e limiti](milestone-063-final-statements.md): rettifiche di esercizi
chiusi, attestazione di chiusure storiche prive di snapshot, revoca di consolidamenti
senza scrittura e riscossione automatica delle differenze richiedono workflow dedicati.
Queste operazioni sono bloccate o non automatizzate. La M07 non è iniziata.
