## Context

A seção da descrição, no topo da conversa, usa `bg-surface` e `border`; as mensagens do solicitante usam `msg-requester-bg` e `msg-requester-border` (ADR-0104).

## Goals / Non-Goals

**Goals:**
- A descrição com a cor do solicitante, como as mensagens dele.

**Non-Goals:**
- Mudar o lugar ou o conteúdo da descrição.

## Decisions

- A seção ganha os tokens do cartão do solicitante e `data-tone="requester"`, como as mensagens; o avatar e o selo já eram os dele.

## Risks / Trade-offs

- Nenhum: os tokens são os das mensagens do solicitante, com o contraste já conferido.
