## 1. Backend

- [x] 1.1 `TicketTransition`, `TicketEvent`, `TicketFlow` e quem pode (`TicketAccess.ActorsOf`), com testes
- [x] 1.2 Tabelas, fluxo inicial pela migração, `ResolvedAt` e `ClosedAt` no chamado
- [x] 1.3 Mudar o status, ler o fluxo, primeira resposta e resposta do solicitante, mensagens fechadas no chamado Fechado
- [x] 1.4 Fechamento automático (`CloseResolvedTickets` e `TicketAutoClose`)
- [x] 1.5 Eventos e ações no `GET /api/tickets/{number}`, interações com os eventos
- [x] 1.6 Testes da API para cada cenário

## 2. Frontend

- [x] 2.1 Ações no cabeçalho do chamado, com o diálogo da mensagem
- [x] 2.2 Eventos e o filtro "Tudo / Mensagens / Histórico" na conversa
- [x] 2.3 Página "Fluxo de chamados"
- [x] 2.4 Testes das telas

## 3. Demonstração e documentação

- [x] 3.1 Chamados em vários status nos dados de demonstração, com o teste
- [x] 3.2 `CLAUDE.md`, glossário e roadmap
