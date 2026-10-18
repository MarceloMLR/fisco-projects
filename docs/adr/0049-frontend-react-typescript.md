---
status: aceita
date: 2026-10-15
decision-makers: Marcelo Ribeiro
---

# ADR-0049: Frontend em React com TypeScript

## Contexto e problema

O ideia.md deixou em aberto o frontend: Blazor ou React. Durante o bloco de arquitetura, Marcelo decidiu que o frontend será em React com TypeScript e uma biblioteca de componentes. A biblioteca de componentes e a biblioteca de Gantt (ADR-0021) serão escolhidas juntas, em ADR próprio.

## Fatores de decisão

- Ecossistema amplo de componentes e de bibliotecas de Gantt.
- Tipagem forte no frontend.
- Facilidade de levar o design system para o código.

## Opções consideradas

- A. React com TypeScript e uma biblioteca de componentes
- B. Blazor

## Resultado da decisão

Opção escolhida: **A. React com TypeScript**, por decisão de Marcelo.

Regras:

- O frontend é uma aplicação React em TypeScript, separada do backend no código.
- O backend ASP.NET Core expõe a API usada pelo frontend.
- A biblioteca de componentes e a biblioteca de Gantt serão decididas juntas.
- A forma de autenticação entre o React e a API (por exemplo, backend que guarda a sessão em cookie, ou tokens no navegador) será decidida em ADR próprio, mantendo a página de entrada do ADR-0009.

### Consequências

- Bom, porque o ecossistema do React oferece mais opções de componentes e de Gantt.
- Bom, porque o TypeScript dá tipagem forte também no frontend.
- Ruim, porque o projeto passa a ter duas linguagens (C# e TypeScript) e dois conjuntos de ferramentas e testes.
- Neutro, porque os contratos da API precisam ser mantidos entre backend e frontend (por exemplo, gerando os tipos do TypeScript a partir da especificação da API).

### Confirmação

O repositório tem o frontend em React com TypeScript em modo estrito, com build e testes próprios rodando no CI.

## Prós e contras das opções

### A. React com TypeScript

- Bom, porque tem o maior ecossistema de componentes e de Gantt.
- Ruim, porque exige duas linguagens no projeto.

### B. Blazor

- Bom, porque mantém tudo em C#.
- Ruim, porque tem menos opções de componentes e de Gantt.
