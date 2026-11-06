import { useState } from 'react'
import type { FormEvent } from 'react'
import { api, condoPath, useResource } from './api'
import type { Period } from './api'
import { DataTable, Empty, Status, WriteButton } from './components'
import { useCanWrite } from './permissions'
import { errorMessage } from './form'
import { euro } from './ledgerTypes'
import type { AllocationRule as Rule } from './allocationTypes'

type Line = { accountId: string; description: string; amount: string; ruleId: string; notes?: string; excludedUnitIds?: string[]; directCharges?: { propertyUnitId: string; personId?: string; amount: string }[] }
type Result = { originalAmount: string; allocatedAmount: string; difference: string; isValid: boolean; errors: string[]; warnings: string[]; lines: { unitCode: string; personName?: string; amount: string; sourcePosition: number; componentPosition: number }[] }
type Budget = { id: string; accountingPeriodId: string; version: number; previousVersionId?: string; description: string; notes?: string; status: string; total: string; previousTotal?: string; difference?: string; approvedAt?: string; decisionReference?: string; lines: Line[]; allocation?: Result }
type Preview = { hash: string; result: Result }
type Account = { id: string; code: string; name: string; type: string; isActive: boolean }
const blankLine = (): Line => ({ accountId: '', description: '', amount: '', ruleId: '' })
export function BudgetsPage({ condominiumId, isAdmin }: { condominiumId: string; isAdmin: boolean }) {
  const base = '/budgets/condominiums/' + condominiumId
  const resource = useResource<Budget[]>(base)
  const periods = useResource<Period[]>(condoPath(condominiumId) + '/accounting-periods')
  const rules = useResource<Rule[]>('/allocations/condominiums/' + condominiumId + '/rules')
  const accounts = useResource<Account[]>('/accounting/condominiums/' + condominiumId + '/accounts')
  const [selected, setSelected] = useState<string>('')
  const [editing, setEditing] = useState(false)
  const [editingId, setEditingId] = useState<string>()
  const [previous, setPrevious] = useState<string>()
  const [period, setPeriod] = useState('')
  const [description, setDescription] = useState('')
  const [notes, setNotes] = useState('')
  const [lines, setLines] = useState<Line[]>([blankLine()])
  const [preview, setPreview] = useState<Preview>()
  const [reference, setReference] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const canWrite = useCanWrite()
  const current = resource.data?.find(b => b.id === selected)
  function edit(b?: Budget, clone = false) {
    setEditingId(clone ? undefined : b?.id); setPrevious(clone ? b?.id : b?.previousVersionId)
    setPeriod(b?.accountingPeriodId ?? ''); setDescription(b?.description ?? ''); setNotes(b?.notes ?? '')
    setLines(b?.lines.map(l => ({ ...l })) ?? [blankLine()]); setEditing(true); setPreview(undefined); setError('')
  }
  async function action(run: () => Promise<void>) { setBusy(true); setError(''); try { await run() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) } }
  async function save(e: FormEvent) {
    e.preventDefault()
    await action(async () => {
      const b = await api<Budget>(base + (editingId ? '/' + editingId : ''), editingId ? 'PUT' : 'POST', { accountingPeriodId: period, description, notes, previousVersionId: previous, lines })
      setSelected(b.id); setEditing(false); setPreview(undefined); resource.reload()
    })
  }
  function patch(index: number, changes: Partial<Line>) { setLines(old => old.map((l, i) => i === index ? { ...l, ...changes } : l)) }
  const result = preview?.result ?? current?.allocation
  return <section><div className="section-heading"><div><h2>Preventivi</h2><p>Versioni amministrative e riparti preventivi, distinti dalle spese contabilizzate.</p></div><WriteButton onClick={() => edit()} disabled={busy}>Nuovo preventivo</WriteButton></div>
    {error && <p className="alert" role="alert">{error}</p>}
    <Status resource={periods}><Status resource={accounts}><Status resource={rules}><Status resource={resource}>
      {editing && <div className="card editor"><h3>{editingId ? 'Modifica bozza' : previous ? 'Nuova versione' : 'Nuovo preventivo'}</h3><form onSubmit={save}><fieldset disabled={busy || !canWrite}><div className="form-grid">
        <label>Esercizio<select required value={period} disabled={!!editingId || !!previous} onChange={e => setPeriod(e.target.value)}><option value="">Seleziona…</option>{periods.data?.filter(p => p.status === 'Open').map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
        <label>Descrizione preventivo<input required maxLength={160} value={description} onChange={e => setDescription(e.target.value)} /></label>
        <label>Note<textarea value={notes} maxLength={4000} onChange={e => setNotes(e.target.value)} /></label>
      </div>{lines.map((line, i) => <fieldset key={i}><legend>Voce {i + 1}</legend><div className="form-grid">
        <label>Categoria<select required value={line.accountId} onChange={e => patch(i, { accountId: e.target.value })}><option value="">Seleziona…</option>{accounts.data?.filter(a => a.isActive && a.type === 'Expense').map(a => <option key={a.id} value={a.id}>{a.code} · {a.name}</option>)}</select></label>
        <label>Descrizione voce<input required value={line.description} maxLength={160} onChange={e => patch(i, { description: e.target.value })} /></label>
        <label>Importo previsto<input required type="number" min="0.01" step="0.01" value={line.amount} onChange={e => patch(i, { amount: e.target.value })} /></label>
        <label>Regola di riparto<select required value={line.ruleId} onChange={e => patch(i, { ruleId: e.target.value })}><option value="">Seleziona…</option>{rules.data?.filter(r => r.status === 'Active').map(r => <option key={r.id} value={r.id}>{r.name} · v{r.version}</option>)}</select></label>
        <label>Note voce<textarea value={line.notes ?? ''} maxLength={4000} onChange={e => patch(i, { notes: e.target.value })} /></label>
      </div><button type="button" onClick={() => setLines(old => old.filter((_, n) => n !== i))} disabled={lines.length === 1}>Rimuovi voce</button></fieldset>)}
      <div className="actions"><button type="button" onClick={() => setLines(old => [...old, blankLine()])} disabled={lines.length >= 100}>Aggiungi voce</button><button className="primary">Salva preventivo</button><button type="button" onClick={() => setEditing(false)}>Annulla</button></div></fieldset></form></div>}
      {!resource.data?.length ? <Empty>Nessun preventivo. Configura esercizio aperto, unità, categorie e regole attive per iniziare.</Empty> : <DataTable headings={['Versione', 'Descrizione', 'Stato', 'Totale', 'Differenza da precedente', 'Azioni']}>{resource.data.map(b => <tr key={b.id}><td>{b.version}</td><td>{b.description}</td><td>{b.status}</td><td>{euro(b.total)}</td><td>{b.difference == null ? '—' : euro(b.difference)}</td><td><button onClick={() => { setSelected(b.id); setPreview(undefined); setReference(b.decisionReference ?? ''); setError('') }}>Dettaglio</button></td></tr>)}</DataTable>}
      {current && <div className="card"><h3>Preventivo v{current.version} · {current.description}</h3><p>Stato: {current.status}. Totale: {euro(current.total)}.</p>
        {current.approvedAt && <p>Approvazione applicativa: {new Date(current.approvedAt).toLocaleString('it-IT')}. Riferimento: {current.decisionReference ?? 'non indicato'}.</p>}
        <p>L’approvazione applicativa non costituisce prova di una delibera assembleare e non genera crediti contabili.</p>
        <DataTable headings={['Voce', 'Categoria', 'Importo', 'Regola']}>{current.lines.map((l, i) => <tr key={i}><td>{l.description}</td><td>{accounts.data?.find(a => a.id === l.accountId)?.name ?? l.accountId}</td><td>{euro(l.amount)}</td><td>{rules.data?.find(r => r.id === l.ruleId)?.name ?? l.ruleId}</td></tr>)}</DataTable>
        <div className="actions">{current.status === 'Draft' && <><WriteButton disabled={busy} onClick={() => edit(current)}>Modifica bozza</WriteButton><WriteButton disabled={busy} onClick={() => action(async () => { await api(base + '/' + current.id + '/propose', 'POST', {}); resource.reload() })}>Proponi preventivo</WriteButton></>}
          <WriteButton disabled={busy || resource.data?.some(b => b.accountingPeriodId === current.accountingPeriodId && b.version > current.version)} onClick={() => edit(current, true)}>Crea nuova versione</WriteButton>
          <button disabled={busy} onClick={() => action(async () => { setPreview(await api<Preview>(base + '/' + current.id + '/preview', 'POST', {})) })}>Simula riparto</button>
        </div>
        {current.status === 'Proposed' && isAdmin && preview?.result.isValid && <><label>Riferimento delibera (facoltativo)<input value={reference} onChange={e => setReference(e.target.value)} maxLength={160} /></label><WriteButton disabled={busy} onClick={() => {
          if (window.confirm('Approvare questa versione e congelare il riparto? Una precedente versione approvata verrà sostituita nello storico.')) void action(async () => { await api(base + '/' + current.id + '/approve', 'POST', { previewHash: preview.hash, decisionReference: reference || null }); setPreview(undefined); resource.reload() })
        }}>Approva preventivo</WriteButton></>}
        {result && <><h3>Riparto {current.allocation ? 'approvato' : 'simulato'}</h3><p>Originale {euro(result.originalAmount)} · ripartito {euro(result.allocatedAmount)} · differenza {euro(result.difference)}</p>{[...result.errors, ...result.warnings].map((message, i) => <p role="alert" key={i}>{message}</p>)}<DataTable headings={['Voce', 'Componente', 'Unità', 'Soggetto esplicito', 'Quota']}>{result.lines.map((l, i) => <tr key={i}><td>{l.sourcePosition + 1}</td><td>{l.componentPosition + 1}</td><td>{l.unitCode}</td><td>{l.personName ?? 'Da designare per le rate'}</td><td>{euro(l.amount)}</td></tr>)}</DataTable></>}
      </div>}
    </Status></Status></Status></Status>
  </section>
}
