## Context

A Lista guarda as tarefas-resumo recolhidas num conjunto de ids (`collapsed` do `ProjectSchedulePage`), e o `ScheduleGantt` guarda o próprio conjunto, com as chaves das linhas, que já são os ids das fases e dos itens (`ganttRowsOf`).

## Decisions

- **Um conjunto só.** O `collapsed` da aba guarda os ids das fases e das tarefas-resumo recolhidas e vai para a Lista, para a Linha do tempo e para o Expandir. O `ScheduleGantt` ganha `collapsed` e `onToggle` opcionais; sem eles, guarda o próprio, como hoje.
- **Linhas da Lista.** O `scheduleRows` deixa de fora os itens da fase recolhida (e continua deixando as subtarefas da tarefa-resumo recolhida); a linha da fase fica.
- **A seta.** O `SubtasksArrow` vira a seta das duas linhas, com o nome acessível da Linha do tempo para a fase ("Recolher Configuração", "Mostrar Configuração"); o clique e o Enter na seta não abrem o diálogo da fase.
- **O que não muda.** O filtro, o PDF e o Excel usam o cronograma inteiro; o recolhido fica só na tela, enquanto a aba está aberta.
