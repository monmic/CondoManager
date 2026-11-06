import { useEffect, useState } from 'react'
import { api, useResource } from './api'
import type { Condominium, CurrentUser } from './api'
import { Badge, Empty, Status } from './components'
import { CondominiumsPage, PeoplePage } from './RegistryPages'
import { AllocationPage, PeriodsPage } from './AccountingPages'
import { AccountPage, AcceptExistingInvitation, AuthPage } from './AuthPages'
import type { SecurityLink } from './AuthPages'
import { DevelopmentMessages, PlatformUsersPage, StudioUsersPage } from './StudioUsersPage'
import { WritePermission } from './permissions'
import { errorMessage } from './form'
import './App.css'

// Tokens arrive only in URL fragments: remove them before any API call/referrer/navigation.
function readSecurityLink(): SecurityLink | null {
  const fragment = new URLSearchParams(window.location.hash.slice(1))
  const found: SecurityLink | null = fragment.has('reset') ? { kind: 'reset', token: fragment.get('reset')! } :
    fragment.has('invite') ? { kind: 'invite', token: fragment.get('invite')! } : null
  if (found) window.history.replaceState(null, '', window.location.pathname + window.location.search)
  return found
}
const initialLink = readSecurityLink()
function App() {
  const session = useResource<CurrentUser>('/auth/me')
  const reloadSession = session.reload
  const [link, setLink] = useState(initialLink)
  const [notice, setNotice] = useState('')
  useEffect(() => {
    const changed = () => { const candidate = readSecurityLink(); if (candidate) setLink(candidate) }
    window.addEventListener('hashchange', changed)
    return () => window.removeEventListener('hashchange', changed)
  }, [])
  useEffect(() => {
    const expired = () => { setNotice('Sessione terminata o scaduta. Accedi di nuovo.'); reloadSession() }
    const permissions = () => { setNotice('I permessi potrebbero essere cambiati.'); reloadSession() }
    window.addEventListener('cm-session-expired', expired)
    window.addEventListener('cm-permission-changed', permissions)
    return () => { window.removeEventListener('cm-session-expired', expired); window.removeEventListener('cm-permission-changed', permissions) }
  }, [reloadSession])
  if (session.loading) return <p className="auth-shell" role="status">Verifica della sessione…</p>
  if (session.status === 401) return <AuthPage link={link} notice={notice} onClearLink={() => setLink(null)} onAuthenticated={() => { setNotice(''); session.reload() }} />
  if (session.error) return <div className="auth-shell"><Status resource={session}><Empty /></Status></div>
  if (!session.data) return null
  if (link?.kind === 'reset') return <AuthPage link={link} notice="" onClearLink={() => { setLink(null); session.reload() }} onAuthenticated={session.reload} />
  return <Workspace key={session.data.id + (session.data.activeTenantId ?? '') + session.data.role} me={session.data}
    link={link} onClearLink={() => { setLink(null); session.reload() }} notice={notice} refreshSession={session.reload} />
}
function Workspace({ me, link, onClearLink, notice, refreshSession }: { me: CurrentUser; link: SecurityLink | null; onClearLink: () => void; notice: string; refreshSession: () => void }) {
  const [active, setActive] = useState('Dashboard')
  const [selected, setSelected] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const condos = useResource<Condominium[]>(me.activeTenantId ? '/condominiums' : null)
  const current = condos.data?.find(x => x.id === selected)
  const sections = ['Dashboard', ...(me.activeTenantId ? ['Condomini', 'Anagrafiche', 'Tabelle millesimali', 'Esercizi contabili'] : []),
    ...(me.role === 'StudioAdmin' ? ['Utenti studio'] : []), ...(me.isPlatformAdmin ? ['Amministrazione'] : []), 'Account']
  return <WritePermission.Provider value={me.canWrite}><div className="shell"><aside>
    <a className="brand" href="#dashboard" onClick={() => setActive('Dashboard')}>CM <span>CondoManager</span></a><p className="nav-label">IL TUO SPAZIO</p>
    <nav aria-label="Navigazione principale">{sections.map(section => <button key={section} aria-current={active === section ? 'page' : undefined} onClick={() => setActive(section)}>{section}</button>)}</nav>
    <p className="milestone">Identity · Milestone 03</p>
  </aside><main><header><div><strong>{me.email}</strong><small>{me.activeTenantName ?? 'Nessuno studio selezionato'}</small></div><div className="actions">
    <Badge active={Boolean(me.role)}>{me.role ?? (me.isPlatformAdmin ? 'PlatformAdmin' : 'Account')}</Badge><button disabled={busy} onClick={async () => {
      setBusy(true); setError('')
      try { await api('/auth/logout', 'POST'); refreshSession() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
    }}>Esci</button></div></header><section aria-labelledby="page-title">
    {!!me.memberships.length && <label className="tenant-selector">Studio attivo<select aria-label="Studio attivo" value={me.activeTenantId ?? ''} disabled={busy} onChange={async event => {
      if (!event.target.value) return
      setBusy(true); setError('')
      try { await api('/auth/tenant', 'POST', { tenantId: event.target.value }); refreshSession() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
    }}><option value="">Seleziona uno studio…</option>{me.memberships.map(member => <option key={member.tenantId} value={member.tenantId}>{member.name} · {member.role}</option>)}</select></label>}
    <p className="eyebrow">GESTIONE CONDOMINIALE</p><h1 id="page-title">{active}</h1>
    {notice && <p role="status">{notice}</p>}{error && <p className="alert" role="alert">{error}</p>}
    {link?.kind === 'invite' && <AcceptExistingInvitation link={link} me={me} onDone={onClearLink} />}
    {active === 'Account' ? <><AccountPage me={me} onSignedOut={refreshSession} />{me.isDevelopment && <DevelopmentMessages />}</> :
      active === 'Amministrazione' && me.isPlatformAdmin ? <PlatformUsersPage /> :
      active === 'Utenti studio' && me.role === 'StudioAdmin' ? <StudioUsersPage me={me} /> :
      !me.activeTenantId ? <Empty>{me.memberships.length ? 'Seleziona lo studio su cui lavorare.' : 'Non hai membership attive. Richiedi un invito a uno StudioAdmin.'}</Empty> :
      <><p className="muted">{me.role === 'Viewer' ? 'Accesso in sola consultazione.' : 'Dati dello studio selezionato.'}</p><Status resource={condos}>{condos.data && <>
        {active === 'Dashboard' && <><div className="card"><h2>Il lavoro di {me.activeTenantName}</h2><div className="metrics"><strong>{condos.data.filter(x => x.isActive).length} condomini attivi</strong><span>{condos.data.length} registrati</span></div><button className="primary" onClick={() => setActive('Condomini')}>Gestisci condomini</button></div>
          {!condos.data.length && <Empty>Lo studio e vuoto.</Empty>}</>}
        {active === 'Condomini' && <CondominiumsPage rows={condos.data} selected={selected} onSelect={setSelected} refresh={condos.reload} />}
        {active === 'Anagrafiche' && <PeoplePage />}
        {['Tabelle millesimali', 'Esercizi contabili'].includes(active) && <>
          <label className="condo-selector">Condominio<select value={selected} onChange={e => setSelected(e.target.value)}><option value="">Seleziona un condominio…</option>{condos.data.map(c => <option key={c.id} value={c.id}>{c.code} · {c.name}{c.isActive ? '' : ' (inattivo)'}</option>)}</select></label>
          {!current ? <Empty>Seleziona un condominio per continuare.</Empty> : !current.isActive ? <Empty>Riattiva il condominio dalla sua anagrafica per gestire i dati collegati.</Empty> :
            active === 'Tabelle millesimali' ? <AllocationPage key={current.id} condominiumId={current.id} /> : <PeriodsPage key={current.id} condominiumId={current.id} />}
        </>}
      </>}</Status></>}
  </section></main></div></WritePermission.Provider>
}
export default App