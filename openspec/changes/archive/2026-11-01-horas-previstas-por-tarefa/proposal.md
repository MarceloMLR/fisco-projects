## Why

O administrador vê o saldo de cada projeto contra as horas contratadas (ADR-0128), mas não sabe em que tarefa as horas passaram do combinado: a parametrização do ICMS foi orçada em 40 horas e já tem 52 apontadas, enquanto a integração da NF-e ainda tem folga. As pessoas já apontam as horas numa tarefa do cronograma; falta dizer quantas horas cada tarefa deveria levar. GanttA-5, do grupo 14 (Aprimoramento do Gantt), a pedido de Marcelo, que decidiu em 01/11/2026 que as previstas são só do administrador (ADR-0138). Elas são a base da carga da equipe (GanttB-5).

## What Changes

- Cada tarefa do cronograma (também a resumo e a subtarefa) passa a ter, opcionalmente, as horas previstas, de 0,25 a 9.999 em quartos de hora, postas, trocadas e tiradas só pelo administrador, em qualquer situação do projeto.
- As previstas de cada tarefa são comparadas com as horas apontadas nela, por qualquer pessoa, com o estado da regra do saldo (acima, perto das previstas a partir de 90% numa tarefa em aberto, dentro e sem horas previstas), num lugar só.
- API, só do administrador: `GET /api/admin/project-hours/{projectId}/tasks` (as tarefas com previstas ou apontadas, na ordem do cronograma, com os totais e as horas apontadas fora das tarefas) e `PUT /api/admin/project-hours/{projectId}/tasks/{itemId}` (as previstas, nulo tira).
- Na tela, só para o administrador:
  - no cronograma, a coluna "Horas" da Lista ("12 de 16 h") e a seção "Horas" do detalhe da tarefa, com as previstas, as apontadas, o estado e "Alterar";
  - no detalhe de Horas dos projetos, a seção "Por tarefa", com as previstas nas tarefas, as apontadas nelas, as apontadas fora das tarefas e cada tarefa com "Alterar".
- A auditoria registra as previstas, com a tarefa pelo nome atual e o projeto como contexto, e elas saem com o projeto excluído.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `horas-do-projeto`: as horas previstas de cada tarefa contra as apontadas nela, na API, na auditoria, na exclusão do projeto e nas telas do administrador.
- `dados-de-demonstracao`: as horas previstas em tarefas do projeto principal, nos três estados.

## Non-goals

- As previstas no XML do MS Project (`Work`), nos modelos de projeto e na linha de base.
- As previstas para o gerente, o líder técnico ou o responsável da tarefa (ADR-0128, ADR-0138).
- A soma das subtarefas na tarefa-resumo (ADR-0138).
- A carga da equipe, que é a GanttB-5.

## Impact

- `src/Modules/TimeTracking`: a tabela `planned_hours` (migração), as regras no domínio, os casos de uso, as rotas, a auditoria, a exclusão do projeto e o aquecimento.
- `src/Modules/Projects.Contracts`: o `ScheduleItemInfo` diz se a tarefa está concluída (`IsDone`), para o estado "perto das previstas".
- `web/src`: o cronograma (a coluna e a seção do detalhe), o detalhe de Horas dos projetos, o diálogo das previstas, os textos, os tipos gerados, a auditoria e os testes; a demonstração põe previstas num projeto.
- Documentação: o ADR-0138, `CLAUDE.md`, `docs/glossario.md` e o roadmap (GanttA-5, ADR-0016, ADR-0128).
