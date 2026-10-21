---
status: aceita
date: 2026-10-21
decision-makers: Marcelo Ribeiro
---

# ADR-0105: O que entra no histórico do navegador

Complementa o ADR-0066 (estrutura das telas) e o ADR-0081 (detalhes em gaveta).

## Contexto e problema

Marcelo notou que, às vezes, precisa clicar duas vezes em Voltar para sair de uma página. Na investigação, clicar no título de um chamado ou no nome de um projeto, dentro da tabela, abria a página duas vezes: o link abria a página e a linha, escolhida pelo mesmo clique, abria de novo. Abrir e fechar a gaveta de um termo de aceite também deixava duas entradas, e o formulário de um projeto ou de um chamado novo continuava no histórico depois de salvo. O que deve entrar no histórico?

## Fatores de decisão

- Um Voltar desfaz uma navegação que a pessoa fez, nunca uma que ela não percebeu.
- Os endereços continuam abrindo o que abrem hoje, inclusive os dos avisos (`?term=`, `?item=`).

## Opções consideradas

1. Corrigir cada tela que navega.
2. Regras únicas para as navegações repetidas, na tabela padrão e nas gavetas.

## Resultado da decisão

Opção 2, com três regras:

- Um link ou botão dentro de uma linha da tabela faz só a ação dele: o clique ou o Enter nele não escolhem a linha também (`DataTable`).
- Uma gaveta aberta pelo endereço (o termo, `?term=`) troca o endereço ao abrir e ao fechar, sem entrar no histórico, como o detalhe do cronograma (`?item=`).
- Um formulário concluído ou cancelado (Novo projeto, Abrir chamado) é trocado pela página seguinte, e quem sai de um projeto vai para a lista no lugar dele: Voltar não volta ao formulário nem ao projeto que não abre mais.

As visões da página Chamados continuam no histórico, como páginas.

### Consequências

- Boa, porque um Voltar sempre volta uma página.
- Neutra, porque a gaveta não fecha pelo Voltar: ela fecha pelo X, pelo Esc ou por um clique fora.
