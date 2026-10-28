## 1. Servidor

- [x] 1.1 `TicketPersonChoice` com o e-mail em `ListTicketPeopleChoices` e `TicketPersonChoiceResponse` nos endpoints de escolha, com o teste "Responsável com o e-mail"

## 2. Telas

- [x] 2.1 `detail` no `Select` e o e-mail no `PersonField`, com o teste "Pessoas de mesmo nome na inclusão"
- [x] 2.2 O e-mail no `PeopleChangeDialog` e no `AcceptanceDialog`, com o teste "Pessoa do pedido de aceite com o e-mail"
- [x] 2.3 O e-mail no responsável da tarefa (`ScheduleItemFields`), em "Passar pendências" (`MemberTickets`) e no filtro por pessoa da Auditoria (`PersonFilter`), com o `PersonLines` comum, e o teste "Responsável da tarefa com o e-mail"

## 3. Documentação e conferência

- [x] 3.1 `CLAUDE.md` e roadmap (Projetos-4.1)
- [x] 3.2 Build sem avisos, testes, `dotnet format`, lint, typecheck e `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
