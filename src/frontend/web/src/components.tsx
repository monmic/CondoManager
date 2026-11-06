import { useCanWrite } from './permissions'
import { errorMessage } from './form'
import { useId, useState } from 'react'
import type { ButtonHTMLAttributes, FormEvent, ReactNode } from 'react'
import type { Resource } from './api'

export type Values = Record<string, string | boolean>
export type Field = { key: string; label: string; type?: 'text' | 'email' | 'number' | 'date' | 'checkbox' | 'textarea' | 'select'; required?: boolean; options?: { value: string; label: string }[]; maxLength?: number; step?: string; min?: string; defaultValue?: string | boolean; help?: string }
export function Status({ resource, children }: { resource: Pick<Resource<unknown>, 'loading' | 'error' | 'reload'>; children: ReactNode }) {
  if (resource.loading) return <p role="status">Caricamento…</p>
  if (resource.error) return <div className="alert" role="alert">{resource.error} <button onClick={resource.reload}>Riprova</button></div>
  return children
}
export function Empty({ children = 'Nessun dato presente.' }: { children?: ReactNode }) { return <p className="empty-state">{children}</p> }
export function Badge({ active, children }: { active?: boolean; children?: ReactNode }) { return <span className={'badge ' + (active ? 'positive' : '')}>{children ?? (active ? 'Attivo' : 'Inattivo')}</span> }
export function Editor({ title, fields, initial = {}, onSave, onCancel }: { title: string; fields: Field[]; initial?: object; onSave: (values: Values) => Promise<void>; onCancel: () => void }) {
  const canWrite = useCanWrite()
  const formId = useId()
  const [values, setValues] = useState<Values>(() => Object.fromEntries(fields.map(f => {
    const value: unknown = (initial as Record<string, unknown>)[f.key] ?? f.defaultValue ?? (f.type === 'checkbox' ? true : '')
    return [f.key, typeof value === 'boolean' ? value : String(value)]
  })))
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setBusy(true)
    try { await onSave(values) } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
  }
  return <div className="editor card"><h3>{title}</h3><form onSubmit={submit}>
    <fieldset disabled={busy || !canWrite}><div className="form-grid">{fields.map(field => <label key={field.key} htmlFor={formId + field.key} className={field.type === 'textarea' ? 'wide' : ''}>
      {field.label}{field.required ? ' *' : ''}
      {field.type === 'checkbox' ? <input id={formId + field.key} type="checkbox" checked={Boolean(values[field.key])} onChange={e => setValues({ ...values, [field.key]: e.target.checked })} />
      : field.type === 'select' ? <select id={formId + field.key} value={String(values[field.key])} required={field.required} onChange={e => setValues({ ...values, [field.key]: e.target.value })}>
        <option value="">Seleziona…</option>{field.options?.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}</select>
      : field.type === 'textarea' ? <textarea id={formId + field.key} maxLength={field.maxLength ?? 4000} value={String(values[field.key])} onChange={e => setValues({ ...values, [field.key]: e.target.value })} />
      : <input id={formId + field.key} type={field.type ?? 'text'} required={field.required} maxLength={field.maxLength ?? 160} step={field.step} min={field.min} value={String(values[field.key])} onChange={e => setValues({ ...values, [field.key]: e.target.value })} />}
      {field.help && <small>{field.help}</small>}
    </label>)}</div></fieldset>
    {error && <p role="alert" className="alert">{error}</p>}
    <div className="actions"><button className="primary" disabled={busy || !canWrite}>{busy ? 'Salvataggio…' : 'Salva'}</button><button type="button" onClick={onCancel} disabled={busy}>Annulla</button></div>
  </form></div>
}
export function DataTable({ headings, children }: { headings: string[]; children: ReactNode }) {
  return <div className="table-scroll"><table><thead><tr>{headings.map(h => <th key={h}>{h}</th>)}</tr></thead><tbody>{children}</tbody></table></div>
}
export function WriteButton(props: ButtonHTMLAttributes<HTMLButtonElement>) {
  const allowed = useCanWrite()
  return allowed ? <button {...props} /> : null
}