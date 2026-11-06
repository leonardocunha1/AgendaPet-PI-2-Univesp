import React, { useEffect, useState } from 'react'
import { NOME_LOJA } from './api.js'
import Agenda from './paginas/Agenda.jsx'
import NovoAgendamento from './paginas/NovoAgendamento.jsx'
import Clientes from './paginas/Clientes.jsx'
import Cliente from './paginas/Cliente.jsx'
import Animal from './paginas/Animal.jsx'
import Hospedagem from './paginas/Hospedagem.jsx'
import Retornos from './paginas/Retornos.jsx'
import Indicadores from './paginas/Indicadores.jsx'
import Configuracoes from './paginas/Configuracoes.jsx'

// Roteamento simples por "#/rota?param=valor" (sem dependências extras).
function lerRota() {
  const h = window.location.hash.replace(/^#/, '') || '/agenda'
  const [caminho, query = ''] = h.split('?')
  return { partes: caminho.split('/').filter(Boolean), params: Object.fromEntries(new URLSearchParams(query)) }
}

const MENU = [
  ['agenda', 'Agenda do dia'],
  ['novo', 'Novo agendamento'],
  ['clientes', 'Clientes e animais'],
  ['hospedagem', 'Hospedagem'],
  ['retornos', 'Retornos'],
  ['indicadores', 'Indicadores'],
  ['config', 'Configurações']
]

export default function App() {
  const [rota, setRota] = useState(lerRota)
  const [menuAberto, setMenuAberto] = useState(false)

  useEffect(() => {
    const f = () => { setRota(lerRota()); setMenuAberto(false); window.scrollTo(0, 0) }
    window.addEventListener('hashchange', f)
    return () => window.removeEventListener('hashchange', f)
  }, [])

  const [pag, id] = rota.partes
  let conteudo
  switch (pag) {
    case 'novo': conteudo = <NovoAgendamento params={rota.params} />; break
    case 'clientes': conteudo = <Clientes />; break
    case 'cliente': conteudo = <Cliente id={id} />; break
    case 'animal': conteudo = <Animal id={id} />; break
    case 'hospedagem': conteudo = <Hospedagem />; break
    case 'retornos': conteudo = <Retornos />; break
    case 'indicadores': conteudo = <Indicadores />; break
    case 'config': conteudo = <Configuracoes />; break
    default: conteudo = <Agenda params={rota.params} />
  }
  const atual = pag === 'cliente' || pag === 'animal' ? 'clientes' : (pag || 'agenda')

  return (
    <div className="app">
      <a className="pular" href="#conteudo" onClick={(e) => { e.preventDefault(); document.getElementById('conteudo').focus() }}>
        Pular para o conteúdo
      </a>
      <header className="topo">
        <button className="btn-menu" aria-expanded={menuAberto} aria-controls="menu" onClick={() => setMenuAberto(!menuAberto)}>
          Menu
        </button>
        <strong>{NOME_LOJA}</strong>
        <span className="sub">Agenda integrada</span>
      </header>
      <nav id="menu" className={menuAberto ? 'lateral aberto' : 'lateral'} aria-label="Menu principal">
        <ul>
          {MENU.map(([k, rotulo]) => (
            <li key={k}>
              <a href={`#/${k}`} aria-current={atual === k ? 'page' : undefined}>{rotulo}</a>
            </li>
          ))}
        </ul>
      </nav>
      <main id="conteudo" tabIndex={-1}>{conteudo}</main>
    </div>
  )
}
