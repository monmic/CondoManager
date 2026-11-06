# Glossario Identity — Milestone 03
| Termine | Significato |
| --- | --- |
| ApplicationUser | Account globale Identity; non appartiene permanentemente a un solo studio. |
| TenantMembership | Legame univoco account/studio, con ruolo e accesso attivo/disabilitato. |
| Studio attivo | Studio scelto nella sessione server e validato contro membership del chiamante. |
| StudioAdmin | Amministratore degli accessi e dati del solo studio della membership. |
| Collaborator | Operatore dello studio, senza gestione degli accessi. |
| Viewer | Consultazione; non autorizzato alle mutazioni. |
| PlatformAdmin | Amministratore tecnico globale, senza accesso implicito ai dati degli studi. |
| UserSession | Sessione server con durata assoluta, studio attivo e revoca. |
| Security stamp | Versione Identity delle credenziali: il cambio invalida sessioni/grant precedenti. |
| SecurityGrant | Hash di un token casuale per invito o reset, con scadenza, consumo e revoca. |
| SecurityAudit | Evento di sicurezza con timestamp e identificativi, senza segreti. |
| CSRF | Verifica che una mutazione con cookie provenga dal client che puo leggere il request token. |

Disabilitare una membership non cancella account, altre membership o dati dello
studio. Disabilitare l'account globale impedisce accesso a tutti gli studi e
revoca le sessioni. Nessun ruolo dello studio equivale al ruolo globale.
Il recupero password non conferisce membership; un invito non cambia le
credenziali di un account esistente.