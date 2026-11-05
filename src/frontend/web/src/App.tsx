import { useState } from 'react'
import './App.css'

const sections = ['Dashboard', 'Condomini', 'Anagrafiche', 'Millesimi', 'Contabilità', 'Assemblee', 'Documenti']
function App() {
  const [active, setActive] = useState('Dashboard')
  return <div className="shell">
    <aside><a className="brand" href="#dashboard" onClick={() => setActive('Dashboard')}>CM <span>CondoManager</span></a>
      <p className="nav-label">IL TUO STUDIO</p>
      <nav aria-label="Navigazione principale">{sections.map(section =>
        <button key={section} aria-current={active === section ? 'page' : undefined} onClick={() => setActive(section)}>{section}</button>)}</nav>
      <p className="milestone">Fondazioni · Milestone 01</p>
    </aside>
    <main><header><span>Spazio di lavoro</span><span className="badge">Accesso non configurato</span></header>
      <section aria-labelledby="page-title"><p className="eyebrow">GESTIONE CONDOMINIALE</p>
        <h1 id="page-title">{active}</h1><p className="intro">Uno spazio per organizzare il lavoro del tuo studio.</p>
        <div className="empty"><div className="empty-symbol" aria-hidden="true">◇</div>
          <h2>{active === 'Dashboard' ? 'Il tuo studio parte da qui' : 'Nessun dato disponibile'}</h2>
          <p>Questa sezione è in preparazione. L'accesso ai dati sarà disponibile dopo la configurazione dell'autenticazione e dello studio.</p>
          <span className="status">Configurazione iniziale</span>
        </div>
        <p className="note">Non ci sono dati caricati. Le funzioni di gestione saranno introdotte nelle prossime milestone.</p>
      </section>
    </main>
  </div>
}
export default App