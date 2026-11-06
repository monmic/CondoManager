# Identity, membership e sicurezza — Milestone 03
## Modello
ApplicationUser estende IdentityUser<Guid> in Infrastructure, con IsActive e
CreatedAt. Domain rimane indipendente da Identity: contiene StudioRole e
TenantMembership (UserId, TenantId, Role, IsActive, audit temporale).
Application definisce ICurrentTenant, DTO, invio messaggi astratto e
MembershipRules per proteggere l'ultimo amministratore.

AppDbContext estende IdentityDbContext mantenendo tutte le tabelle di M01/M02.
Aggiunge AspNet*, Memberships, Sessions, SecurityGrants e SecurityAudits.
Le membership hanno FK Restrict verso utente/studio e unique (TenantId,UserId);
il ruolo non e una proprieta permanente dell'utente.
Gli account restano globali e univoci per email normalizzata.

IdentityService in Infrastructure e l'adapter Identity/EF: coordina transazioni,
password manager, sessioni, grant, invio ed audit. Le API espongono solo DTO.
Nessun repository generico o progetto parallelo.

## Ruoli e permessi
| Ruolo | Ambito | Permessi |
| --- | --- | --- |
| PlatformAdmin | Identity globale | Elenco tecnico degli account e abilitazione/disabilitazione globale. Nessun accesso implicito a dati tenant. |
| StudioAdmin | Membership dello studio attivo | Operazioni anagrafiche, inviti, ruoli, accessi ed audit del solo studio. |
| Collaborator | Membership dello studio attivo | Operazioni anagrafiche e amministrative operative di M02, nessuna gestione utenti. |
| Viewer | Membership dello studio attivo | Consultazione dei dati, nessuna mutazione. |

PlatformAdmin si assegna solo con bootstrap locale protetto, non via API,
invito o selettore ruoli. Il bootstrap del primo studio crea separatamente
una membership StudioAdmin nello studio esplicitamente indicato.
Non esistono endpoint di impersonazione o supporto senza membership.

Modifiche membership e disabilitazione globale controllano gli altri
StudioAdmin con account attivo. Lock transazionale sulla riga Tenant impedisce
che due declassamenti concorrenti rimuovano tutti gli amministratori.
Disabilitazione globale ordina i lock per TenantId e usa un lock amministrativo
globale; e protetto anche l'ultimo PlatformAdmin attivo.
Input ruolo/stato obbligatori: omissioni non diventano privilegi o disabilitazioni.

## Risoluzione tenant e difese
1. ASP.NET Core verifica e decifra il cookie firmato tramite Data Protection.
2. OnValidatePrincipal verifica account attivo, sessione non revocata/non scaduta
   e security stamp uguale a quello corrente.
3. La sessione contiene il solo ActiveTenantId server-side.
4. Il middleware legge la membership attiva del relativo utente per ogni richiesta.
5. CurrentTenant scoped viene popolato dal risultato: ruolo e tenant non sono
   ottenuti da DTO/header/claim tenant non verificati.
6. Policy su route, guardie nel servizio e SaveChanges, filtri EF e FK composite
   contribuiscono insieme all'isolamento.

POST /auth/tenant accetta un candidato UUID e verifica membership attiva
prima di salvarlo nella sessione. Nessun fallback al primo tenant disponibile
per account con piu membership. L'unica membership viene selezionata al login.
GET /auth/me elenca soltanto le membership attive del chiamante.
La query che ignora il filtro su Tenants e vincolata dalle membership del
medesimo utente autenticato: non e un elenco globale degli studi.

L'header facoltativo X-Expected-Tenant e una PRECONDIZIONE, non un selettore:
se diverso dal tenant verificato della sessione, risponde 409 tenant_changed.
Il frontend lo usa contro modifiche da schede con selezione obsoleta.
TenantId e X-Tenant-Id arbitrari non concedono alcuna autorizzazione.
Un client che omette la precondizione rimane comunque soggetto a membership,
policy e al tenant server, senza poter sceglierlo tramite header.

Il vecchio DevelopmentTenant__Id viene ignorato in tutti gli ambienti.
Gli endpoint ora esistono anche in produzione, ma richiedono autenticazione.
L'assenza di membership attiva produce 403, ID fuori ambito 404.
I filtri EF non sono l'unica barriera e non costituiscono PostgreSQL RLS.
SQL privilegiato e operazioni bulk restano riservati e non esposti ai client.

## Sessioni e CSRF
Cookie __Host-CondoManager, Secure/HttpOnly/SameSite=Lax, Path=/, senza Domain.
Sessioni DB con scadenza assoluta 8 ore, senza sliding expiration o persistenza
remember-me. Logout marca RevokedAt: il replay del vecchio cookie e rifiutato.
Cambio/reset password e account disable revocano tutte le sessioni.
Revoca membership e cambio ruolo sono riletti a ogni richiesta;
non interrompono retroattivamente operazioni gia autorizzate in corso.

CSRF richiesto su TUTTE le operazioni HTTP /api non GET/HEAD/OPTIONS, incluse
login, reset e accettazione inviti anonimi. GET /auth/csrf rilascia request token;
cookie antiforgery HttpOnly/Secure/Strict. React conserva il request token solo
in memoria e lo rinnova ai cambi di identita. Nessun JWT/localStorage.
API HTTP rifiutate, nessun redirect 302 al login: 401/403 con ProblemDetails.
Rate limiter sugli endpoint auth, lockout Identity 5 tentativi/15 minuti,
password 12-128 caratteri con complessita.

## Inviti e recupero
Token da 32 byte casuali, SHA-256 nel DB, scadenza (inviti 48h, reset 30m),
UsedAt/RevokedAt e lock di riga per consumo monouso anche concorrente.
Il record SecurityGrant e distinto per Purpose; un token di invito non puo
essere usato per un reset e viceversa.
Reset e associato al security stamp: un cambio password invalida i grant
precedenti. Il nuovo hash password passa attraverso ASP.NET Core Identity.

Invito accettabile solo se l'emittente e ancora StudioAdmin attivo con account
attivo nello studio. Una nuova emissione revoca inviti pendenti per stessa email
e studio. Revoca esplicita disponibile solo allo StudioAdmin di quello studio.
Account esistente: richiede sessione dell'utente con stessa email, non cambia
password o account. Account nuovo: crea IdentityUser e membership nella stessa
transazione dopo validazione del grant e della password.

Link con frammento #invite= o #reset= (non query string), rimosso immediatamente
dalla history del browser. Il token non entra nelle richieste GET o nei referrer.
La richiesta forgot-password restituisce 202 uniforme per account noti/ignoti
quando il canale e disponibile. In produzione senza SMTP restituisce 503
uniforme prima di interrogare il destinatario, senza pubblicare token.

ISecurityMessageSender permette SMTP con TLS e configurazione da ambiente.
DevelopmentMailbox e solo memoria bounded, senza log o scrittura di token:
un utente vede i propri reset; uno StudioAdmin vede solo gli inviti emessi
da lui nello studio attivo. Nessun privilegio inbox globale a PlatformAdmin.
L'endpoint non esiste fuori da Development. Non sostituisce una casella email
per un utente che ha gia perso ogni sessione: configurare SMTP per quel flusso.

## Audit e hardening
SecurityAudits contiene timestamp, codice evento controllato, TenantId/ActorId/
SubjectId UUID. Niente password, token, cookie o payload libero.
Eventi: login riuscito/fallito/lockout, logout, scelta studio, password changed/reset,
invito creato/revocato/accettato, cambi ruolo/stato membership, account disable/
enable e bootstrap. Gli audit dello studio sono filtrati per tenant e StudioAdmin.
Eventi globali sono consultabili amministrativamente nel DB, non tramite
un endpoint pubblico che elenchi studi diversi.

Header no-store, nosniff, Referrer-Policy no-referrer, X-Frame-Options DENY,
CSP per la SPA e HSTS fuori da Development.
AllowedHosts locale per default, nessun CORS permissivo o trust automatico di
forwarded headers. Key ring Data Protection protetto/persistente e limitazione
multi-istanza sono responsabilita del deployment, configurabili nel README.
SMTP reale, domini/certificati pubblici e distribuzione SaaS non sono simulati.

## Migrazione
20261105233533_IdentitySecurityMilestone03 e additiva, senza seed o alterazioni
dei dati tenant. Nessun drop/reset nel percorso Up. Down vietato per non perdere
account, accessi e audit. Anche gli aggiornamenti del database di test passano
dal normale strumento EF; nessuna disattivazione delle policy per i test HTTP.