## Why

As férias e as folgas da equipe ficam fora do FiscoProjects, e a gerente só descobre que a tarefa da Carla cai nas férias dela quando a tarefa atrasa. GanttB-4, do grupo 15 (Aprimoramento do Gantt 2), a pedido de Marcelo, que decidiu em 01/11/2026 que o administrador e o gerente cadastram; o resto, com os cuidados de dado pessoal, fica no ADR-0141. As ausências são também a base da carga da equipe (GanttB-5).

## What Changes

- Cada pessoa pode ter ausências: o início e o fim de um período sem trabalho, de 1 a 366 dias corridos, sem sobreposição, sem motivo.
- Pela aba Membros de um projeto em aberto, o administrador e o gerente do projeto incluem e apagam as ausências das pessoas dele; o líder técnico e as outras pessoas do projeto recebem 403, e quem não está no projeto, 404.
- As pessoas dos mesmos projetos veem as ausências: no detalhe da pessoa em Membros, na lista de Membros ("Ausente até 14/11", "Ausente de 03/11 a 14/11") e no cronograma.
- O cronograma avisa a tarefa em aberto com datas próprias cujo responsável está ausente durante ela, com os períodos, na Lista e no detalhe; as datas não mudam.
- A ausência some sozinha um ano depois do fim e na anonimização da pessoa; a auditoria registra a inclusão e a exclusão sem as datas.
- API: `GET`, `POST` e `DELETE /api/projects/{id}/members/{userId}/absences[/{absenceId}]`; a lista de membros traz `nextAbsence`, e cada item do `GET /schedule`, `ownerAbsences`.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `membros-do-projeto`: as ausências das pessoas, quem cadastra e quem vê, na API e na tela Membros.
- `cronograma`: o aviso da tarefa cujo responsável está ausente durante ela.
- `dados-pessoais`: a retenção de um ano das ausências e o apagamento na anonimização.
- `dados-de-demonstracao`: ausências que caem em tarefas do projeto principal.

## Non-goals

- Motivo, tipo ou observação da ausência; meio dia.
- A pessoa cadastrando a própria ausência.
- A ausência empurrando as tarefas, e avisos por e-mail ou no sino.
- A carga da equipe, que é a GanttB-5.

## Impact

- `src/Modules/Projects`: a tabela `person_absences` (migração), as regras no domínio, os casos de uso, as rotas, a lista de membros, o cronograma, a auditoria, a limpeza em segundo plano e o apagador da anonimização; os testes.
- `web/src`: a seção "Ausências" no detalhe da pessoa, o diálogo, a marca na lista de Membros, o aviso no cronograma, os textos, os tipos gerados e os testes; a demonstração.
- Documentação: o ADR-0141, `CLAUDE.md`, `docs/glossario.md` e o roadmap (GanttB-4, ADR-0018, ADR-0058).
