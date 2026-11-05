import { useState } from 'react'
import { useResource } from './api'
import type { Condominium } from './api'
import { Badge, Empty, Status } from './components'
import { CondominiumsPage, PeoplePage } from './RegistryPages'
import { AllocationPage, PeriodsPage } from './AccountingPages'
import './App.css'

const sections = ['Dashboard', 'Condomini', 'Anagrafiche', 'Tabelle millesimali', 'Esercizi contabili']
function App() {
  const [active, setActive] = useState('Dashboard')
  const [selected, setSelected] = useState('')
  const session = useResource<{ dataAccessEnabled: boolean; mode: string }>('/session')
  const enabled = session.data?.dataAccessEnabled === true
  const condos = useResource<Condominium[]>(enabled ? '/condominiums' : null)
  const current = condos.data?.find(x => x.id === selected)
  return <div className="shell"><aside>
    <a className="brand" href="#dashboard" onClick={() => setActive('Dashboard')}>CM <span>CondoManager</span></a>
    <p className="nav-label">IL TUO STUDIO</p>
    <nav aria-label="Navigazione principale">{sections.map(section => <button key={section} aria-current={active === section ? 'page' : undefined} onClick={() => setActive(section)}>{section}</button>)}</nav>
    <p className="milestone">Anagrafiche · Milestone 02</p>
  </aside><main><header><span>Spazio di lavoro</span><Badge active={enabled}>{enabled ? 'Studio di sviluppo · accesso locale' : 'Accesso non disponibile'}</Badge></header>
    <section aria-labelledby="page-title"><p className="eyebrow">GESTIONE CONDOMINIALE</p><h1 id="page-title">{active}</h1>
      <Status resource={session}>{session.data && (!enabled ? <div className="card"><h2>Accesso dati non configurato</h2><p>Questa versione consente di lavorare con uno studio configurato dal server esclusivamente nell'ambiente di sviluppo.</p><p>Segui il README per avviare database e API e inizializzare lo studio. L'accesso autenticato sara introdotto in una milestone successiva.</p></div> :
        <><p className="development-notice">Ambiente Development senza autenticazione. Usa esclusivamente dati di prova e accesso locale.</p>
          <Status resource={condos}>{condos.data && <>
            {active === 'Dashboard' && <><div className="card"><h2>Il lavoro dello studio</h2><div className="metrics"><strong>{condos.data.filter(x => x.isActive).length} condomini attivi</strong><span>{condos.data.length} registrati</span></div>
              <p>Gestisci anagrafiche, unita, quote millesimali ed esercizi. I dati provengono dal database dello studio configurato.</p><button className="primary" onClick={() => setActive('Condomini')}>Gestisci condomini</button></div>
              {!condos.data.length && <Empty>Lo studio e vuoto. Inizia creando il primo condominio.</Empty>}</>}
            {active === 'Condomini' && <CondominiumsPage rows={condos.data} selected={selected} onSelect={setSelected} refresh={condos.reload} />}
            {active === 'Anagrafiche' && <PeoplePage />}
            {['Tabelle millesimali', 'Esercizi contabili'].includes(active) && <>
              <label className="condo-selector">Condominio <select value={selected} onChange={e => setSelected(e.target.value)}><option value="">Seleziona un condominio…</option>{condos.data.map(c => <option key={c.id} value={c.id}>{c.code} · {c.name}{c.isActive ? '' : ' (inattivo)'}</option>)}</select></label>
              {!current ? <Empty>Seleziona un condominio per continuare.</Empty> : !current.isActive ? <Empty>Riattiva il condominio dalla sua anagrafica per gestire i dati collegati.</Empty> :
                active === 'Tabelle millesimali' ? <AllocationPage key={current.id} condominiumId={current.id} /> : <PeriodsPage key={current.id} condominiumId={current.id} />}
            </>}
          </>}</Status>
        </>)}</Status>
    </section></main></div>
}
export default App