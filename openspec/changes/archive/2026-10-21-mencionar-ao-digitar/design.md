## Context

O `RichTextEditor` tem um nó `mention` próprio (id e nome) e um botão "@" que abre um menu com as pessoas (`mentionable`). A API valida o nó no `RichText`.

## Goals / Non-Goals

**Goals:**
- Mostrar as pessoas ao digitar "@" e parte do nome, com teclado e mouse.

**Non-Goals:**
- Trocar o nó de menção ou as pessoas que podem ser mencionadas.

## Decisions

- **`typedMention.ts`**: a extensão `typedMention` usa o `Suggestion` com `char: '@'` e `allowSpaces: true`; o comando troca o texto digitado pelo nó `mention` e um espaço. O `TypedMentions` guarda as pessoas de agora e a lista mostrada, e trata as teclas (setas marcam, Enter e Tab escolhem); Esc fica com o próprio plugin. `peopleTyped` filtra pelo começo do nome ou de uma palavra, sem acento, até 8.
- **Tela**: a lista num `Popper` junto do texto (o `clientRect` do plugin), com `MenuList` em `role="listbox"` e as opções em `role="option"`; o foco fica no editor (o `mousedown` das opções é cancelado).
- **Por que um objeto, não refs**: o editor é criado uma vez, e as regras do React não deixam ler refs durante o desenho; o `TypedMentions` vem de um `useState` e recebe as pessoas num efeito.

## Risks / Trade-offs

- [Um "@" num e-mail digitado abre a lista] → sem ninguém que comece com o texto, a lista não aparece, e Esc a fecha.
