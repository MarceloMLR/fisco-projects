## Why

Na conversa do chamado, todos os papéis têm uma cor de destaque e o solicitante fica em cinza neutro. Marcelo pediu uma cor própria para ele e escolheu o rosa entre três prévias (Chamados-2.2, a pedido de Marcelo).

## What Changes

- ADR-0104: o solicitante em rosa (cartão, selo, nome e avatar), com os tokens novos no design system; o administrador fora do projeto e quem não tem papel nele continuam no cinza neutro.
- Design system: `msg-requester-*` em rosa, mais `role-requester-*`, `avatar-requester` e `msg-neutral-*`; `tokens.json` copiado de novo.
- Conversa, descrição do chamado e painel "Pessoas" com o rosa no solicitante e o tom `neutral` para quem não tem cor de papel.

## Capabilities

### New Capabilities

### Modified Capabilities
- `chamados`: a cor do solicitante e o cinza de quem não tem papel na conversa.

## Impact

- `web/src/theme/tokens.json`, `conversation.ts` (`toneOf`, `MessageTone`), `TicketConversation.tsx` (`RoleTag`, `PersonAvatar`, descrição), `TicketPage.tsx`, `Avatar.tsx` e testes; o design system FiscoProjects.
