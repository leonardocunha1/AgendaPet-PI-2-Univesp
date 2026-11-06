# Agenda Integrada para Petshop

Sistema web de agendamento unificado (banho e tosa, consulta veterinária, vacinação e hospedagem),
desenvolvido no Projeto Integrador em Computação II (PJI240) da UNIVESP.

## O que o sistema faz

- **Cadastro único** de tutor (com consentimento LGPD) e animal (com alertas: alergias, comportamento, focinheira).
- **Agenda do dia** com os quatro serviços em uma só tela, uma coluna por profissional + hospedagem. Cada serviço tem cor **e** rótulo em texto.
- **Bloqueio automático de conflitos**: o mesmo profissional, a mesma baia ou o mesmo animal não podem ter dois atendimentos ao mesmo tempo. O motivo aparece em texto.
- **Horários livres** do profissional sugeridos ao agendar.
- **Hospedagem por período** (entrada e saída em dias diferentes) com ocupação por baia.
- **Retornos** de vacina, vermífugo e consulta da semana, com aviso pelo WhatsApp (link de mensagem pronta).
- **Confirmação pelo WhatsApp** de cada horário (link de mensagem pronta, enviada pela atendente).
- **Histórico** completo de cada animal.
- **Indicadores**: total de agendamentos, faltas, conflitos evitados pelo sistema.
- Interface responsiva (funciona no celular da equipe) e com critérios de acessibilidade (WCAG 2.1): rótulos em todos os campos, navegação por teclado, contraste adequado, erros em texto.

## Tecnologias

| Camada | Tecnologia |
|---|---|
| API REST | C# / ASP.NET Core 8 (Minimal APIs) |
| Acesso a dados | Entity Framework Core |
| Banco | SQLite (arquivo `agendapet.db`, criado automaticamente) |
| Interface web | React 18 + Vite |
| Testes | xUnit (regras de conflito) |
| CI | GitHub Actions |
| Contêiner | Docker (multi-stage) + Docker Compose |

## Como rodar (Windows)

Pré-requisito: [.NET SDK 8 ou mais recente](https://dotnet.microsoft.com/download).

1. Dê dois cliques em `iniciar.bat` **ou** rode:
   ```
   cd src\AgendaPet.Api
   dotnet run
   ```
2. Abra **http://localhost:5000**.

O front-end já vem compilado em `src/AgendaPet.Api/wwwroot`. Só é preciso Node.js para alterar as telas:

```
cd web
npm install
npm run dev      # desenvolvimento em http://localhost:5173 (a API precisa estar rodando)
npm run build    # gera de novo a pasta wwwroot
```

### Rodar com Docker

Pré-requisito: [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```
docker compose up -d --build
```

Abra **http://localhost:5000**. O build compila o front-end, roda os testes xUnit e gera uma imagem só com o runtime.
Os dados ficam no volume `agendapet-dados`, preservados entre atualizações. Para parar: `docker compose down`.
A mesma imagem pode ser publicada em um serviço de nuvem que aceite contêineres.

### Modo demonstração (dados fictícios)

Para apresentar ou gravar vídeo sem expor dados reais, use `iniciar-demo.bat`.
Ele cria um banco separado (`agendapet-demo.db`) com tutores, animais e agendamentos fictícios para hoje, ontem e amanhã.
O banco real (`agendapet.db`) não é alterado. Para recomeçar a demonstração do zero, apague `agendapet-demo.db`.

### Acessar pelo celular (mesma rede Wi-Fi)

1. No PC, descubra o IP com `ipconfig` (ex.: `192.168.0.15`).
2. No celular, abra `http://192.168.0.15:5000`.
3. Se não abrir, permita o acesso quando o Firewall do Windows perguntar (rede privada).

### Primeiro uso

1. Em **Configurações**, troque os nomes dos profissionais, marque os serviços que cada um faz e ajuste as durações e as baias.
2. Em **Clientes e animais**, cadastre os tutores e os animais.
3. Use **Novo agendamento** e a **Agenda do dia** no dia a dia.

### Backup

Todos os dados ficam em `src/AgendaPet.Api/agendapet.db`. Para fazer backup, copie esse arquivo com o sistema fechado.

## Testes

```
dotnet test tests/AgendaPet.Tests
```

## Estrutura

```
src/AgendaPet.Api/
  Models/        entidades (Cliente, Animal, Servico, Profissional, Baia, Agendamento)
  Domain/        RegrasAgendamento: regra de conflito e horários livres (sem banco, testável)
  Data/          DbContext (EF Core) e dados iniciais
  Endpoints/     rotas da API REST
  wwwroot/       front-end compilado
web/             código-fonte do front-end React
tests/           testes xUnit
```

## Rotas principais da API

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/agenda?data=AAAA-MM-DD` | atendimentos do dia |
| POST | `/api/agendamentos` | cria agendamento (409 em caso de conflito) |
| PUT | `/api/agendamentos/{id}` | altera agendamento |
| PATCH | `/api/agendamentos/{id}/status` | Agendado / Confirmado / Realizado / Falta / Cancelado |
| GET | `/api/horarios-livres` | horários livres do profissional |
| GET | `/api/hospedagem?data=` | ocupação das baias |
| GET | `/api/retornos?dias=7` | retornos previstos |
| GET | `/api/indicadores` | indicadores de uso |
| GET/POST/PUT | `/api/clientes`, `/api/animais`, `/api/profissionais`, `/api/baias` | cadastros |

## Limitações conhecidas

- Sem login: pensado para rodar no computador do próprio estabelecimento, na rede local.
- O lembrete pelo WhatsApp é um link de mensagem pronta; o envio é feito pela atendente.
- Não há aplicativo móvel nativo: a equipe usa a versão web responsiva no celular.
