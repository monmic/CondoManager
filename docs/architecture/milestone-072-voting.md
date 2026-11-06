# M07.2 — Quorum, votazioni e delibere

## Componenti

VotingCriteria e VotingThreshold sono valori di dominio; VotingEngine in Application
è puro, senza database/UI. MeetingService implementa IMeetingDecisionService con
le stesse transazioni, tenant e revisioni M07.1. Nuove entità: VotingRule,
MeetingQuorumCheck, MeetingVote, MeetingBallot, MeetingBallotRevision,
MeetingResolution, ResolutionApplication, MeetingAgendaDisposition,
MeetingCompletion e MeetingDecisionRequest. Tutte persistenti e limitate al tenant
ed al condominio; i figli referenziano anche lo stesso MeetingId.

## Criteri e formule

Ogni versione del criterio conserva finalità Constitutive/Deliberative, chiamata
First/Second, categoria facoltativa, due soglie indipendenti, fonte e ambito,
validità, stato e revisione amministrativa. Modifica tramite nuova versione:
Draft → Active → Retired, senza alterare i contenuti già usati. Più versioni possono
restare attive: si seleziona sempre il UUID esatto e si verifica la decorrenza.

Ogni soglia ha numeratore/denominatore interi, confronto `>` oppure `>=` e base.
Teste: Eligible, Present, Expressed. Valore: stesse basi oppure Building, cioè il
denominatore nominale della tabella. Expressed considera favorevoli e contrari,
escludendo astensioni e non votanti; Present li mantiene. Nessuna convenzione
universale per l'astensione. Le soglie costitutive non usano Expressed.

Il confronto esegue prodotti incrociati con BigInteger e valori decimal a sei
cifre, senza dividere o arrotondare la soglia. Esempio: 333,333333 su 1000 non
raggiunge esattamente un terzo; 333,333334 sì. Il motore richiede **entrambi** i
requisiti. Un denominatore nullo, una duplicazione o anomalia dà Undetermined.

Non vengono prodotti float/double per quote o confronti. Le conversioni Number
nel frontend riguardano soltanto coefficienti interi delle frazioni, limitati a
1.000.000; nessun importo monetario viene convertito in binario per calcoli.

## Platea e quorum

La fonte anagrafica resta MeetingSnapshot M07.1. Le quote appartengono all'unità,
una volta sola; si raggruppano unità con identica titolarità in un centro di voto.
Un proprietario con più unità conserva una testa e la somma delle sue quote.
Comproprietà con identico insieme di titolari ha un solo centro rappresentato;
insiemi sovrapposti/ambigui non vengono risolti arbitrariamente e bloccano l'esito.

La platea per materia consente esclusioni motivate di unità e scelta motivata del
titolare tra quelli storicizzati, senza moltiplicare la quota. Titoli quali usufrutto
richiedono scelta esplicita per la materia. La presenza fisica può essere registrata
anche senza unità: non attribuisce da sola diritti di voto, che derivano dalla platea.

Un soggetto è presente se CheckInAt <= istante e CheckOutAt è nullo o successivo.
Un delegato presente può rappresentare più deleganti distinti tramite deleghe
Verified coerenti con unità/titolare e riferimento scritto. Un ingresso dopo
l'apertura non aggiunge diritti a quella votazione; un'uscita precedente li esclude.
Revoche prima del voto vengono rispettate. Valutazioni storiche di deleghe revocate
che non siano ricostruibili con certezza producono un'anomalia.

Il quorum è conservato per sessione/istante con platea, regola e risultato.
Prima dell'apertura viene riesaminato con presenze correnti e il riesame è incluso
nel VoteEvidence. Prima/seconda convocazione non condividono automaticamente il
risultato. Nessuna costituzione è dichiarata quando la rappresentanza è anomala.
Gli invii ai potenziali aventi diritto inclusi devono risultare almeno dichiarati
Sent/Delivered; questo non li trasforma in prove verificate di regolare convocazione.

## Voti e delibere

L'apertura crea direttamente Open e congela platea, criterio/versione, riesame
costitutivo, istante, fonti e contenuto della versione M06 collegata. Draft è
riservato a futuri flussi preparatori, senza endpoint fittizi.

Un MeetingBallot per centro di voto: scelta, titolare, soggetto esprimente,
valore, operatore e data. Il soggetto esprimente deriva dallo snapshot server,
non da un ID arbitrario del client. Le unità non danno voti separati a uno stesso
proprietario. Un delegato può scegliere diversamente per deleganti distinti.

Modifiche esplicite con BallotId e revisione solo in Open. Ogni versione è
conservata in MeetingBallotRevision e consultabile. Duplicati e aggiornamenti
concorrenti sono rifiutati. Assenti, astenuti e presenti non votanti sono distinti.
Voti invalidi vengono rifiutati, non trasformati silenziosamente in voti validi.

Close conserva il risultato; Finalized conserva la delibera con testo, attore,
data, versione e intero snapshot. Approved/Rejected/Undetermined derivano dal
motore: non sono selezionabili manualmente. Un esito indeterminato non diventa
un'approvazione. Le rettifiche disponibili sono testuali sullo stesso voto e
stesso esito, con catena immutabile e motivo; annullamenti sostanziali richiedono
una nuova procedura/assemblea, non una riscrittura del voto precedente.

## Applicazione M06

La votazione non modifica preventivi, consuntivi, rate o giornale. Il comando
StudioAdmin verifica delibera corrente Approved, criterio congelato, stesso
scope, UUID/versione e fingerprint del contenuto M06. Applica soltanto Proposed,
chiamando IBudgetService.Approve o IFinalStatementService.ApproveFinal.
Le loro validazioni su fonti, esercizio e stati rimangono attive.

Gli helper transazionali M06 partecipano alla transazione già esistente senza
commetterla: approvazione e ResolutionApplication sono atomiche. Il test PostgreSQL
inietta un errore sul collegamento e verifica il rollback dell'approvazione.

Per oggetti già Approved occorre HistoricalOnly esplicito: si conserva il link
senza ripetere la transizione, cambiarne data o riferimento originario. Per un
esercizio si registra soltanto un collegamento storico. Nessuna generazione
automatica di rate, crediti, conguagli o movimenti. Duplicazioni impedite da indici.
Versioni mutate dopo l'apertura richiedono una nuova operazione deliberativa.

## Conclusione

Ogni punto deve risultare deliberato oppure DiscussedWithoutVote/Deferred con
motivazione. Non è richiesta una delibera per ogni punto. Non si conclude con voti
Open/Closed non consolidati, presenze senza uscita, quorum assente/indeterminato
o checklist modificata. Con quorum NotSatisfied si può soltanto chiudere senza
delibere e con tutti i punti rinviati; non si attesta una costituzione riuscita.

Conclusione StudioAdmin con fingerprint, RequestId, motivo, snapshot e audit.
La sessione in corso termina e l'assemblea diventa Completed, senza avviare la
seconda convocazione. Retry identici sono idempotenti anche dopo Completed.
Nuove modifiche incompatibili sono bloccate. L'applicazione M06 va completata prima
della conclusione: il collegamento successivo ad assemblea conclusa non è ancora
un workflow disponibile. Nessuna riapertura automatica o produzione PDF.

## Sicurezza e integrità

Si riusano cookie Identity, TenantMember, CSRF, X-Expected-Tenant e ICurrentTenant.
Viewer consulta; Collaborator gestisce quorum/voti; StudioAdmin revisiona criteri,
consolida/rettifica delibere, applica M06 e conclude. Il ruolo è verificato anche
sui retry. Nessun recapito riservato viene aggiunto ai DTO deliberativi.

Mutazioni serializzate per condominio e ExpectedVersion. RequestId/hash proteggono
apertura, consolidamento, applicazione e conclusione; riuso con parametri diversi
è rifiutato. Vincoli SQL verificano rappresentanza congelata, unicità, risultati,
catene e collegamenti; snapshot, revisioni e delibere sono append-only.

Migration `20261106210126_VotingMilestone072`: dieci tabelle additive. Nessuna
migration applicata al database di lavoro o automaticamente all'avvio.
[Fonti e limiti normativi](milestone-072-legal-sources.md).
