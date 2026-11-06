import { useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { CurrentUser } from './api'
import { errorMessage } from './form'

export type SecurityLink = { kind: 'reset' | 'invite'; token: string }
export function AuthPage({ link, onAuthenticated, onClearLink, notice }: { link: SecurityLink | null; onAuthenticated: () => void; onClearLink: () => void; notice: string }) {
  const [mode, setMode] = useState<'login' | 'forgot' | 'reset' | 'invite'>(link?.kind ?? 'login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)
  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setMessage(''); setBusy(true)
    try {
      if (mode === 'login') { await api('/auth/login', 'POST', { email, password }); onAuthenticated() }
      else if (mode === 'forgot') {
        await api('/auth/forgot-password', 'POST', { email })
        setMessage('Richiesta ricevuta. Se l’account esiste, controlla il canale email configurato.')
      } else if (mode === 'reset' && link) {
        await api('/auth/reset-password', 'POST', { token: link.token, newPassword: password })
        setMessage('Password aggiornata. Ora puoi accedere.'); onClearLink(); setMode('login'); setPassword('')
      } else if (mode === 'invite' && link) {
        await api('/auth/accept-invitation', 'POST', { token: link.token, password })
        setMessage('Invito accettato. Accedi con la tua email e password.'); onClearLink(); setMode('login'); setPassword('')
      }
    } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
  }
  return <div className="auth-shell"><a className="brand" href="/">CM <span>CondoManager</span></a><div className="card auth-card">
    <p className="eyebrow">ACCESSO SICURO</p><h1>{mode === 'login' ? 'Accedi al tuo spazio' : mode === 'forgot' ? 'Recupera la password' : mode === 'reset' ? 'Imposta una nuova password' : 'Accetta l’invito'}</h1>
    {notice && <p role="status" className="development-notice">{notice}</p>}
    <form onSubmit={submit}><fieldset disabled={busy}>
      {['login', 'forgot'].includes(mode) && <label>Email<input name="email" type="email" autoComplete="username" required maxLength={256} value={email} onChange={e => setEmail(e.target.value)} /></label>}
      {mode !== 'forgot' && <label>{mode === 'login' ? 'Password' : 'Nuova password'}<input name="password" type="password" autoComplete={mode === 'login' ? 'current-password' : 'new-password'} required maxLength={128} minLength={mode === 'login' ? undefined : 12} value={password} onChange={e => setPassword(e.target.value)} /></label>}
      {['reset', 'invite'].includes(mode) && <p className="muted">Almeno 12 caratteri, con maiuscole, minuscole, numeri e un simbolo. Massimo 128 caratteri.</p>}
    </fieldset>{error && <p className="alert" role="alert">{error}</p>}{message && <p role="status">{message}</p>}
      <button className="primary" disabled={busy}>{busy ? 'Attendi…' : mode === 'login' ? 'Accedi' : mode === 'forgot' ? 'Richiedi recupero' : mode === 'reset' ? 'Aggiorna password' : 'Crea account e accetta'}</button>
    </form><div className="actions auth-links">
      {mode === 'login' && <button onClick={() => { setMode('forgot'); setError('') }}>Password dimenticata?</button>}
      {mode !== 'login' && <button onClick={() => { setMode('login'); setError('') }}>{mode === 'invite' ? 'Ho gia un account: accedi' : 'Torna al login'}</button>}
    </div><p className="muted">Nessuna registrazione pubblica degli studi. Richiedi un invito al tuo amministratore.</p>
  </div></div>
}
export function AcceptExistingInvitation({ link, me, onDone }: { link: SecurityLink; me: CurrentUser; onDone: () => void }) {
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  return <div className="card"><h2>Invito a uno studio</h2><p>Accetti con l’account {me.email}. L’invito deve essere destinato alla stessa email.</p>
    {error && <p role="alert" className="alert">{error}</p>}<div className="actions">
      <button className="primary" disabled={busy} onClick={async () => {
        setBusy(true); setError('')
        try { await api('/auth/accept-invitation', 'POST', { token: link.token, password: null }); onDone() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
      }}>Accetta invito</button><button disabled={busy} onClick={onDone}>Ignora invito</button>
    </div></div>
}
export function AccountPage({ me, onSignedOut }: { me: CurrentUser; onSignedOut: () => void }) {
  const [current, setCurrent] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  return <div className="card"><h2>Il tuo account</h2><p>{me.email}</p><h3>Cambio password</h3>
    <p className="muted">Il cambio password termina tutte le sessioni. Dovrai accedere di nuovo.</p>
    <form onSubmit={async event => {
      event.preventDefault(); setBusy(true); setError('')
      try { await api('/auth/change-password', 'POST', { currentPassword: current, newPassword: password }); onSignedOut() } catch (reason) { setError(errorMessage(reason)) } finally { setBusy(false) }
    }}><fieldset disabled={busy}><div className="form-grid">
      <label>Password attuale<input type="password" autoComplete="current-password" required maxLength={128} value={current} onChange={e => setCurrent(e.target.value)} /></label>
      <label>Nuova password<input type="password" autoComplete="new-password" required minLength={12} maxLength={128} value={password} onChange={e => setPassword(e.target.value)} /></label>
    </div></fieldset><p className="muted">Almeno 12 caratteri, maiuscole, minuscole, numeri e simboli.</p>
      {error && <p role="alert" className="alert">{error}</p>}<button className="primary" disabled={busy}>Cambia password</button>
    </form></div>
}