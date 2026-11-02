---
status: aceita
date: 2026-11-02
decision-makers: Marcelo Ribeiro
---

# ADR-0147: Fundo da página de entrada na cor secundária

Complementa o ADR-0146. Substitui o item "Fora da troca" dele só no fundo atrás do painel da entrada, que passa a mudar com as cores da instalação.

## Contexto e problema

Pelo ADR-0146, a cor secundária pinta o painel da página de entrada, mas o fundo em volta do cartão continua o azul-marinho do design system (`signin-bg`, com o brilho `signin-glow`). Ao testar a seção Cores com uma secundária vinho, Marcelo viu o painel vinho sobre o fundo azul e pediu que o fundo também mudasse. O fundo deve acompanhar as cores, e como?

## Fatores de decisão

- A página de entrada é a primeira coisa que a pessoa vê, e deve ter a cor da empresa.
- Nenhum texto fica direto sobre o fundo: o cartão é uma superfície, e o painel tem o próprio contraste.
- Sem nenhuma cor, a página fica exatamente a do design system.

## Opções consideradas

- A. O fundo e o brilho acompanham a cor do painel, com a claridade do design system e o matiz da cor
- B. O fundo fica o do design system, como no ADR-0146
- C. Uma cor própria para o fundo da entrada

## Resultado da decisão

Opção escolhida: **A**, porque o painel e o fundo ficam da mesma cor sem pedir uma quarta cor.

Regras:

- **Cor que manda:** `signin-bg` e `signin-glow` seguem a cor secundária ou, sem ela, a principal, como o painel.
- **Claridade:** cada um mantém a claridade do design system nos dois temas e ganha o matiz e a saturação da cor, como os outros tokens derivados (ADR-0082). O fundo continua escuro em qualquer cor, inclusive no amarelo.
- **Sem nenhuma cor:** os valores do `tokens.json`.
- **Prévia:** a da seção Cores mostra o painel sobre o fundo da entrada.

### Consequências

- Bom, porque a entrada fica toda na cor da empresa.
- Bom, porque não há contraste novo a conferir: nenhum texto fica sobre o fundo.
- Neutro, porque uma cor muito saturada vira um fundo escuro bem tingido. A prévia mostra antes de salvar.

### Confirmação

Testes cobrindo:

- sem cor, os valores do design system;
- com a secundária, o fundo no matiz dela, nos dois temas;
- sem a secundária, o fundo no matiz da principal;
- o fundo tão escuro quanto o do design system.

## Prós e contras das opções

### A. O fundo acompanha a cor do painel

- Bom, porque a página fica coerente com o painel.
- Ruim, porque o azul do design system some da entrada quando há cor.

### B. O fundo do design system

- Bom, porque não muda nada.
- Ruim, porque um painel de outra cor sobre o azul-marinho destoa.

### C. Uma cor própria

- Bom, porque dá controle total.
- Ruim, porque pede uma quarta cor para conferir, e quase sempre seria a secundária.
