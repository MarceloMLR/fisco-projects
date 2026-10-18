---
status: aceita
date: 2026-10-16
decision-makers: Marcelo Ribeiro
---

# ADR-0062: Direção visual profissional e sóbria

## Contexto e problema

O FiscoProjects é usado o dia inteiro, com telas densas (listas de chamados, Gantt, documentos), em temas claro e escuro (ADR-0014), com MUI (ADR-0050). Marcelo quer um visual com identidade própria, que não pareça genérico nem uma tela da Microsoft. Qual a direção visual?

## Fatores de decisão

- Conforto no uso diário e legibilidade em telas densas.
- Destaque para o que importa (status, prazos, atrasos).
- Identidade própria.

## Opções consideradas

- A. Profissional e sóbrio
- B. Moderno e expressivo
- C. Parecido com o Microsoft 365

## Resultado da decisão

Opção escolhida: **A. Profissional e sóbrio**: fundos neutros, cor de marca usada com parcimônia (botão principal, item ativo, links), cores de status bem marcadas, cantos discretos e sem ilustrações decorativas.

### Consequências

- Bom, porque tabelas e Gantt ficam legíveis e o status se destaca sobre um fundo calmo.
- Bom, porque o visual cansa menos no uso prolongado.
- Ruim, porque impressiona menos numa primeira olhada; a identidade vem de detalhes (cor, tipografia, barra lateral), definidos nos ADRs seguintes.

### Confirmação

O design system no Claude Design aplica a cor de marca só em ações principais, itens ativos e links, e as telas são revisadas nos dois temas.

## Prós e contras das opções

### A. Profissional e sóbrio

- Bom, porque é confortável e legível.
- Ruim, porque é menos chamativo em demonstrações.

### B. Moderno e expressivo

- Bom, porque chama atenção.
- Ruim, porque cansa e polui telas densas.

### C. Parecido com o Microsoft 365

- Bom, porque é familiar.
- Ruim, porque tira a identidade própria do produto.
