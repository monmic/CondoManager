# ADR 0002 — Eccezione tenant di sviluppo
Stato: accettato per Milestone 02. Integra ADR 0001 limitatamente a Development.

L'autenticazione non e ancora disponibile. Per rendere utilizzabili le
anagrafiche, il committente autorizza uno studio configurato esplicitamente
sul server e utilizzabile solo nell'ambiente Development.

Decisione:
- DevelopmentTenant:Id, da variabile DevelopmentTenant__Id, deve essere UUID non
  vuoto. Assente: API dati non registrate; invalido: avvio Development rifiutato.
- In Production/Staging viene ignorato e le route dei dati non esistono.
- ITenantContext scoped ricava il valore dalla configurazione server.
  Nessun parametro/header/cookie/body client viene consultato.
- Lo studio deve gia esistere; endpoint filter lo verifica. Provisioning
  solo tramite script esplicito Development, senza inizializzazioni automatiche.
- Tutti i filtri EF, concurrency token TenantId e FK composite di ADR 0001
  rimangono applicati. Il lock SQL dei servizi e parametrizzato e filtrato per
  tenant e condominio.
- Nessun CORS generico; il frontend locale usa il proxy /api di Vite.
- Session espone solo disponibilita e modalita, non dati identificativi tenant.

Conseguenze: nessuna autenticazione nella modalita di sviluppo. Limitare
l'ascolto a loopback e usare dati sintetici. Questa configurazione non e
autorizzata per una piattaforma esposta o SaaS.
Non e PostgreSQL RLS: codice privilegiato e SQL diretto restano fuori dalla
barriera EF. Prima di esporre API reali servono identita verificata, membership
studio, ruoli e test di autorizzazione end-to-end. ADR 0001 resta valido per
questo futuro percorso; nessun header TenantId diventera attendibile.