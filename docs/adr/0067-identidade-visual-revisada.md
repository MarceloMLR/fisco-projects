---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0067: Identidade visual revisada, com estrutura clara, marinho só na entrada, verde no concluído e cantos por tamanho

Substitui, no ADR-0063, a regra "Tema claro: barra lateral marinho como assinatura de marca" e a consequência de a barra lateral acompanhar a cor principal; e, no ADR-0065, a linha "Cantos: pequenos (cerca de 4 px)".

## Contexto e problema

Com a nova estrutura das telas (ADR-0066), a barra lateral marinho do ADR-0063 deixa de existir, e na revisão do design Marcelo pediu a parte concluída das barras em verde, para dar destaque ao que já foi feito, e uma página de entrada mais bonita, com fundo de contraste e sombra. Os cantos de 4 px do ADR-0065 ficaram iguais em peças de tamanhos muito diferentes. Como fica a identidade visual?

## Fatores de decisão

- Telas calmas para uso o dia inteiro (ADR-0062).
- Identidade reconhecível sem uma barra colorida.
- O que já foi concluído visível de relance.
- Contraste AA nos dois temas (ADR-0063).

## Opções consideradas

- A. Manter os ADR-0063 e ADR-0065 como estão (barra lateral marinho, azul no concluído, cantos de 4 px)
- B. Estrutura clara, marinho só na página de entrada, verde no concluído e cantos por tamanho da peça

## Resultado da decisão

Opção escolhida: **B**, aprovada por Marcelo na revisão do design.

Regras:

| Item | Definição |
| --- | --- |
| Estrutura | Barra lateral em cinza levíssimo e cabeçalho na cor do conteúdo; nenhuma área grande em marinho dentro do produto. A cor da marca continua só em ações, item ativo e links (ADR-0062) |
| Página de entrada | Único lugar com o marinho inteiro: cartão dividido com sombra sobre fundo marinho profundo; painel com degradê e formas geométricas discretas (anéis, pontos e cartões em camadas). É também o único lugar com degradê |
| Concluído | Verde (o mesmo do status "sucesso") na parte concluída das barras de progresso, da faixa de fases e das tarefas do cronograma, inclusive no caminho crítico, que mantém o contorno e o restante em vermelho |
| Vermelho | Reservado a notificações não lidas, atraso e perigo |
| Cantos | Crescem com a peça: cerca de 3 px em barras e etiquetas de papel, 6 px em botões e campos, 10 px em cartões, tabelas e diálogos, 12 px no cartão da entrada; valores exatos no design system |
| Troca da cor principal | Acompanham a nova cor o item ativo da barra lateral e o painel da entrada; a barra lateral continua clara |

Continuam valendo as demais regras dos ADR-0063 (azul de ação, neutros frios, escuro em grafite, status independentes da marca, contraste AA) e ADR-0065 (status por ícone, cor e texto; bordas em vez de sombras; densidade confortável).

### Consequências

- Bom, porque as telas ficam mais leves e o conteúdo ganha destaque.
- Bom, porque o verde mostra o que já foi feito em listas, projetos e no cronograma.
- Ruim, porque a identidade passa a depender da tipografia, da faixa de fases e da página de entrada, e não mais de uma barra colorida.
- Neutro, porque o verde do concluído é o mesmo do status "sucesso"; a etiqueta de status continua com ícone e texto para não haver confusão.

### Confirmação

O design system FiscoProjects registra os tokens dos dois temas (estrutura, `brand-panel-*`, `signin-*`, `gantt-progress` e cantos) e passa na verificação de contraste AA.

## Prós e contras das opções

### A. Manter os ADRs como estão

- Bom, porque não exige mudanças.
- Ruim, porque não combina com a estrutura aprovada e não destaca o concluído.

### B. Ajustes da revisão

- Bom, porque é coerente com a nova estrutura e com o pedido de destaque do concluído.
- Ruim, porque reduz a cor de marca visível dentro do produto.
