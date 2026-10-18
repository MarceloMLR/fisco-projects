---
status: aceita (barra lateral marinho substituída pelo ADR-0067)
date: 2026-10-16
decision-makers: Marcelo Ribeiro
---

# ADR-0063: Cores: azul corporativo de ação, neutros frios e barra lateral marinho

## Contexto e problema

Com a direção visual profissional e sóbria (ADR-0062), falta definir as cores padrão do FiscoProjects nos temas claro e escuro. Cada instalação pode trocar a cor principal pela da empresa (ADR-0050); esta é a identidade padrão do produto. Marcelo preferiu o azul corporativo, mas achou a primeira versão, sobretudo no escuro, com cara de "IA genérica" (fundo escuro azulado, cores pastel). Os valores exatos ficam no design system.

## Fatores de decisão

- Identidade própria, sem parecer genérica nem uma tela da Microsoft.
- Contraste e legibilidade nos dois temas.
- Cores de status inconfundíveis com a cor da marca.

## Opções consideradas

- A. Azul petróleo
- B. Índigo
- C. Azul corporativo, com duas variações:
  - C1. Azul de ação, neutros frios (grafite) e barra lateral marinho no tema claro
  - C2. Azul de ação, neutros quentes e barra lateral clara

## Resultado da decisão

Opção escolhida: **C1. Azul corporativo de ação, neutros frios e barra lateral marinho**.

Regras:

- Azul corporativo como cor de ação (botão principal, item ativo, links), mais saturado no tema escuro, sem tons pastel.
- Neutros frios (cinza-grafite) para fundos, bordas e textos.
- Tema claro: barra lateral marinho como assinatura de marca (ADR-0011). Substituída pelo ADR-0067: barra lateral clara e marinho só na página de entrada.
- Tema escuro: fundos em grafite neutro, sem tom azulado.
- Cores de status independentes da marca: verde (resolvido), âmbar (aguardando), vermelho (atrasado) e azul (em andamento).
- Contraste mínimo AA (WCAG) em textos e controles, nos dois temas.

### Consequências

- Bom, porque a barra lateral marinho dá identidade reconhecível.
- Bom, porque o tema escuro evita o visual genérico de fundo azulado e cores pastel.
- Ruim, porque o azul de "em andamento" fica próximo do azul da marca; o design system precisa diferenciá-los (por exemplo, pelo formato do indicador).
- Neutro, porque a cor da barra lateral precisa acompanhar a cor principal quando uma instalação trocar a marca. Substituída pelo ADR-0067: acompanham a cor principal o item ativo da barra lateral e o painel da entrada.

### Confirmação

O design system registra os valores dos dois temas e passa na verificação de contraste AA.

## Prós e contras das opções

### A. Azul petróleo

- Bom, porque é distinto e ligado ao contexto fiscal.
- Ruim, porque não foi a preferência de Marcelo.

### B. Índigo

- Bom, porque é moderno.
- Ruim, porque é comum em produtos SaaS e fica perto do roxo.

### C1. Azul corporativo com marinho e neutros frios

- Bom, porque tem identidade corporativa forte.
- Ruim, porque exige cuidado para distinguir o azul da marca do azul de status.

### C2. Azul corporativo com neutros quentes

- Bom, porque é acolhedor.
- Ruim, porque tem menos assinatura visual.
