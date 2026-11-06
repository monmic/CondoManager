import { useState } from 'react'
import type { AllocationTable } from './api'
import { api } from './api'
import { errorMessage } from './form'
import { useCanWrite } from './permissions'
import type { AllocationGroup, AllocationRule, Criterion, RuleComponent } from './allocationTypes'
import { criterionNames, decimalString, decimalUnits } from './allocationTypes'
const component = (criterion: Criterion = 'EqualShares', percentage = '100'): RuleComponent => ({ criterion, percentage, tableId: null, groupId: null, consumptionType: null, unitOfMeasure: null, normalizeAdmitted: false })
export function AllocationRuleEditor({ base, row, tables, groups, onDone }: { base: string; row?: AllocationRule; tables: AllocationTable[]; groups: AllocationGroup[]; onDone: () => void }) {
  const canWrite = useCanWrite()
  const [name, setName] = useState(row?.name ?? '')
  const [description, setDescription] = useState(row?.description ?? '')
  const [type, setType] = useState<Criterion>(row?.type ?? 'EqualShares')
  const [components, setComponents] = useState<RuleComponent[]>(row?.components ?? [component()])
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  function change(index: number, value: Partial<RuleComponent>) { setComponents(components.map((c, i) => i === index ? { ...c, ...value } : c)) }
  let total = 'Percentuali non valide'
  try { total = decimalString(components.reduce((sum, c) => sum + decimalUnits(c.percentage, 6), 0n), 6) + '%' } catch { /* validation remains visible */ }
  return <div className="editor card"><h3>{row?.status === 'Active' ? 'Nuova versione della regola attiva' : 'Regola di ripartizione'}</h3><form onSubmit={async event => {
    event.preventDefault(); setError(''); setBusy(true)
    try { await api(base + '/rules' + (row ? '/' + row.id : ''), row ? 'PUT' : 'POST', { name, description: description || null, type, components }); onDone() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
  }}><fieldset disabled={busy || !canWrite}><div className="form-grid"><label>Nome regola<input required maxLength={160} value={name} onChange={e => setName(e.target.value)} /></label><label>Tipo regola<select value={type} onChange={e => { const next = e.target.value as Criterion; setType(next); setComponents(next === 'Mixed' ? [component('Millesimal', '50'), { ...component('Consumption', '50'), consumptionType: 'HEATING', unitOfMeasure: 'KWH' }] : [next === 'Consumption' ? { ...component(next), consumptionType: 'COLDWATER', unitOfMeasure: 'M3' } : component(next)]) }}>{Object.entries(criterionNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><label>Descrizione regola<input maxLength={160} value={description} onChange={e => setDescription(e.target.value)} /></label></div>
    {components.map((c, index) => <div className="card" key={index}><h4>Componente {index + 1}</h4><div className="form-grid"><label>Criterio {index + 1}<select value={c.criterion} disabled={type !== 'Mixed'} onChange={e => { const next = e.target.value as Criterion; change(index, { ...component(next, c.percentage), ...(next === 'Consumption' ? { consumptionType: 'HEATING', unitOfMeasure: 'KWH' } : {}) }) }}>{Object.entries(criterionNames).filter(([value]) => value !== 'Mixed' && (type !== 'Mixed' || value !== 'DirectCharge')).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><label>Peso percentuale {index + 1}<input required type="number" min="0.000001" max="100" step="0.000001" value={c.percentage} onChange={e => change(index, { percentage: e.target.value })} /></label><label>Gruppo {index + 1}<select value={c.groupId ?? ''} onChange={e => change(index, { groupId: e.target.value || null })}><option value="">Tutte le unita ammesse</option>{groups.filter(g => g.isActive).map(g => <option key={g.id} value={g.id}>{g.name}</option>)}</select></label>
      {c.criterion === 'Millesimal' && <><label>Tabella millesimale {index + 1}<select required value={c.tableId ?? ''} onChange={e => change(index, { tableId: e.target.value || null })}><option value="">Seleziona tabella</option>{tables.map(t => <option key={t.id} value={t.id}>{t.code} · {t.name} · {t.status}</option>)}</select></label><label>Normalizza quote ammesse {index + 1}<input type="checkbox" checked={c.normalizeAdmitted} onChange={e => change(index, { normalizeAdmitted: e.target.checked })} /><small>Esplicito: usa il totale effettivo del gruppo/esclusioni anziche il denominatore nominale. La scelta verra tracciata.</small></label></>}
      {c.criterion === 'Consumption' && <><label>Tipo consumo {index + 1}<input required maxLength={160} value={c.consumptionType ?? ''} onChange={e => change(index, { consumptionType: e.target.value })} /><small>COLDWATER, HOTWATER, HEATING o un codice personalizzato, uguale a quello delle letture.</small></label><label>Unita di misura {index + 1}<input required maxLength={160} value={c.unitOfMeasure ?? ''} onChange={e => change(index, { unitOfMeasure: e.target.value })} /></label></>}
    </div>{type === 'Mixed' && components.length > 2 && <button type="button" onClick={() => setComponents(components.filter((_, i) => i !== index))}>Rimuovi componente {index + 1}</button>}</div>)}
    {type === 'Mixed' && <button type="button" disabled={components.length >= 20} onClick={() => setComponents([...components, component('EqualShares', '1')])}>Aggiungi componente</button>}<p>Totale pesi: <strong>{total}</strong> · Richiesto esattamente 100%.</p></fieldset>{error && <p className="alert" role="alert">{error}</p>}<div className="actions"><button disabled={busy || !canWrite}>Salva regola</button><button type="button" disabled={busy} onClick={onDone}>Annulla regola</button></div></form></div>
}
