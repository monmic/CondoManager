# M06 — Preventivi: incremento implementato

> Rapporto storico M06.1. Piani, crediti e incassi sono stati aggiunti nella [M06.2](milestone-062-finance.md); consuntivi e chiusura restano M06.3.

Questo incremento non completa la Milestone 06. Implementa preventivi, versioni,
workflow amministrativo e riparto preventivo. Rate persistenti, crediti, incassi
attribuiti, consuntivi e chiusura amministrativa restano da implementare.
Non usare il preventivo approvato come richiesta di pagamento o credito contabile.

## Modello e workflow
Budget appartiene a tenant, condominio ed esercizio aperto. BudgetLine collega
una categoria Expense attiva e una regola M05 attiva dello stesso condominio.
Importi numeric(18,2), parametri e snapshot jsonb con decimal serializzati come
stringhe per non perdere precisione nel browser. I gruppi derivano dai componenti
della regola; esclusioni e addebiti diretti sono configurabili tramite DTO.
L'editor web configura categoria, importo e regola per ciascuna voce; conserva
esclusioni/addebiti già presenti ma non offre ancora un editor dedicato.

Draft è modificabile; Proposed congela le voci; Approved congela il riparto e
registra attore, istante e riferimento facoltativo; Superseded conserva il vecchio
approvato. Le modifiche a proposte e approvati richiedono una nuova versione.
Il riferimento assembleare non è una prova della delibera. La versione successiva
si collega all'ultima versione dello stesso esercizio. Il confronto espone totale
precedente e differenza; un confronto analitico riga per riga rimane da aggiungere.

La simulazione non persiste risultati. L'approvazione ricalcola fonti e fingerprint,
richiede StudioAdmin e salva lo snapshot atomico. Il lock del condominio serializza
approvazioni, modifiche alle unità, regole, tabelle, consumi e operazioni contabili.
Le persone degli addebiti diretti sono bloccate in lettura come in M05. Una nuova
approvazione sostituisce soltanto una versione approvata meno recente. Indice
univoco filtrato e token di concorrenza EF difendono ulteriormente l'unicità.
Un retry dell'approvazione identica restituisce lo stesso preventivo.

## Riuso del motore
ResolveCriteria è condiviso con AllocationService M05: tabelle attive, gruppi,
consumi completi, normalizzazione esplicita e persone storicamente associate
seguono le medesime verifiche. Le fonti del calcolo dichiarano SourceKind BudgetLine.
Il campo legacy ExpenseDocumentId del contratto matematico contiene in questo caso
l'identificativo della voce, non un riferimento alla tabella ExpenseDocuments.
La discriminante è obbligatoria per interpretarlo; resta ExpenseDocument per gli
snapshot M05 preesistenti. Nessuna fattura fittizia e nessuna scrittura vengono create.
Il calcolo conserva la versione largest-remainder-v1 e usa la stessa funzione
SplitCents, ora condivisa anche con InstallmentCalculator.

## Fondazioni matematiche per le fasi successive
InstallmentCalculator è puro e non è ancora collegato a persistenza/API/UI.
Divide ciascuna quota individuale su scadenze ordinate e univoche, con percentuali
esattamente 100 oppure pesi positivi espliciti. I pesi possono rappresentare importi
custom relativi, ma non costituiscono ancora un workflow per totali custom per scadenza.
Gli arrotondamenti usano resti maggiori razionali esatti, spareggio per posizione
cronologica della rata. Quadratura di ogni debitore garantita al centesimo;
nessun debitore viene cercato automaticamente nello storico durante questo calcolo.

SettlementAmounts è soltanto una formula testata, non un saldo persistito:
- conguaglio competenza = quota effettiva - quota preventiva;
- saldo finanziario = quota effettiva + debito iniziale + rettifiche - incassi - crediti.
Le rettifiche sono attualmente finanziarie; l'attribuzione economica pertinente
richiederà un modello di rettifiche esplicito. Non esiste un nuovo registro parallelo.

## Sicurezza e database
Cookie Identity, CSRF e TenantMember esistenti proteggono le API. Viewer può leggere
e simulare, Collaborator può gestire bozze/proposte, StudioAdmin approva. TenantId
non è accettato nei DTO. Gli ID sono sempre risolti nel tenant e condominio attivi.
X-Expected-Tenant protegge anche questa sezione dal cambio studio concorrente.

Migration 20261106112924_AdministrativeCycleMilestone06: due nuove tabelle,
FK composite, unicità versione/corrente, precisione, indici e trigger di immutabilità.
Nessuna modifica alle scritture, nessuna cancellazione dei dati precedenti.
Downgrade vietato per conservare lo storico. Migration non applicata al database
di lavoro; verifica soltanto in PostgreSQL Docker isolato.

## API
Prefisso /api/budgets/condominiums/{condo}:
- GET /?period=UUID: versioni e dettaglio delle voci/riparto approvato.
- POST /: nuova bozza; previousVersionId per nuova versione.
- PUT /{id}: modifica bozza.
- POST /{id}/propose: congela proposta.
- POST /{id}/preview: snapshot e hash delle fonti, senza scritture.
- POST /{id}/approve: previewHash e decisionReference facoltativo.

## Verifica manuale dell'incremento
1. Avviare database/API/frontend seguendo README, con migration manuale esplicita.
2. Accedere come StudioAdmin e selezionare uno studio.
3. Creare condominio, tre unità, esercizio aperto e regola attiva in parti uguali.
4. In Preventivi scegliere condominio e Nuovo preventivo; inserire esercizio,
   descrizione e una voce da 100,00 EUR con categoria e regola attive.
5. Salvare, proporre, simulare: quote 33,34/33,33/33,33 e differenza zero.
6. Approvare dopo la conferma. Lo snapshot resta consultabile.
7. Creare nuova versione da 110,00 EUR: differenza +10,00, precedente conservato.
8. Proporre/simulare/approvare la nuova versione: precedente Superseded.

Non è ancora possibile verificare un ciclo amministrativo completo dalla UI:
mancano piani persistenti, rilevazione dei crediti, attribuzione incassi,
riconciliazione, consuntivi versionati, rettifiche e checklist di chiusura.
La chiusura M02 rimane quella precedente e non implementa i controlli M06.
