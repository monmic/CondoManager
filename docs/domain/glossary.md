# Glossario — Milestone 02
| Termine | Significato |
| --- | --- |
| Studio / Tenant | Ambito isolato di uno studio che amministra piu condomini. |
| Condominio | Anagrafica amministrata dallo studio, con indirizzo e stato attivo. |
| Edificio / Building | Raggruppamento facoltativo di scale/unita nello stesso condominio. |
| Scala / Staircase | Raggruppamento facoltativo, collegato a edificio o direttamente al condominio. |
| Unita / PropertyUnit | Bene immobiliare: appartamento, box, cantina, negozio o altro. |
| Soggetto / Person | Persona fisica o giuridica, distinta dall'unita. |
| Associazione / OwnershipPeriod | Legame storicizzato soggetto/unita con ruolo e date. |
| Proprietario / comproprietario | Ruolo con quota frazionaria decimal nell'intervallo (0,1]. |
| Inquilino / usufruttuario / altro | Ruolo senza quota di proprieta nel modello corrente. |
| Tabella / AllocationTable | Quote del condominio con denominatore configurabile e decorrenza. |
| Quota / AllocationTableEntry | Valore decimal non negativo riferito a una unita e tabella. |
| Draft / bozza | Dato modificabile; una tabella puo essere incompleta. |
| Active / attiva | Tabella validata, con configurazione e quote immutabili. |
| Esercizio / AccountingPeriod | Intervallo amministrativo Draft/Open/Closed, anche non solare. |
| Closed / chiuso | Esercizio definitivo, non modificabile o riapribile. |
| Audit | CreatedAt/UpdatedAt UTC applicati dalla persistenza. |

Date di validita inclusive; fine nulla = aperta. Storico dei legami conservato
concludendo il vecchio e creando un nuovo record.
Quote mancanti diverse da zero. Per attivare una tabella servono tutte le unita
attive e somma esattamente uguale al denominatore.
Nessuna somma automatica della comproprieta, riparto spese, bilancio o regola
fiscale e implicita in questi termini.