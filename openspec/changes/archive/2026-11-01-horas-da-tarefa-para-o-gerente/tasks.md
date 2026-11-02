## 1. Decisão e roadmap

- [x] 1.1 ADR-0144 e GanttA-5.1 em andamento no roadmap

## 2. Regras e API

- [x] 2.1 `SeesTaskHours`, `ChangesPlannedHours` e `MayChange` no `TimeEntryAccess`; testes das regras
- [x] 2.2 `GetTaskHours` com quem pede, `ListTaskTimeEntries`, as linhas dos apontamentos num lugar só, as rotas sob `/api/projects/{projectId}/task-hours` e o aquecimento; testes "Horas das tarefas pela gerente", "Permissões nas horas previstas por papel", "Gerente de outro projeto", "Apontamentos de uma tarefa", "Correção pelos apontamentos da tarefa" e "Apontamentos fora de uma tarefa", e os testes existentes na rota nova

## 3. Tela

- [x] 3.1 `npm run api:update`; a consulta na rota nova, a dos apontamentos da tarefa e o `seesTaskHours`
- [x] 3.2 A coluna e a seção "Horas" para a gerente, "Alterar" pelo `canChangePlanned`; testes "Horas na Lista da gerente", "Horas no detalhe para a gerente" e "Cronograma sem horas para a gestão"
- [x] 3.3 "Ver apontamentos" e o diálogo dos apontamentos da tarefa; teste "Ver apontamentos pelo detalhe da tarefa"
- [x] 3.4 Conferência no navegador, no computador e no celular, nos temas claro e escuro

## 4. Documentação

- [x] 4.1 `CLAUDE.md` com as horas das tarefas para o gerente e "Ver apontamentos"
