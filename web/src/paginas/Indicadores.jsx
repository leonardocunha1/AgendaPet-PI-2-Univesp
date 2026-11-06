import React, { useEffect, useState } from 'react'
import { api, hojeISO, somaDias, dataBR } from '../api.js'
import { Carregando } from '../componentes.jsx'

export default function Indicadores() {
  const [de, setDe] = useState(somaDias(hojeISO(), -30))
  const [ate, setAte] = useState(hojeISO())
  const [d, setD] = useState(null)

  useEffect(() => { api.get(`/api/indicadores?de=${de}&ate=${ate}`).then(setD) }, [de, ate])

  return (
    <section>
      <h1>Indicadores de uso</h1>
      <div className="barra">
        <label htmlFor="de">De</label><input id="de" type="date" value={de} onChange={(e) => setDe(e.target.value)} />
        <label htmlFor="ate">Até</label><input id="ate" type="date" value={ate} onChange={(e) => setAte(e.target.value)} />
      </div>
      {!d ? <Carregando /> : (
        <>
          <div className="numeros">
            <div><span>{d.totalAgendamentos}</span>agendamentos</div>
            <div><span>{d.conflitosEvitados}</span>conflitos de horário evitados</div>
            <div><span>{d.taxaFaltas}%</span>de faltas (sobre realizados + faltas)</div>
            <div><span>{d.clientesCadastrados}</span>clientes</div>
            <div><span>{d.animaisCadastrados}</span>animais</div>
          </div>
          <div className="grade2">
            <div className="painel">
              <h2>Por serviço</h2>
              <ul>{d.porServico.map((s) => <li key={s.servico}>{s.servico}: {s.quantidade}</li>)}</ul>
            </div>
            <div className="painel">
              <h2>Por status</h2>
              <ul>{d.porStatus.map((s) => <li key={s.status}>{s.status}: {s.quantidade}</li>)}</ul>
            </div>
          </div>
          <div className="painel">
            <h2>Últimos conflitos barrados pelo sistema</h2>
            {d.ultimosConflitos.length === 0 ? <p>Nenhum no período.</p> : (
              <ul>{d.ultimosConflitos.map((c, i) => <li key={i}><strong>{dataBR(c.quando)}</strong> — {c.motivo}</li>)}</ul>
            )}
          </div>
        </>
      )}
    </section>
  )
}
