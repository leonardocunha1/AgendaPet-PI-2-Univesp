import React, { useEffect, useState } from 'react'
import { api, hojeISO, somaDias, dataBR, hora, diaSemana } from '../api.js'
import { Carregando } from '../componentes.jsx'

export default function Hospedagem() {
  const [data, setData] = useState(hojeISO())
  const [baias, setBaias] = useState(null)

  useEffect(() => { api.get(`/api/hospedagem?data=${data}`).then(setBaias) }, [data])

  return (
    <section>
      <h1>Hospedagem — ocupação</h1>
      <div className="barra">
        <button onClick={() => setData(somaDias(data, -1))}>◀ Anterior</button>
        <label className="sr" htmlFor="dHosp">Data</label>
        <input id="dHosp" type="date" value={data} onChange={(e) => e.target.value && setData(e.target.value)} />
        <button onClick={() => setData(somaDias(data, 1))}>Próximo ▶</button>
        <a className="btn primario" href={`#/novo?data=${data}&servico=4`}>+ Nova hospedagem</a>
      </div>
      <p className="dia-extenso">{diaSemana(data)}</p>
      {!baias ? <Carregando /> : (
        <div className="baias">
          {baias.map((b) => {
            const o = b.ocupacao[0]
            let status = 'Livre'
            if (o) status = o.fim.slice(0, 10) === data ? 'Saída hoje' : o.inicio.slice(0, 10) === data ? 'Entrada hoje' : 'Ocupada'
            return (
              <article key={b.id} className={`baia ${o ? 'ocupada' : 'livre'}`}>
                <h2>{b.nome}</h2>
                <p className="status-baia">Status: <strong>{status}</strong></p>
                {b.ocupacao.map((x) => (
                  <div key={x.id}>
                    <a href={`#/animal/${x.animalId}`}>{x.animal}</a> — {x.tutor}
                    <div>{dataBR(x.inicio)} {hora(x.inicio)} até {dataBR(x.fim)} {hora(x.fim)}</div>
                    {x.alertasAnimal && <div className="alerta">⚠ {x.alertasAnimal}</div>}
                  </div>
                ))}
              </article>
            )
          })}
        </div>
      )}
    </section>
  )
}
