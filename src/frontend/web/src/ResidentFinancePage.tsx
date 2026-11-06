import { useState } from 'react'
import type { FormEvent } from 'react'
import { api, condoPath, today, useResource } from './api'
import type { Association, Period, Person, Structure } from './api'
import { DataTable, Editor, Empty, Status, WriteButton } from './components'
import type { Field } from './components'
import { errorMessage } from './form'
import { euro } from './ledgerTypes'
import { decimalString, decimalUnits } from './allocationTypes'
import { useCanWrite } from './permissions'

type Schedule = { dueDate: string; weight: string }
type Debtor = { propertyUnitId: string; personId: string; share: string }
type PlanInput = { budgetId: string; debtorDate: string; mode: string; schedule: Schedule[]; debtors: Debtor[]; requestId: string; previousPlanId?: string }
type Preview = { hash: string; total: string; quotas: { propertyUnitId: string; debtorId: string; amount: string }[]; installments: { propertyUnitId: string; debtorId: string; position: number; dueDate: string; amount: string }[]; differences: { propertyUnitId: string; debtorId: string; previousAmount: string; newAmount: string; difference: string }[] }
type Plan = { id: string; budgetId: string; accountingPeriodId: string; version: number; status: string; total: string; journalEntryId?: string; input: PlanInput; snapshot: Preview }
type Budget = { id: string; accountingPeriodId: string; description: string; version: number; status: string }
type Credit = { id: string; planId: string; accountingPeriodId: string; propertyUnitId: string; unitCode: string; debtorId: string; debtorName: string; dueDate: string; amount: string; paid: string; residual: string; status: string }
type Receipt = { id: string; accountingPeriodId: string; financialAccountId: string; paymentDate: string; amount: string; reference?: string; debtorId?: string; payerId?: string; status: string; bankJournalId?: string; allocated: string; unallocated: string; allocations: { id: string; installmentId: string; amount: string; journalEntryId: string; reversesAllocationId?: string; isReversed: boolean }[] }
type Bank = { id: string; name: string; isActive: boolean }
type Journal = { id: string; date: string; description: string; reference?: string; lines: { debit: string; credit: string; name: string }[] }
type Position = { propertyUnitId: string; unitCode: string; debtorId: string; debtorName: string; issued: string; allocated: string; residual: string; overdue: string; notDue: string; opening?: string; definitiveAdjustment?: string }
type Balances = { positions: Position[]; issued: string; allocated: string; residual: string; unattributed: string; unmatched: string }
const emptyInput = (): PlanInput => ({ budgetId: '', debtorDate: today(), mode: 'Equal', schedule: [{ dueDate: today(), weight: '1' }], debtors: [], requestId: crypto.randomUUID() })
const statusLabel: Record<string, string> = { Unmatched: 'Da associare', Matched: 'Associato', Reconciled: 'Riconciliato', Paid: 'Pagata', PartiallyPaid: 'Parzialmente pagata', Unpaid: 'Da pagare', Overdue: 'Scaduta', NotRequired: 'Quota zero', Draft: 'Bozza', Approved: 'Emesso', Superseded: 'Sostituito' }
export function ResidentFinancePage({ condominiumId, isAdmin }: { condominiumId: string; isAdmin: boolean }) {
  const base = '/resident-finance/condominiums/' + condominiumId
  const plans = useResource<Plan[]>(base + '/plans')
  const credits = useResource<Credit[]>(base + '/credits')
  const receipts = useResource<Receipt[]>(base + '/receipts')
  const balances = useResource<Balances>(base + '/balances')
  const movements = useResource<Journal[]>(base + '/bank-movements')
  const banks = useResource<Bank[]>('/accounting/condominiums/' + condominiumId + '/financial-accounts')
  const budgets = useResource<Budget[]>('/budgets/condominiums/' + condominiumId)
  const periods = useResource<Period[]>(condoPath(condominiumId) + '/accounting-periods')
  const structure = useResource<Structure>(condoPath(condominiumId) + '/structure')
  const people = useResource<Person[]>('/people')
  const associations = useResource<Association[]>(condoPath(condominiumId) + '/associations')
  const [tab, setTab] = useState('Piani rateali')
  const [periodFilter, setPeriodFilter] = useState('')
  const [debtorFilter, setDebtorFilter] = useState('')
  const [input, setInput] = useState<PlanInput>()
  const [preview, setPreview] = useState<Preview>()
  const [selectedPlan, setSelectedPlan] = useState<Plan>()
  const [receiptId, setReceiptId] = useState('')
  const [receiptEditorKey, setReceiptEditorKey] = useState<string>()
  const [assignments, setAssignments] = useState<Record<string, string>>({})
  const [planApprovalKey, setPlanApprovalKey] = useState(() => crypto.randomUUID())
  const [allocationKey, setAllocationKey] = useState(() => crypto.randomUUID())
  const [date, setDate] = useState(today())
  const [postingPeriod, setPostingPeriod] = useState('')
  const [bankMovement, setBankMovement] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [reverseJournal, setReverseJournal] = useState<string>()
  const [reverseKey, setReverseKey] = useState(() => crypto.randomUUID())
  const canWrite = useCanWrite()
  const receipt = receipts.data?.find(r => r.id === receiptId)
  const name = (id?: string) => people.data?.find(p => p.id === id)?.displayName ?? id ?? 'Non identificato'
  const unit = (id: string) => structure.data?.units.find(u => u.id === id)?.code ?? id
  function refresh() { plans.reload(); credits.reload(); receipts.reload(); balances.reload(); movements.reload(); banks.reload() }
  async function action(run: () => Promise<void>) { setBusy(true); setError(''); try { await run() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) } }
  function changeInput(value: PlanInput) { setInput(value); setPreview(undefined) }
  function startPlan(previous?: Plan) { setInput(previous ? { ...previous.input, previousPlanId: previous.id, requestId: crypto.randomUUID() } : emptyInput()); setPreview(undefined); setSelectedPlan(undefined); setError('') }
  async function simulate(e: FormEvent) { e.preventDefault(); if (input) await action(async () => { setPreview(await api<Preview>(base + '/plans/preview', 'POST', input)) }) }
  const receiptFields: Field[] = [
    { key: 'accountingPeriodId', label: 'Esercizio incasso', type: 'select', required: true, options: periods.data?.filter(p => p.status === 'Open').map(p => ({ value: p.id, label: p.name })) },
    { key: 'financialAccountId', label: 'Conto finanziario incasso', type: 'select', required: true, options: banks.data?.filter(b => b.isActive).map(b => ({ value: b.id, label: b.name })) },
    { key: 'paymentDate', label: 'Data incasso', type: 'date', required: true, defaultValue: today() }, { key: 'amount', label: 'Importo incasso', type: 'number', min: '0.01', step: '0.01', required: true },
    { key: 'debtorId', label: 'Debitore identificato', type: 'select', options: people.data?.filter(p => associations.data?.some(a => a.personId === p.id) || credits.data?.some(c => c.debtorId === p.id)).map(p => ({ value: p.id, label: p.displayName })) },
    { key: 'payerId', label: 'Soggetto pagante', type: 'select', options: people.data?.map(p => ({ value: p.id, label: p.displayName })) },
    { key: 'reference', label: 'Riferimento incasso' }, { key: 'recordOnly', label: 'Da riconciliare con movimento esistente', type: 'checkbox', defaultValue: false, help: 'Se selezionato non viene creato un movimento bancario. Riconciliare prima di attribuire.' },
  ]
  const rateRows = credits.data?.filter(c => (!periodFilter || c.accountingPeriodId === periodFilter) && (!debtorFilter || c.debtorId === debtorFilter)) ?? []
  return <section><h2>Rate e incassi</h2><p>Crediti emessi, pagamenti ricevuti e somme attribuite sono distinti. I debitori delle rate emesse rimangono quelli originari.</p>
    <div className="tabs">{['Piani rateali', 'Rate e crediti', 'Incassi', 'Riconciliazione', 'Saldi'].map(t => <button key={t} className={t === tab ? 'active' : ''} onClick={() => { setTab(t); setError('') }}>{t}</button>)}</div>
    {error && <p role="alert" className="alert">{error}</p>}
    <Status resource={people}><Status resource={periods}><Status resource={structure}><Status resource={associations}><Status resource={budgets}><Status resource={banks}>
    {tab === 'Piani rateali' && <Status resource={plans}><WriteButton onClick={() => startPlan()} disabled={busy}>Nuovo piano rateale</WriteButton>
      {input && <div className="card editor"><h3>Configura piano</h3><form onSubmit={simulate}><fieldset disabled={busy}><div className="form-grid"><label>Preventivo approvato<select required value={input.budgetId} onChange={e => changeInput({ ...input, budgetId: e.target.value })}><option value="">Seleziona…</option>{budgets.data?.filter(b => b.status === 'Approved').map(b => <option key={b.id} value={b.id}>{b.description} · v{b.version}</option>)}</select></label>
        <label>Data designazione debitori<input required type="date" value={input.debtorDate} onChange={e => changeInput({ ...input, debtorDate: e.target.value })} /></label>
        <label>Modalità rate<select value={input.mode} onChange={e => changeInput({ ...input, mode: e.target.value })}><option value="Equal">Rate uguali</option><option value="Percentages">Percentuali</option><option value="Amounts">Importi per scadenza</option></select></label>
      </div><h4>Scadenze</h4>{input.schedule.map((s, i) => <div className="form-grid" key={i}><label>Scadenza {i + 1}<input type="date" required value={s.dueDate} onChange={e => changeInput({ ...input, schedule: input.schedule.map((r, n) => n === i ? { ...r, dueDate: e.target.value } : r) })} /></label>{input.mode !== 'Equal' && <label>{input.mode === 'Percentages' ? 'Percentuale' : 'Importo'} {i + 1}<input type="number" required min={input.mode === 'Amounts' ? '0.01' : '0.000001'} step={input.mode === 'Amounts' ? '0.01' : '0.000001'} value={s.weight} onChange={e => changeInput({ ...input, schedule: input.schedule.map((r, n) => n === i ? { ...r, weight: e.target.value } : r) })} /></label>}<button type="button" disabled={input.schedule.length === 1} onClick={() => changeInput({ ...input, schedule: input.schedule.filter((_, n) => n !== i) })}>Rimuovi scadenza {i + 1}</button></div>)}<button type="button" disabled={input.schedule.length >= 120} onClick={() => changeInput({ ...input, schedule: [...input.schedule, { dueDate: '', weight: '1' }] })}>Aggiungi scadenza</button>
      <h4>Debitori espliciti</h4><p>Selezionare le associazioni valide alla data indicata e quote con somma 1 per unità. Gli addebiti già personali conservano il soggetto dello snapshot.</p>
      <DataTable headings={['Seleziona', 'Unità', 'Soggetto', 'Ruolo', 'Quota']}>
        {associations.data?.filter(a => a.validFrom <= input.debtorDate && (!a.validTo || a.validTo >= input.debtorDate)).map(a => { const selected = input.debtors.find(d => d.propertyUnitId === a.propertyUnitId && d.personId === a.personId); return <tr key={a.id}><td><input aria-label={'Designa ' + a.unitCode + ' ' + a.personName} type="checkbox" checked={!!selected} onChange={e => changeInput({ ...input, debtors: e.target.checked ? [...input.debtors, { propertyUnitId: a.propertyUnitId, personId: a.personId, share: a.fraction ?? '1' }] : input.debtors.filter(d => d !== selected) })} /></td><td>{a.unitCode}</td><td>{a.personName}</td><td>{a.role}</td><td>{selected && <input aria-label={'Quota debitore ' + a.unitCode + ' ' + a.personName} type="number" min="0.000001" max="1" step="0.000001" value={selected.share} onChange={e => changeInput({ ...input, debtors: input.debtors.map(d => d === selected ? { ...d, share: e.target.value } : d) })} />}</td></tr> })}
      </DataTable><button className="primary">Simula piano rateale</button></fieldset></form>{preview && <><p>Totale {euro(preview.total)}. Rate quadrate per ciascun debitore.</p><RatePreview preview={preview} unit={unit} name={name} /><WriteButton disabled={busy} onClick={() => action(async () => { const plan = await api<Plan>(base + '/plans', 'POST', input); setSelectedPlan(plan); setPlanApprovalKey(crypto.randomUUID()); setInput(undefined); setPreview(undefined); refresh() })}>Salva piano in bozza</WriteButton></>}</div>}
      {!plans.data?.length ? <Empty>Nessun piano rateale.</Empty> : <DataTable headings={['Versione', 'Stato', 'Totale', 'Azioni']}>{plans.data.map(p => <tr key={p.id}><td>{p.version}</td><td>{statusLabel[p.status]}</td><td>{euro(p.total)}</td><td><button onClick={() => { setSelectedPlan(p); setDate(today()); setPlanApprovalKey(crypto.randomUUID()) }}>Dettaglio piano</button> <WriteButton onClick={() => startPlan(p)}>Nuova versione piano</WriteButton></td></tr>)}</DataTable>}
      {selectedPlan && <div className="card"><h3>Piano v{selectedPlan.version} · {statusLabel[selectedPlan.status]}</h3><RatePreview preview={selectedPlan.snapshot} unit={unit} name={name} />
        <details><summary>Differenze dal piano precedente</summary><DataTable headings={['Unità', 'Debitore', 'Precedente', 'Nuovo', 'Differenza']}>{selectedPlan.snapshot.differences.map(d => <tr key={d.propertyUnitId + d.debtorId}><td>{unit(d.propertyUnitId)}</td><td>{name(d.debtorId)}</td><td>{euro(d.previousAmount)}</td><td>{euro(d.newAmount)}</td><td>{euro(d.difference)}</td></tr>)}</DataTable></details>
        {isAdmin && selectedPlan.status === 'Draft' && <><label>Data emissione piano<input type="date" value={date} onChange={e => setDate(e.target.value)} /></label><WriteButton disabled={busy || !date} onClick={() => { if (window.confirm('Emettere il piano? Saranno rilevati crediti contabili e le rate diventeranno immutabili.')) void action(async () => { setSelectedPlan(await api<Plan>(base + '/plans/' + selectedPlan.id + '/approve', 'POST', { requestId: planApprovalKey, date, previewHash: selectedPlan.snapshot.hash })); refresh() }) }}>Approva ed emetti crediti</WriteButton></>}
      </div>}
    </Status>}
    {tab === 'Rate e crediti' && <Status resource={credits}><div className="form-grid"><label>Filtra esercizio<select value={periodFilter} onChange={e => setPeriodFilter(e.target.value)}><option value="">Tutti</option>{periods.data?.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label><label>Filtra debitore<select value={debtorFilter} onChange={e => setDebtorFilter(e.target.value)}><option value="">Tutti</option>{people.data?.map(p => <option key={p.id} value={p.id}>{p.displayName}</option>)}</select></label></div><DataTable headings={['Unità', 'Debitore originario', 'Scadenza', 'Emesso', 'Attribuito', 'Residuo', 'Stato']}>{rateRows.map(c => <tr key={c.id}><td>{c.unitCode}</td><td>{c.debtorName}</td><td>{c.dueDate}</td><td>{euro(c.amount)}</td><td>{euro(c.paid)}</td><td>{euro(c.residual)}</td><td>{statusLabel[c.status]}</td></tr>)}</DataTable>{!rateRows.length && <Empty>Nessun credito emesso.</Empty>}</Status>}
    {(tab === 'Incassi' || tab === 'Riconciliazione') && <Status resource={receipts}><Status resource={movements}><WriteButton disabled={busy} onClick={() => setReceiptEditorKey(crypto.randomUUID())}>Nuovo incasso</WriteButton>
      {receiptEditorKey && <Editor key={receiptEditorKey} title="Registra incasso" fields={receiptFields} onCancel={() => setReceiptEditorKey(undefined)} onSave={async values => { const r = await api<Receipt>(base + '/receipts', 'POST', { ...values, debtorId: values.debtorId || null, payerId: values.payerId || null, reference: values.reference || null, requestId: receiptEditorKey }); setReceiptId(r.id); setPostingPeriod(r.accountingPeriodId); setDate(r.paymentDate); setAssignments({}); setAllocationKey(crypto.randomUUID()); setReceiptEditorKey(undefined); refresh() }} />}
      <DataTable headings={['Data', 'Pagante', 'Debitore', 'Importo', 'Attribuito', 'Non attribuito', 'Riconciliazione', 'Azioni']}>{receipts.data?.map(r => <tr key={r.id}><td>{r.paymentDate}</td><td>{name(r.payerId)}</td><td>{name(r.debtorId)}</td><td>{euro(r.amount)}</td><td>{euro(r.allocated)}</td><td>{euro(r.unallocated)}</td><td>{statusLabel[r.status]}</td><td><button onClick={() => { setReceiptId(r.id); setAssignments({}); setAllocationKey(crypto.randomUUID()); setPostingPeriod(r.accountingPeriodId); setDate(r.paymentDate); setBankMovement(r.bankJournalId ?? '') }}>Dettaglio incasso</button></td></tr>)}</DataTable>
      {receipt && <div className="card"><h3>Incasso {euro(receipt.amount)} · {statusLabel[receipt.status]}</h3><p>Pagante: {name(receipt.payerId)} · debitore: {name(receipt.debtorId)} · non attribuito {euro(receipt.unallocated)}</p>
        {receipt.status !== 'Reconciled' && <><label>Movimento bancario<select value={bankMovement} onChange={e => setBankMovement(e.target.value)}><option value="">Seleziona…</option>{receipt.bankJournalId && <option value={receipt.bankJournalId}>Movimento già associato</option>}{movements.data?.map(m => <option key={m.id} value={m.id}>{m.date} · {m.description} · {euro(decimalString(m.lines.reduce((sum, l) => sum + decimalUnits(l.debit), 0n)))}</option>)}</select></label><WriteButton disabled={busy || !bankMovement} onClick={() => action(async () => { await api(base + '/receipts/' + receipt.id + '/match', 'POST', { journalEntryId: bankMovement }); refresh() })}>Associa movimento</WriteButton><WriteButton disabled={busy || !bankMovement} onClick={() => { if (window.confirm('Confermare la riconciliazione esatta con questo movimento?')) void action(async () => { await api(base + '/receipts/' + receipt.id + '/reconcile', 'POST', { journalEntryId: bankMovement }); refresh() }) }}>Conferma riconciliazione</WriteButton></>}
        {receipt.status === 'Reconciled' && canWrite && <><h4>Attribuisci alle rate</h4><div className="form-grid"><label>Esercizio attribuzione<select value={postingPeriod} onChange={e => setPostingPeriod(e.target.value)}><option value="">Seleziona…</option>{periods.data?.filter(p => p.status === 'Open').map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label><label>Data attribuzione<input type="date" value={date} onChange={e => setDate(e.target.value)} /></label></div>
          <DataTable headings={['Unità', 'Debitore', 'Scadenza', 'Residuo', 'Importo da attribuire']}>{credits.data?.filter(c => decimalUnits(c.residual) > 0n && (!receipt.debtorId || receipt.debtorId === c.debtorId)).map(c => <tr key={c.id}><td>{c.unitCode}</td><td>{c.debtorName}</td><td>{c.dueDate}</td><td>{euro(c.residual)}</td><td><input aria-label={'Attribuzione ' + c.unitCode + ' ' + c.dueDate} type="number" min="0" step="0.01" value={assignments[c.id] ?? ''} onChange={e => setAssignments({ ...assignments, [c.id]: e.target.value })} /></td></tr>)}</DataTable><WriteButton disabled={busy || !postingPeriod || !date} onClick={() => { if (window.confirm('Attribuire gli importi selezionati? La correzione richiederà uno storno tracciato.')) void action(async () => { await api(base + '/receipts/' + receipt.id + '/allocate', 'POST', { requestId: allocationKey, accountingPeriodId: postingPeriod, date, allocations: Object.entries(assignments).filter(([, amount]) => amount && decimalUnits(amount) > 0n).map(([installmentId, amount]) => ({ installmentId, amount })) }); setAssignments({}); setAllocationKey(crypto.randomUUID()); refresh() }) }}>Conferma attribuzione</WriteButton></>}
        <h4>Storico attribuzioni</h4><DataTable headings={['Rata', 'Importo', 'Operazione', 'Azioni']}>{receipt.allocations.map(a => <tr key={a.id}><td>{credits.data?.find(c => c.id === a.installmentId)?.dueDate ?? a.installmentId}</td><td>{euro(a.amount)}</td><td>{a.reversesAllocationId ? 'Storno' : a.isReversed ? 'Stornata' : 'Attribuita'}</td><td>{isAdmin && !a.isReversed && !a.reversesAllocationId && <WriteButton onClick={() => { setReverseJournal(a.journalEntryId); setReverseKey(crypto.randomUUID()) }}>Storna attribuzione</WriteButton>}</td></tr>)}</DataTable>
        {reverseJournal && <Editor key={reverseJournal} title="Rettifica attribuzione (intero gruppo)" fields={[{ key: 'accountingPeriodId', label: 'Esercizio rettifica', type: 'select', required: true, options: periods.data?.filter(p => p.status === 'Open').map(p => ({ value: p.id, label: p.name })) }, { key: 'date', label: 'Data rettifica', type: 'date', required: true, defaultValue: today() }, { key: 'reason', label: 'Motivo rettifica', required: true }]} onCancel={() => setReverseJournal(undefined)} onSave={async values => { if (!window.confirm('Stornare tutto il gruppo di attribuzioni conservando lo storico?')) return; await api(base + '/allocations/' + reverseJournal + '/reverse', 'POST', { ...values, requestId: reverseKey }); setReverseJournal(undefined); refresh() }} />}
      </div>}
      {tab === 'Riconciliazione' && <><h3>Movimenti disponibili</h3><p>Solo incassi generici M04 non stornati e non già associati. Collegamento esatto uno a uno; nessun movimento bancario duplicato.</p><DataTable headings={['Data', 'Causale', 'Riferimento', 'Importo']}>{movements.data?.map(m => <tr key={m.id}><td>{m.date}</td><td>{m.description}</td><td>{m.reference}</td><td>{euro(decimalString(m.lines.reduce((sum, l) => sum + decimalUnits(l.debit), 0n)))}</td></tr>)}</DataTable></>}
    </Status></Status>}
    {tab === 'Saldi' && <Status resource={balances}>{balances.data && <><p>Emesso {euro(balances.data.issued)} · attribuito {euro(balances.data.allocated)} · residuo {euro(balances.data.residual)}</p><p>Incassi riconciliati non attribuiti {euro(balances.data.unattributed)} · da riconciliare {euro(balances.data.unmatched)}</p><DataTable headings={['Unità', 'Debitore', 'Emesso', 'Attribuito', 'Iniziale importato', 'Conguagli contabilizzati', 'Saldo finanziario', 'Scaduto rate', 'Non scaduto rate']}>{balances.data.positions.map(p => <tr key={p.propertyUnitId + p.debtorId}><td>{p.unitCode}</td><td>{p.debtorName}</td><td>{euro(p.issued)}</td><td>{euro(p.allocated)}</td><td>{euro(p.opening ?? "0")}</td><td>{euro(p.definitiveAdjustment ?? "0")}</td><td>{euro(p.residual)}</td><td>{euro(p.overdue)}</td><td>{euro(p.notDue)}</td></tr>)}</DataTable><DebtorSummary positions={balances.data.positions} receipts={receipts.data ?? []} /><h3>Riepilogo unità</h3><DataTable headings={['Unità', 'Emesso', 'Attribuito', 'Residuo']}>{Array.from(new Set(balances.data.positions.map(p => p.propertyUnitId))).map(id => { const rows = balances.data!.positions.filter(p => p.propertyUnitId === id); const sum = (key: 'issued' | 'allocated' | 'residual') => euro(decimalString(rows.reduce((s, p) => s + decimalUnits(p[key]), 0n))); return <tr key={id}><td>{rows[0].unitCode}</td><td>{sum('issued')}</td><td>{sum('allocated')}</td><td>{sum('residual')}</td></tr> })}</DataTable></>}</Status>}
    </Status></Status></Status></Status></Status></Status>
  </section>
}
function RatePreview({ preview, unit, name }: { preview: Preview; unit: (id: string) => string; name: (id: string) => string }) {
  return <DataTable headings={['Unità', 'Debitore', 'Scadenza', 'Quota']}>
    {preview.installments.map((r, i) => <tr key={i}><td>{unit(r.propertyUnitId)}</td><td>{name(r.debtorId)}</td><td>{r.dueDate}</td><td>{euro(r.amount)}</td></tr>)}
  </DataTable>
}

function DebtorSummary({ positions, receipts }: { positions: Position[]; receipts: Receipt[] }) {
  const ids = Array.from(new Set([...positions.map(p => p.debtorId), ...receipts.filter(r => r.debtorId).map(r => r.debtorId!)]))
  return <><h3>Riepilogo debitori</h3><DataTable headings={['Debitore', 'Emesso', 'Attribuito', 'Residuo', 'Scaduto', 'Eccedenze non attribuite']}>{ids.map(id => {
    const rows = positions.filter(p => p.debtorId === id)
    const sum = (field: 'issued' | 'allocated' | 'residual' | 'overdue') => euro(decimalString(rows.reduce((v, p) => v + decimalUnits(p[field]), 0n)))
    const excess = receipts.filter(r => r.debtorId === id && r.status === 'Reconciled').reduce((v, r) => v + decimalUnits(r.unallocated), 0n)
    return <tr key={id}><td>{rows[0]?.debtorName ?? id}</td><td>{sum('issued')}</td><td>{sum('allocated')}</td><td>{sum('residual')}</td><td>{sum('overdue')}</td><td>{euro(decimalString(excess))}</td></tr>
  })}</DataTable></>
}