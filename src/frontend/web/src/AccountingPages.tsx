import { useCanWrite } from './permissions'
import { errorMessage, text } from './form'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { api, condoPath, today, useResource } from './api'
import type { AllocationTable, Period, Structure, Unit } from './api'
import { Badge, DataTable, Editor, Empty, Status, WriteButton } from './components'
import type { Field } from './components'

const tableFields: Field[] = [
  { key: 'code', label: 'Codice tabella', required: true }, { key: 'name', label: 'Denominazione', required: true },
  { key: 'denominator', label: 'Denominatore', required: true, type: 'number', min: '0.000001', step: '0.000001', defaultValue: '1000' },
  { key: 'validFrom', label: 'Decorrenza', required: true, type: 'date', defaultValue: today(), help: 'Non modificabile dopo aver inserito quote.' },
]
function EntryEditor({ unit, table, base, reload }: { unit: Unit; table: AllocationTable; base: string; reload: () => void }) {
  const canWrite = useCanWrite()
  const entry = table.entries.find(e => e.propertyUnitId === unit.id)
  const [value, setValue] = useState(entry?.value ?? '')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  async function save(e: FormEvent) {
    e.preventDefault(); setBusy(true); setError('')
    try { await api(base + '/' + table.id + '/entries/' + unit.id, 'PUT', { value }); reload() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
  }
  return <tr><td>{unit.code}</td><td>{entry?.value ?? <span className="missing">Mancante</span>}</td><td>
    {table.status === 'Draft' && !table.isArchived && canWrite ? <form className="inline-form" onSubmit={save}>
      <input aria-label={'Quota ' + unit.code} type="number" min="0" step="0.000001" required value={value} onChange={e => setValue(e.target.value)} disabled={busy} />
      <WriteButton disabled={busy}>{busy ? 'Salvo…' : 'Salva quota'}</WriteButton>
      {error && <span className="alert" role="alert">{error}</span>}
    </form> : 'Sola lettura'}
  </td></tr>
}
export function AllocationPage({ condominiumId }: { condominiumId: string }) {
  const base = condoPath(condominiumId) + '/allocation-tables'
  const resource = useResource<AllocationTable[]>(base)
  const structure = useResource<Structure>(condoPath(condominiumId) + '/structure')
  const [selected, setSelected] = useState('')
  const [edit, setEdit] = useState<AllocationTable | 'new' | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const current = resource.data?.find(x => x.id === selected)
  async function action(table: AllocationTable, activate: boolean) {
    if (!window.confirm(activate ? 'Attivare la tabella? Quote e configurazione diventeranno immutabili. Per variazioni future servira una nuova tabella.' : 'Archiviare questa bozza? I dati saranno conservati e non piu modificabili.')) return
    setBusy(true); setError('')
    try { await api(base + '/' + table.id + (activate ? '/activate' : ''), activate ? 'POST' : 'DELETE'); resource.reload() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
  }
  return <><div className="section-heading"><h2>Tabelle del condominio</h2><WriteButton className="primary" onClick={() => setEdit('new')}>Nuova tabella</WriteButton></div>
    <p className="muted">Salva le quote anche incomplete come bozza. Per attivare occorrono tutte le unita attive e un totale esattamente uguale al denominatore. Zero e una quota esplicita.</p>
    {edit && <Editor key={edit === 'new' ? 'new' : edit.id} title="Tabella millesimale" fields={tableFields} initial={edit === 'new' ? {} : edit} onCancel={() => setEdit(null)} onSave={async values => {
      const result = await api<AllocationTable>(base + (edit === 'new' ? '' : '/' + edit.id), edit === 'new' ? 'POST' : 'PUT', values)
      setEdit(null); setSelected(result.id); resource.reload()
    }} />}
    {error && <p role="alert" className="alert">{error}</p>}
    <Status resource={resource}>{!resource.data?.length ? <Empty /> : <DataTable headings={['Codice / nome', 'Decorrenza', 'Totale / denominatore', 'Stato', 'Azioni']}>{resource.data.map(row =>
      <tr key={row.id}><td>{row.code} · {row.name}</td><td>{row.validFrom}</td><td>{row.validation.total} / {row.denominator}</td><td><Badge active={row.status === 'Active'}>{row.isArchived ? 'Archiviata' : row.status === 'Active' ? 'Attiva' : 'Bozza'}</Badge></td><td className="actions">
        <button onClick={() => setSelected(row.id)}>Quote</button>{row.status === 'Draft' && !row.isArchived && <><WriteButton onClick={() => setEdit(row)}>Modifica tabella</WriteButton>
          <WriteButton disabled={busy || !row.validation.isValid || !row.entries.length} onClick={() => void action(row, true)}>Attiva</WriteButton><WriteButton disabled={busy} onClick={() => void action(row, false)}>Archivia</WriteButton></>}
      </td></tr>)}</DataTable>}</Status>
    {current && <div className="card"><h3>{current.name} · Quote</h3><div className="metrics"><strong>Totale salvato: {current.validation.total} / {current.denominator}</strong>
      <Badge active={current.validation.isValid}>{current.validation.isValid && current.entries.length ? 'Validazione superata' : 'Da completare'}</Badge></div>
      {!!current.validation.missingUnitIds.length && <p className="missing">Quote mancanti: {current.validation.missingUnitIds.map(id => structure.data?.units.find(u => u.id === id)?.code ?? id).join(', ')}</p>}
      {!!current.validation.unexpectedUnitIds.length && <p className="alert">Sono presenti quote per unita non piu attive. Per le bozze, riattivare l'unita o creare una nuova tabella.</p>}
      {current.validation.hasDuplicates && <p className="alert">Quote storiche sovrapposte: occorre una verifica dei dati.</p>}
      <Status resource={structure}>{!structure.data?.units.filter(u => u.isActive).length ? <Empty>Inserisci prima le unita immobiliari dal dettaglio condominio.</Empty> :
        <DataTable headings={['Unita', 'Quota salvata', 'Modifica']}>{structure.data.units.filter(u => u.isActive).map(unit =>
          <EntryEditor key={current.id + unit.id + current.entries.find(e => e.propertyUnitId === unit.id)?.value} unit={unit} table={current} base={base} reload={resource.reload} />)}</DataTable>}
      </Status><p className="muted">Le quote di una tabella attiva sono definitive. Per aggiornarle crea una nuova tabella, conservando lo storico.</p>
    </div>}
  </>
}
const periodFields: Field[] = [
  { key: 'code', label: 'Codice esercizio', required: true }, { key: 'name', label: 'Denominazione', required: true },
  { key: 'startsOn', label: 'Data inizio', required: true, type: 'date', defaultValue: today() },
  { key: 'endsOn', label: 'Data fine (inclusa)', required: true, type: 'date' },
]
export function PeriodsPage({ condominiumId }: { condominiumId: string }) {
  const base = condoPath(condominiumId) + '/accounting-periods'
  const resource = useResource<Period[]>(base)
  const [edit, setEdit] = useState<Period | 'new' | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  async function transition(row: Period, close: boolean) {
    if (!window.confirm(close ? 'Chiudere definitivamente questo esercizio? Non sara possibile modificarlo o riaprirlo.' : 'Aprire questo esercizio? Date e denominazione non saranno piu modificabili.')) return
    setBusy(true); setError('')
    try { await api(base + '/' + row.id + (close ? '/close' : '/open'), 'POST'); resource.reload() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
  }
  return <><div className="section-heading"><h2>Esercizi del condominio</h2><WriteButton className="primary" onClick={() => setEdit('new')}>Nuovo esercizio</WriteButton></div>
    <p className="muted">Intervalli liberi, anche diversi dall'anno solare. Nessuna sovrapposizione, inclusi gli estremi. Nessun calcolo di bilancio in questa milestone.</p>
    {edit && <Editor key={edit === 'new' ? 'new' : edit.id} title="Esercizio contabile" fields={periodFields} initial={edit === 'new' ? {} : edit} onCancel={() => setEdit(null)} onSave={async values => {
      await api(base + (edit === 'new' ? '' : '/' + edit.id), edit === 'new' ? 'POST' : 'PUT', {
        ...values, startsOn: text(values, 'startsOn'), endsOn: text(values, 'endsOn'),
      }); setEdit(null); resource.reload()
    }} />}
    {error && <p className="alert" role="alert">{error}</p>}
    <Status resource={resource}>{!resource.data?.length ? <Empty /> : <DataTable headings={['Codice / denominazione', 'Inizio', 'Fine inclusa', 'Stato', 'Data chiusura', 'Azioni']}>{resource.data.map(row =>
      <tr key={row.id}><td>{row.code} · {row.name}</td><td>{row.startsOn}</td><td>{row.endsOn}</td><td><Badge active={row.status === 'Open'}>{row.status}</Badge></td><td>{row.closedAt ? new Date(row.closedAt).toLocaleString('it-IT') : '—'}</td><td className="actions">
        {row.status === 'Draft' && <><WriteButton onClick={() => setEdit(row)}>Modifica esercizio</WriteButton><WriteButton disabled={busy} onClick={() => void transition(row, false)}>Apri esercizio</WriteButton></>}
        {row.status === 'Open' && <WriteButton disabled={busy} onClick={() => void transition(row, true)}>Chiudi definitivamente</WriteButton>}
        {row.status === 'Closed' && <span className="muted">Sola lettura</span>}
      </td></tr>)}</DataTable>}</Status>
  </>
}