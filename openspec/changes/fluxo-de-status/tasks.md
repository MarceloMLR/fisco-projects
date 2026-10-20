## 1. Backend

- [ ] 1.1 `TicketTransition`, `TicketEvent`, `TicketFlow` e quem pode (`TicketAccess.ActorsOf`), com testes
- [ ] 1.2 Tabelas, fluxo inicial pela migração, `ResolvedAt` e `ClosedAt` no chamado
- [ ] 1.3 Mudar o status, ler o fluxo, primeira resposta e resposta do solicitante, mensagens fechadas no chamado Fechado
- [ ] 1.4 Fechamento automático (`CloseResolvedTickets` e `TicketAutoClose`)
- [ ] 1.5 Eventos e ações no `GET /api/tickets/{number}`, interações com os eventos
- [ ] 1.6 Testes da API para cada cenário

## 2. Frontend

- [ ] 2.1 Ações no cabeçalho do chamado, com o diálogo da mensagem
- [ ] 2.2 Eventos e o filtro "Tudo / Mensagens / Histórico" na conversa
- [ ] 2.3 Página "Fluxo de chamados"
- [ ] 2.4 Testes das telas

## 3. Demonstração e documentação

- [ ] 3.1 Chamados em vários status nos dados de demonstração, com o teste
- [ ] 3.2 `CLAUDE.md`, glossário e roadmap
