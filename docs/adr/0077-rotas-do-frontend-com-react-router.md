---
status: aceita
date: 2026-10-18
decision-makers: Marcelo Ribeiro
---

# ADR-0077: Rotas do frontend com React Router

## Contexto e problema

O frontend é uma aplicação de página única em React (ADR-0049, ADR-0076), servida pelo host da API, que entrega a página principal para todo caminho fora de `/api` (ADR-0047). A barra lateral, a trilha do cabeçalho e o menu do celular (ADR-0066) precisam saber em que tela a pessoa está, e cada tela precisa de um endereço próprio para ser aberta direto ou por um link de e-mail (ADR-0037). Com que biblioteca as rotas são definidas?

## Fatores de decisão

- Endereços próprios para cada tela, com voltar e avançar do navegador.
- Rotas aninhadas (projeto e as abas do projeto, ADR-0011).
- Biblioteca madura, de licença permissiva, sem exigir um servidor Node.

## Opções consideradas

- A. React Router, no modo de biblioteca
- B. TanStack Router
- C. Rotas próprias sobre a History API

## Resultado da decisão

Opção escolhida: **A. React Router**, no modo de biblioteca (sem o modo de framework, que traz servidor e renderização no servidor).

Regras:

- As rotas ficam num lugar só no frontend, com caminhos em inglês (ADR-0074): `/`, `/projects`, `/tickets` e os que vierem com cada módulo.
- Endereço desconhecido leva ao Início.
- Carregar dados pelas rotas (loaders) só com decisão própria, junto da primeira chamada à API.

### Consequências

- Bom, porque é a biblioteca de rotas mais usada com React e cobre rotas aninhadas e links diretos.
- Bom, porque não depende de servidor próprio: o host do ASP.NET Core continua servindo a página principal.
- Ruim, porque as versões principais do React Router mudam a API com frequência; a troca de versão segue o ADR-0060.

### Confirmação

As telas da navegação principal abrem pelo próprio endereço, e os testes do frontend cobrem as rotas e o endereço desconhecido.

## Prós e contras das opções

### A. React Router

- Bom, porque é maduro, conhecido e tem licença MIT.
- Ruim, porque a tipagem dos parâmetros é menos forte que a do TanStack Router.

### B. TanStack Router

- Bom, porque tem rotas e parâmetros totalmente tipados.
- Ruim, porque é menos difundido e muda de versão com frequência.

### C. Rotas próprias

- Bom, porque não traz dependência.
- Ruim, porque reimplementa o que as bibliotecas resolvem (rotas aninhadas, links, voltar e avançar).
