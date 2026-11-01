## Context

O `TimeTracking` guarda os apontamentos (`time_entries`, com a tarefa opcional, `ScheduleItemId`), as horas contratadas e as adicionais de cada projeto, e calcula o saldo num lugar só (`HoursBalance`). As tarefas do cronograma são do `Projects`, e os outros módulos as leem pelo contrato `IScheduleItems` (`ScheduleItemInfo`: o id, o tipo, o nome e, do marco, o dia). As rotas do saldo ficam em `/api/admin/project-hours`, com `SystemPolicies.Administrator`. O ADR-0138 decidiu que as previstas são só do administrador e que cada tarefa tem as próprias, sem somar as subtarefas.

## Goals / Non-Goals

**Goals:**

- As previstas no módulo das horas, com a mesma regra do saldo, sem mudar o cronograma nem o contrato de quem edita.
- Mostrar as previstas onde o administrador olha: o cronograma e o detalhe de Horas dos projetos.

**Non-Goals:**

- MS Project, modelos, linha de base, desfazer do cronograma, carga da equipe.

## Decisions

- **Domínio.** `PlannedHours` (`timetracking.planned_hours`): o projeto, a tarefa (`ScheduleItemId`, única), as horas (`decimal(6,2)`, de 0,25 a 9.999 em quartos, `PlannedHours.IsValid` pelo `Quarters`) e quando mudou. O estado de cada tarefa é o `HoursBalance.Of(previstas, 0, apontadas, concluída)`: a regra do saldo, num lugar só, com a tarefa concluída no lugar do projeto encerrado; `TaskHours` junta a tarefa, as previstas, as apontadas e o saldo.
- **Contrato.** O `ScheduleItemInfo` ganha `IsDone` (padrão falso), que o `ScheduleItemDirectory` preenche nas tarefas pelo progresso de 100% (`ScheduleItem.IsDone`); os outros módulos não mudam.
- **Casos de uso.** `GetTaskHours` (o projeto pelo `IProjectCatalog`, as tarefas pelo `IScheduleItems`, as previstas e as apontadas por tarefa pelo `IProjectHoursStore`): as tarefas com previstas ou apontadas, na ordem do contrato, e os totais (as previstas nas tarefas, as apontadas nelas e as fora delas, o total do projeto menos as apontadas nas tarefas que existem); nulo sem o projeto. `ChangePlannedHours`: 404 sem o projeto ou quando o item não é uma tarefa do cronograma dele, 400 nas horas, nulo tira, igual não grava. As previstas de uma tarefa excluída ficam na tabela e não aparecem: o desfazer que traz a tarefa de volta, com o mesmo id, traz as previstas.
- **API.** `GET /api/admin/project-hours/{projectId}/tasks` (`GetTaskHours`): `planned`, `logged`, `outside` e `items` (`itemId`, `name`, `planned` nulo sem previstas, `logged` e `balance`, o `HoursBalanceResponse` com `contracted` sendo as previstas). `PUT .../tasks/{itemId}` (`ChangePlannedHours`) com `{ hours }`, nulo tira: 204, 400 em `hours`, 404. As duas no grupo do administrador.
- **Auditoria.** `planned_hours` na auditoria pela tarefa (o rótulo pela referência `ScheduleItem`, ADR-0084), com o projeto como contexto; `UpdatedAt` fora. Na tela da Auditoria, o tipo "Horas previstas" e o campo "Horas".
- **Exclusão.** O `TimeEntryProjectDeletion` apaga as previstas do projeto junto com as contratadas e as adicionais.
- **Aquecimento.** O `TimeTrackingWarmUp` lê as horas das tarefas do projeto do último apontamento.
- **Tela.**
  - `admin/taskHours.ts`: o texto da coluna ("52 de 40 h", "12 h" sem previstas), o texto do saldo da tarefa ("12 h acima", "Restam 2 h", "Sem horas previstas") e o `taskHoursQuery`; o visual de cada estado é o `balanceLook` do saldo.
  - `admin/TaskHours.tsx`: o `PlannedHoursDialog` (o campo "Horas previstas", vazio tira, as horas como "1,5" ou "1:30" pelo `parseHours`), o `TaskHoursSection` do detalhe da tarefa e a `TaskHoursCell` da Lista, com o ícone e a cor do estado só quando acima ou perto.
  - Cronograma: a coluna "Horas" depois de "Progresso" e a seção "Horas" depois da folga, só com `role === 'Administrator'` e só nas tarefas; a consulta só é feita para o administrador.
  - Horas dos projetos: a seção "Por tarefa" depois de "Por pessoa", com os três totais, a tabela das tarefas (`hoursTableSx`) com "Alterar" em cada uma e "Abrir o cronograma".
  - Depois de salvar, invalidam-se as horas das tarefas do projeto e a auditoria.
- **Demonstração.** O plano ganha `plannedHours` por tarefa num projeto, postas pelo administrador, com apontamentos nessas tarefas para mostrar acima, perto e dentro.

## Risks / Trade-offs

- [A tarefa-resumo não soma as subtarefas] → Decisão do ADR-0138; a seção "Por tarefa" lista cada tarefa e os totais do projeto.
- [Previstas de tarefas excluídas ficam na tabela] → Não aparecem nem somam; saem com o projeto e voltam com o desfazer.
- [Duas consultas no cronograma do administrador] → A das horas é pequena e só para o administrador; o cronograma não espera por ela.
