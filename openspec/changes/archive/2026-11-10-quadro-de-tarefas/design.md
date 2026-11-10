## Context

A aba Cronograma (`ProjectSchedulePage`) alterna a Linha do tempo e a Lista pelo `?view=` do endereço, com o filtro (`?filter=`), o detalhe da tarefa numa gaveta (`?item=` ou a escolha na tela) e a barra com "Filtrar", o desfazer, "Nova fase", "Nova tarefa" e o "⋮". Cada tarefa do `GET /schedule` traz o progresso, o dia da conclusão, o responsável, o atraso e `canUpdateProgress`; o progresso muda pelo `PUT .../progress`, que o detalhe ("Iniciar", "Atualizar progresso", "Concluir") e o Meu trabalho já usam (`completeTask`, `ProgressDialog`). O design system já prevê o quadro nos tokens: `bg-subtle` para as colunas e `radius-md` para os cartões.

## Goals / Non-Goals

**Goals:** ver as tarefas em colunas pelo progresso e mover o cartão para mudar o progresso, com as regras e as rotas que já existem, também pelo teclado e no celular.

**Non-Goals:** tarefas fora do cronograma, colunas próprias, raias, ordem à mão e os marcos no quadro.

## Decisions

- **A visualização.** `ScheduleView` ganha `'board'`, com a opção "Quadro" (ícone `ViewKanbanOutlined`) no `SegmentedControl` das visualizações. No quadro, a barra tem "Filtrar", o desfazer, "Nova fase", "Nova tarefa" e o "⋮"; a escala, "Hoje", "Exibir", "Expandir" e "Editar na tabela" são da Linha do tempo e da Lista. O resumo (progresso, atrasos, linha de base e filtro) fica acima das colunas.
- **Regras num lugar só.** `scheduleBoard.ts`: `boardColumns` (as colunas com os cartões, na ordem), `columnOf` (a coluna pelo progresso), `moveOf` (o que mover para uma coluna faz: concluir, voltar a 0% ou pedir o percentual) e o tipo do cartão (a tarefa, a fase e a tarefa-mãe). Entram as tarefas sem subtarefas; a tarefa-resumo (`hasSubtasks`) e os marcos ficam fora.
- **O componente.** `ScheduleBoard` recebe o cronograma já filtrado, o projeto, a tarefa escolhida e `onChoose`. Cada coluna é uma `section` com o título (ícone, texto e a quantidade) e uma lista que rola por dentro; no computador, as três colunas dividem a largura; abaixo de `md`, ficam lado a lado com a largura de quase a tela e a rolagem lateral com encaixe.
- **O cartão.** Um botão com o nome abre o detalhe (o nome acessível do cartão é o da tarefa); o resto do cartão também abre pelo clique. Mostra a nota (`NoteMark`), a fase e a tarefa-mãe, o responsável (`CardAvatar`, o logo do cliente ou "Sem responsável", com o aviso de ausência), o fim ou o atraso (`DueDate`), a barra do progresso em Em andamento e "Concluída em" em Concluída. O escolhido fica em `bg-selected`.
- **Os ícones das colunas.** A fazer com `RadioButtonUncheckedOutlined` (neutro), Em andamento com `AutorenewOutlined` (info) e Concluída com `CheckCircleOutlined` (sucesso), os mesmos sentidos dos status do produto (ADR-0065).
- **Mover.** Com `canUpdateProgress`: no computador, o arrastar nativo do navegador (`draggable`, `dragover`, `drop`), com a coluna sob o cartão marcada; em qualquer tela, "Mover" (um `IconButton` com o menu das outras colunas). Concluída pede o progresso 100 (`completeTask`); A fazer, o progresso 0; Em andamento abre o `ProgressDialog` ("Iniciar tarefa" a 0%, "Atualizar progresso" a 100%). Enquanto o pedido não volta, o cartão aparece na coluna nova com `aria-busy`; com a resposta, o cronograma é pedido de novo e o rascunho sai; na recusa, o rascunho sai e o aviso de erro aparece.
- **Avisos.** "Tarefa X concluída." (o de "Concluir"), "Tarefa X voltou para A fazer." e, pelo diálogo, "Progresso de X atualizado para N%.".

## Risks / Trade-offs

- [O arrastar nativo não funciona no toque] → no celular, "Mover" faz o mesmo; o arrastar é um atalho do computador.
- [Em andamento pede o percentual, um passo a mais que no Jira] → é a regra do ADR-0086: nada vira "em andamento" sem dizer quanto.
- [Concluída cresce com o projeto] → a coluna rola por dentro, com as mais recentes no alto.
