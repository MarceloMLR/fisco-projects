## Context

O `ScheduleGantt` (`web/src/schedule-gantt`) isola o Syncfusion atrás das propriedades do FiscoProjects: o cronograma da API (`schedule`), a escala, quem edita, o modo completo do Expandir, a linha escolhida, a linha de base e o caminho crítico ligados em "Exibir", o nome do cliente (ADR-0102), a licença e os eventos `onChoose`, `onMove`, `onResize` e `onLink`, além do `goToToday`. O `ScheduleTimeline` o carrega sob demanda e trata os eventos, chamando a API e pedindo o cronograma de novo. As linhas saem do `ganttRowsOf` (fases, tarefas, subtarefas, marcos, a parte crítica e a linha de base) e a escala do `ganttPeriod` e do `ganttUnitWidth` (ADR-0071). A página de impressão e a Visão geral já desenham barras em SVG com os tokens do Gantt, pelas contas de `schedulePrint.ts`. O componente Gantt do design system descreve o desenho: linhas de 40px, barras de 18px com contorno de 1px, fase fina e escura com pontas, marco em losango, linha de base fina embaixo, caminho crítico em vermelho com o concluído em verde, a linha tracejada de hoje com "Hoje", os dias não úteis sombreados e a linha escolhida em `bg-selected`.

## Goals / Non-Goals

**Goals:**

- O mesmo comportamento das specs e o mesmo desenho, sem o Syncfusion, com as mesmas propriedades e eventos, para que a aba Cronograma e o Expandir não mudem.
- Acessível pelo teclado e pelo leitor de tela.
- Testável no jsdom, sem simulador.

**Non-Goals:**

- Novos recursos de cronograma; a chave da instalação (Gantt-2) e a importação (Gantt-3).

## Decisions

- **Mesmas propriedades, um componente novo.** O `ScheduleGantt` mantém a assinatura de hoje, menos a `license`, e é trocado por dentro. O `ScheduleTimeline` deixa de passar a licença (a consulta da chave fica até a Gantt-2). Assim a troca não toca as telas.
- **Tabela e gráfico lado a lado, numa rolagem só.** Um contêiner com a altura disponível rola na vertical; dentro dele, a tabela à esquerda (larguras de hoje: 320px, 560px no Expandir, 45% no celular) e o gráfico à direita, que rola sozinho para o lado. O cabeçalho das duas partes fica preso no alto (`position: sticky`). Alternativa: dois painéis com a rolagem sincronizada por código, como o Syncfusion, que exige esconder barras de rolagem e corrigir a largura (o problema que o CSS de hoje contorna).
- **Tabela como árvore acessível.** `role="treegrid"`, com `aria-level`, `aria-expanded` e a linha ativa por `aria-activedescendant`; setas para cima e para baixo trocam a linha, para a esquerda e a direita recolhem e abrem, Enter escolhe (abre o detalhe, como o clique). A seta ao lado do nome recolhe e abre sem escolher a linha, como na Lista (ADR-0088).
- **Barras em HTML, setas e hoje em SVG.** Cada linha do gráfico é um elemento com a altura da linha; os dias não úteis são faixas por coluna, a barra, o marco e a linha de base são elementos posicionados por `left` e `width` em pixels, a partir do dia e da largura da unidade. As setas das dependências (do fim de uma barra ao início da outra, com o cotovelo do design system) e a linha de hoje ficam numa camada SVG por cima, sem receber clique. HTML nas barras deixa o foco, o nome acessível e o arrastar simples; SVG nas linhas finas deixa as setas precisas. Alternativa: tudo em SVG, como a impressão, que complica o foco e o arrastar; ou `canvas`, que não é acessível.
- **A escala de hoje.** `ganttPeriod` e `ganttUnitWidth` continuam decidindo o período e a largura de cada dia, semana ou mês, com o ajuste ao espaço medido pelo `ResizeObserver`. Os cabeçalhos ("Out 2026" em cima, os dias, as semanas ou os meses embaixo) saem de um `timelineHeader` testado sem tela. As linhas continuam do `ganttRowsOf`, que deixa de converter para `Date` e passa a dar os dias (`DayValue`) e a ordem com o nível de cada linha.
- **Arrastar pelos eventos de ponteiro.** No `pointerdown` numa barra (com o botão principal e só para quem edita, no computador), a barra entra em rascunho; o `pointermove` a desloca em dias inteiros (a largura da unidade dividida pelos dias dela), e a borda, até 6px das pontas de uma tarefa, muda o fim ou o início. No `pointerup`, se mudou, o evento vai com o novo início, ou o início e o fim, e a barra fica no rascunho até o cronograma voltar da API; numa recusa, o cronograma de antes volta e a barra com ele. Um clique sem movimento escolhe a linha. Durante o arrastar, a barra ganha `shadow-drag` e o ponteiro é capturado (`setPointerCapture`).
- **Ligar pela ponta.** Quem edita vê, ao passar sobre uma barra, a ponta de ligação no fim dela; arrastar dessa ponta desenha uma linha provisória até o ponteiro, e soltar sobre outra barra de tarefa ou marco com datas próprias manda `onLink(origem, destino)`. A tarefa-resumo e as fases não ligam (ADR-0088).
- **Hoje.** `goToToday` rola o gráfico até deixar a linha de hoje à vista.
- **Desenho só pelos tokens.** O estilo fica num `sx` por parte (ou no CSS do componente, sem cores escritas), com os tokens `gantt-*`, `bg-selected`, `shadow-drag` e as fontes do design system, nos dois temas, sem trocar a paleta em tempo de execução.
- **Muitas linhas.** Cronogramas de até algumas centenas de linhas desenham inteiros; as linhas são memorizadas por item, e o arrastar mexe só na barra em rascunho. Se o desempenho pedir, as linhas fora da vista podem deixar de ser desenhadas depois, sem mudar a interface.
- **Sem o Syncfusion no projeto.** Saem os pacotes, o CSS, a paleta, o `Telemetry.disable()`, a remoção da fonte do Google no `vite.config.ts` e a espera do Expandir (`expandedLeft`). O lint passa a recusar `@syncfusion/*` em todo o `web/src`, e um teste confere que o `package.json` não o tem. O `frontend.sh` continua reprovando a fonte do Google no build (ADR-0064).

## Risks / Trade-offs

- [O arrastar e a rolagem passam a ser nossos] → testes de ponteiro no jsdom (arrastar, borda, ligar, clique sem movimento, recusa) e conferência no navegador, no computador e no celular.
- [Diferenças pequenas de desenho em relação ao Syncfusion] → a referência é o componente Gantt do design system, conferido lado a lado nos dois temas.
- [Cronogramas muito grandes] → medidos com o maior cronograma da demonstração; as linhas fora da vista ficam como próxima melhoria, se preciso.
