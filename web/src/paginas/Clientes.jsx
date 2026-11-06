import React, { useEffect, useState } from 'react'
import { api } from '../api.js'
import { Erros } from '../componentes.jsx'
import { FormCliente } from './Cliente.jsx'

export default function Clientes() {
  const [busca, setBusca] = useState('')
  const [lista, setLista] = useState([])
  const [novo, setNovo] = useState(false)
  const [erros, setErros] = useState([])

  useEffect(() => {
    const t = setTimeout(() => api.get(`/api/clientes?busca=${encodeURIComponent(busca)}`).then(setLista).catch((e) => setErros([e.message])), 250)
    return () => clearTimeout(t)
  }, [busca])

  async function criar(dados) {
    const c = await api.post('/api/clientes', dados)
    window.location.hash = `#/cliente/${c.id}`
  }

  return (
    <section>
      <h1>Clientes e animais</h1>
      <div className="barra">
        <label className="sr" htmlFor="buscaCli">Buscar</label>
        <input id="buscaCli" value={busca} onChange={(e) => setBusca(e.target.value)} placeholder="Buscar tutor, animal ou telefone" />
        <button className="primario" onClick={() => setNovo(!novo)} aria-expanded={novo}>+ Novo cliente</button>
      </div>
      <Erros erros={erros} />
      {novo && (
        <div className="painel">
          <h2>Novo cliente (tutor)</h2>
          <FormCliente onSalvar={criar} />
        </div>
      )}
      <table className="tabela">
        <caption className="sr">Lista de clientes</caption>
        <thead><tr><th scope="col">Tutor</th><th scope="col">Telefone</th><th scope="col">Animais</th></tr></thead>
        <tbody>
          {lista.map((c) => (
            <tr key={c.id}>
              <td><a href={`#/cliente/${c.id}`}>{c.nome}</a>{!c.consentimentoLgpd && <span className="etiqueta svc-alerta">sem consentimento LGPD</span>}</td>
              <td>{c.telefone}</td>
              <td>{c.animais.map((a, i) => <span key={a.id}>{i > 0 && ', '}<a href={`#/animal/${a.id}`}>{a.nome}</a></span>)}</td>
            </tr>
          ))}
          {lista.length === 0 && <tr><td colSpan={3}>Nenhum cliente encontrado.</td></tr>}
        </tbody>
      </table>
    </section>
  )
}
