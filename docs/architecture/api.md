# API — Milestone 05
Tutte le API richiedono HTTPS. Cookie Identity, nessun bearer token.
GET /health pubblico: solo vitalita del processo.
La SPA compilata e gli asset sono intenzionalmente pubblici, senza dati tenant.

## Autenticazione
| Metodo e route | Autorizzazione |
| --- | --- |
| GET /api/auth/csrf | Pubblico, token richiesta antiforgery |
| POST /api/auth/login | Pubblico + CSRF + rate limit |
| POST /api/auth/forgot-password | Pubblico + CSRF + rate limit; 202 uniforme |
| POST /api/auth/reset-password | Pubblico + CSRF + grant + rate limit |
| POST /api/auth/accept-invitation | Pubblico + CSRF + grant + rate limit; account esistente deve autenticarsi |
| GET /api/auth/me | Autenticato, proprie membership |
| POST /api/auth/logout | Autenticato + CSRF; revoca DB |
| POST /api/auth/tenant | Autenticato + CSRF; membership attiva nel candidato |
| POST /api/auth/change-password | Autenticato + CSRF + rate limit |

GET csrf restituisce token, da inviare nell'header X-CSRF-TOKEN su ogni mutazione.
Login: email/password; reset: token/newPassword; cambio: currentPassword/newPassword;
invito accettato: token/password (null per account gia autenticato).
Il frontend cancella il request token in memoria al cambio identita e lo rinnova.
Nessuna risposta di login/reset contiene cookie o token in JSON.

## Gestione accessi
| Metodo e route | Autorizzazione |
| --- | --- |
| GET /api/studio/members | StudioAdmin del tenant attivo |
| PUT /api/studio/members/{id} | StudioAdmin; role e isActive obbligatori |
| GET/POST /api/studio/invitations | StudioAdmin; POST email/role obbligatori |
| POST /api/studio/invitations/{id}/revoke | StudioAdmin, invito del proprio studio |
| GET /api/studio/audit | StudioAdmin, ultimi 200 eventi del proprio studio |
| GET /api/platform/users | PlatformAdmin, solo metadati tecnici degli account |
| PUT /api/platform/users/{id}/status | PlatformAdmin; isActive obbligatorio |
| GET /api/development/messages | Solo Development, autenticato, messaggi autorizzati del chiamante |

Nessun endpoint assegna PlatformAdmin o crea studi pubblicamente.
Il vecchio /api/session e stato rimosso: usare /api/auth/me.
DevelopmentTenant__Id e ignorato.

## API operative M02
Prefisso /api. GET richiede membership attiva di qualsiasi ruolo; mutazioni
richiedono StudioAdmin o Collaborator, oltre a CSRF. TenantId non e un input
delle anagrafiche. Il server rifiuta relazioni/ID fuori dall'ambito corrente.

| Risorsa | Operazioni |
| --- | --- |
| /condominiums[/{id}] | GET elenco/dettaglio, POST, PUT con id, DELETE logico con id |
| /condominiums/{c}/structure | GET edifici, scale e unita |
| /condominiums/{c}/buildings[/{id}] | POST, PUT con id |
| /condominiums/{c}/staircases[/{id}] | POST, PUT con id |
| /condominiums/{c}/units[/{id}] | POST, PUT con id |
| /people[/{id}] | GET elenco, POST, PUT con id |
| /condominiums/{c}/associations | GET storico |
| /condominiums/{c}/units/{u}/associations | POST legame storico |
| /condominiums/{c}/associations/{id}/end | POST conclusione, validTo incluso |
| /condominiums/{c}/allocation-tables[/{id}] | GET elenco/quote/validazione, POST, PUT, DELETE archiviazione bozza |
| /condominiums/{c}/allocation-tables/{id}/entries/{u} | PUT quota value |
| /condominiums/{c}/allocation-tables/{id}/activate | POST attivazione |
| /condominiums/{c}/accounting-periods[/{id}] | GET elenco, POST, PUT con id |
| /condominiums/{c}/accounting-periods/{id}/open | POST apertura |
| /condominiums/{c}/accounting-periods/{id}/close | POST chiusura |

PUT invia DTO completo; disattivazioni dei figli: isActive=false.
Date YYYY-MM-DD, audit ISO UTC, enum stringhe. Decimal in uscita stringhe
invarianti, in ingresso stringhe o numeri JSON. Contratti in Application.
X-Expected-Tenant opzionale confronta una selezione obsoleta con quella del
server (409 tenant_changed), senza mai selezionare/autorizzare un tenant.

ProblemDetails: 400 formato/CSRF/validazione; 401 sessione assente/scaduta;
403 ruolo/membership insufficienti; 404 risorsa fuori ambito/inesistente;
409 duplicati, stato immutabile, ultimo admin, sovrapposizione o selezione obsoleta;
429 rate limit; 503 database/email indisponibili. Nessun dettaglio SQL,
stack trace, password, cookie o token nelle risposte di errore.
Swagger solo Development e autenticato; rimane documentata qui la serializzazione
decimal stringa, diversa dallo schema numerico predefinito del generatore.
## API contabili M04
Prefisso /api/accounting. Membership verificata, cookie HTTPS, CSRF su mutazioni.
Viewer legge; Collaborator/StudioAdmin scrivono. Solo StudioAdmin storna e usa
Saldi di apertura nelle rettifiche manuali. Nessun TenantId nei DTO.

| Route sotto il prefisso | Operazioni |
| --- | --- |
| /suppliers | GET, filtro search; POST |
| /suppliers/{id} | PUT completo; DELETE disattivazione logica |
| /condominiums/{c}/accounts | GET piano; POST conto |
| /condominiums/{c}/accounts/{id} | PUT; type deve rimanere invariato |
| /condominiums/{c}/accounts/initialize | POST piano mancante, senza overwrite |
| /condominiums/{c}/expenses | GET, filtro period UUID; POST bozza |
| /condominiums/{c}/expenses/{id} | PUT bozza; DELETE annullamento bozza |
| /condominiums/{c}/expenses/{id}/register | POST PostingInput: requestId UUID |
| /condominiums/{c}/expenses/{id}/payments | POST PaymentInput |
| /condominiums/{c}/financial-accounts | GET saldi attuali; POST con apertura |
| /condominiums/{c}/financial-accounts/{id} | PUT nome/IBAN/attivo, non saldo |
| /condominiums/{c}/receipts | POST incasso generico non attribuito |
| /condominiums/{c}/journals | GET filtri period/account/from/to/search; POST scrittura manuale |
| /condominiums/{c}/journals/{id}/reverse | POST ReversalInput, StudioAdmin |
| /condominiums/{c}/summary | GET totali e saldi, filtro period |

DTO completi: AccountingContracts.cs. Date YYYY-MM-DD, enum stringhe, EUR come
stringhe decimal invarianti in uscita; input numerico o stringa, massimo due
decimali. Le righe JournalInput richiedono accountId/debit/credit, almeno due,
solo un lato positivo per riga. Errori coerenti con ProblemDetails sopra.

PaymentInput: requestId, accountingPeriodId, financialAccountId, date, amount,
reference opzionale. ReversalInput: requestId, accountingPeriodId, date, reason.
ReceiptInput aggiunge description. FinancialAccountInput include code del nuovo
Asset dedicato, name, type BankAccount/Cash, iban opzionale, openingBalance,
accountingPeriodId, date e requestId. Apertura zero non produce una scrittura zero.

I POST contabili rispondono con JournalDto della registrazione; stesso RequestId
con identico comando restituisce lo stesso UUID anche dopo la chiusura.
Chiave riusata con contenuto diverso: 409. Solo salvataggi bozza/configurazione
sono modificabili: nessuna PUT/DELETE per giornale o pagamenti contabilizzati.
Una fattura registrata si annulla stornandone la scrittura dopo i pagamenti.
Le liste forniscono i DTO di dettaglio, righe e storico inclusi; non espongono EF.

## API ripartizioni M05
Prefisso /api/allocations/condominiums/{c}. Tenant server-side, cookie HTTPS,
CSRF su tutte le mutazioni, incluso POST preview. X-Expected-Tenant supportato.
GET e preview per tutti i membri attivi; altre operazioni per ruoli operativi;
conferma sostitutiva con previousRunId soltanto StudioAdmin.

| Route relativa | Operazioni |
| --- | --- |
| /rules | GET elenco/versioni/componenti; POST bozza |
| /rules/{id} | PUT bozza, o nuova versione se la regola e attiva |
| /rules/{id}/activate | POST attivazione dopo validazione |
| /groups | GET gruppi/membri; POST gruppo |
| /groups/{id} | PUT nome/stato/membri espliciti |
| /readings | GET storico, filtro period; POST lettura o rettifica append-only |
| /consumptions | GET con period/type/unitOfMeasure/from/to, risultati e anomalie per unita |
| /assignments | GET; PUT associazione accountId oppure expenseDocumentId e ruleId |
| /preview | POST input di calcolo, nessun salvataggio |
| /confirm | POST requestId/previewHash/input, risultato persistito/idempotente |
| /runs | GET storico, filtro period facoltativo |
| /runs/{id} | GET dettaglio, snapshot e quote congelate |

AllocationPreviewInput: accountingPeriodId, competenceFrom, competenceTo,
sources[] (expenseDocumentId, amount, ruleId opzionale, excludedUnitIds,
directCharges[] con propertyUnitId/personId opzionale/amount), previousRunId
opzionale solo per sostituzione. Tutti gli importi devono essere positivi e
rispettare il budget residuo dei documenti. Nessun TenantId ricevuto e autorita.

Preview restituisce previewHash, snapshot risolto e result con importo originale,
quote, rettifiche componenti/unita, totale ripartito, differenza, warnings/errors.
Errori di contesto/fonti mancanti sono ProblemDetails espliciti; anomalie del
calcolo ritornano result non valido e non confermabile. Fonti cambiate, esercizio
chiuso, budget esaurito o requestId incompatibile: 409. Relazioni fuori ambito: 404.
Formato/percentuali/importi non validi: 400. Autorizzazione: 401/403.

Le conferme rispondono AllocationRunDto; stesso RequestId/contenuto restituisce
lo stesso UUID. Nessuna PUT/DELETE per run, righe o letture. Il risultato storico
e letto dallo snapshot, mai ricalcolato. Decimal in uscita stringhe invarianti;
input numerico o stringa, come le API precedenti. Snapshot/evidence interni
preservano i numeri come stringhe per evitare perdite di precisione nei client.
Vedere AllocationContracts.cs e [modello M05](milestone-05-allocations.md).

AllocationRunDto.sourceWarnings segnala fonti annullate in contabilita dopo la
conferma, senza alterare il risultato congelato. Non e un calcolo di crediti.

## Preventivi — incremento M06
Tutte le route `/api/budgets/condominiums/{condo}` richiedono TenantMember.
GET elenca le versioni (filtro period facoltativo); POST crea una bozza;
PUT /{id} modifica solo Draft; POST /{id}/propose congela la proposta;
POST /{id}/preview simula senza persistenza; POST /{id}/approve richiede
StudioAdmin e il previewHash corrente. Si applicano cookie, CSRF e
X-Expected-Tenant. Il DTO di lettura include voci, totale precedente/differenza,
approvazione e quote congelate. Nessun endpoint di questo incremento genera
crediti, rate persistenti o movimenti contabili.

## Rate e incassi — M06.2
Prefisso `/api/resident-finance/condominiums/{condo}`. TenantMember obbligatorio,
CSRF per POST e supporto X-Expected-Tenant. TenantId non è un campo dei DTO.

| Metodo | Percorso | Operazione |
| --- | --- | --- |
| POST | /plans/preview | Simulazione pura, anche Viewer |
| POST | /plans | Bozza persistente, ruoli operativi |
| GET | /plans?period=UUID | Versioni e snapshot |
| POST | /plans/{id}/approve | Emissione con PlanApproval, solo StudioAdmin |
| GET | /credits?period=UUID&asOf=YYYY-MM-DD | Rate emesse e residui; asOf solo scadenziario |
| POST | /receipts | ResidentReceiptInput, creazione o acquisizione movimento |
| GET | /receipts?period=UUID | Incassi e attribuzioni, incluso non attribuito |
| POST | /receipts/{id}/allocate | Attribuzione atomica con chiave di idempotenza |
| POST | /allocations/{journalId}/reverse | Storno intero gruppo, solo StudioAdmin |
| GET | /bank-movements | Incassi generici M04 disponibili |
| POST | /receipts/{id}/match | Prenotazione movimento esatto |
| POST | /receipts/{id}/reconcile | Conferma associazione |
| GET | /balances?period=UUID&asOf=YYYY-MM-DD | Posizioni unità/debitore e fondi non attribuiti |

GET richiede almeno Viewer; mutazioni salvo approvazione/storno richiedono
Collaborator o StudioAdmin. Le operazioni con date contabili richiedono esercizio
Open. Chiavi UUID obbligatorie per generazione, emissione, incasso, attribuzione e
storno; una chiave riutilizzata con parametri diversi restituisce 409.
## Consuntivi M06.3

Base: `/api/final-statements/condominiums/{condo}`. Tenant validato lato server,
autenticazione e CSRF sulle mutazioni; nessun TenantId in input.

| Metodo | Suffisso | Operazione |
|---|---|---|
| GET / POST | `/` | Versioni per esercizio / conservazione bozza |
| POST | `/preview` | Simulazione senza persistenza |
| POST | `/{id}/propose`, `/{id}/approve` | Workflow con fingerprint |
| POST | `/{id}/settlement-preview`, `/{id}/settlement-confirm` | Anteprima e snapshot conguagli |
| GET | `/settlements` | Storico contabilizzazioni |
| POST | `/settlements/{id}/post`, `/settlements/{id}/reverse` | Registrazione / storno controllato |
| GET / POST | `/periods/{period}/opening` | Consultazione / attestazione apertura |
| GET | `/periods/{period}/review` | Checklist e saldi correnti |
| POST | `/periods/{period}/close` | Chiusura con fingerprint |
| GET | `/closures` | Storico chiusure immutabili |

Viewer consulta e simula; Collaborator prepara e propone; StudioAdmin approva,
gestisce aperture, conferma/contabilizza/storna e chiude. Le letture di dettaglio,
confronto e fonti sono incluse nei DTO degli snapshot. RequestId UUID e fingerprint
proteggono retry e fonti modificate; gli errori di stato restituiscono 409.

## Assemblee M07.1

Base: `/api/meetings/condominiums/{condo}`. Cookie, TenantMember, CSRF sulle
mutazioni e controllo X-Expected-Tenant. Nessun TenantId in input.

| Metodo | Suffisso | Uso |
|---|---|---|
| GET / POST | `/` | Elenco filtrabile `?period=UUID` / creazione |
| GET / PUT | `/{id}` | Dettaglio / modifica prima del consolidamento |
| POST | `/{id}/status` | Programmazione, consolidamento, annullamento |
| POST | `/{id}/sessions/{session}` | Avvio, termine o non tenuta di una sessione |
| POST | `/{id}/agenda` | Inserimento/modifica punto |
| POST | `/{id}/agenda/{item}/delete`, `/{id}/agenda/reorder` | Rimozione/ordine |
| POST | `/{id}/participants/preview` | Ricostruzione senza persistenza |
| POST | `/{id}/snapshots` | Consolidamento/riconciliazione esplicita |
| POST | `/{id}/notices` | Preparazione destinatario da snapshot |
| POST | `/{id}/notices/{notice}/status` | Evento manuale dichiarato |
| POST | `/{id}/proxies` | Preparazione delega con unità |
| POST | `/{id}/proxies/{proxy}/status` | Presentazione/verifica/rifiuto/revoca |
| POST | `/{id}/attendances` | Ingresso diretto/remoto/per delega |
| POST | `/{id}/attendances/{attendance}/exit` | Uscita tracciata |
| POST | `/{id}/absences` | Assenza dichiarata |
| GET | `/{id}/history` | Revisioni complete, solo ruoli operativi |

Tutte le mutazioni richiedono ExpectedVersion: 0 per creazione, versione corrente
per modifiche; mismatch 409. Ogni risposta di mutazione restituisce l'aggregato
aggiornato. Il dettaglio include sessioni, agenda, snapshot, convocazioni, eventi,
deleghe, presenze e assenze; non espone entità EF. Viewer riceve i recapiti oscurati
e non riceve convocazioni o note riservate. Anteprima partecipanti e revisioni
complete richiedono Collaborator/StudioAdmin. La verifica delle deleghe richiede
StudioAdmin, le altre mutazioni almeno Collaborator. Le liste e i riferimenti sono
sempre limitati al tenant autenticato e al condominio della route.

## Deliberazioni M07.2

Stessa base tenant M07.1: `/api/meetings/condominiums/{condo}`.

| Metodo | Suffisso | Operazione |
|---|---|---|
| GET / POST | `/voting-rules` | Criteri e tutte le versioni / nuova versione Draft |
| GET | `/voting-rules/templates` | Modelli numerici senza inserimento automatico |
| POST | `/voting-rules/{id}/activate`, `/retire` | Revisione StudioAdmin |
| GET | `/{meeting}/decisions` | Quorum, voti, revisioni, delibere, applicazioni e conclusione |
| POST | `/{meeting}/quorum` | Verifica costitutiva storicizzata per sessione/istante |
| POST | `/{meeting}/votes` | Apertura e congelamento platea |
| POST | `/{meeting}/votes/{vote}/ballots` | Nuovo voto o modifica con BallotId/BallotRevision |
| POST | `/{meeting}/votes/{vote}/close`, `/cancel` | Chiusura/calcolo o annullamento |
| POST | `/{meeting}/resolutions` | Consolidamento o rettifica testuale con PreviousResolutionId |
| POST | `/{meeting}/resolutions/{resolution}/apply` | Applicazione M06 / HistoricalOnly esplicito |
| POST | `/{meeting}/agenda-dispositions` | Discussione senza voto o rinvio motivato |
| GET | `/{meeting}/completion-review?session=UUID` | Checklist e fingerprint |
| POST | `/{meeting}/complete` | Conclusione StudioAdmin |

Mutazioni meeting con ExpectedVersion. Apertura, delibera, applicazione e conclusione
richiedono RequestId; stesso UUID con parametri diversi dà 409. Gli errori non
trasformano uno stato indeterminato in approvazione. Il soggetto esprimente di un
ballot è risolto dal server, senza ID autorizzativi ricevuti dal client.

Viewer consulta; Collaborator registra quorum e voti; StudioAdmin gestisce criteri,
consolidamenti, rettifiche, M06 e conclusione. I controlli valgono anche sui retry.
Cookie, membership, tenant, CSRF e X-Expected-Tenant rimangono obbligatori. I Viewer
ricevono uno snapshot di conclusione sintetico senza i recapiti M07.1 riservati.
