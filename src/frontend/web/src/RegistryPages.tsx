import { errorMessage, optional, options, text } from './form'
import { useState } from 'react'
import { api, condoPath, today, useResource } from './api'
import type { Association, Building, Condominium, Person, Staircase, Structure, Unit } from './api'
import { Badge, DataTable, Editor, Empty, Status, WriteButton } from './components'
import type { Field } from './components'
const condoFields: Field[] = [
  { key: 'code', label: 'Codice interno', required: true }, { key: 'name', label: 'Denominazione', required: true },
  { key: 'taxCode', label: 'Codice fiscale', maxLength: 32 }, { key: 'address', label: 'Indirizzo', required: true },
  { key: 'postalCode', label: 'CAP', required: true, maxLength: 16 }, { key: 'city', label: 'Comune', required: true },
  { key: 'province', label: 'Provincia', required: true, maxLength: 40 }, { key: 'country', label: 'Stato', required: true, maxLength: 80 },
  { key: 'isActive', label: 'Attivo', type: 'checkbox' },
]
export function CondominiumsPage({ rows, selected, onSelect, refresh }: { rows: Condominium[]; selected: string; onSelect: (id: string) => void; refresh: () => void }) {
  const [editing, setEditing] = useState<Condominium | 'new' | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const current = rows.find(x => x.id === selected)
  async function deactivate(row: Condominium) {
    if (!window.confirm('Disattivare il condominio? I dati e lo storico saranno conservati.')) return
    setBusy(true); setError('')
    try { await api(condoPath(row.id), 'DELETE'); refresh() } catch (e) { setError(errorMessage(e)) } finally { setBusy(false) }
  }
  return <>
    <div className="section-heading"><h2>Condomini dello studio</h2><WriteButton className="primary" onClick={() => setEditing('new')}>Nuovo condominio</WriteButton></div>
    {editing && <Editor key={editing === 'new' ? 'new' : editing.id} title={editing === 'new' ? 'Nuovo condominio' : 'Modifica condominio'} fields={condoFields} initial={editing === 'new' ? {} : editing}
      onCancel={() => setEditing(null)} onSave={async values => {
        const saved = await api<Condominium>('/condominiums' + (editing === 'new' ? '' : '/' + editing.id), editing === 'new' ? 'POST' : 'PUT', { ...values, taxCode: optional(values, 'taxCode') })
        setEditing(null); onSelect(saved.id); refresh()
      }} />}
    {error && <p role="alert" className="alert">{error}</p>}
    {!rows.length ? <Empty>Crea il primo condominio dello studio.</Empty> : <DataTable headings={['Codice', 'Denominazione', 'Comune', 'Stato', 'Azioni']}>
      {rows.map(row => <tr key={row.id}><td>{row.code}</td><td>{row.name}</td><td>{row.city || 'Da completare'}</td><td><Badge active={row.isActive} /></td><td className="actions">
        <button onClick={() => onSelect(row.id)}>Apri</button><WriteButton onClick={() => setEditing(row)}>Modifica</WriteButton>
        {row.isActive && <WriteButton disabled={busy} onClick={() => void deactivate(row)}>Disattiva</WriteButton>}
      </td></tr>)}</DataTable>}
    {current && <div className="detail" key={current.id}><div className="section-heading"><h2>{current.name}</h2><Badge active={current.isActive} /></div>
      <p>{current.address} · {current.postalCode} {current.city} {current.province} · {current.country}</p>
      <p className="muted">Codice fiscale: {current.taxCode ?? 'Non indicato'} · Creato: {new Date(current.createdAt).toLocaleString('it-IT')} · Aggiornato: {new Date(current.updatedAt).toLocaleString('it-IT')}</p>
      {!current.isActive ? <Empty>Condominio inattivo. Riattivalo dalla modifica per gestire i dati collegati.</Empty> : <><StructurePanel condominiumId={current.id} /><AssociationPanel condominiumId={current.id} /></>}
    </div>}
  </>
}
const kindOptions = [
  { value: 'Apartment', label: 'Appartamento' }, { value: 'Garage', label: 'Box' }, { value: 'Cellar', label: 'Cantina' },
  { value: 'Shop', label: 'Negozio' }, { value: 'Other', label: 'Altro' },
]
type StructureEdit = { kind: 'buildings'; row?: Building } | { kind: 'staircases'; row?: Staircase } | { kind: 'units'; row?: Unit }
function StructurePanel({ condominiumId }: { condominiumId: string }) {
  const resource = useResource<Structure>(condoPath(condominiumId) + '/structure')
  const [edit, setEdit] = useState<StructureEdit | null>(null)
  const data = resource.data
  const basic: Field[] = [{ key: 'code', label: 'Codice', required: true }, { key: 'name', label: 'Denominazione', required: true }, { key: 'isActive', label: 'Attivo', type: 'checkbox' }]
  const buildingField: Field = { key: 'buildingId', label: 'Edificio (facoltativo)', type: 'select', options: options(data?.buildings.filter(x => x.isActive || x.id === (edit?.row as Unit | Staircase | undefined)?.buildingId) ?? []) }
  const fields: Field[] = edit?.kind === 'units' ? [
    { key: 'code', label: 'Codice unita', required: true }, buildingField,
    { key: 'staircaseId', label: 'Scala (facoltativa)', type: 'select', options: options(data?.staircases.filter(x => x.isActive || x.id === (edit.row as Unit | undefined)?.staircaseId) ?? []) },
    { key: 'interior', label: 'Interno' }, { key: 'floor', label: 'Piano' },
    { key: 'kind', label: 'Tipologia', type: 'select', required: true, options: kindOptions, defaultValue: 'Apartment' },
    { key: 'cadastralData', label: 'Dati catastali', type: 'textarea', maxLength: 2000 }, { key: 'notes', label: 'Note', type: 'textarea' },
    { key: 'isActive', label: 'Attiva', type: 'checkbox' },
  ] : edit?.kind === 'staircases' ? [...basic, buildingField] : basic
  return <div className="card"><div className="section-heading"><h3>Edifici, scale e unita</h3><div className="actions">
    <WriteButton onClick={() => setEdit({ kind: 'buildings' })}>Nuovo edificio</WriteButton><WriteButton onClick={() => setEdit({ kind: 'staircases' })}>Nuova scala</WriteButton><WriteButton onClick={() => setEdit({ kind: 'units' })}>Nuova unita</WriteButton>
  </div></div><p className="muted">Edificio e scala sono facoltativi. Una scala con edificio determina anche l'edificio dell'unita.</p>
    <Status resource={resource}>{data && <>
      {edit && <Editor key={edit.kind + (edit.row?.id ?? 'new')} title={edit.row ? 'Modifica' : 'Crea'} fields={fields} initial={edit.row} onCancel={() => setEdit(null)} onSave={async values => {
        const body = edit.kind === 'buildings' ? values : edit.kind === 'staircases' ? { ...values, buildingId: optional(values, 'buildingId') } : {
          ...values, buildingId: optional(values, 'buildingId'), staircaseId: optional(values, 'staircaseId'),
          interior: optional(values, 'interior'), floor: optional(values, 'floor'), notes: optional(values, 'notes'), cadastralData: optional(values, 'cadastralData'),
        }
        await api(condoPath(condominiumId) + '/' + edit.kind + (edit.row ? '/' + edit.row.id : ''), edit.row ? 'PUT' : 'POST', body)
        setEdit(null); resource.reload()
      }} />}
      <h4>Edifici</h4>{!data.buildings.length ? <Empty>Nessun edificio. Le unita possono essere collegate direttamente al condominio.</Empty> : <DataTable headings={['Codice', 'Nome', 'Stato', 'Azioni']}>{data.buildings.map(row =>
        <tr key={row.id}><td>{row.code}</td><td>{row.name || row.code}</td><td><Badge active={row.isActive} /></td><td><WriteButton onClick={() => setEdit({ kind: 'buildings', row })}>Modifica edificio</WriteButton></td></tr>)}</DataTable>}
      <h4>Scale</h4>{!data.staircases.length ? <Empty /> : <DataTable headings={['Codice', 'Nome', 'Edificio', 'Stato', 'Azioni']}>{data.staircases.map(row =>
        <tr key={row.id}><td>{row.code}</td><td>{row.name}</td><td>{data.buildings.find(b => b.id === row.buildingId)?.code ?? 'Diretta al condominio'}</td><td><Badge active={row.isActive} /></td><td><WriteButton onClick={() => setEdit({ kind: 'staircases', row })}>Modifica scala</WriteButton></td></tr>)}</DataTable>}
      <h4>Unita immobiliari</h4>{!data.units.length ? <Empty /> : <DataTable headings={['Codice', 'Interno / piano', 'Tipologia', 'Edificio / scala', 'Stato', 'Azioni']}>{data.units.map(row =>
        <tr key={row.id}><td>{row.code}</td><td>{row.interior ?? '—'} / {row.floor ?? '—'}</td><td>{kindOptions.find(k => k.value === row.kind)?.label}</td><td>{data.buildings.find(b => b.id === row.buildingId)?.code ?? '—'} / {data.staircases.find(s => s.id === row.staircaseId)?.code ?? '—'}</td><td><Badge active={row.isActive} /></td><td><WriteButton onClick={() => setEdit({ kind: 'units', row })}>Modifica unita</WriteButton></td></tr>)}</DataTable>}
    </>}</Status></div>
}
const personFields: Field[] = [
  { key: 'code', label: 'Codice interno', required: true },
  { key: 'kind', label: 'Tipo soggetto', type: 'select', required: true, defaultValue: 'NaturalPerson', options: [{ value: 'NaturalPerson', label: 'Persona fisica' }, { value: 'LegalEntity', label: 'Persona giuridica' }] },
  { key: 'firstName', label: 'Nome', help: 'Obbligatorio per persona fisica.' }, { key: 'lastNameOrCompany', label: 'Cognome / ragione sociale', required: true },
  { key: 'taxCode', label: 'Codice fiscale', required: true, maxLength: 32 }, { key: 'vatNumber', label: 'Partita IVA', maxLength: 32 },
  { key: 'email', label: 'Email', type: 'email', maxLength: 254 }, { key: 'phone', label: 'Telefono', maxLength: 60 },
  { key: 'mailingAddress', label: 'Indirizzo di recapito', maxLength: 500 }, { key: 'notes', label: 'Note', type: 'textarea' }, { key: 'isActive', label: 'Attivo', type: 'checkbox' },
]
export function PeoplePage() {
  const resource = useResource<Person[]>('/people')
  const [edit, setEdit] = useState<Person | 'new' | null>(null)
  return <><div className="section-heading"><h2>Soggetti dello studio</h2><WriteButton className="primary" onClick={() => setEdit('new')}>Nuovo soggetto</WriteButton></div>
    <p className="muted">Persone fisiche e giuridiche. Le associazioni alle unita si gestiscono nel dettaglio del condominio.</p>
    {edit && <Editor key={edit === 'new' ? 'new' : edit.id} title="Anagrafica soggetto" fields={personFields} initial={edit === 'new' ? {} : edit} onCancel={() => setEdit(null)} onSave={async values => {
      await api('/people' + (edit === 'new' ? '' : '/' + edit.id), edit === 'new' ? 'POST' : 'PUT', { ...values,
        firstName: optional(values, 'firstName'), vatNumber: optional(values, 'vatNumber'), email: optional(values, 'email'),
        phone: optional(values, 'phone'), mailingAddress: optional(values, 'mailingAddress'), notes: optional(values, 'notes'),
      })
      setEdit(null); resource.reload()
    }} />}
    <Status resource={resource}>{!resource.data?.length ? <Empty /> : <DataTable headings={['Codice', 'Soggetto', 'Codice fiscale', 'Email', 'Stato', 'Azioni']}>{resource.data.map(row =>
      <tr key={row.id}><td>{row.code}</td><td>{row.displayName}{row.kind === 'Unspecified' && <small> Dati legacy da completare</small>}</td><td>{row.taxCode ?? 'Non indicato'}</td><td>{row.email ?? '—'}</td><td><Badge active={row.isActive} /></td><td><WriteButton onClick={() => setEdit(row)}>Modifica soggetto</WriteButton></td></tr>)}</DataTable>}</Status>
  </>
}
const roleOptions = [{ value: 'Owner', label: 'Proprietario' }, { value: 'CoOwner', label: 'Comproprietario' }, { value: 'Tenant', label: 'Inquilino' }, { value: 'Usufructuary', label: 'Usufruttuario' }, { value: 'Other', label: 'Altro' }]
function AssociationPanel({ condominiumId }: { condominiumId: string }) {
  const resource = useResource<Association[]>(condoPath(condominiumId) + '/associations')
  const structure = useResource<Structure>(condoPath(condominiumId) + '/structure')
  const people = useResource<Person[]>('/people')
  const [create, setCreate] = useState(false)
  const [ending, setEnding] = useState<Association | null>(null)
  const fields: Field[] = [
    { key: 'unitId', label: 'Unita', required: true, type: 'select', options: options(structure.data?.units.filter(x => x.isActive) ?? []) },
    { key: 'personId', label: 'Soggetto', required: true, type: 'select', options: people.data?.filter(x => x.isActive).map(x => ({ value: x.id, label: x.displayName + ' (' + x.code + ')' })) ?? [] },
    { key: 'role', label: 'Ruolo', required: true, type: 'select', options: roleOptions, defaultValue: 'Owner' },
    { key: 'validFrom', label: 'Valido dal', type: 'date', required: true, defaultValue: today() },
    { key: 'validTo', label: 'Valido fino al (incluso)', type: 'date' },
    { key: 'fraction', label: 'Quota proprieta (0–1)', type: 'number', min: '0.0000000001', step: '0.0000000001', help: 'Solo proprietario/comproprietario; es. 0,5. Nessuna quota per gli altri ruoli.' },
  ]
  return <div className="card"><div className="section-heading"><h3>Soggetti e storico delle associazioni</h3><WriteButton onClick={() => { structure.reload(); people.reload(); setCreate(true) }}>Associa soggetto</WriteButton></div>
    <p className="muted">Per un cambio di proprietario, concludi la precedente associazione e creane una nuova. Le date finali sono inclusive.</p>
    {create && <Status resource={structure}><Status resource={people}><Editor title="Nuova associazione" fields={fields} onCancel={() => setCreate(false)} onSave={async values => {
      await api(condoPath(condominiumId) + '/units/' + text(values, 'unitId') + '/associations', 'POST', {
        personId: text(values, 'personId'), role: text(values, 'role'), validFrom: text(values, 'validFrom'), validTo: optional(values, 'validTo'),
        fraction: ['Owner', 'CoOwner'].includes(text(values, 'role')) ? optional(values, 'fraction') : null,
      })
      setCreate(false); resource.reload()
    }} /></Status></Status>}
    {ending && <Editor key={ending.id} title={'Concludi associazione: ' + ending.personName} fields={[{ key: 'validTo', label: 'Ultimo giorno di validita', required: true, type: 'date', defaultValue: today(), min: ending.validFrom }]} onCancel={() => setEnding(null)} onSave={async values => {
      if (!window.confirm('Concludere questa associazione? La data finale non sara modificabile.')) return
      await api(condoPath(condominiumId) + '/associations/' + ending.id + '/end', 'POST', values)
      setEnding(null); resource.reload()
    }} />}
    <Status resource={resource}>{!resource.data?.length ? <Empty /> : <DataTable headings={['Unita', 'Soggetto', 'Ruolo', 'Dal', 'Al (incluso)', 'Quota', 'Azioni']}>{resource.data.map(row =>
      <tr key={row.id}><td>{row.unitCode}</td><td>{row.personName}</td><td>{roleOptions.find(r => r.value === row.role)?.label}</td><td>{row.validFrom}</td><td>{row.validTo ?? 'Aperta'}</td><td>{row.fraction ?? '—'}</td><td>{!row.validTo && <WriteButton onClick={() => setEnding(row)}>Concludi</WriteButton>}</td></tr>)}</DataTable>}</Status>
  </div>
}