# API — Milestone 04
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
