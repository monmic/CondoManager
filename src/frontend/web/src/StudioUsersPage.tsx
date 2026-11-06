import { useEffect, useState } from 'react'
import { api, useResource } from './api'
import type { CurrentUser, StudioRole } from './api'
import { Badge, DataTable, Empty, Status } from './components'
import { errorMessage } from './form'

type Member = { id: string; userId: string; email: string; role: StudioRole; isActive: boolean; userIsActive: boolean }
type Invitation = { id: string; email: string; role: StudioRole; expiresAt: string; usedAt: string | null; revokedAt: string | null }
type Audit = { id: string; timestamp: string; event: string; actorUserId: string | null; subjectId: string | null }
const roles: StudioRole[] = ['StudioAdmin', 'Collaborator', 'Viewer']
function MemberRow({ member, reload }: { member: Member; reload: () => void }) {
  const [role, setRole] = useState(member.role)
  const [active, setActive] = useState(member.isActive)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  return <tr><td>{member.email}</td><td><Badge active={member.userIsActive}>{member.userIsActive ? 'Abilitato' : 'Account disabilitato'}</Badge></td><td>
    <form className="inline-form" onSubmit={async event => {
      event.preventDefault()
      if (!window.confirm('Applicare ruolo e stato di accesso a questo studio?')) return
      setBusy(true); setError('')
      try { await api('/studio/members/' + member.id, 'PUT', { role, isActive: active }); reload() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
    }}><select aria-label={'Ruolo ' + member.email} value={role} onChange={e => setRole(e.target.value as StudioRole)} disabled={busy}>{roles.map(r => <option key={r}>{r}</option>)}</select>
      <label className="check-inline"><input type="checkbox" aria-label={'Accesso ' + member.email} checked={active} onChange={e => setActive(e.target.checked)} disabled={busy} />Accesso attivo</label>
      <button disabled={busy}>Salva accesso</button>
    </form>{error && <p className="alert" role="alert">{error}</p>}
  </td></tr>
}
export function StudioUsersPage({ me }: { me: CurrentUser }) {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => { const timer = window.setInterval(() => setNow(Date.now()), 60_000); return () => window.clearInterval(timer) }, [])
  const members = useResource<Member[]>('/studio/members')
  const invitations = useResource<Invitation[]>('/studio/invitations')
  const audit = useResource<Audit[]>('/studio/audit')
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<StudioRole>('Collaborator')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  function refresh() { members.reload(); invitations.reload(); audit.reload() }
  return <><div className="card"><h2>Utenti di {me.activeTenantName}</h2><p className="muted">Il ruolo vale solo per questo studio. Non e possibile disabilitare o declassare l’ultimo StudioAdmin attivo.</p>
    <Status resource={members}>{!members.data?.length ? <Empty /> : <DataTable headings={['Utente', 'Account', 'Accesso allo studio']}>{members.data.map(member => <MemberRow key={member.id + member.role + member.isActive} member={member} reload={refresh} />)}</DataTable>}</Status>
  </div><div className="card"><h2>Invita un utente</h2><p>Un account gia esistente potra accettare dopo il login, senza duplicare l’utente.</p>
    <form onSubmit={async event => {
      event.preventDefault(); setBusy(true); setError(''); setNotice('')
      try {
        await api('/studio/invitations', 'POST', { email, role }); setEmail(''); refresh()
        setNotice('Invito creato. Scade tra 48 ore. Un eventuale invito precedente alla stessa email e stato revocato.')
      } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
    }}><fieldset disabled={busy}><div className="form-grid">
      <label>Email destinatario<input type="email" required maxLength={256} value={email} onChange={e => setEmail(e.target.value)} /></label>
      <label>Ruolo da assegnare<select value={role} onChange={e => setRole(e.target.value as StudioRole)}>{roles.map(r => <option key={r}>{r}</option>)}</select></label>
    </div></fieldset>{error && <p role="alert" className="alert">{error}</p>}{notice && <p role="status">{notice}</p>}<button className="primary" disabled={busy}>Invia invito</button></form>
    <h3>Inviti dello studio</h3><Status resource={invitations}>{!invitations.data?.length ? <Empty /> : <DataTable headings={['Email', 'Ruolo', 'Scadenza', 'Stato', 'Azioni']}>{invitations.data.map(row =>
      <tr key={row.id}><td>{row.email}</td><td>{row.role}</td><td>{new Date(row.expiresAt).toLocaleString('it-IT')}</td><td>{row.usedAt ? 'Accettato' : row.revokedAt ? 'Revocato' : Date.parse(row.expiresAt) <= now ? 'Scaduto' : 'In attesa'}</td><td>{!row.usedAt && !row.revokedAt && <button disabled={busy} onClick={async () => {
        if (!window.confirm('Revocare questo invito? Il link non sara piu utilizzabile.')) return
        setBusy(true); setError('')
        try { await api('/studio/invitations/' + row.id + '/revoke', 'POST'); refresh() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
      }}>Revoca invito</button>}</td></tr>)}</DataTable>}</Status>
  </div>{me.isDevelopment && <DevelopmentMessages refreshKey={invitations.data?.length ?? 0} />}
  <div className="card"><h2>Audit dello studio</h2><Status resource={audit}>{!audit.data?.length ? <Empty /> : <DataTable headings={['Quando', 'Evento', 'Attore', 'Soggetto']}>{audit.data.map(row => <tr key={row.id}><td>{new Date(row.timestamp).toLocaleString('it-IT')}</td><td>{row.event}</td><td>{row.actorUserId ?? '—'}</td><td>{row.subjectId ?? '—'}</td></tr>)}</DataTable>}</Status></div></>
}
export function DevelopmentMessages({ refreshKey = 0 }: { refreshKey?: number }) {
  const resource = useResource<{ id: string; recipient: string; kind: string; link: string }[]>('/development/messages?revision=' + refreshKey)
  return <div className="card"><div className="section-heading"><h3>Posta di sviluppo</h3><button onClick={resource.reload}>Aggiorna posta</button></div>
    <p className="muted">Disponibile solo in Development. Mostra i tuoi link di recupero e gli inviti emessi da te come amministratore di questo studio. Per consegna reale configurare SMTP.</p>
    <Status resource={resource}>{!resource.data?.length ? <Empty>Nessun messaggio in memoria. Se SMTP e configurato, controlla la posta del destinatario.</Empty> : <DataTable headings={['Destinatario', 'Tipo', 'Link personale']}>{resource.data.map(row => <tr key={row.id}><td>{row.recipient}</td><td>{row.kind === 'invite' ? 'Invito' : 'Recupero password'}</td><td><a href={row.link} target="_blank" rel="noopener noreferrer">Apri link personale</a></td></tr>)}</DataTable>}</Status>
  </div>
}
export function PlatformUsersPage() {
  const resource = useResource<{ id: string; email: string; isActive: boolean }[]>('/platform/users')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  return <div className="card"><h2>Amministrazione degli account</h2><p className="muted">Gestione tecnica della piattaforma. Non concede accesso ai dati degli studi e non assegna il ruolo globale PlatformAdmin.</p>
    {error && <p className="alert" role="alert">{error}</p>}<Status resource={resource}>{!resource.data?.length ? <Empty /> : <DataTable headings={['Email', 'Stato', 'Azioni']}>{resource.data.map(user => <tr key={user.id}><td>{user.email}</td><td><Badge active={user.isActive} /></td><td><button disabled={busy} onClick={async () => {
      if (!window.confirm(user.isActive ? 'Disabilitare l’account e terminare tutte le sue sessioni?' : 'Riabilitare l’account? Le membership rimarranno invariate.')) return
      setBusy(true); setError('')
      try { await api('/platform/users/' + user.id + '/status', 'PUT', { isActive: !user.isActive }); resource.reload() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
    }}>{user.isActive ? 'Disabilita account' : 'Riabilita account'}</button></td></tr>)}</DataTable>}</Status></div>
}