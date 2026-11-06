# ADR 0010 — Criteri razionali e platea immutabile per voto

Stato: adottato per M07.2, 6 novembre 2026.

## Decisione

Separare quorum costitutivo e deliberativo, versionando le due soglie e i loro
denominatori. La matematica è un servizio puro in Application; il confronto usa
prodotti incrociati BigInteger, senza arrotondare frazioni come un terzo.
Ogni voto congela criteri, teste, unità, valori, rappresentanze, presenza all'istante
di apertura e versione/contenuto dell'eventuale bilancio M06.

Le teste sono centri di titolarità, non numero indiscriminato di unità né persone
fisicamente in sala. Le ambiguità non risolvibili producono Undetermined.
Le regole personalizzate non sono dichiarate conformi alla legge; i modelli
pubblicati richiedono revisione esplicita di materia e ambito.

Voti modificabili solo in Open, con revisioni append-only. Delibere consolidate
immutabili; rettifica testuale in catena, senza cambiare il risultato originario.
Applicazione M06 soltanto con comando StudioAdmin, fingerprint e transazione
condivisa con i servizi esistenti. Un oggetto già approvato riceve solo un link
storico esplicito. Nessun effetto contabile automatico dal voto.

## Conseguenze

Storico riproducibile anche dopo variazioni anagrafiche o presenze successive.
I retry non duplicano aperture, delibere, applicazioni o conclusioni. I vincoli SQL
sono difesa aggiuntiva alle policy, al tenant server e alle validazioni applicative.
Il riuso dei servizi M06 evita una seconda implementazione delle approvazioni.

Non si codifica un catalogo normativo esaustivo: la fonte primaria consultata e
l'impossibilità di certificare l'intero testo vigente sono documentate separatamente.
Rettifiche sostanziali, riaperture e applicazioni dopo conclusione richiedono un
workflow successivo esplicito; nessuna riscrittura silenziosa.
