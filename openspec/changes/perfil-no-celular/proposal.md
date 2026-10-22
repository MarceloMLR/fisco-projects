## Why

Marcelo abriu Meu perfil no celular e a Conta apareceu por cima das Preferências e dos Avisos por e-mail, com o Sair em cima do resumo diário. Abaixo de uma coluna (celular e janelas até 1100 px), a página é uma grade da altura da janela, e as duas partes, que podem encolher, ficam mais baixas que o conteúdo, que transborda e é coberto pela parte seguinte (Acesso-5.1, a pedido de Marcelo).

## What Changes

- Em uma coluna, as Preferências e os Avisos por e-mail ficam com a altura do conteúdo, a Conta logo abaixo, preenchendo o que sobra da janela, e a página rola inteira; nada fica por cima de nada.
- No computador, nada muda: as duas colunas, cada uma rolando por dentro (ADR-0066).
- As outras telas foram conferidas no celular e numa janela de 1024 px com os dados de demonstração, e só Meu perfil tinha conteúdo transbordando.

## Capabilities

### New Capabilities

### Modified Capabilities
- `perfil`: Meu perfil no celular, com a conta abaixo das preferências e dos avisos, sem cobri-los.

## Impact

- `web/src/profile/ProfilePage.tsx` (linhas da grade e altura mínima das partes em uma coluna) e `profile.test.tsx`.
