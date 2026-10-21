---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0107: Mencionar ao digitar "@" no editor dos chamados

Complementa o ADR-0026 (mensagens, anexos e menções nos chamados).

## Contexto e problema

O editor dos chamados (Tiptap) só mostra as pessoas para mencionar pelo botão "@" da barra. Marcelo notou que digitar "@" no texto não mostra nada, e é assim que se menciona alguém em qualquer outra ferramenta. Como mostrar as pessoas ao digitar?

## Fatores de decisão

- O mesmo nó de menção de hoje (o id e o nome), que a API valida (`RichText`).
- As mesmas pessoas do botão, procuradas pelo começo do nome, sem acento.
- Escolher pelo teclado (setas, Enter, Tab, Esc) e pelo mouse, sem tirar o foco do texto.

## Opções consideradas

1. O `@tiptap/suggestion`, o utilitário oficial do Tiptap para listas abertas por um caractere.
2. Procurar o "@" antes do cursor à mão, a cada mudança do texto.
3. O `@tiptap/extension-mention`, que traz um nó de menção próprio.

## Resultado da decisão

Opção 1. O `@tiptap/suggestion` (MIT, a mesma versão do Tiptap) acha o "@" e o que vem depois, aceita espaços ("@Diego T"), sai com Esc e troca o texto digitado pela menção. A lista é a do botão, filtrada pelo começo do nome ou de uma palavra dele, sem acento, com até 8 pessoas; o editor a mostra num `Popper` junto do texto, com as opções em `role="option"`. A opção 3 trocaria o nó que a API já valida, e a 2 refaria à mão o que o utilitário já resolve.

### Consequências

- Boa, porque mencionar fica como em qualquer ferramenta, sem tirar a mão do teclado.
- Neutra, porque o projeto ganha uma dependência pequena do próprio Tiptap.
