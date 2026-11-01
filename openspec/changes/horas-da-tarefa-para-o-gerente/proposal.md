## Why

As horas das tarefas (ADR-0138) só aparecem para o administrador, e nem ele vê, pelo cronograma, quem apontou as horas de uma tarefa: a seção "Horas" diz "Apontadas 15 h", sem as pessoas nem os dias. Marcelo pediu, em 01/11/2026, que o gerente do projeto também veja as horas das tarefas nos projetos que gerencia e que a seção tenha "Ver apontamentos", com quem apontou, em que dia e quantas horas (ADR-0144). GanttA-5.1.

## What Changes

- Quem vê as horas das tarefas passa a ser o administrador, em qualquer projeto, e o gerente do projeto, nos projetos que gerencia, em qualquer situação; o líder técnico, o colaborador, o desenvolvedor e o leitor recebem 403, e quem está fora, 404. A regra fica num lugar só, no `TimeEntryAccess`.
- **BREAKING** (só para a tela do próprio sistema): a leitura das horas das tarefas sai de `GET /api/admin/project-hours/{projectId}/tasks` e passa para `GET /api/projects/{projectId}/task-hours`, com `canChangePlanned`. Pôr, trocar e tirar as previstas continua só do administrador, em `PUT /api/admin/project-hours/{projectId}/tasks/{itemId}`.
- Nova rota `GET /api/projects/{projectId}/task-hours/{itemId}/entries`: os apontamentos de uma tarefa, de todas as pessoas, com a pessoa, o dia, as horas, a descrição e se quem pede pode corrigi-lo, do dia mais recente para o mais antigo, com o total; 404 para o marco, a fase e a tarefa de outro projeto.
- No cronograma, a coluna "Horas" da Lista e a seção "Horas" do detalhe aparecem também para o gerente do projeto; "Alterar" as previstas só para o administrador.
- Na seção "Horas", "Ver apontamentos", ao lado das apontadas, abre os apontamentos da tarefa agrupados por dia, com a pessoa, as horas, a descrição e o total; o "⋮" de corrigir e apagar só nos que a pessoa pode corrigir (ADR-0128).

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `horas-do-projeto`: quem vê as horas das tarefas, os apontamentos de cada tarefa na API e na tela, e a regra de ver as horas de outra pessoa.

## Non-goals

- O gerente pôr, trocar ou tirar as horas previstas (ADR-0144, opção C2, para depois).
- O líder técnico ou o responsável da tarefa verem as horas das tarefas.
- O gerente ver o saldo do projeto, as contratadas, as adicionais, os apontamentos fora das tarefas ou a carga da equipe, que continuam só do administrador (ADR-0128, ADR-0142).
- "Ver apontamentos" em cada tarefa da seção "Por tarefa" de Horas dos projetos.

## Impact

- `src/Modules/TimeTracking`: a regra de quem vê as horas das tarefas no `TimeEntryAccess`, os casos de uso `GetTaskHours` (com quem pede) e `ListTaskTimeEntries`, a montagem das linhas dos apontamentos num lugar só, as rotas sob `/api/projects/{projectId}/task-hours` e o aquecimento.
- `web/src`: a consulta das horas das tarefas na rota nova, o `seesTaskHours`, a coluna e a seção do cronograma para o gerente, "Alterar" pelo `canChangePlanned`, o diálogo dos apontamentos da tarefa (pelo `TimeEntryTable`, sem a coluna da tarefa), os textos, os tipos gerados e os testes.
- Documentação: o ADR-0144, `CLAUDE.md` e o roadmap (ADR-0128, ADR-0138).
