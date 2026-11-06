import React, { useEffect, useState } from 'react'
import { api } from '../api.js'
import { Campo, Erros, Sucesso, Carregando } from '../componentes.jsx'
import { FormAnimal } from './Animal.jsx'

export function FormCliente({ inicial, onSalvar }) {
  const [f, setF] = useState(inicial ?? { nome: '', telefone: '', email: '', endereco: '', consentimentoLgpd: false })
  const [erros, setErros] = useState([])
  const [ok, setOk] = useState('')
  const set = (k) => (e) => setF({ ...f, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value })

  async function enviar(e) {
    e.preventDefault()
    try { await onSalvar(f); setErros([]); setOk('Dados salvos.') }
    catch (err) { setErros(err.erros ?? [err.message]); setOk('') }
  }

  return (
    <form onSubmit={enviar} noValidate>
      <div className="linha">
        <Campo id="cNome" rotulo="Nome do tutor"><input id="cNome" value={f.nome} onChange={set('nome')} required /></Campo>
        <Campo id="cTel" rotulo="Telefone / WhatsApp"><input id="cTel" type="tel" value={f.telefone} onChange={set('telefone')} placeholder="(16) 99999-9999" required /></Campo>
      </div>
      <div className="linha">
        <Campo id="cEmail" rotulo="E-mail (opcional)"><input id="cEmail" type="email" value={f.email ?? ''} onChange={set('email')} /></Campo>
        <Campo id="cEnd" rotulo="Endereço (opcional)"><input id="cEnd" value={f.endereco ?? ''} onChange={set('endereco')} /></Campo>
      </div>
      <div className="check">
        <input id="cLgpd" type="checkbox" checked={f.consentimentoLgpd} onChange={set('consentimentoLgpd')} />
        <label htmlFor="cLgpd">O tutor autorizou o uso dos dados para agendamentos e lembretes (LGPD).</label>
      </div>
      <Erros erros={erros} />
      <Sucesso texto={ok} />
      <button type="submit" className="primario">Salvar cliente</button>
    </form>
  )
}

export default function Cliente({ id }) {
  const [dados, setDados] = useState(null)
  const [novoAnimal, setNovoAnimal] = useState(false)

  const carregar = () => api.get(`/api/clientes/${id}`).then(setDados)
  useEffect(() => { carregar() }, [id])

  if (!dados) return <Carregando />
  const { cliente, animais } = dados

  return (
    <section>
      <p><a href="#/clientes">← Clientes</a></p>
      <h1>{cliente.nome}</h1>
      <div className="grade2">
        <div className="painel">
          <h2>Dados do tutor</h2>
          <FormCliente inicial={cliente} onSalvar={async (f) => { await api.put(`/api/clientes/${id}`, f); carregar() }} />
        </div>
        <div className="painel">
          <h2>Animais</h2>
          <ul className="lista">
            {animais.map((a) => (
              <li key={a.id}>
                <a href={`#/animal/${a.id}`}><strong>{a.nome}</strong></a> — {a.especie}{a.raca ? `, ${a.raca}` : ''}{a.porte ? `, ${a.porte}` : ''}
                {a.observacoes && <div className="alerta">⚠ {a.observacoes}</div>}
                <a className="btn" href={`#/novo?animal=${a.id}`}>Agendar</a>
              </li>
            ))}
            {animais.length === 0 && <li>Nenhum animal cadastrado.</li>}
          </ul>
          <button onClick={() => setNovoAnimal(!novoAnimal)} aria-expanded={novoAnimal}>+ Adicionar animal</button>
          {novoAnimal && (
            <FormAnimal clienteId={cliente.id} onSalvar={async (f) => { await api.post('/api/animais', f); setNovoAnimal(false); carregar() }} />
          )}
        </div>
      </div>
    </section>
  )
}
