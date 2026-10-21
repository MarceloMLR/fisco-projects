## Context

A conversa pinta cada mensagem pelo tom do papel (`toneOf` em `conversation.ts`) com os tokens `msg-{tom}-*`, `role-{tom}-*` e `avatar-{tom}` (ADR-0069). O solicitante, o administrador fora do projeto e quem não tem papel dividem o tom `requester`, cinza, com o selo e o avatar neutros.

## Goals / Non-Goals

**Goals:**
- O solicitante com a cor dele, no mesmo peso das outras.
- Quem não tem cor de papel continua em cinza.

**Non-Goals:**
- Mudar as outras cores por papel ou o lado das mensagens.

## Decisions

- **Cor**: os tons do violeta do gerente com o matiz trocado para 350 em OKLCH: cartão `#fbeff4`/`#301824`, contorno `#edc9d9`/`#592c43`, selo `#f6dce7`/`#432031` com texto `#86205b`/`#eeadcb` (6,9:1 e 7,7:1) e avatar `#a1286e`/`#af3379` (iniciais brancas com 6,9:1 e 5,9:1).
- **Design system primeiro**: os tokens entram no design system (com o componente Message e o Avatar), e o `tokens.json` é copiado de novo para o projeto.
- **Tom neutro**: `MessageTone` ganha `neutral` para o administrador e para "Sem papel no projeto", com `msg-neutral-*` (os valores antigos do solicitante), o selo em `bg-subtle` e `text-secondary` e o avatar neutro, como antes.

## Risks / Trade-offs

- [Rosa perto do violeta do gerente] → a mesma claridade com 60° de matiz entre eles, e o selo diz o papel.
