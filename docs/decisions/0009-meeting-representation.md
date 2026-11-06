# ADR 0009 — Rappresentanza per unità e snapshot assembleari

Stato: adottato per M07.1, 6 novembre 2026.

## Decisione

Una quota millesimale informativa appartiene all'unità, non a ciascun proprietario.
Lo snapshot conserva tutte le titolarità efficaci alla data scelta e una scelta
operativa di rappresentante unitario. Comproprietà e titoli concorrenti richiedono
una scelta manuale esplicita; il diritto legale per argomento resta fuori M07.1.
Deleghe e presenze prenotano unità con unicità PostgreSQL e registri per sessione.

Snapshot partecipanti append-only e revisioni dell'intero aggregato conservano
nomi, recapiti, diritti preparatori, deleghe ed eventi. Un aggiornamento anagrafico
richiede confronto e riconciliazione espliciti. Le convocazioni sono dichiarazioni
manuali: nessuna consegna è qualificata automaticamente come prova verificata.

## Conseguenze

Non si duplicano millesimi in comproprietà o delega. La rappresentanza è ricostruibile
senza interrogare soltanto le anagrafiche correnti. Si conservano UUID e versioni dei
bilanci M06, senza cambiarne le approvazioni. Il database impedisce duplicazioni
attive e modifiche agli snapshot; il servizio applica ruoli e regole di workflow.

Non vengono codificate soglie normative, quorum o automatismi di voto. La M07.2
richiederà regole esplicite per argomento e gestione controllata delle eccezioni.
