# Glossario M06.2

- **Piano in bozza**: simulazione persistita, non genera crediti.
- **Piano emesso**: approvato da StudioAdmin, credito rilevato atomicamente nel giornale.
- **Scadenza**: data di esigibilità, diversa dalla rilevazione contabile.
- **Debitore originario**: soggetto designato e congelato per quota/unità; non cambia con la proprietà.
- **Pagante**: chi versa, anche diverso dal debitore.
- **Incasso segnalato**: RecordOnly/Unmatched, senza movimento finanziario e non attribuibile.
- **Incasso associato**: Matched, movimento riservato ma riconciliazione da confermare.
- **Incasso riconciliato**: Reconciled, corrisponde a un unico movimento M04 verificato.
- **Attribuzione**: parte di fondi riconciliati destinata a un credito, senza nuova movimentazione bancaria.
- **Non attribuito**: quota ancora nel transitorio; non è una rata pagata.
- **Residuo**: emesso meno attribuzioni nette; non è modificabile manualmente.
- **Eccedenza**: incasso non attribuito, identificato per debitore se noto; nessuna compensazione automatica.
- **Rettifica attribuzione**: storno dell'intero gruppo, con nuovi record e giornale opposto.

Le formule di conguaglio M06.1 non sono ancora consuntivi o conguagli definitivi:
questi restano nella M06.3.
