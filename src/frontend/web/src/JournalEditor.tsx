import { useState } from 'react'
import { api, today } from './api'
import type { Period } from './api'
import type { LedgerAccount } from './ledgerTypes'
import { euro } from './ledgerTypes'
import { errorMessage } from './form'
import { useCanWrite } from './permissions'
export function JournalEditor({ base, accounts, periods, isAdmin, onDone }: { base: string; accounts: LedgerAccount[]; periods: Period[]; isAdmin: boolean; onDone: () => void }) {
  const canWrite = useCanWrite()
  const [requestId] = useState(() => crypto.randomUUID())
  const [period, setPeriod] = useState(periods[0]?.id ?? '')
  const [date, setDate] = useState(today)
  const [description, setDescription] = useState('')
  const [reference, setReference] = useState('')
  const [lines, setLines] = useState([{ accountId: '', debit: '0', credit: '0' }, { accountId: '', debit: '0', credit: '0' }])
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const selectable = accounts.filter(a => a.isActive && (a.purpose === 'General' || isAdmin && a.purpose === 'OpeningEquity'))
  const total = (key: 'debit' | 'credit') => { try { const sum = lines.reduce((s, l) => { if (!/^\d+(\.\d{0,2})?$/.test(l[key])) throw new Error(); const [whole, fraction = ''] = l[key].split('.'); return s + BigInt(whole) * 100n + BigInt(fraction.padEnd(2, '0')) }, 0n); return euro((sum / 100n).toString() + '.' + (sum % 100n).toString().padStart(2, '0')) } catch { return 'Importo non valido' } }
  return <div className="editor card"><h3>Nuova scrittura manuale</h3><p>I conti di controllo fornitori e incassi da attribuire sono riservati ai flussi dedicati.</p><form onSubmit={async event => {
    event.preventDefault(); if (!window.confirm('Contabilizzare questa scrittura? Una rettifica richiede uno storno.')) return
    setBusy(true); setError('')
    try { await api(base + '/journals', 'POST', { requestId, accountingPeriodId: period, date, description, reference: reference || null, lines }); onDone() }
    catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
  }}><fieldset disabled={busy || !canWrite}><div className="form-grid"><label>Esercizio della scrittura<select required value={period} onChange={e => setPeriod(e.target.value)}><option value="">Seleziona</option>{periods.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label><label>Data scrittura<input required type="date" value={date} onChange={e => setDate(e.target.value)} /></label><label>Causale scrittura<input required maxLength={160} value={description} onChange={e => setDescription(e.target.value)} /></label><label>Riferimento scrittura<input maxLength={160} value={reference} onChange={e => setReference(e.target.value)} /></label></div>
    {lines.map((line, index) => <div className="ledger-filters" key={index}><label>Conto riga {index + 1}<select required value={line.accountId} onChange={e => setLines(lines.map((l, i) => i === index ? { ...l, accountId: e.target.value } : l))}><option value="">Seleziona</option>{selectable.map(a => <option key={a.id} value={a.id}>{a.code} · {a.name}</option>)}</select></label>{(['debit', 'credit'] as const).map(key => <label key={key}>{key === 'debit' ? 'Dare' : 'Avere'} riga {index + 1}<input required type="number" step="0.01" min="0" value={line[key]} onChange={e => setLines(lines.map((l, i) => i === index ? { ...l, [key]: e.target.value } : l))} /></label>)}{lines.length > 2 && <button type="button" onClick={() => setLines(lines.filter((_, i) => i !== index))}>Rimuovi riga {index + 1}</button>}</div>)}
    <button type="button" disabled={lines.length >= 200} onClick={() => setLines([...lines, { accountId: '', debit: '0', credit: '0' }])}>Aggiungi riga</button><p>Totale Dare: {total('debit')} · Totale Avere: {total('credit')}</p></fieldset>
    {error && <p className="alert" role="alert">{error}</p>}<div className="actions"><button disabled={busy || !canWrite}>Contabilizza scrittura</button><button type="button" disabled={busy} onClick={onDone}>Annulla scrittura</button></div>
  </form></div>
}
