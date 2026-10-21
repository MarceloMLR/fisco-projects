## Why

No editor dos chamados, só dá para mencionar alguém pelo botão "@" da barra; digitar "@" no texto não mostra ninguém. Marcelo pediu para mencionar ao digitar (Chamados-2.3, a pedido de Marcelo).

## What Changes

- ADR-0107: menção ao digitar "@", com o `@tiptap/suggestion`.
- Digitar "@" e parte do nome mostra as pessoas que podem ser mencionadas, as mesmas do botão, pelo começo do nome ou de uma palavra dele, sem acento; setas, Enter, Tab e o clique escolhem, e Esc fecha.

## Capabilities

### New Capabilities

### Modified Capabilities
- `chamados`: mencionar ao digitar.

## Impact

- `web/package.json` (`@tiptap/suggestion`), `RichTextEditor.tsx`, `typedMention.ts` e testes.
