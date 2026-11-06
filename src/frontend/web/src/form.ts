import type { Values } from './components'
export const options = (items: { id: string; code: string }[]) => items.map(x => ({ value: x.id, label: x.code }))
export const text = (v: Values, key: string) => String(v[key] ?? '')
export const optional = (v: Values, key: string) => text(v, key).trim() || null
export const errorMessage = (reason: unknown) => reason instanceof Error ? reason.message : 'Operazione non riuscita.'