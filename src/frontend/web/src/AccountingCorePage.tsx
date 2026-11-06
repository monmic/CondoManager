import { useRef, useState } from 'react'
import { JournalEditor } from './JournalEditor'
import { api, condoPath, today, useResource } from './api'
import type { Period } from './api'
import { Badge, DataTable, Editor, Empty, Status, WriteButton } from './components'
import type { Field, Values } from './components'
import { errorMessage, optional, text } from './form'
import { euro } from './ledgerTypes'
import type { Expense, FinancialAccount, Journal, LedgerAccount, LedgerSummary, Supplier } from './ledgerTypes'

type Editing = { kind: string; row?: LedgerAccount | Supplier | FinancialAccount | Expense | Journal; requestId: string }
const amount = (key: string, label: string): Field => ({ key, label, type: 'number', step: '0.01', min: '0', required: true, defaultValue: '0' })
const required = (key: string, label: string): Field => ({ key, label, required: true })
const active: Field = { key: 'isActive', label: 'Attivo', type: 'checkbox' }
const accountTypes = ['Asset', 'Liability', 'Equity', 'Income', 'Expense']
const labels: Record<string, string> = { Asset: 'Attivita', Liability: 'Passivita', Equity: 'Patrimonio', Income: 'Entrate', Expense: 'Spese', Draft: 'Bozza', Registered: 'Registrato', PartiallyPaid: 'Parzialmente pagato', Paid: 'Pagato', Cancelled: 'Annullato' }
export function AccountingCorePage({ condominiumId, isAdmin }: { condominiumId: string; isAdmin: boolean }) {
  const base = '/accounting/condominiums/' + condominiumId
  const [tab, setTab] = useState('Riepilogo')
  const [periodId, setPeriodId] = useState('')
  const [editing, setEditing] = useState<Editing | null>(null)
  const [detail, setDetail] = useState('')
  const [supplierSearch, setSupplierSearch] = useState('')
  const [expenseSearch, setExpenseSearch] = useState('')
  const [expenseStatus, setExpenseStatus] = useState('')
  const [journalAccount, setJournalAccount] = useState('')
  const [journalSearch, setJournalSearch] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const keys = useRef(new Map<string, string>())
  const accounts = useResource<LedgerAccount[]>(base + '/accounts')
  const suppliers = useResource<Supplier[]>('/accounting/suppliers')
  const periods = useResource<Period[]>(condoPath(condominiumId) + '/accounting-periods')
  const financial = useResource<FinancialAccount[]>(base + '/financial-accounts')
  const periodQuery = periodId ? '?period=' + periodId : ''
  const expenses = useResource<Expense[]>(base + '/expenses' + periodQuery)
  const summary = useResource<LedgerSummary>(base + '/summary' + periodQuery)
  const journalQuery = new URLSearchParams()
  if (periodId) journalQuery.set('period', periodId)
  if (journalAccount) journalQuery.set('account', journalAccount)
  if (journalSearch) journalQuery.set('search', journalSearch)
  if (from) journalQuery.set('from', from)
  if (to) journalQuery.set('to', to)
  const journals = useResource<Journal[]>(base + '/journals?' + journalQuery)
  function reload() { accounts.reload(); suppliers.reload(); financial.reload(); expenses.reload(); summary.reload(); journals.reload() }
  function edit(kind: string, row?: Editing['row']) { setEditing({ kind, row, requestId: crypto.randomUUID() }); setError('') }
  async function action(key: string, path: string, confirmation: string, method = 'POST') {
    if (!window.confirm(confirmation)) return
    setBusy(true); setError('')
    if (!keys.current.has(key)) keys.current.set(key, crypto.randomUUID())
    try { await api(base + path, method, method === 'DELETE' ? undefined : { requestId: keys.current.get(key) }); reload() }
    catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
  }
  const openPeriods = periods.data?.filter(p => p.status === 'Open') ?? []
  const choices = (rows: { id: string; name: string }[]) => rows.map(x => ({ value: x.id, label: x.name }))
  const selectPeriod: Field = { key: 'accountingPeriodId', label: 'Esercizio aperto', type: 'select', required: true, options: choices(openPeriods), defaultValue: openPeriods.find(p => p.id === periodId)?.id ?? openPeriods[0]?.id }
  const date: Field = { key: 'date', label: 'Data operazione', type: 'date', required: true, defaultValue: today() }
  const selectFinancial: Field = { key: 'financialAccountId', label: 'Conto finanziario', type: 'select', required: true, options: choices((financial.data ?? []).filter(x => x.isActive)) }
  const row = editing?.row
  const fields: Field[] = editing?.kind === 'account' ? [required('code', 'Codice conto'), required('name', 'Nome conto'), { key: 'type', label: 'Tipologia conto', type: 'select', required: true, options: accountTypes.map(x => ({ value: x, label: labels[x] })), defaultValue: 'Expense' }, { key: 'description', label: 'Descrizione' }, { key: 'parentAccountId', label: 'Conto padre', type: 'select', options: choices((accounts.data ?? []).filter(x => x.id !== row?.id && x.isActive).map(x => ({ id: x.id, name: x.code + ' · ' + x.name }))) }, active] :
    editing?.kind === 'supplier' ? [required('name', 'Denominazione fornitore'), { key: 'taxCode', label: 'Codice fiscale' }, { key: 'vatNumber', label: 'Partita IVA' }, { key: 'email', label: 'Email', type: 'email', maxLength: 254 }, { key: 'phone', label: 'Telefono' }, { key: 'address', label: 'Indirizzo fornitore' }, { key: 'iban', label: 'IBAN' }, { key: 'notes', label: 'Note', type: 'textarea' }, active] :
    editing?.kind === 'expense' ? [selectPeriod, { key: 'supplierId', label: 'Fornitore', type: 'select', required: true, options: choices((suppliers.data ?? []).filter(x => x.isActive)) }, { key: 'accountId', label: 'Categoria di spesa', type: 'select', required: true, options: choices((accounts.data ?? []).filter(x => x.isActive && x.type === 'Expense').map(x => ({ id: x.id, name: x.code + ' · ' + x.name }))) }, required('documentNumber', 'Numero documento'), { key: 'documentDate', label: 'Data documento', type: 'date', required: true, defaultValue: today() }, { key: 'dueDate', label: 'Scadenza', type: 'date' }, required('description', 'Descrizione spesa'), amount('netAmount', 'Imponibile EUR'), amount('vatAmount', 'IVA EUR'), amount('grossAmount', 'Totale EUR'), { key: 'notes', label: 'Note', type: 'textarea' }] :
    editing?.kind === 'financial' ? [...(!row ? [required('code', 'Codice conto contabile'), { key: 'type', label: 'Tipo conto finanziario', type: 'select' as const, required: true, options: ['BankAccount', 'Cash'].map(x => ({ value: x, label: x === 'Cash' ? 'Cassa' : 'Banca' })), defaultValue: 'BankAccount' }, selectPeriod, date, { ...amount('openingBalance', 'Saldo iniziale EUR'), min: undefined, help: 'Immutabile dopo la creazione. Un saldo diverso da zero genera una scrittura contro Saldi di apertura.' }] : [active]), required('name', 'Nome conto finanziario'), { key: 'iban', label: 'IBAN' }] :
    editing?.kind === 'payment' ? [selectPeriod, date, selectFinancial, { ...amount('amount', 'Importo pagamento EUR'), defaultValue: (row as Expense)?.residual }, { key: 'reference', label: 'Riferimento operazione' }] :
    editing?.kind === 'receipt' ? [selectPeriod, date, selectFinancial, amount('amount', 'Importo incasso EUR'), required('description', 'Causale incasso'), { key: 'reference', label: 'Riferimento operazione' }] :
    editing?.kind === 'reverse' ? [selectPeriod, date, required('reason', 'Motivo dello storno')] : []
  async function save(v: Values) {
    if (!editing) return
    const id = row?.id
    let path = '', method = id ? 'PUT' : 'POST', input: unknown
    const value = (k: string) => text(v, k)
    if (editing.kind === 'account') { path = '/accounts' + (id ? '/' + id : ''); input = { code: value('code'), name: value('name'), type: value('type'), description: optional(v, 'description'), parentAccountId: optional(v, 'parentAccountId'), isActive: v.isActive } }
    if (editing.kind === 'supplier') { path = '/accounting/suppliers' + (id ? '/' + id : ''); input = { name: value('name'), taxCode: optional(v, 'taxCode'), vatNumber: optional(v, 'vatNumber'), email: optional(v, 'email'), phone: optional(v, 'phone'), address: optional(v, 'address'), iban: optional(v, 'iban'), notes: optional(v, 'notes'), isActive: v.isActive } }
    if (editing.kind === 'expense') { path = '/expenses' + (id ? '/' + id : ''); input = { accountingPeriodId: value('accountingPeriodId'), supplierId: value('supplierId'), accountId: value('accountId'), documentNumber: value('documentNumber'), documentDate: value('documentDate'), dueDate: optional(v, 'dueDate'), description: value('description'), netAmount: value('netAmount'), vatAmount: value('vatAmount'), grossAmount: value('grossAmount'), notes: optional(v, 'notes') } }
    if (editing.kind === 'financial') { path = '/financial-accounts' + (id ? '/' + id : ''); input = id ? { name: value('name'), iban: optional(v, 'iban'), isActive: v.isActive } : { requestId: editing.requestId, code: value('code'), name: value('name'), type: value('type'), iban: optional(v, 'iban'), openingBalance: value('openingBalance'), accountingPeriodId: value('accountingPeriodId'), date: value('date') } }
    if (editing.kind === 'payment' || editing.kind === 'receipt') { method = 'POST'; path = editing.kind === 'payment' ? '/expenses/' + id + '/payments' : '/receipts'; input = { requestId: editing.requestId, accountingPeriodId: value('accountingPeriodId'), financialAccountId: value('financialAccountId'), date: value('date'), amount: value('amount'), reference: optional(v, 'reference'), ...(editing.kind === 'receipt' ? { description: value('description') } : {}) } }
    if (editing.kind === 'reverse') { method = 'POST'; path = '/journals/' + id + '/reverse'; input = { requestId: editing.requestId, accountingPeriodId: value('accountingPeriodId'), date: value('date'), reason: value('reason') } }
    if (['payment', 'receipt', 'reverse'].includes(editing.kind) || editing.kind === 'financial' && !id) {
      if (!window.confirm('Confermare l’operazione contabile? Le scritture registrate si correggono con uno storno.')) return
    }
    await api(editing.kind === 'supplier' ? path : base + path, method, input)
    setEditing(null); reload()
  }
  const expenseDetail = expenses.data?.find(x => x.id === detail)
  function journalTable(rows: Journal[]) { return !rows.length ? <Empty>Nessun movimento presente.</Empty> : <DataTable headings={['Data', 'Causale / riferimento', 'Tipo', 'Dare', 'Avere', 'Azioni']}>{rows.map(j => <tr key={j.id}><td>{j.date}</td><td><button onClick={() => setDetail(j.id)}>{j.description}</button><small>{j.reference}</small></td><td>{j.kind}{j.reversedByEntryId && ' · Stornato'}</td><td>{euro(sumDecimal(j.lines.map(l => l.debit)))}</td><td>{euro(sumDecimal(j.lines.map(l => l.credit)))}</td><td>{isAdmin && !j.reversedByEntryId && j.kind !== 'Reversal' && <WriteButton onClick={() => edit('reverse', j)}>Storna</WriteButton>}</td></tr>)}</DataTable> }
  const journalDetail = journals.data?.find(x => x.id === detail) ?? summary.data?.recentEntries.find(x => x.id === detail)
  const resources = [accounts, suppliers, periods, financial, expenses, summary]
  const pending = resources.find(r => r.loading || r.error)
  return <div className="ledger"><nav className="ledger-tabs" aria-label="Sezioni contabili">{['Riepilogo', 'Piano dei conti', 'Fornitori', 'Documenti di spesa', 'Movimenti', 'Conti finanziari'].map(t => <button key={t} aria-current={tab === t ? 'page' : undefined} onClick={() => { setTab(t); setDetail(''); setEditing(null) }}>{t}</button>)}</nav>
    <label>Esercizio da consultare<select value={periodId} onChange={e => { setPeriodId(e.target.value); setDetail(''); setEditing(null) }}><option value="">Tutti gli esercizi</option>{periods.data?.map(p => <option key={p.id} value={p.id}>{p.name} · {p.status}</option>)}</select></label>
    {!openPeriods.length && <p className="alert">Nessun esercizio aperto. Aprire un esercizio dalla sezione Esercizi contabili prima di registrare documenti o movimenti.</p>}
    {error && <p className="alert" role="alert">{error}</p>}{pending ? <Status resource={pending}><Empty /></Status> : <>
      {editing?.kind === 'manual' && <JournalEditor key={editing.requestId} base={base} accounts={accounts.data ?? []} periods={openPeriods} isAdmin={isAdmin} onDone={() => { setEditing(null); reload() }} />}
      {editing && editing.kind !== 'manual' && <Editor key={editing.requestId} title={{ account: 'Conto contabile', supplier: 'Fornitore', expense: 'Documento di spesa', financial: 'Conto finanziario', payment: 'Pagamento', receipt: 'Incasso da attribuire', reverse: 'Storno' }[editing.kind] ?? 'Modifica'} fields={fields} initial={['payment', 'reverse'].includes(editing.kind) ? {} : editing.row} onSave={save} onCancel={() => setEditing(null)} />}
      {tab === 'Riepilogo' && summary.data && <><div className="ledger-metrics">{[['Spese registrate nette di storni', summary.data.expenses], ['Pagamenti netti di storni', summary.data.payments], ['Variazione debiti fornitori nel periodo', summary.data.payables], ['Disponibilita attuale banca e cassa', summary.data.availability]].map(([label, value]) => <div className="card" key={label}><small>{label}</small><strong>{euro(value)}</strong></div>)}</div><p>I saldi dei conti sotto riportati sono Dare−Avere per Attivita/Spese, Avere−Dare per le altre tipologie. Con un esercizio selezionato mostrano la variazione del solo esercizio, senza riporto automatico.</p><h2>Saldi per conto</h2><DataTable headings={['Codice', 'Conto', 'Dare', 'Avere', 'Saldo naturale']}>{summary.data.balances.map(b => <tr key={b.accountId}><td>{b.code}</td><td>{b.name}</td><td>{euro(b.debit)}</td><td>{euro(b.credit)}</td><td>{euro(b.balance)}</td></tr>)}</DataTable><h2>Ultimi movimenti</h2>{journalTable(summary.data.recentEntries)}</>}
      {tab === 'Piano dei conti' && <><div className="actions"><WriteButton onClick={() => edit('account')}>Nuovo conto</WriteButton><WriteButton disabled={busy} onClick={() => action('initialize', '/accounts/initialize', 'Aggiungere i conti predefiniti mancanti senza modificare quelli esistenti?')}>Completa piano predefinito</WriteButton></div><p>Le categorie contabili sono distinte dalle tabelle millesimali. Tipo e funzione dei conti esistenti sono immutabili.</p>{!accounts.data?.length ? <Empty /> : <DataTable headings={['Codice', 'Nome / gerarchia', 'Tipo', 'Stato', 'Azioni']}>{hierarchy(accounts.data).map(({ account: a, depth }) => <tr key={a.id}><td>{a.code}</td><td style={{ paddingLeft: 12 + depth * 20 }}>{a.name}{a.purpose !== 'General' && <small>Conto di controllo · {a.purpose}</small>}</td><td>{labels[a.type]}</td><td><Badge active={a.isActive} /></td><td><WriteButton onClick={() => edit('account', a)}>Modifica conto</WriteButton></td></tr>)}</DataTable>}</>}
      {tab === 'Fornitori' && <><WriteButton onClick={() => edit('supplier')}>Nuovo fornitore</WriteButton><label>Ricerca fornitore<input value={supplierSearch} onChange={e => setSupplierSearch(e.target.value)} /></label><p>Fornitori condivisi tra i condomini dello studio attivo. La disattivazione conserva lo storico.</p>{!suppliers.data?.length ? <Empty /> : <DataTable headings={['Denominazione', 'Codice fiscale / IVA', 'Contatti', 'Stato', 'Azioni']}>{suppliers.data.filter(s => s.name.toLocaleLowerCase().includes(supplierSearch.toLocaleLowerCase())).map(s => <tr key={s.id}><td><button onClick={() => setDetail(s.id)}>{s.name}</button></td><td>{s.taxCode}<small>{s.vatNumber}</small></td><td>{s.email}<small>{s.phone}</small></td><td><Badge active={s.isActive} /></td><td><WriteButton onClick={() => edit('supplier', s)}>Modifica fornitore</WriteButton></td></tr>)}</DataTable>}{suppliers.data?.filter(s => s.id === detail).map(s => <div className="card" key={s.id}><h3>{s.name}</h3><p>{s.address}</p><p>IBAN: {s.iban ?? 'Non indicato'}</p><p>{s.notes}</p><h4>Documenti di questo condominio</h4>{expenses.data?.filter(e => e.supplierId === s.id).map(e => <p key={e.id}>{e.documentNumber} · {euro(e.grossAmount)} · {labels[e.status]}</p>)}</div>)}</>}
      {tab === 'Documenti di spesa' && <><WriteButton disabled={!openPeriods.length} onClick={() => edit('expense')}>Nuova spesa</WriteButton><div className="ledger-filters"><label>Cerca documento<input value={expenseSearch} onChange={e => setExpenseSearch(e.target.value)} /></label><label>Stato documento<select value={expenseStatus} onChange={e => setExpenseStatus(e.target.value)}><option value="">Tutti</option>{['Draft', 'Registered', 'PartiallyPaid', 'Paid', 'Cancelled'].map(s => <option key={s} value={s}>{labels[s]}</option>)}</select></label></div>{!expenses.data?.length ? <Empty /> : <DataTable headings={['Numero / data', 'Fornitore', 'Descrizione', 'Totale', 'Residuo', 'Stato', 'Azioni']}>{expenses.data.filter(e => (!expenseStatus || e.status === expenseStatus) && (e.documentNumber + ' ' + e.description).toLocaleLowerCase().includes(expenseSearch.toLocaleLowerCase())).map(e => <tr key={e.id}><td><button onClick={() => setDetail(e.id)}>{e.documentNumber}</button><small>{e.documentDate}</small></td><td>{suppliers.data?.find(s => s.id === e.supplierId)?.name}</td><td>{e.description}</td><td>{euro(e.grossAmount)}</td><td>{euro(e.residual)}</td><td>{labels[e.status]}</td><td>{e.status === 'Draft' ? <><WriteButton onClick={() => edit('expense', e)}>Modifica spesa</WriteButton><WriteButton disabled={busy} onClick={() => action('register:' + e.id, '/expenses/' + e.id + '/register', 'Contabilizzare definitivamente il documento?')}>Registra</WriteButton><WriteButton disabled={busy} onClick={() => action('cancel:' + e.id, '/expenses/' + e.id, 'Annullare la bozza conservandone lo storico?', 'DELETE')}>Annulla bozza</WriteButton></> : ['Registered', 'PartiallyPaid'].includes(e.status) && <WriteButton disabled={!openPeriods.length} onClick={() => edit('payment', e)}>Paga</WriteButton>}</td></tr>)}</DataTable>}{expenseDetail && <div className="card"><h3>Documento {expenseDetail.documentNumber}</h3><p>Imponibile {euro(expenseDetail.netAmount)} · IVA {euro(expenseDetail.vatAmount)} · Totale {euro(expenseDetail.grossAmount)}</p><p>Scadenza: {expenseDetail.dueDate ?? 'Non indicata'} · {expenseDetail.notes}</p><h4>Pagamenti · Residuo {euro(expenseDetail.residual)}</h4>{!expenseDetail.payments.length ? <Empty>Nessun pagamento.</Empty> : <DataTable headings={['Data', 'Conto', 'Importo', 'Riferimento', 'Stato']}>{expenseDetail.payments.map(p => <tr key={p.id}><td>{p.date}</td><td>{financial.data?.find(f => f.id === p.financialAccountId)?.name}</td><td>{euro(p.amount)}</td><td>{p.reference}</td><td>{p.isReversed ? 'Stornato' : 'Contabilizzato'}</td></tr>)}</DataTable>}</div>}</>}
      {tab === 'Conti finanziari' && <><div className="actions"><WriteButton disabled={!openPeriods.length} onClick={() => edit('financial')}>Nuovo conto finanziario</WriteButton><WriteButton disabled={!openPeriods.length} onClick={() => edit('receipt')}>Registra incasso generico</WriteButton></div><p>Gli incassi generici restano da attribuire e non sono rate pagate. I saldi comprendono tutti gli esercizi, incluso il saldo iniziale contabilizzato.</p>{!financial.data?.length ? <Empty /> : <DataTable headings={['Conto', 'Tipo / IBAN', 'Saldo iniziale', 'Saldo attuale', 'Stato', 'Azioni']}>{financial.data.map(f => <tr key={f.id}><td>{f.name}</td><td>{f.type === 'Cash' ? 'Cassa' : 'Banca'}<small>{f.iban}</small></td><td>{euro(f.openingBalance)}</td><td>{euro(f.balance)}</td><td><Badge active={f.isActive} /></td><td><WriteButton onClick={() => edit('financial', f)}>Modifica conto finanziario</WriteButton><button onClick={() => { setJournalAccount(f.accountId); setTab('Movimenti'); setDetail('') }}>Movimenti conto</button></td></tr>)}</DataTable>}</>}
      {tab === 'Movimenti' && <><WriteButton disabled={!openPeriods.length} onClick={() => edit('manual')}>Nuova scrittura manuale</WriteButton><div className="ledger-filters"><label>Conto da consultare<select value={journalAccount} onChange={e => setJournalAccount(e.target.value)}><option value="">Tutti i conti</option>{accounts.data?.map(a => <option key={a.id} value={a.id}>{a.code} · {a.name}</option>)}</select></label><label>Causale da cercare<input value={journalSearch} onChange={e => setJournalSearch(e.target.value)} /></label><label>Dal<input type="date" value={from} onChange={e => setFrom(e.target.value)} /></label><label>Al<input type="date" value={to} onChange={e => setTo(e.target.value)} /></label></div><Status resource={journals}>{journals.data && journalTable(journals.data)}</Status></>}
      {journalDetail && <div className="card"><h3>Scrittura: {journalDetail.description}</h3><p>{journalDetail.date} · {journalDetail.reference} · {journalDetail.kind}</p><small>Identificativo: {journalDetail.id}</small>{journalDetail.reversesEntryId && <p>Storno della scrittura {journalDetail.reversesEntryId}</p>}{journalDetail.reversedByEntryId && <p>Stornata dalla scrittura {journalDetail.reversedByEntryId}</p>}<DataTable headings={['Codice', 'Conto', 'Dare EUR', 'Avere EUR']}>{journalDetail.lines.map((l, i) => <tr key={i}><td>{l.code}</td><td>{l.name}</td><td>{euro(l.debit)}</td><td>{euro(l.credit)}</td></tr>)}</DataTable></div>}
    </>}
  </div>
}
// BigInt is used only for exact cent totals; the server remains authoritative.
function sumDecimal(values: string[]) { const cents = values.reduce((sum, v) => { const [whole, fraction = ''] = v.split('.'); return sum + BigInt(whole) * 100n + BigInt(fraction.padEnd(2, '0')) }, 0n); return (cents / 100n).toString() + '.' + (cents % 100n).toString().padStart(2, '0') }
function hierarchy(accounts: LedgerAccount[]) {
  const result: { account: LedgerAccount; depth: number }[] = [], visited = new Set<string>()
  function walk(parent: string | null, depth: number) { for (const account of accounts.filter(a => a.parentAccountId === parent)) { if (visited.has(account.id)) continue; visited.add(account.id); result.push({ account, depth }); walk(account.id, depth + 1) } }
  walk(null, 0); for (const account of accounts) if (!visited.has(account.id)) result.push({ account, depth: 0 }); return result
}
