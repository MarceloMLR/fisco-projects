## Context

Cada uma das 16 telas com confirmação passa `anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}` ao `Snackbar` do MUI. O design system diz "canto inferior esquerdo da área de conteúdo" e que a confirmação não cobre a barra lateral. No computador, o cabeçalho invisível (`topbar-height`, 56px) tem a trilha à esquerda e o sino e a conta à direita; no celular, a barra do topo tem o menu, o título e o sino.

## Goals / Non-Goals

**Goals:**
- Todas as confirmações no alto, no meio, num lugar só.

**Non-Goals:**
- Mudar o tempo, o texto ou o visual das confirmações.

## Decisions

- **Tema**: `MuiSnackbar.defaultProps.anchorOrigin` = alto, no meio, e `styleOverrides.anchorOriginTopCenter`: abaixo da barra do topo (`topbar-height` + `space-2`) e, a partir de 900px (onde a barra lateral aparece), centrado na faixa do cabeçalho (a altura do cabeçalho menos os 44px do Snackbar, ao meio) e no meio da área de conteúdo (`50%` mais metade da `sidebar-width`).
- **Telas**: tiram o `anchorOrigin`; um teste das fontes reprova o `Snackbar` que escolhe a própria posição.
- **Design system**: o README do Snackbar troca "canto inferior" pela posição nova.

## Risks / Trade-offs

- [Trilha longa coberta no fim por alguns segundos] → a confirmação some sozinha; o título da tela fica no começo da trilha.
