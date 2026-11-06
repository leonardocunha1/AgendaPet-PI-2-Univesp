import React, { useEffect, useState } from 'react'
import { api, SERVICOS } from '../api.js'
import { Erros, Sucesso } from '../componentes.jsx'

const TIPOS = ['BanhoTosa', 'Consulta', 'Vacina']

export default function Configuracoes() {
  const [profs, setProfs] = useState([])
  const [servicos, setServicos] = useState([])
  const [baias, setBaias] = useState([])
  const [erros, setErros] = useState([])
  const [ok, setOk] = useState('')

  const carregar = () => Promise.all([api.get('/api/profissionais'), api.get('/api/servicos'), api.get('/api/baias')])
    .then(([p, s, b]) => { setProfs(p); setServicos(s); setBaias(b) })
  useEffect(() => { carregar() }, [])

  async function executar(fn, msg) {
    try { await fn(); setErros([]); setOk(msg); carregar() }
    catch (e) { setErros(e.erros ?? [e.message]); setOk('') }
  }

  return (
    <section>
      <h1>Configurações</h1>
      <Erros erros={erros} />
      <Sucesso texto={ok} />

      <div className="painel">
        <h2>Profissionais</h2>
        {profs.map((p) => <LinhaProf key={p.id} p={p} onSalvar={(f) => executar(() => api.put(`/api/profissionais/${p.id}`, f), 'Profissional atualizado.')} />)}
        <h3>Adicionar profissional</h3>
        <LinhaProf p={{ nome: '', funcao: '', servicos: 'BanhoTosa', ativo: true }} novo
          onSalvar={(f) => executar(() => api.post('/api/profissionais', f), 'Profissional adicionado.')} />
      </div>

      <div className="painel">
        <h2>Duração padrão dos serviços</h2>
        {servicos.map((s) => (
          <form key={s.id} className="linha" onSubmit={(e) => {
            e.preventDefault()
            const v = Number(e.target.elements[`dur-${s.id}`].value)
            executar(() => api.put(`/api/servicos/${s.id}`, { duracaoPadraoMinutos: v }), 'Duração atualizada.')
          }}>
            <label htmlFor={`dur-${s.id}`}>{s.nome} (minutos)</label>
            <input id={`dur-${s.id}`} type="number" min="5" defaultValue={s.duracaoPadraoMinutos} />
            <button type="submit">Salvar</button>
          </form>
        ))}
      </div>

      <div className="painel">
        <h2>Baias da hospedagem</h2>
        {baias.map((b) => (
          <form key={b.id} className="linha" onSubmit={(e) => {
            e.preventDefault()
            const el = e.target.elements
            executar(() => api.put(`/api/baias/${b.id}`, { nome: el[`bn-${b.id}`].value, ativa: el[`ba-${b.id}`].checked }), 'Baia atualizada.')
          }}>
            <label htmlFor={`bn-${b.id}`} className="sr">Nome da baia</label>
            <input id={`bn-${b.id}`} defaultValue={b.nome} />
            <span className="check"><input id={`ba-${b.id}`} type="checkbox" defaultChecked={b.ativa} /><label htmlFor={`ba-${b.id}`}>Ativa</label></span>
            <button type="submit">Salvar</button>
          </form>
        ))}
        <button onClick={() => executar(() => api.post('/api/baias', { nome: `Baia ${baias.length + 1}`, ativa: true }), 'Baia adicionada.')}>+ Adicionar baia</button>
      </div>
    </section>
  )
}

function LinhaProf({ p, onSalvar, novo }) {
  const [f, setF] = useState(p)
  const sel = f.servicos.split(',').map((x) => x.trim()).filter(Boolean)
  const alternar = (t) => setF({ ...f, servicos: (sel.includes(t) ? sel.filter((x) => x !== t) : [...sel, t]).join(',') })
  const k = novo ? 'novo' : p.id
  return (
    <form className="linha prof" onSubmit={(e) => { e.preventDefault(); onSalvar(f); if (novo) setF(p) }}>
      <label htmlFor={`pn-${k}`} className="sr">Nome</label>
      <input id={`pn-${k}`} value={f.nome} onChange={(e) => setF({ ...f, nome: e.target.value })} placeholder="Nome" />
      <label htmlFor={`pf-${k}`} className="sr">Função</label>
      <input id={`pf-${k}`} value={f.funcao} onChange={(e) => setF({ ...f, funcao: e.target.value })} placeholder="Função" />
      <fieldset className="inline">
        <legend className="sr">Serviços que realiza</legend>
        {TIPOS.map((t) => (
          <span className="check" key={t}>
            <input id={`ps-${k}-${t}`} type="checkbox" checked={sel.includes(t)} onChange={() => alternar(t)} />
            <label htmlFor={`ps-${k}-${t}`}>{SERVICOS[t].rotulo}</label>
          </span>
        ))}
      </fieldset>
      {!novo && (
        <span className="check">
          <input id={`pa-${k}`} type="checkbox" checked={f.ativo} onChange={(e) => setF({ ...f, ativo: e.target.checked })} />
          <label htmlFor={`pa-${k}`}>Ativo</label>
        </span>
      )}
      <button type="submit">{novo ? 'Adicionar' : 'Salvar'}</button>
    </form>
  )
}
