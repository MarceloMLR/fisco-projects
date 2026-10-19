---
status: proposta
date: 2026-10-18
decision-makers: Marcelo Ribeiro
---

# ADR-0082: Tokens da marca derivados da cor principal em OKLCH, com a cor recusada sem contraste

## Contexto e problema

Cada instalação pode trocar a cor principal (ADR-0003, ADR-0050). O design system lista os tokens que mudam juntos com ela, nos dois temas: `action`, `action-hover`, `action-pressed`, `action-text`, `control-selected`, `focus-ring`, `sidebar-active-bg`, `sidebar-text-active`, `sidebar-indicator`, `avatar-owner` e a família `brand-panel-*`. As cores de status nunca mudam. Antes de aceitar a cor, ele pede três conferências:

- `on-action` com 4,5:1 sobre `action`;
- `action-text` com 4,5:1 sobre `bg-surface` e `bg-page`;
- `sidebar-text-active` com 4,5:1 sobre `sidebar-active-bg`.

O design system não diz como achar os valores de cada token a partir de uma cor só. Como derivar os tokens, e o que fazer com uma cor sem contraste?

## Fatores de decisão

- Com a cor padrão, as telas ficam exatamente como no design aprovado.
- Contraste AA nos dois temas (ADR-0063, ADR-0067), com qualquer cor aceita.
- O administrador escolhe uma cor só, sem ajustar tons um a um.
- Nenhuma dependência nova para uma conta de cores.

## Opções consideradas

- A. Derivar cada token do valor padrão dele, em OKLCH: trocar o matiz pelo da nova cor e escalar a saturação pela dela. A luminosidade padrão é mantida, menos nos tokens que são a própria cor no tema claro. Depois, os pares de texto e fundo são ajustados até o contraste mínimo.
- B. Gerar uma escala de tons (50 a 900) a partir da cor e escolher um degrau por token.
- C. Deixar o administrador escolher a cor de cada token.

## Resultado da decisão

Opção escolhida: **A. Derivar cada token do valor padrão dele, em OKLCH**.

O OKLCH separa luminosidade, saturação e matiz de um jeito próximo do olho. Manter a luminosidade de cada token mantém o contraste do design, e partir do valor padrão faz a cor padrão devolver os tokens do design system sem mudança.

Regras:

- **No tema claro:**
  - `action`, `action-text`, `control-selected`, `focus-ring`, `sidebar-indicator`, `avatar-owner` e `brand-panel-bg-2` são a própria cor escolhida;
  - `action-hover` e `action-pressed` mantêm a diferença de luminosidade que têm para a cor padrão;
  - os demais tokens da lista mantêm a luminosidade padrão e recebem o matiz e a saturação da nova cor.
- **No tema escuro:** todos os tokens da lista mantêm a luminosidade padrão e recebem o matiz e a saturação da nova cor.
- **Pares de contraste:** depois de derivados, os pares do design system (os três acima, o texto do painel da entrada e as iniciais do avatar) têm a luminosidade ajustada até o mínimo, quando preciso. Isso nunca vale para a cor escolhida no tema claro.
- **Cor recusada:** a cor escolhida, no tema claro, precisa de 4,5:1 com o branco, porque é o fundo do texto branco do botão e o texto dos links sobre o fundo branco. Sem isso, a cor é recusada com o motivo, e o servidor confere a mesma regra.
- **Fora da troca:** as cores de status, as superfícies, os textos neutros e o fundo da página de entrada não mudam.
- **Onde fica:** a derivação fica no frontend, num lugar só. O servidor guarda só a cor em hexadecimal (ou nada, para a padrão) e confere o contraste com o branco.

### Consequências

- Bom, porque a cor padrão reproduz o design aprovado, e qualquer cor aceita mantém o contraste AA.
- Bom, porque o administrador escolhe uma cor só e vê a Prévia na hora.
- Ruim, porque cores muito claras (amarelo, verde-limão) não podem ser a cor principal, já que não dão contraste com o branco.
- Neutro, porque uma conta de cores de cerca de cem linhas passa a ser do projeto, com testes.

### Confirmação

Testes cobrindo:

- a cor padrão devolve os valores do `tokens.json`;
- uma cor aceita tem os pares do design system com o contraste mínimo nos dois temas;
- uma cor clara é recusada na tela e pela API;
- as cores de status ficam iguais.

## Prós e contras das opções

### A. Derivar do valor padrão, em OKLCH

- Bom, porque preserva o design e o contraste de cada token.
- Bom, porque não pede biblioteca: a conversão entre sRGB e OKLCH tem fórmulas públicas e curtas.
- Ruim, porque a saturação muito alta de algumas cores precisa ser reduzida para caber no sRGB.

### B. Escala de tons

- Bom, porque é um padrão conhecido (Tailwind, Material).
- Ruim, porque nenhum degrau coincide com os valores aprovados, e a cor padrão mudaria as telas.

### C. Cor de cada token

- Bom, porque dá controle total.
- Ruim, porque pede conhecimento de design a quem administra e abre caminho para combinações sem contraste.
