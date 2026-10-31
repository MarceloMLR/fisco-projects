## Why

A Linha do tempo do cronograma usa o Gantt do Syncfusion pela licença comunitária (ADR-0051), que só cobre empresas pequenas: cada comprador maior teria de comprar a licença dele, e a chave passa pelo Key Vault, pelo Terraform, pelo Codespaces e pela API (ADR-0087). Além disso, o visual vem de CSS por cima do tema dele, ele só funciona com uma Linha do tempo montada por vez e os testes de tela usam um simulador no lugar dele. Em 31/10/2026, com a v1 concluída, Marcelo pediu o Gantt próprio, previsto no ADR-0051 como evolução, para tirar a dependência de licença antes de vender e abrir caminho para importar do MS Project (ADR-0134). Linha do tempo própria, Gantt-1.

## What Changes

- A Linha do tempo passa a ser desenhada por um componente do FiscoProjects, com o mesmo comportamento e o mesmo visual de hoje, que segue o componente Gantt do design system: a tabela ao lado, as escalas Dias, Semanas e Meses ajustadas ao espaço, a linha de hoje, os dias não úteis, as fases, as tarefas, as subtarefas e os marcos, o progresso, a linha de base, o caminho crítico, as setas das dependências, a escolha da linha e o Expandir.
- Quem edita, no computador, continua arrastando a barra, puxando a borda e ligando o fim de uma barra ao início de outra; nada muda até a API responder, e a barra volta se ela recusar.
- As fases e as tarefas-resumo continuam se recolhendo pela seta ao lado do nome.
- A tabela passa a ser navegável pelo teclado, e cada barra tem um nome para o leitor de tela.
- A Linha do tempo aparece completa em qualquer instalação, sem chave de licença e sem aviso de versão de avaliação.
- Saem do frontend os pacotes `@syncfusion/*`, o CSS do tema dele, a paleta, a remoção da fonte do Google no build e a espera do Expandir pela Linha do tempo da aba.

## Capabilities

### New Capabilities

- Nenhuma.

### Modified Capabilities

- `cronograma`: recolher na Linha do tempo, a Linha do tempo pelo teclado e sem licença de terceiros.

## Non-goals

- Tirar a chave do Syncfusion da instalação (Key Vault, Terraform, Codespaces e `GET /api/client-settings`): Gantt-2.
- Importar do MS Project: Gantt-3.
- Mudar o comportamento do cronograma: os tipos de dependência, o progresso pela barra (ADR-0086), mover linhas ou as regras de quem edita.
- Mudar o visual aprovado da Linha do tempo.

## Impact

- `web/src/schedule-gantt`: o `ScheduleGantt` reescrito, com a tabela, o gráfico e as interações; o `ganttLayout.ts` e o `ganttRows.ts` sem as conversões para o Syncfusion; saem o `ScheduleGantt.css` do tema, o `syncfusionPalette.ts` e o `ganttTesting.tsx`.
- `web/src/projects`: o `ScheduleTimeline` sem a licença, e o `ProjectSchedulePage` sem a espera do Expandir; os testes de tela passam a usar o componente real.
- `web/package.json`, `web/vite.config.ts`, `web/eslint.config.js` e `scripts/ci/frontend.sh`: sem o Syncfusion.
- Design system: o guia do componente Gantt ("No código").
- ADRs: 0051, 0071, 0087, 0134 (novo).
