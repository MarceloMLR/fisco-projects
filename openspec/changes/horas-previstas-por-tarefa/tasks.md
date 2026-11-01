## 1. Decisão e roadmap

- [x] 1.1 ADR-0138 e GanttA-5 em andamento no roadmap

## 2. Domínio e API

- [x] 2.1 `PlannedHours` e `TaskHours` no domínio, com a migração; testes das regras
- [x] 2.2 `IsDone` no `ScheduleItemInfo`
- [x] 2.3 `GetTaskHours` e `ChangePlannedHours`, as rotas, a auditoria, a exclusão do projeto e o aquecimento; testes "Pôr as horas previstas", "Cronograma igual com as previstas", "Tirar as horas previstas", "Horas previstas inválidas", "Horas previstas fora de uma tarefa", "Horas previstas na exclusão do projeto", "Tarefa acima das previstas", "Tarefa perto das previstas", "Tarefa concluída perto das previstas", "Tarefa sem horas previstas", "Horas fora das tarefas", "Permissões nas horas previstas por papel" e "Horas previstas na auditoria"

## 3. Tela

- [x] 3.1 `npm run api:update`; as regras e o diálogo das previstas
- [x] 3.2 A coluna e a seção "Horas" no cronograma; testes "Horas na Lista do administrador", "Alterar as previstas pelo detalhe da tarefa" e "Cronograma sem horas para a gestão"
- [x] 3.3 A seção "Por tarefa" no detalhe de Horas dos projetos; testes "Por tarefa no detalhe das horas", "Alterar as previstas pelo detalhe das horas" e "Horas previstas inválidas pela tela"
- [x] 3.4 A auditoria na tela e a demonstração; teste "Horas previstas na demonstração"
- [ ] 3.5 Conferência no navegador, no computador e no celular, nos temas claro e escuro

## 4. Documentação

- [x] 4.1 `CLAUDE.md` e `docs/glossario.md` com as horas previstas
