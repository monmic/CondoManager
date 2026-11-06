# Glossario contabile essenziale
- **Conto contabile (Account):** voce del piano di un condominio. Tipologia Asset,
  Liability, Equity, Income o Expense; codice univoco nel condominio.
- **Conto finanziario:** banca o cassa del condominio, collegata a un Asset dedicato.
- **Dare/Avere:** due sezioni della scrittura; ogni riga valorizza soltanto una.
- **Partita doppia:** per ogni scrittura totale Dare uguale totale Avere.
- **Saldo naturale:** Dare-Avere per Asset/Expense; Avere-Dare per gli altri conti.
- **Fornitore:** soggetto dello studio riutilizzabile in piu condomini; distinto
  dall'anagrafica dei residenti e non collegato automaticamente alle unita.
- **Documento di spesa:** bozza con imponibile/IVA/totale; la registrazione crea
  costo lordo e debito verso fornitori. Nessun recupero IVA automatico.
- **Pagamento:** estinzione totale/parziale del documento con uscita finanziaria;
  genera una scrittura e conserva data, conto e riferimento.
- **Residuo:** totale documento meno pagamenti non stornati.
- **Storno:** nuova scrittura opposta collegata all'originale immutabile.
- **Saldo iniziale:** dichiarazione storica alla creazione del conto finanziario;
  se non zero viene contabilizzata. Non e un saldo corrente modificabile.
- **Incasso da attribuire:** entrata finanziaria provvisoria, senza automatismi
  verso rate o debiti di residenti.
- **Idempotenza:** ripetere il medesimo comando con lo stesso UUID non duplica
  il fatto contabile; chiave riusata con dati diversi viene rifiutata.
- **Variazione per esercizio:** somma delle sole scritture dell'esercizio, senza
  riporto automatico dei saldi precedenti.
- **Categoria contabile / tabella millesimale:** concetti distinti, senza
  collegamento di riparto nella Milestone 04.
