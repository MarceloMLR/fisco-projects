## Context

O `GET /schedule` já traz, em cada tarefa e marco, o fim na linha de base (`baselineStart`, `baselineEnd`, `baselineDeviation`, ADR-0021) e o dia da conclusão (`completedOn`, a tarefa-resumo com o da última subtarefa) ou do marco atingido (`reachedOn`, ADR-0020). A fase traz o início, o fim, o progresso e o atraso, calculados no servidor, mas nada da linha de base nem da conclusão. A Lista é um `DataTable` com colunas fixas; o Expandir usa a tabela da Linha do tempo (`TimelineTable`, colunas `detailed`), e o Excel sai do cronograma da aba pelo `scheduleReport`.

## Goals / Non-Goals

**Goals:**

- O fim previsto e o real lado a lado com o fim, nos três lugares, pelas mesmas regras de tela.
- Nenhuma mudança na API, no domínio ou no que já é gravado.

**Non-Goals:**

- Editar o dia da conclusão; o início real; o fim real no XML do MS Project e no PDF.

## Decisions

1. **Regras na tela, num lugar só.** `plannedEndOf(item)` devolve o `baselineEnd`, e `actualEndOf(item)`, o `completedOn` da tarefa ou o `reachedOn` do marco; `phaseEndsOf(phase)` devolve o maior fim previsto dos itens da fase e, quando todas as tarefas com datas próprias, as tarefas-resumo e os marcos dela estão concluídos, o maior fim real. Ficam em `schedule.ts`, usados pela Lista, pelo Expandir (pelas linhas do `ganttRows.ts`) e pelo Excel. Alternativa: calcular a fase no servidor, como o fim e o progresso dela; descartada porque são só os dados que a resposta já traz, sem dia útil nem calendário, e mudar a API traria contrato novo sem regra nova.
2. **Lista.** "Fim previsto" e "Fim real" entram logo depois de "Fim", com as datas pelo `formatDayText`, sem texto de diferença (a diferença fica no detalhe e no Excel). "Fim previsto" sai das colunas quando `schedule.baseline` é nulo, como a coluna "Horas" sai para quem não as vê. As duas ficam fora do `editableFields` do modo de edição na tabela.
3. **Expandir.** A tabela `detailed` ganha as duas colunas depois de "Fim"; o "Fim previsto" aparece com a linha de base salva e `showBaseline` ligado, o mesmo que desenha a barra fina da linha de base. A tabela compacta da aba e a do celular não mudam. Larguras pelos tokens no `ScheduleGantt.css`, como as outras colunas.
4. **Excel.** As colunas passam a ser: Início, Fim, Fim previsto (linha de base), Fim real, Duração e as demais; a antiga "Fim na linha de base" é a nova "Fim previsto", e "Início na linha de base" e "Diferença da linha de base" ficam no fim, como hoje. Datas como datas do Excel (`day`).

## Risks / Trade-offs

- [Mais duas colunas numa Lista já larga] → a Lista rola para o lado como hoje, e o "Fim previsto" só aparece com a linha de base.
- [A diferença da linha de base compara o fim de hoje, não o fim real] → as duas colunas mostram só as datas; a diferença do fim real fica para o relatório de status, se ele pedir.
- [Na demonstração, o fim real é o dia da carga] → o sistema grava o dia em que a tarefa chega a 100% (ADR-0020), e a carga conclui as tarefas no dia em que roda; os projetos que nascem do XML do MS Project ficam com o fim real no fim de cada tarefa.
