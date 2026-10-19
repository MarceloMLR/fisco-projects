# Proposta: linha-do-tempo (Cronograma-3)

## Why

A aba Cronograma só tem a Lista. Os ADR-0021 e ADR-0071 pedem a Linha do tempo: as barras no tempo, arrastadas e redimensionadas no computador, com zoom, ajuste ao espaço e a opção de expandir. É a visão em que o gerente planeja e acompanha o projeto, e as próximas changes do cronograma (linha de base, caminho crítico, desfazer e exportação) se apoiam nela.

## What Changes

- **Linha do tempo** na aba Cronograma, que alterna com a Lista (ADR-0071):
  - à esquerda, o nome e o progresso de cada fase, tarefa e marco;
  - à direita, as fases como barras de resumo, as tarefas com a parte concluída em verde (ADR-0067), os marcos como losangos, as setas de dependência, os dias não úteis sombreados e a linha de hoje;
  - abre na Linha do tempo no computador e na Lista no celular;
  - a palavra "Gantt" não aparece.
- **Escala e ajuste ao espaço:** Dias, Semanas e Meses. O período ocupa toda a largura; se não couber, a rolagem lateral fica só dentro da linha do tempo. "Hoje" leva a linha do tempo ao dia de hoje.
- **Arrastar no computador**, para quem edita o cronograma (ADR-0021, ADR-0043):
  - arrastar a barra muda o início e mantém a duração;
  - puxar a borda muda a duração;
  - ligar o fim de uma barra ao início de outra cria a dependência.
  - O servidor recalcula as datas e empurra as seguintes; a tela mostra o que ele devolve (ADR-0051).
- **Expandir:** um diálogo que cobre quase a tela inteira, com a tabela completa (início, fim, responsável e progresso) ao lado da linha do tempo.
- **API:**
  - `PUT /schedule/items/{itemId}/dates`, com o início e, numa tarefa, o fim; o servidor conta os dias úteis;
  - o cronograma passa a trazer os dias não úteis da instalação no período, para a tela não repetir a regra do calendário;
  - `GET /api/client-settings` entrega a chave do Syncfusion da configuração.
- **Componente `ScheduleGantt`:** o único que importa `@syncfusion/*`, carregado só quando a Linha do tempo abre.
- **ADR-0087** (novo, proposta): a chave do Syncfusion chega ao navegador pela API, lida da configuração da instalação.

## Non-goals

- "Exibir", linha de base e caminho crítico: Cronograma-4.
- Desfazer e refazer: Cronograma-5.
- Exportar e o menu "⋮": Cronograma-6.
- Editar nas células da tabela ao lado: a edição continua no detalhe (ADR-0081).
- A linha do tempo menor da Visão geral e a faixa de fases de Meu trabalho (ADR-0070, ADR-0071): ficam com as telas de síntese.
- Arrastar no celular (ADR-0021: arrastar é no computador).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: a aba com Linha do tempo e Lista, a Linha do tempo, a escala e o ajuste ao espaço, o arrastar e o ligar das barras, Expandir, as novas datas pela API e os dias não úteis no cronograma.

## Impact

- **Backend (`Projects`):**
  - `Schedule.Reschedule` (início e, numa tarefa, o fim), o caso de uso `RescheduleScheduleItem` e a rota `PUT /schedule/items/{itemId}/dates`;
  - os dias não úteis do período em `GET /schedule`, pelo `IBusinessDays` do `Calendar`.
- **Host da API:** `GET /api/client-settings`, com a chave da configuração `Syncfusion:LicenseKey`.
- **Contrato OpenAPI** e tipos do frontend.
- **Frontend:**
  - pacotes `@syncfusion/ej2-react-gantt` e `@syncfusion/ej2-material3-theme`, só em `web/src/schedule-gantt`;
  - regra de lint que proíbe `@syncfusion/*` fora dele;
  - a aba Cronograma com a barra de ferramentas, a Linha do tempo e Expandir;
  - os textos e os testes.
- **Build e CI:**
  - o tema do Syncfusion sem a fonte do Google (ADR-0064);
  - a verificação de licenças do frontend aceita os pacotes `@syncfusion/*`, pelo ADR-0051.
- **Documentação:** ADR-0087, glossário, `CLAUDE.md`, `docs/instalacao.md` (a chave) e roadmap.
