## Why

Na Chamados-2.2, o solicitante passou a falar em rosa, mas a descrição do chamado, que é a primeira mensagem dele, continuou no cartão neutro. Marcelo pediu a descrição em rosa também (Chamados-2.4, a pedido de Marcelo).

## What Changes

- A descrição do chamado fica no cartão na cor do solicitante (`msg-requester-bg` e `msg-requester-border`), como as mensagens dele.

## Capabilities

### New Capabilities

### Modified Capabilities
- `chamados`: a descrição na cor do solicitante.

## Impact

- `TicketConversation.tsx` e `TicketConversation.test.tsx`.
