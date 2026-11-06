import React, { useEffect, useState } from 'react'
import { api, hojeISO, somaDias, hora } from '../api.js'
import { Campo, Erros, Carregando } from '../componentes.jsx'

export default function NovoAgendamento({ params }) {
  const editId = params.id ? Number(params.id) : null
  const [servicos, setServicos] = useState(null)
  const [profs, setProfs] = useState([])
  const [baias, setBaias] = useState([])

  const [busca, setBusca] = useState('')
  const [resultados, setResultados] = useState([])
  const [animal, setAnimal] = useState(null) // { id, nome, raca, observacoes, tutor }

  const [servicoId, setServicoId] = useState(params.servico ? Number(params.servico) : '')
  const [profId, setProfId] = useState('')
  const [baiaId, setBaiaId] = useState('')
  const [data, setData] = useState(params.data || hojeISO())
  const [horaIni, setHoraIni] = useState('')
  const [duracao, setDuracao] = useState('')
  const [dataSaida, setDataSaida] = useState(somaDias(params.data || hojeISO(), 1))
  const [horaSaida, setHoraSaida] = useState('17:00')
  const [obs, setObs] = useState('')
  const [retorno, setRetorno] = useState('')
  const [descRetorno, setDescRetorno] = useState('')

  const [livres, setLivres] = useState([])
  const [erros, setErros] = useState([])
  const [salvando, setSalvando] = useState(false)

  useEffect(() => {
    Promise.all([api.get('/api/servicos'), api.get('/api/profissionais'), api.get('/api/baias')])
      .then(([s, p, b]) => { setServicos(s); setProfs(p.filter((x) => x.ativo)); setBaias(b.filter((x) => x.ativa)) })
    if (params.animal) carregarAnimal(Number(params.animal))
    if (editId) api.get(`/api/agendamentos/${editId}`).then((ag) => {
      carregarAnimal(ag.animalId)
      setServicoId(ag.servicoId); setProfId(ag.profissionalId ?? ''); setBaiaId(ag.baiaId ?? '')
      setData(ag.inicio.slice(0, 10)); setHoraIni(hora(ag.inicio))
      setDataSaida(ag.fim.slice(0, 10)); setHoraSaida(hora(ag.fim))
      const min = Math.round((new Date(ag.fim) - new Date(ag.inicio)) / 60000)
      setDuracao(String(min)); setObs(ag.observacoes ?? '')
      setRetorno(ag.proximoRetorno ?? ''); setDescRetorno(ag.descricaoRetorno ?? '')
    })
  }, [])

  async function carregarAnimal(id) {
    const r = await api.get(`/api/animais/${id}`)
    setAnimal({ ...r.animal, tutor: r.tutor?.nome })
  }

  // Busca de tutor/animal/telefone com pequeno atraso
  useEffect(() => {
    if (busca.trim().length < 2) { setResultados([]); return }
    const t = setTimeout(() => api.get(`/api/clientes?busca=${encodeURIComponent(busca)}`).then(setResultados), 250)
    return () => clearTimeout(t)
  }, [busca])

  const servico = servicos?.find((s) => s.id === Number(servicoId))
  const hospedagem = servico?.tipo === 'Hospedagem'
  const profsDoServico = servico ? profs.filter((p) => p.servicos.split(',').map((x) => x.trim()).includes(servico.tipo)) : []

  useEffect(() => { if (servico && !editId) setDuracao(String(servico.duracaoPadraoMinutos)) }, [servicoId, servicos])
  useEffect(() => {
    if (profsDoServico.length === 1 && !profId) setProfId(profsDoServico[0].id)
  }, [servicoId, profs])

  // Horários livres do profissional escolhido
  useEffect(() => {
    if (!servico || hospedagem || !profId || !data) { setLivres([]); return }
    api.get(`/api/horarios-livres?profissionalId=${profId}&servicoId=${servico.id}&data=${data}`).then(setLivres).catch(() => setLivres([]))
  }, [profId, servicoId, data])

  async function salvar(e) {
    e.preventDefault()
    const faltando = []
    if (!animal) faltando.push('Escolha o animal.')
    if (!servico) faltando.push('Escolha o serviço.')
    if (!hospedagem && !horaIni) faltando.push('Informe o horário.')
    if (faltando.length) { setErros(faltando); return }

    const inicio = `${data}T${hospedagem ? (horaIni || '08:00') : horaIni}:00`
    const fim = hospedagem
      ? `${dataSaida}T${horaSaida}:00`
      : null
    const corpo = {
      animalId: animal.id,
      servicoId: servico.id,
      profissionalId: hospedagem ? null : (profId ? Number(profId) : null),
      baiaId: hospedagem ? (baiaId ? Number(baiaId) : null) : null,
      inicio,
      fim: fim ?? somarMinutos(inicio, Number(duracao || servico.duracaoPadraoMinutos)),
      observacoes: obs
    }
    setSalvando(true)
    try {
      const salvo = editId ? await api.put(`/api/agendamentos/${editId}`, corpo) : await api.post('/api/agendamentos', corpo)
      if (retorno || editId) await api.patch(`/api/agendamentos/${salvo.id}/retorno`, { proximoRetorno: retorno || null, descricaoRetorno: descRetorno })
      window.location.hash = `#/agenda?data=${data}`
    } catch (err) {
      setErros(err.erros ?? [err.message])
      document.getElementById('erros-agendamento')?.focus()
    } finally { setSalvando(false) }
  }

  if (!servicos) return <Carregando />

  return (
    <section>
      <h1>{editId ? 'Editar agendamento' : 'Novo agendamento'}</h1>
      <form onSubmit={salvar} className="form-agendamento" noValidate>
        <div className="grade2">
          <div>
            <fieldset>
              <legend>1. Animal</legend>
              {animal ? (
                <div className="escolhido">
                  <strong>{animal.nome}</strong>{animal.raca ? ` — ${animal.raca}` : ''}{animal.porte ? `, ${animal.porte}` : ''}
                  <div>Tutor: {animal.tutor}</div>
                  {!editId && <button type="button" onClick={() => setAnimal(null)}>Trocar animal</button>}
                </div>
              ) : (
                <>
                  <Campo id="busca" rotulo="Buscar tutor, animal ou telefone">
                    <input id="busca" value={busca} onChange={(e) => setBusca(e.target.value)} autoComplete="off" placeholder="Ex.: Thor, Ana, 16 99..." />
                  </Campo>
                  <ul className="resultados" aria-live="polite">
                    {resultados.map((c) => filtrarAnimais(c, busca).map((a) => (
                      <li key={a.id}>
                        <button type="button" onClick={() => { setAnimal({ ...a, tutor: c.nome }); setBusca('') }}>
                          {a.nome} ({a.raca || a.especie}) — {c.nome}, {c.telefone}
                        </button>
                      </li>
                    )))}
                    {busca.length >= 2 && resultados.length === 0 && <li>Nada encontrado. <a href="#/clientes">Cadastrar cliente</a></li>}
                  </ul>
                </>
              )}
            </fieldset>

            <fieldset>
              <legend>2. Serviço e horário</legend>
              <Campo id="servico" rotulo="Serviço">
                <select id="servico" value={servicoId} onChange={(e) => { setServicoId(e.target.value); setProfId('') }}>
                  <option value="">Selecione…</option>
                  {servicos.map((s) => <option key={s.id} value={s.id}>{s.nome}</option>)}
                </select>
              </Campo>

              {servico && !hospedagem && (
                <>
                  <Campo id="prof" rotulo="Profissional">
                    <select id="prof" value={profId} onChange={(e) => setProfId(e.target.value)}>
                      <option value="">Selecione…</option>
                      {profsDoServico.map((p) => <option key={p.id} value={p.id}>{p.nome}</option>)}
                    </select>
                  </Campo>
                  <div className="linha">
                    <Campo id="data" rotulo="Data">
                      <input id="data" type="date" value={data} onChange={(e) => setData(e.target.value)} />
                    </Campo>
                    <Campo id="hora" rotulo="Horário">
                      <input id="hora" type="time" value={horaIni} onChange={(e) => setHoraIni(e.target.value)} />
                    </Campo>
                    <Campo id="duracao" rotulo="Duração (min)">
                      <input id="duracao" type="number" min="5" step="5" value={duracao} onChange={(e) => setDuracao(e.target.value)} />
                    </Campo>
                  </div>
                </>
              )}

              {hospedagem && (
                <>
                  <Campo id="baia" rotulo="Baia">
                    <select id="baia" value={baiaId} onChange={(e) => setBaiaId(e.target.value)}>
                      <option value="">Selecione…</option>
                      {baias.map((b) => <option key={b.id} value={b.id}>{b.nome}</option>)}
                    </select>
                  </Campo>
                  <div className="linha">
                    <Campo id="entrada" rotulo="Entrada">
                      <input id="entrada" type="date" value={data} onChange={(e) => setData(e.target.value)} />
                    </Campo>
                    <Campo id="horaEntrada" rotulo="Hora">
                      <input id="horaEntrada" type="time" value={horaIni || '08:00'} onChange={(e) => setHoraIni(e.target.value)} />
                    </Campo>
                  </div>
                  <div className="linha">
                    <Campo id="saida" rotulo="Saída">
                      <input id="saida" type="date" value={dataSaida} onChange={(e) => setDataSaida(e.target.value)} />
                    </Campo>
                    <Campo id="horaSaida" rotulo="Hora">
                      <input id="horaSaida" type="time" value={horaSaida} onChange={(e) => setHoraSaida(e.target.value)} />
                    </Campo>
                  </div>
                </>
              )}

              <Campo id="obs" rotulo="Observações (opcional)">
                <textarea id="obs" rows={2} value={obs} onChange={(e) => setObs(e.target.value)} />
              </Campo>

              {servico && (servico.tipo === 'Vacina' || servico.tipo === 'Consulta') && (
                <div className="linha">
                  <Campo id="retorno" rotulo="Próximo retorno (opcional)">
                    <input id="retorno" type="date" value={retorno} onChange={(e) => setRetorno(e.target.value)} />
                  </Campo>
                  <Campo id="descRetorno" rotulo="Do quê?">
                    <input id="descRetorno" value={descRetorno} onChange={(e) => setDescRetorno(e.target.value)} placeholder="Ex.: 2ª dose V10, vermífugo" />
                  </Campo>
                </div>
              )}
            </fieldset>
          </div>

          <aside aria-label="Informações do agendamento">
            {animal?.observacoes && (
              <div className="painel alerta-painel">
                <h2>Alertas do animal</h2>
                <p>⚠ {animal.observacoes}</p>
              </div>
            )}
            {servico && !hospedagem && profId && (
              <div className="painel">
                <h2>Horários livres em {data.split('-').reverse().join('/')}</h2>
                {livres.length === 0 ? <p>Nenhum horário livre neste dia.</p> : (
                  <div className="horarios">
                    {livres.map((h) => (
                      <button type="button" key={h} aria-pressed={horaIni === h} onClick={() => setHoraIni(h)}>{h}</button>
                    ))}
                  </div>
                )}
              </div>
            )}
            <div id="erros-agendamento" tabIndex={-1}><Erros erros={erros} /></div>
          </aside>
        </div>

        <div className="barra">
          <button type="submit" className="primario" disabled={salvando}>{salvando ? 'Salvando…' : 'Salvar'}</button>
          <a className="btn" href="#/agenda">Cancelar</a>
        </div>
      </form>
    </section>
  )
}

// Se a busca bate com o nome de algum animal, mostra só esse(s); senão, todos os animais do tutor.
function filtrarAnimais(cliente, busca) {
  const b = busca.trim().toLowerCase()
  const doNome = cliente.animais.filter((a) => a.nome.toLowerCase().includes(b))
  return doNome.length ? doNome : cliente.animais
}

function somarMinutos(isoLocal, min) {
  const [d, t] = isoLocal.split('T')
  const [y, m, dd] = d.split('-').map(Number)
  const [hh, mi] = t.split(':').map(Number)
  const x = new Date(y, m - 1, dd, hh, mi + min)
  const p = (n) => String(n).padStart(2, '0')
  return `${x.getFullYear()}-${p(x.getMonth() + 1)}-${p(x.getDate())}T${p(x.getHours())}:${p(x.getMinutes())}:00`
}
