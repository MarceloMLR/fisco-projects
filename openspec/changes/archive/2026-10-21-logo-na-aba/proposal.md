## Why

A aba do navegador mostra o nome da instalação, mas nenhum ícone. Marcelo pediu o logo enviado nas Configurações gerais também na aba (Acesso-8.4, a pedido de Marcelo).

## What Changes

- ADR-0108: o logo da instalação na aba; sem logo, o selo da barra lateral na cor principal.
- A tela troca o ícone da aba quando a marca chega e quando o logo ou a cor muda.

## Capabilities

### New Capabilities

### Modified Capabilities
- `configuracao-da-instalacao`: o logo na aba do navegador.

## Impact

- `web/src/installation/favicon.ts`, `InstallationRoot.tsx` e testes.
