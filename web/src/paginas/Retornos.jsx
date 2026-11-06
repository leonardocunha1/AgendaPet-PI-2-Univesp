import React, { useEffect, useState } from 'react'
import { api, dataBR, linkWhatsApp, mensagemRetorno, NOME_LOJA } from '../api.js'
import { Carregando, EtiquetaServico } from '../componentes.jsx'

export default function Retornos() {
  const [dias, setDias] = useState(7)
  const [lista, setLista] = useState(null)
  const [servicos, setServicos] = useState([])

  useEffect(() => { api.get('/api/servicos').then(setServicos) }, [])
  useEffect(() => { api.get(`/api/retornos?dias=${dias}`).then(setLista) }, [dias])

  const idServico = (tipo) => servicos.find((s) => s.tipo === tipo)?.id ?? ''

  return (
    <section>
      <h1>Retornos de vacina, vermífugo e consulta</h1>
      <div className="barra">
        <label htmlFor="dias">Mostrar os próximos</label>
        <select id="dias" value={dias} onChange={(e) => setDias(Number(e.target.value))}>
          <option value={7}>7 dias</option><option value={15}>15 dias</option><option value={30}>30 dias</option>
        </select>
      </div>
      {!lista ? <Carregando /> : lista.length === 0 ? <p>Nenhum retorno previsto no período.</p> : (
        <table className="tabela">
          <caption className="sr">Retornos previstos</caption>
          <thead><tr><th scope="col">Data</th><th scope="col">Animal</th><th scope="col">Tutor</th><th scope="col">Retorno</th><th scope="col">Ações</th></tr></thead>
          <tbody>
            {lista.map((r) => (
              <tr key={r.id}>
                <td>{dataBR(r.proximoRetorno)} {r.atrasado && <span className="etiqueta svc-alerta">atrasado</span>}</td>
                <td><a href={`#/animal/${r.animalId}`}>{r.animal}</a></td>
                <td>{r.tutor}<br /><small>{r.telefone}</small></td>
                <td><EtiquetaServico tipo={r.tipo} /> {r.descricaoRetorno}</td>
                <td className="acoes">
                  <a className="btn" target="_blank" rel="noreferrer" href={linkWhatsApp(r.telefone, mensagemRetorno(r, NOME_LOJA))}>Avisar no WhatsApp</a>
                  <a className="btn" href={`#/novo?animal=${r.animalId}&servico=${idServico(r.tipo)}&data=${r.proximoRetorno}`}>Agendar</a>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}
