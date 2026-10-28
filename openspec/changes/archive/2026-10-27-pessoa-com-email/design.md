## Context

A busca de pessoas (`GET /api/people`) já devolve o e-mail; o `PersonField` mostra só o nome e, para quem nunca entrou, "Nunca entrou". A escolha do responsável e do solicitante dos chamados (`ListTicketPeopleChoices`) devolve só o id e o nome. O pedido de aceite escolhe entre as pessoas do projeto que vêm com o projeto (`ProjectPersonResponse`, sem e-mail), e o `Select` do design system só tem o rótulo de cada opção. A menção já mostra o e-mail embaixo do nome desde a Chamados-2.5.

## Goals / Non-Goals

**Goals:**
- Separar pessoas de mesmo nome em toda escolha, do mesmo jeito da menção.

**Non-Goals:**
- Mudar o que a resposta do projeto traz para todos.

## Decisions

- **`Select` com texto de apoio.** `SelectOption.detail` opcional: o item da lista mostra o rótulo e, embaixo, o texto em `text-secondary`, 12 px, como a menção; o campo mostra só o rótulo (`renderValue`). As outras listas não mudam.
- **`PersonLines`.** O nome, o e-mail embaixo e uma nota ("Nunca entrou"), num componente só, usado pelo `PersonField`, pelo filtro por pessoa da Auditoria (`PersonFilter`) e pela menção; o e-mail vazio (anonimizado) não ocupa linha.
- **`PersonField`.** `PersonChoice.email` opcional, preenchido pela busca e pela pessoa da sessão.
- **Chamados.** `ListTicketPeopleChoices` lê as pessoas pelo `IUserDirectory.PeopleAsync` e devolve `TicketPersonChoice(Id, Name, Email)`, em ordem de nome e e-mail; o endpoint responde `TicketPersonChoiceResponse`, sem mexer no `TicketPersonResponse` dos chamados. O e-mail vazio (anonimizado) vem nulo.
- **Pedido de aceite e responsável da tarefa.** O `AcceptanceDialog` e o `ScheduleItemFields` leem o e-mail das pessoas pelo `projectMembersQuery` (a aba Membros, que já o traz), num mapa pelo id (`emailsOf`); as pessoas oferecidas continuam as mesmas, e sem a lista de membros as opções ficam só com o nome.
- **Passar pendências.** O `MemberTickets` usa as mesmas opções de responsável e de solicitante, agora com o e-mail.

## Risks / Trade-offs

- [Uma leitura a mais ao abrir o pedido de aceite] → a mesma consulta da aba Membros, que fica no cache.
