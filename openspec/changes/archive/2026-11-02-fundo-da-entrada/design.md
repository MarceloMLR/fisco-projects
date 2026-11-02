## Context

Os tokens da marca saem do `brandColors.ts` (ADR-0082, ADR-0146). O painel da entrada (`brand-panel-*`) já segue a secundária ou, sem ela, a principal. O fundo da `SignInPage` usa `signin-bg` com dois brilhos `signin-glow` e uma grade de pontos (`signin-dots`), que ficam fora da lista dos tokens derivados.

## Goals / Non-Goals

**Goals:**

- O fundo e o brilho na cor do painel, sem tocar no cartão.

**Non-Goals:**

- Uma cor nova, um ajuste de contraste novo ou a grade de pontos (branco translúcido, que serve a qualquer fundo escuro).

## Decisions

- **Tokens:** `signin-bg` e `signin-glow` entram em `brandTokenNames`. Assim, sem cor, saem do `tokens.json`, e o teste "Cor padrão" já os confere.
- **Principal:** o `applyMain` os recolore com a principal, ao lado do painel.
- **Secundária:** o `applySecondary` os recolore com a secundária, depois da principal, então ela manda quando existe.
- **Conta:** o `recolored`, a mesma dos outros tokens derivados: a claridade do design system, com o matiz e a saturação da cor.
- **Prévia:** o bloco da entrada vira o fundo, com um brilho, e o painel dentro dele.

## Risks / Trade-offs

- **[Cor muito saturada vira um fundo escuro bem tingido]:** a prévia mostra antes de salvar, e a claridade fica a do design system.
