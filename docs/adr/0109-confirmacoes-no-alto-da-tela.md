---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0109: Confirmações no alto da tela, no meio

Complementa o ADR-0066 (estrutura das telas). Muda a posição do Snackbar do design system, que ficava no canto inferior.

## Contexto e problema

As confirmações curtas ("Configurações salvas.", "Chamado #105 aberto.") apareciam embaixo, no meio da tela, longe de onde a pessoa olha depois de salvar. Marcelo pediu que apareçam no alto, no meio. Onde exatamente, sem cobrir a barra lateral nem os botões do cabeçalho?

## Fatores de decisão

- Um lugar só para todas as confirmações, definido no tema, sem cada tela escolher o seu.
- Não cobrir a barra lateral (regra do Snackbar no design system), o menu e o sino do celular.
- Cobrir o mínimo da tela de trabalho enquanto a confirmação está à vista.

## Opções consideradas

1. No computador, na faixa do cabeçalho invisível, no meio da área de conteúdo; no celular, logo abaixo da barra do topo.
2. Logo abaixo do cabeçalho, nos dois.
3. No alto da janela, sobre o cabeçalho, nos dois.

## Resultado da decisão

Opção 1. O tema põe o Snackbar no alto, no meio (`anchorOrigin` padrão); no computador (a partir de 900px), centrado na faixa do cabeçalho e no meio da área de conteúdo, à direita da barra lateral, onde ficam só a trilha, à esquerda, e o sino e a conta, à direita; no celular, logo abaixo da barra do topo, sem cobrir o menu e o sino. Nenhuma tela escolhe a posição da própria confirmação.

### Consequências

- Boa, porque a confirmação aparece perto do título da tela e não cobre os botões do cabeçalho da página.
- Ruim, porque uma trilha muito longa pode ficar coberta no fim enquanto a confirmação está à vista.
