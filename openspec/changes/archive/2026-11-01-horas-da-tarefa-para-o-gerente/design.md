## Context

O `TimeTracking` guarda os apontamentos (`time_entries`, com a tarefa opcional, `ScheduleItemId`) e as horas previstas de cada tarefa (`planned_hours`, ADR-0138). As horas das tarefas saem do `GetTaskHours`, hoje em `GET /api/admin/project-hours/{projectId}/tasks`, no grupo com `SystemPolicies.Administrator`, e a tela as pede só para o administrador. Quem vê e muda os apontamentos sai do `TimeEntryAccess`: cada pessoa os próprios, o administrador os de qualquer pessoa. O papel de cada pessoa num projeto chega pelo `IProjectAccess` do `Projects.Contracts` (`TimeEntryProjects.FindAsync`, com o papel nulo para quem não está no projeto). O ADR-0144 decidiu que o gerente do projeto também vê as horas das tarefas e os apontamentos de cada tarefa, e que só o administrador muda as previstas.

## Goals / Non-Goals

**Goals:**

- Uma regra só, no módulo das horas, para quem vê as horas das tarefas e os apontamentos de uma tarefa.
- O gerente vê, no cronograma, o que o administrador vê, sem "Alterar".
- "Ver apontamentos" com o mesmo visual e as mesmas ações dos apontamentos de Horas dos projetos.

**Non-Goals:**

- O gerente mudar as previstas; o saldo, as contratadas, as adicionais e a carga da equipe para o gerente.

## Decisions

- **Regra.** `TimeEntryAccess.SeesTaskHours(viewer, project)`: o administrador, ou o papel `Manager` no projeto, em qualquer situação; `ChangesPlannedHours(viewer)`: só o administrador. `MayChange(viewer, entry, project)` junta o `SeesEntry` e o `RefusalToChange`, para a lista da tarefa, que mostra apontamentos de outras pessoas ao gerente, não oferecer a ele a correção deles (a rota de corrigir já responde 404).
- **Casos de uso.** O `GetTaskHours` passa a receber quem pede e devolve o resultado com o motivo (`TaskHoursOutcome`: `NotFound` sem o projeto ou para quem não o vê, `Forbidden` para quem o vê sem ver as horas) e `CanChangePlanned`. O `ListTaskTimeEntries` confere o mesmo, depois a tarefa (um item `Task` do cronograma do projeto, pelo `IScheduleItems`, senão 404), e lê os apontamentos da tarefa (`ITimeEntryStore.OfTaskAsync`). As linhas dos apontamentos (o nome atual da pessoa, o projeto, a tarefa, o chamado e o `canChange`) passam a ser montadas num lugar só (`TimeEntryRows`), usado também pelo `ListTimeEntries`.
- **API.** Um grupo novo do `TimeTracking`, `/api/projects/{projectId:guid}/task-hours`, só com sessão: `GET /` (`GetTaskHours`, o `ProjectTaskHoursResponse` com `canChangePlanned`) e `GET /{itemId:guid}/entries` (`ListTaskTimeEntries`, o `TimeEntryListResponse` dos apontamentos, o mesmo de `GET /api/time-entries`). O `GET` do grupo do administrador sai; o `PUT .../tasks/{itemId}` fica nele. A mesma forma de rota já é usada por outros módulos sob o projeto (documentos, links, acessos).
- **Aquecimento.** O `TimeTrackingWarmUp` lê as horas das tarefas como o administrador.
- **Tela.**
  - `taskHoursQuery` passa a pedir a rota nova (a mesma chave, sob `queryKeys.projectHours`), usada pelo cronograma e pela seção "Por tarefa" de Horas dos projetos; `taskEntriesQuery(projectId, itemId)` pede os apontamentos da tarefa.
  - `seesTaskHours(project, user)` em `admin/taskHours.ts`: o administrador ou o gerente do projeto, como o `managesTerms` dos termos; a API decide, e isso só evita pedir o que daria 403.
  - O cronograma usa o `seesTaskHours` no lugar do `isAdministrator` para a coluna "Horas" e a seção "Horas"; o `TaskHoursSection` recebe `canChange` (do `canChangePlanned`) para "Alterar" e mostra "Ver apontamentos" ao lado das apontadas, com horas apontadas.
  - `TaskEntriesDialog` (`admin/TaskEntriesDialog.tsx`): o `Dialog` com `size="lg"`, o nome da tarefa no `subtitle` e o `TimeEntryTable` com a pessoa na primeira coluna e sem a coluna "Tarefa ou chamado" (`showWork={false}`), o total no rodapé; corrigir ou apagar avisa e invalida as horas das tarefas, os apontamentos da tarefa e a auditoria.

## Risks / Trade-offs

- [A regra do gerente fica no `TimeTracking` e a tela repete a pergunta pelo papel] → Como no `managesTerms`: a tela só esconde o que daria 403, e a API decide.
- [Uma tarefa com muitos apontamentos] → A lista da tarefa vem inteira: uma tarefa tem no máximo 999 dias úteis e poucas pessoas, e a tabela rola por dentro do diálogo.
- [Mudança de rota] → Só a tela do próprio sistema usa a leitura das horas das tarefas; o contrato e os tipos gerados mudam juntos.
