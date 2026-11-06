import React from 'react'
import { SERVICOS } from './api.js'

/** Etiqueta do serviço: cor + texto (a cor nunca é a única informação). */
export function EtiquetaServico({ tipo }) {
  const s = SERVICOS[tipo] ?? { rotulo: tipo, classe: '' }
  return <span className={`etiqueta ${s.classe}`}>{s.rotulo}</span>
}

/** Mensagens de erro anunciadas por leitores de tela. */
export function Erros({ erros }) {
  if (!erros || erros.length === 0) return null
  return (
    <div className="aviso erro" role="alert">
      <strong>Não foi possível salvar:</strong>
      <ul>{erros.map((e, i) => <li key={i}>{e}</li>)}</ul>
    </div>
  )
}

export function Sucesso({ texto }) {
  if (!texto) return null
  return <div className="aviso ok" role="status">{texto}</div>
}

/** Campo com rótulo sempre associado ao controle. */
export function Campo({ id, rotulo, ajuda, children }) {
  return (
    <div className="campo">
      <label htmlFor={id}>{rotulo}</label>
      {children}
      {ajuda && <small id={`${id}-ajuda`}>{ajuda}</small>}
    </div>
  )
}

export function Carregando() {
  return <p role="status">Carregando…</p>
}
