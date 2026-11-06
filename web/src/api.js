// Acesso à API REST (ASP.NET Core).
async function req(method, url, body) {
  const r = await fetch(url, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined
  })
  if (r.status === 204) return null
  const data = await r.json().catch(() => null)
  if (!r.ok) {
    const erros = data?.erros ?? (data?.title ? [data.title] : ['Erro ao comunicar com o servidor.'])
    const e = new Error(erros.join(' '))
    e.erros = erros
    e.status = r.status
    throw e
  }
  return data
}

export const api = {
  get: (u) => req('GET', u),
  post: (u, b) => req('POST', u, b),
  put: (u, b) => req('PUT', u, b),
  patch: (u, b) => req('PATCH', u, b)
}

// ---------- utilitários de data ----------
export const hojeISO = () => isoData(new Date())
export function isoData(d) {
  const p = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`
}
export function somaDias(iso, n) {
  const [y, m, d] = iso.split('-').map(Number)
  return isoData(new Date(y, m - 1, d + n))
}
export const hora = (dt) => dt.slice(11, 16)
export function dataBR(iso) {
  if (!iso) return ''
  const [y, m, d] = iso.slice(0, 10).split('-')
  return `${d}/${m}/${y}`
}
export function diaSemana(iso) {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d).toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' })
}

// ---------- serviços: cor sempre acompanhada de texto ----------
export const SERVICOS = {
  BanhoTosa: { rotulo: 'Banho e tosa', classe: 'svc-banho' },
  Consulta: { rotulo: 'Consulta', classe: 'svc-consulta' },
  Vacina: { rotulo: 'Vacina', classe: 'svc-vacina' },
  Hospedagem: { rotulo: 'Hospedagem', classe: 'svc-hosp' }
}

export const STATUS = ['Agendado', 'Confirmado', 'Realizado', 'Falta', 'Cancelado']

// ---------- WhatsApp: link de mensagem pronta ----------
export function linkWhatsApp(telefone, texto) {
  let n = (telefone || '').replace(/\D/g, '')
  if (n.length === 10 || n.length === 11) n = '55' + n
  return `https://wa.me/${n}?text=${encodeURIComponent(texto)}`
}

export function mensagemConfirmacao(item, nomeLoja) {
  const dia = dataBR(item.inicio)
  if (item.tipo === 'Hospedagem')
    return `Olá, ${item.tutor}! Aqui é do ${nomeLoja}. Confirmando a hospedagem do(a) ${item.animal} com entrada em ${dia} às ${hora(item.inicio)}. Podemos confirmar?`
  return `Olá, ${item.tutor}! Aqui é do ${nomeLoja}. Passando para confirmar o horário de ${SERVICOS[item.tipo].rotulo.toLowerCase()} do(a) ${item.animal} em ${dia} às ${hora(item.inicio)}. Podemos confirmar?`
}

export function mensagemRetorno(r, nomeLoja) {
  const desc = r.descricaoRetorno || SERVICOS[r.tipo]?.rotulo.toLowerCase() || 'retorno'
  return `Olá, ${r.tutor}! Aqui é do ${nomeLoja}. Está chegando a data de ${desc} do(a) ${r.animal} (${dataBR(r.proximoRetorno)}). Quer agendar um horário?`
}

export const NOME_LOJA = 'Isadora Pezatti PetShop'
