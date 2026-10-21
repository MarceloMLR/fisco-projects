## Why

As confirmações curtas aparecem embaixo, no meio da tela, longe de onde a pessoa olha depois de salvar ou abrir um chamado. Marcelo pediu que apareçam no alto, no meio (Fundação-5.3, a pedido de Marcelo).

## What Changes

- ADR-0109: as confirmações no alto, no meio da área de conteúdo, na faixa do cabeçalho no computador e logo abaixo da barra do topo no celular.
- O tema define a posição de todo Snackbar; as telas deixam de escolher a própria.
- O Snackbar do design system passa a dizer a posição nova.

## Capabilities

### New Capabilities

### Modified Capabilities
- `estrutura-das-telas`: a posição das confirmações.

## Impact

- `web/src/theme/theme.ts`, as 16 telas com confirmação e os testes do tema; o README do Snackbar no design system.
