---
status: aceita
date: 2026-10-13
decision-makers: Marcelo Ribeiro
---

# ADR-0013: Interface em português, com textos em arquivos de tradução desde a v1

## Contexto e problema

O código fica em inglês (CLAUDE.md global). Falta decidir o idioma das telas e dos e-mails e como tratar datas, números e fusos. O código pode ser adquirido por empresas de fora do Brasil ou multinacionais (ADR-0002, ADR-0003).

## Fatores de decisão

- Esforço da v1.
- Custo de acrescentar outro idioma no futuro.
- Consistência de datas e prazos independentemente do servidor.

## Opções consideradas

- A. Só português, com os textos escritos direto nas telas
- B. Só português na v1, com os textos em arquivos de tradução desde o início
- C. Português e inglês já na v1

## Resultado da decisão

Opção escolhida: **B. Português na v1, com textos em arquivos de tradução**, porque o custo extra é pequeno e acrescentar outro idioma depois passa a ser apenas traduzir arquivos.

Escopo:

| Item | Classificação |
| --- | --- |
| Interface e e-mails em português do Brasil, com os textos em arquivos de tradução (chaves em inglês) | v1 |
| Datas, números e moeda no formato brasileiro | v1 |
| Datas e horas guardadas em UTC e exibidas no fuso da instalação (configurável, padrão America/Sao_Paulo) | v1 |
| Tradução para inglês | depois |
| Idioma e fuso escolhidos por pessoa | depois |

### Consequências

- Bom, porque um novo idioma não exige alterar telas nem regras.
- Bom, porque prazos e datas não dependem do fuso do servidor.
- Ruim, porque cada texto de tela e de e-mail ganha uma chave em arquivo, um pequeno esforço a mais por tela.

### Confirmação

Revisão de código: nenhum texto visível ao usuário escrito direto nas telas ou nos modelos de e-mail. Testes cobrindo a conversão de datas entre UTC e o fuso configurado.

## Prós e contras das opções

### A. Só português, textos direto nas telas

- Bom, porque é o mais rápido de escrever.
- Ruim, porque traduzir depois exige caçar textos em todo o sistema.

### B. Português, com arquivos de tradução

- Bom, porque prepara outros idiomas com pouco custo.
- Ruim, porque acrescenta uma indireção em cada texto.

### C. Português e inglês na v1

- Bom, porque o produto já nasce pronto para mais mercados.
- Ruim, porque dobra a escrita e a revisão de textos, e-mails e testes sem demanda real.
