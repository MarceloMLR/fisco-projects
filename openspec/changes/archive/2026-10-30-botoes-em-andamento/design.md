# Design

## Context

As telas usam o `Button` e o `IconButton` do MUI direto (75 e 24 arquivos), com o estilo do fp-btn no tema (`MuiButton`). Os formulários desabilitam o botão de envio com o `isSubmitting` do React Hook Form, e muitas ações fazem `onClick={() => { void acao(); }}`, sem estado. O MUI 9 tem o `loading` no `Button` e no `IconButton` (o indicador no lugar do ícone com `loadingPosition="start"`, e o botão desabilitado).

## Goals / Non-Goals

**Goals:** o estado em andamento em todos os botões que mandam algo ao servidor, numa regra só, que todo botão novo herde.

**Non-Goals:** os itens dos menus e os indicadores das listas.

## Decisions

- **`Button` e `IconButton` do sistema** (`web/src/components/Button.tsx`), com as mesmas propriedades do MUI: quando o `onClick` devolve uma promessa, o botão fica em andamento até ela terminar, e o clique durante o andamento é ignorado; `loading` continua aceito para o estado vindo de fora (uma mutação). No `Button`, o indicador fica no começo (`loadingPosition="start"`): no lugar do `startIcon`, ou antes do texto.
- **`Form`** (`web/src/components/Form.tsx`): o `<form>` do sistema, que recebe o `onSubmit` assíncrono, ignora um novo envio durante o anterior e diz aos botões de envio de dentro (`type="submit"`) que estão em andamento, por um contexto. O `Dialog` com `onSubmit` passa a usá-lo; os formulários de página também.
- **Visual no tema:** o indicador do MUI (`CircularProgress` de 16px, na cor do texto do botão). O botão em andamento mantém as cores da variante, em vez do cinza do desabilitado, sem hover, com `cursor: progress` e `aria-busy`.
- **Lint:** `no-restricted-imports` para `@mui/material/Button` e `@mui/material/IconButton`, menos no `Button.tsx`. Os outros controles do MUI (os itens de menu, o `ToggleButton`) ficam como estão.
- **Telas:** cada uma passa a importar os botões do sistema, a devolver a promessa no clique (`onClick={remind}` no lugar de `onClick={() => { void remind(); }}`) e no envio (`onSubmit={save}`), e as mutações passam `loading={mutation.isPending}` ou usam `mutateAsync`. O `disabled={isSubmitting}` sai onde o `Form` já cuida.
- **Design system:** o README do Button ganha o estado em andamento, e a prévia mostra os botões em andamento.

## Risks / Trade-offs

- [Mudança grande nas telas] → Mecânica, guiada pelo lint, e coberta pelos testes de tela que já existem; os testes novos ficam nos componentes e em algumas telas de cada área.
- [Botão desabilitado perde o foco do teclado] → O foco volta ao fluxo normal da tela, como no envio de hoje; o `aria-busy` avisa o leitor de tela.
- [Resposta rápida pisca o indicador] → Escolha de Marcelo (B1 do ADR-0131): o sinal na hora vale mais que o piscar.
