## Why

O gerente que monta o status report compara, tarefa a tarefa, o fim combinado com o cliente e o dia em que a tarefa de fato terminou, como faz no MS Project (Término da linha de base e Término real). No GerenWare, o fim da linha de base só aparece no detalhe de cada item e no Excel, longe do fim, e o dia da conclusão (ADR-0020) não aparece em lugar nenhum da Lista: a tarefa concluída com atraso mostra só o fim planejado. Status-3, do grupo 18 (Status do projeto), pedido por Marcelo em 19/11/2026 junto com o relatório de status (ADR-0165).

## What Changes

- **Fim previsto**: o fim do item na linha de base (o dia do marco), já devolvido pela API (`baselineEnd`); vazio no item incluído depois da linha de base (ADR-0021).
- **Fim real**: o dia da conclusão da tarefa (`completedOn`, gravado ao chegar a 100%, ADR-0020; na tarefa-resumo, o da última subtarefa) e o dia em que o marco foi atingido (`reachedOn`); vazio no item em aberto.
- Na linha da fase, o fim previsto é o maior fim previsto dos itens dela, e o fim real, o maior fim real quando todas as tarefas e marcos dela estão concluídos, com a regra num lugar só na tela (`schedule.ts`).
- Na Lista, as colunas "Fim previsto" (só com a linha de base salva) e "Fim real" logo depois de "Fim", só para ler, também no modo de edição na tabela (ADR-0143).
- No Expandir, as mesmas colunas na tabela completa, depois de "Fim"; o "Fim previsto" segue "Exibir → Linha de base" (ADR-0071).
- No Excel, "Fim previsto (linha de base)" e "Fim real" logo depois de "Fim"; a coluna "Fim na linha de base" vira o "Fim previsto", e "Início na linha de base" e "Diferença da linha de base" ficam onde estão.
- Sem mudança na API nem nas regras do servidor: as datas já vêm no `GET /schedule`.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: o fim previsto e o fim real, a Lista, o Expandir e a exportação para o Excel.

## Non-goals

- Informar ou corrigir à mão o dia da conclusão ou do marco atingido (hoje, o sistema grava o dia em que a tarefa chega a 100% ou o marco é atingido, ADR-0020).
- O início real, a diferença entre o fim real e o previsto e o "Término real" no XML do MS Project e no PDF.
- Mudar a diferença da linha de base, que continua comparando o fim de hoje com o da linha de base (ADR-0021).

## Impact

- `web/src/projects`: as colunas da Lista (`ProjectSchedulePage.tsx`), o Excel (`scheduleReport.ts`) e as regras de tela em `schedule.ts` (`plannedEndOf`, `actualEndOf`).
- `web/src/schedule-gantt`: as colunas da tabela no Expandir (`TimelineTable`, `ganttRows.ts`, `ScheduleGantt.css`).
- Textos em `pt-BR.json`; testes de tela, do Excel e das linhas da Linha do tempo; `CLAUDE.md` e o roadmap.
