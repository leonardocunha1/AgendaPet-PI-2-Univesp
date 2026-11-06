import React, { useEffect, useState } from 'react'
import { api, dataBR, hora } from '../api.js'
import { Campo, Erros, Sucesso, Carregando, EtiquetaServico } from '../componentes.jsx'

export function FormAnimal({ clienteId, inicial, onSalvar }) {
  const [f, setF] = useState(inicial ?? { nome: '', especie: 'Cão', raca: '', porte: '', dataNascimento: '', observacoes: '' })
  const [erros, setErros] = useState([])
  const [ok, setOk] = useState('')
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value })

  async function enviar(e) {
    e.preventDefault()
    try {
      await onSalvar({ ...f, clienteId, dataNascimento: f.dataNascimento || null })
      setErros([]); setOk('Dados salvos.')
    } catch (err) { setErros(err.erros ?? [err.message]); setOk('') }
  }

  return (
    <form onSubmit={enviar} noValidate className="form-animal">
      <div className="linha">
        <Campo id="aNome" rotulo="Nome do animal"><input id="aNome" value={f.nome} onChange={set('nome')} required /></Campo>
        <Campo id="aEsp" rotulo="Espécie">
          <select id="aEsp" value={f.especie} onChange={set('especie')}>
            <option>Cão</option><option>Gato</option><option>Outro</option>
          </select>
        </Campo>
      </div>
      <div className="linha">
        <Campo id="aRaca" rotulo="Raça"><input id="aRaca" value={f.raca ?? ''} onChange={set('raca')} /></Campo>
        <Campo id="aPorte" rotulo="Porte">
          <select id="aPorte" value={f.porte ?? ''} onChange={set('porte')}>
            <option value="">—</option><option>Pequeno</option><option>Médio</option><option>Grande</option>
          </select>
        </Campo>
        <Campo id="aNasc" rotulo="Nascimento"><input id="aNasc" type="date" value={f.dataNascimento ?? ''} onChange={set('dataNascimento')} /></Campo>
      </div>
      <Campo id="aObs" rotulo="Alertas (alergias, comportamento, focinheira…)" ajuda="Aparece em destaque sempre que o animal for agendado.">
        <textarea id="aObs" rows={2} value={f.observacoes ?? ''} onChange={set('observacoes')} aria-describedby="aObs-ajuda" />
      </Campo>
      <Erros erros={erros} />
      <Sucesso texto={ok} />
      <button type="submit" className="primario">Salvar animal</button>
    </form>
  )
}

export default function Animal({ id }) {
  const [dados, setDados] = useState(null)
  const [hist, setHist] = useState([])

  const carregar = () => Promise.all([api.get(`/api/animais/${id}`), api.get(`/api/animais/${id}/historico`)])
    .then(([d, h]) => { setDados(d); setHist(h) })
  useEffect(() => { carregar() }, [id])

  if (!dados) return <Carregando />
  const { animal, tutor } = dados

  return (
    <section>
      <p><a href={`#/cliente/${tutor.id}`}>← {tutor.nome}</a></p>
      <h1>{animal.nome}</h1>
      <div className="barra"><a className="btn primario" href={`#/novo?animal=${animal.id}`}>+ Agendar para {animal.nome}</a></div>
      <div className="grade2">
        <div className="painel">
          <h2>Cadastro</h2>
          <FormAnimal clienteId={animal.clienteId} inicial={animal}
            onSalvar={async (f) => { await api.put(`/api/animais/${id}`, f); carregar() }} />
        </div>
        <div className="painel">
          <h2>Histórico de atendimentos</h2>
          {hist.length === 0 ? <p>Nenhum atendimento ainda.</p> : (
            <ol className="historico">
              {hist.map((h) => (
                <li key={h.id}>
                  <div><strong>{dataBR(h.inicio)}</strong> {h.tipo !== 'Hospedagem' && hora(h.inicio)} <EtiquetaServico tipo={h.tipo} /> <span className="status">{h.status}</span></div>
                  <div>{h.profissional ?? h.baia}{h.tipo === 'Hospedagem' && ` até ${dataBR(h.fim)}`}</div>
                  {h.observacoes && <div className="obs">{h.observacoes}</div>}
                  {h.proximoRetorno && <div>Próximo retorno: {dataBR(h.proximoRetorno)} {h.descricaoRetorno && `(${h.descricaoRetorno})`}</div>}
                </li>
              ))}
            </ol>
          )}
        </div>
      </div>
    </section>
  )
}
