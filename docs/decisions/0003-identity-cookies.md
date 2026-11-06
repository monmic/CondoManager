# ADR 0003 — Identity, cookie revocabili e tenant verificato
Stato: accettato per Milestone 03.
Sostituisce l'eccezione Development di ADR 0002; mantiene le difese DB di ADR 0001.

Contesto: browser React e API same-origin, account potenzialmente multi-studio,
revoche immediatamente efficaci sulle nuove richieste, nessuna registrazione
pubblica degli studi o dipendenza da provider esterni obbligatori.

Decisione: ASP.NET Core Identity su PostgreSQL; cookie HttpOnly/Secure/Lax
con ticket Data Protection e CSRF esplicito. Nessun JWT o token persistente
in localStorage. Una sessione DB server-side conserva studio attivo, scadenza
e revoca; il cookie identifica utente/sessione senza autorizzare un tenant.

Ogni richiesta verifica account, stamp, sessione e membership. Policy e use case
controllano i ruoli, EF filtra i dati e valida scritture/FK. Selezionare uno studio
significa validare un candidato contro membership, non fidarsi del suo UUID.
PlatformAdmin e globale e non e un bypass delle membership.

Conseguenze: un controllo DB per richiesta e un'invalidazione semplice e
verificabile (logout, disable, cambio password). Database non disponibile:
nessuna autorizzazione da claim in cache. Revoche non annullano operazioni
gia autorizzate prima del commit della revoca.
Serve un key ring Data Protection persistente/protetto tra riavvii e istanze.
Rate limiting locale va affiancato a un ingresso condiviso per distribuzioni
multi-istanza. Questi costi sono preferiti a token bearer persistenti nel browser.

Inviti/reset: grant casuali monouso con hash, scadenza e lock transazionale;
inviti per account esistenti richiedono login. Consegna email astratta, SMTP TLS
e casella Development autorizzata e non disponibile in produzione.
Bootstrap solo CLI esplicita con segreti da ambiente/prompt, mai HTTP o automatico.

Non vengono introdotti registrazione pubblica SaaS, impersonazione, motore
contabile o una nuova struttura a microservizi.