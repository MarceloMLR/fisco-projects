## Why

Com a entrada pelo Entra, o `dev` passou a ter duas "Marina Costa": a fictícia e a do tenant. Na Chamados-2.5, o e-mail apareceu embaixo do nome só na menção; nas outras escolhas de pessoa, como incluir alguém no projeto, só o nome aparece, e não dá para saber quem é quem. Marcelo pediu o e-mail em toda escolha de pessoa (Projetos-4.1, fora do roadmap).

## What Changes

- O e-mail embaixo do nome em toda lista para escolher uma pessoa: gerente e líder técnico no Novo projeto, incluir no projeto, responsável de uma tarefa, responsável e solicitante de chamados (também em "Passar pendências"), a pessoa do pedido de aceite e o filtro por pessoa da Auditoria, como já é na menção.
- `GET /api/tickets/assignee-choices` e `requester-choices` passam a trazer o e-mail de cada pessoa (`TicketPersonChoiceResponse`).
- O `Select` das telas ganha o texto de apoio de uma opção (`detail`), mostrado na lista embaixo do nome; o campo mostra só o nome escolhido.
- O pedido de aceite e o responsável de uma tarefa leem o e-mail das pessoas do projeto pela lista de membros, que já o traz.

## Capabilities

### New Capabilities

### Modified Capabilities
- `usuarios-e-papeis`: o e-mail embaixo do nome em toda escolha de pessoa.

## Non-goals

- Mudar as listas que mostram pessoas sem escolher (a aba Membros, a conversa): o cartão da pessoa já mostra o e-mail ao clicar na foto (ADR-0118).
- Buscar pelo e-mail onde a busca é só pelo nome.

## Impact

- `Tickets` (a escolha do responsável e do solicitante, o endpoint e os testes), telas (`Select`, `PersonField`, `PersonLines`, `PeopleChangeDialog`, `MemberTickets`, `AcceptanceDialog`, `ScheduleItemFields`, `PersonFilter`), tipos da API.
- `docs/roadmap.md` e `CLAUDE.md`.
- ADRs: 0005, 0026, 0027, 0035, 0107, 0118.
