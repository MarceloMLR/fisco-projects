## Context

O botão do design system (fp-btn) separa o ícone do texto só pelo `gap` de 8px (`space-2`), e o ícone do começo tem `margin-left: -4px`. O tema já usa o `gap`, mas o MUI põe no ícone `margin-right: 8px` (do começo) e `margin-left: 8px` (do fim), e os dois espaços se somam.

## Goals / Non-Goals

**Goals:**
- O mesmo espaço do design system em todos os botões com ícone.

**Non-Goals:**
- Mudar o tamanho dos ícones (20px no padrão e 18px no pequeno, os do MUI e do design system) ou o respiro dos botões.

## Decisions

- **Tema**: `styleOverrides.startIcon` com `marginRight: 0` e `marginLeft: calc(var(--space-1) * -1)`, e `endIcon` sem margens. As sobreposições do tema entram depois dos estilos do MUI, inclusive os do tamanho pequeno.

## Risks / Trade-offs

- [Algum botão contava com o espaço maior] → nenhum componente mexe nas margens do ícone; a conferência visual cobre os botões das telas principais.
