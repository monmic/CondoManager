import { useCallback, useEffect, useState } from 'react'

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) { super(message); this.status = status }
}
let csrfToken: string | null = null
let expectedTenant: string | null = null
async function csrf(): Promise<string> {
  if (!csrfToken) {
    const response = await fetch('/api/auth/csrf', { credentials: 'same-origin', cache: 'no-store' })
    if (!response.ok) throw new ApiError('Impossibile verificare la sessione sicura.', response.status)
    const data = await response.json() as { token: string }
    csrfToken = data.token
  }
  return csrfToken
}
export async function api<T>(path: string, method = 'GET', body?: unknown, signal?: AbortSignal): Promise<T> {
  const headers: Record<string, string> = {}
  if (expectedTenant && ['/condominiums', '/people', '/studio'].some(prefix => path.startsWith(prefix))) headers['X-Expected-Tenant'] = expectedTenant
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) headers['X-CSRF-TOKEN'] = await csrf()
  const response = await fetch('/api' + path, {
    method, signal, credentials: 'same-origin', cache: 'no-store', headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; code?: string } | null
    if (response.status === 401 && !['/auth/login', '/auth/me'].includes(path))
      window.dispatchEvent(new Event('cm-session-expired'))
    if (problem?.code === 'tenant_changed') window.dispatchEvent(new Event('cm-permission-changed'))
    if (response.status === 403 && !path.startsWith('/auth/')) window.dispatchEvent(new Event('cm-permission-changed'))
    if (response.status === 400 || response.status === 401) csrfToken = null
    throw new ApiError(problem?.detail ?? problem?.title ?? 'Impossibile completare la richiesta (' + response.status + ').', response.status)
  }
  if (['/auth/login', '/auth/logout', '/auth/change-password', '/auth/reset-password'].includes(path)) csrfToken = null
  if (path === '/auth/logout' || path === '/auth/change-password') expectedTenant = null
  if (response.status === 204 || response.status === 202) return undefined as T
  const data: unknown = await response.json()
  if (path === '/auth/me') expectedTenant = (data as { activeTenantId: string | null }).activeTenantId
  return data as T
}
export function useResource<T>(path: string | null) {
  const [revision, setRevision] = useState(0)
  const key = path + '#' + revision
  const [result, setResult] = useState<{ key: string; data: T | null; error: string; status: number }>({ key: '', data: null, error: '', status: 0 })
  const reload = useCallback(() => setRevision(v => v + 1), [])
  useEffect(() => {
    if (path === null) return
    const controller = new AbortController()
    api<T>(path, 'GET', undefined, controller.signal).then(data => {
      if (!controller.signal.aborted) setResult({ key, data, error: '', status: 0 })
    }).catch((reason: unknown) => {
      if (!controller.signal.aborted) setResult({ key, data: null, error: reason instanceof Error ? reason.message : 'Errore di rete.', status: reason instanceof ApiError ? reason.status : 0 })
    })
    return () => controller.abort()
  }, [path, key])
  return { data: path !== null && result.key === key ? result.data : null,
    loading: path !== null && result.key !== key, error: result.key === key ? result.error : '', status: result.key === key ? result.status : 0, reload }
}
export type Resource<T> = ReturnType<typeof useResource<T>>
export type Condominium = { id: string; code: string; name: string; taxCode: string | null; address: string; postalCode: string; city: string; province: string; country: string; isActive: boolean; createdAt: string; updatedAt: string }
export type Building = { id: string; code: string; name: string; isActive: boolean }
export type Staircase = Building & { buildingId: string | null }
export type Unit = { id: string; code: string; buildingId: string | null; staircaseId: string | null; interior: string | null; floor: string | null; kind: string; cadastralData: string | null; notes: string | null; isActive: boolean }
export type Structure = { buildings: Building[]; staircases: Staircase[]; units: Unit[] }
export type Person = { id: string; code: string; kind: string; displayName: string; firstName: string | null; lastNameOrCompany: string | null; taxCode: string | null; vatNumber: string | null; email: string | null; phone: string | null; mailingAddress: string | null; notes: string | null; isActive: boolean }
export type Association = { id: string; propertyUnitId: string; personId: string; personName: string; unitCode: string; role: string; validFrom: string; validTo: string | null; fraction: string | null }
export type AllocationTable = { id: string; code: string; name: string; denominator: string; validFrom: string; status: 'Draft' | 'Active'; isArchived: boolean; validation: { total: string; denominator: string; missingUnitIds: string[]; unexpectedUnitIds: string[]; hasDuplicates: boolean; isValid: boolean }; entries: { id: string; propertyUnitId: string; value: string }[] }
export type Period = { id: string; code: string; name: string; startsOn: string; endsOn: string; status: 'Draft' | 'Open' | 'Closed'; closedAt: string | null }
export const today = () => new Date().toLocaleDateString('sv-SE')
export const condoPath = (id: string) => '/condominiums/' + id
export type StudioRole = 'StudioAdmin' | 'Collaborator' | 'Viewer'
export type CurrentUser = { id: string; email: string; isPlatformAdmin: boolean; memberships: { tenantId: string; name: string; role: StudioRole }[]; activeTenantId: string | null; activeTenantName: string | null; role: StudioRole | null; canWrite: boolean; isDevelopment: boolean }