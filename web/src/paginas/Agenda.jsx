import React, { useEffect, useState } from 'react'
import { api, hojeISO, somaDias, diaSemana, hora, dataBR, STATUS, linkWhatsApp, mensagemConfirmacao, NOME_LOJA } from '../api.js'
import { EtiquetaServico, Carregando, Erros } from '../componentes.jsx'

export default function Agenda({ params }) {
  const [data, setData] = useState(params.data || hojeISO())
  const [itens, setItens] = useState(null)
  const [profs, setProfs] = useState([])
  const [retornos, setRetornos] = useState([])
  const [erros, setErros] = useState([])

  async function carregar() {
    try {
      const [a, p, r] = await Promise.all([
        api.get(`/api/agenda?data=${data}`),
        api.get('/api/profissionais'),
        api.get('/api/retornos?dias=7')
      ])
      setItens(a); setProfs(p.filter((x) => x.ativo)); setRetornos(r); setErros([])
    } catch (e) { setErros(e.erros ?? [e.message]) }
  }
  useEffect(() => { carregar() }, [data])

  async function mudarStatus(item, status) {
    try { await api.patch(`/api/agendamentos/${item.id}/status`, { status }); carregar() }
    catch (e) { setErros(e.erros ?? [e.message]) }
  }

  const hosp = (itens ?? []).filter((i) => i.tipo === 'Hospedagem')
  const colunas = profs.map((p) => ({ p, itens: (itens ?? []).filter((i) => i.profissionalId === p.id) }))

  return (
    <section>
      <h1>Agenda do dia</h1>
      <div className="barra">
        <button onClick={() => setData(somaDias(data, -1))} aria-label="Dia anterior">◀ Anterior</button>
        <label className="sr" htmlFor="dataAgenda">Data da agenda</label>
        <input id="dataAgenda" type="date" value={data} onChange={(e) => e.target.value && setData(e.target.value)} />
        <button onClick={() => setData(somaDias(data, 1))} aria-label="Próximo dia">Próximo ▶</button>
        <button onClick={() => setData(hojeISO())}>Hoje</button>
        <a className="btn primario" href={`#/novo?data=${data}`}>+ Novo agendamento</a>
      </div>
      <p className="dia-extenso">{diaSemana(data)}</p>
      <Erros erros={erros} />

      {retornos.length > 0 && data === hojeISO() && (
        <div className="aviso info">
          <strong>{retornos.length} retorno(s) de vacina/vermífugo/consulta nesta semana.</strong>{' '}
          <a href="#/retornos">Ver retornos</a>
        </div>
      )}

      {itens === null ? <Carregando /> : (
        <div className="colunas">
          {colunas.map(({ p, itens }) => (
            <div className="coluna" key={p.id}>
              <h2>{p.nome} <small>({p.funcao})</small></h2>
              {itens.length === 0 && <p className="vazio">Sem atendimentos.</p>}
              {itens.map((i) => <Cartao key={i.id} i={i} onStatus={mudarStatus} />)}
            </div>
          ))}
          <div className="coluna">
            <h2>Hospedagem</h2>
            {hosp.length === 0 && <p className="vazio">Nenhum animal hospedado.</p>}
            {hosp.map((i) => <Cartao key={i.id} i={i} onStatus={mudarStatus} dia={data} />)}
          </div>
        </div>
      )}
      <p className="legenda">Status: Agendado → Confirmado → Realizado (ou Falta / Cancelado).</p>
    </section>
  )
}

function Cartao({ i, onStatus, dia }) {
  const hospedagem = i.tipo === 'Hospedagem'
  let quando = `${hora(i.inicio)}–${hora(i.fim)}`
  if (hospedagem) {
    const entra = i.inicio.slice(0, 10) === dia, sai = i.fim.slice(0, 10) === dia
    quando = entra ? `Entrada ${hora(i.inicio)} (até ${dataBR(i.fim)})`
      : sai ? `Saída hoje ${hora(i.fim)}` : `Hospedado até ${dataBR(i.fim)}`
  }
  return (
    <article className={`cartao st-${i.status.toLowerCase()}`} aria-label={`${i.animal}, ${quando}`}>
      <div className="cartao-topo">
        <span className="horario">{quando}</span>
        <EtiquetaServico tipo={i.tipo} />
      </div>
      <div className="animal">
        <a href={`#/animal/${i.animalId}`}>{i.animal}</a>{i.raca ? ` (${i.raca})` : ''} — {i.tutor}
        {hospedagem && i.baia && <> · {i.baia}</>}
      </div>
      {i.alertasAnimal && <div className="alerta">⚠ {i.alertasAnimal}</div>}
      {i.observacoes && <div className="obs">{i.observacoes}</div>}
      <div className="acoes">
        <label className="sr" htmlFor={`st-${i.id}`}>Status de {i.animal}</label>
        <select id={`st-${i.id}`} value={i.status} onChange={(e) => onStatus(i, e.target.value)}>
          {STATUS.map((s) => <option key={s}>{s}</option>)}
        </select>
        <a className="btn" target="_blank" rel="noreferrer" href={linkWhatsApp(i.telefone, mensagemConfirmacao(i, NOME_LOJA))}>
          Confirmar no WhatsApp
        </a>
        <a className="btn" href={`#/novo?id=${i.id}`}>Editar</a>
      </div>
    </article>
  )
}
