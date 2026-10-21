## Why

Marcelo notou que, nos botões com ícone ("Iniciar atendimento", "Enviar"), o ícone fica longe demais do texto e o botão parece largo. O tema soma o espaço do design system (8px) às margens que o MUI já põe no ícone, e o ícone fica a 16px do texto (Fundação-5.1, a pedido de Marcelo).

## What Changes

- O ícone de um botão fica a 8px do texto, só pelo espaço do botão, como no design system (fp-btn); o ícone do começo avança 4px sobre o respiro, e o do fim não tem margem.
- Vale para todos os botões com ícone, de uma vez, no tema do MUI.

## Capabilities

### New Capabilities

### Modified Capabilities
- `estrutura-das-telas`: o espaço entre o ícone e o texto dos botões.

## Impact

- `web/src/theme/theme.ts` (`MuiButton`, `startIcon` e `endIcon`) e `theme.test.tsx`.
