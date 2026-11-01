## Context

O `GET /schedule` traz o cronograma inteiro, com o atraso (`late`, `lateDays`), o caminho crítico (`critical`), o responsável (`owner`, `ownedByClient`), a data-limite (`deadline`, `deadlineDeviation`) e o dia de hoje da instalação (`today`). A Lista monta as linhas por `scheduleRows` e a Linha do tempo por `ganttRowsOf`, ambas a partir do `Schedule`; a visão e a escala já ficam no endereço (`?view=`, `?scale=`). O "Exibir" é um botão com menu (`DisplayMenu`).

## Goals / Non-Goals

**Goals:**

- Um filtro por vez, igual na Lista, na Linha do tempo e no Expandir, sem nenhuma mudança na API.
- Não perder o lugar: o item filtrado aparece sob a fase e, se for subtarefa, sob a tarefa dele.

**Non-Goals:**

- Combinar ou salvar filtros, filtrar a exportação.

## Decisions

- **Só na tela.** O cronograma já vem inteiro; filtrar no servidor só mudaria o lugar da regra e faria cada filtro pedir de novo à API. A regra fica num lugar só, `scheduleFilter.ts`: `matchesFilter` (o item passa ou não) e `filteredSchedule`, que devolve um `Schedule` com as mesmas fases e os mesmos números, só com os itens que passam e as tarefas-resumo deles. A Lista e a Linha do tempo recebem esse `Schedule`, então as linhas, a árvore e as setas saem do código que já existe; o detalhe, a inclusão e o resumo continuam com o cronograma inteiro.
- **Os filtros:**
  - Atrasadas: `late`, como o resto da tela já conta;
  - Datas-limite estouradas: `missesDeadline` (GanttA-2);
  - Caminho crítico: `critical`;
  - Minhas tarefas: o responsável é a pessoa na sessão;
  - Próximas duas semanas: o item em aberto (`isDone` falso) cujo início vem até 13 dias corridos depois de hoje e cujo fim não vem antes de hoje — o que está acontecendo ou vai começar; o atrasado fica no filtro "Atrasadas";
  - Por responsável: a pessoa (`owner.id`), o cliente (`ownedByClient`) ou ninguém. As opções são as pessoas que cuidam de alguma tarefa do cronograma, por nome, o cliente quando o projeto tem cliente e "Sem responsável".
- **Contexto.** Uma subtarefa que passa traz a tarefa-resumo dela; a fase aparece quando tem algum item. A tarefa-resumo que passa sozinha aparece sem as subtarefas que não passam.
- **Período.** A Linha do tempo mantém o período do cronograma inteiro, para o filtro não mexer na escala nem na rolagem.
- **Endereço.** `?filter=late|missedDeadlines|critical|mine|nextTwoWeeks` e, por responsável, `?filter=owner&owner={id}|client|none`; um valor que não existe é ignorado.
- **Na tela.** O "Filtrar" é um ícone ao lado do "Exibir", com o menu de opções exclusivas (`menuitemradio`): a barra da Linha do tempo já ocupa a largura toda num computador de 1440 px, e um botão com texto a quebraria em duas linhas. Com um filtro, o ícone fica marcado e o nome dele diz "Filtro: Atrasadas"; o resumo da aba diz "Atrasadas · 4 de 20 tarefas e marcos", com "Limpar filtro", e o rodapé da Lista, "4 de 20 tarefas e marcos". No Expandir, o mesmo ícone no cabeçalho.

## Risks / Trade-offs

- [Arrastar uma barra com o filtro ligado empurra itens que não aparecem] → É o mesmo cálculo de sempre no servidor (ADR-0019); o aviso do empurrão já diz quantos foram empurrados.
- [Um item que deixa de passar some depois de salvar, como a tarefa que deixa de atrasar] → É o esperado de um filtro; o aviso da gravação continua aparecendo.
