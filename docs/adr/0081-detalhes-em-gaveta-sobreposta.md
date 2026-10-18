---
status: aceita
date: 2026-10-18
decision-makers: Marcelo Ribeiro
---

# ADR-0081: Detalhes de um item da lista numa gaveta sobreposta, com o fundo escurecido

Substitui o item "Detalhe do item selecionado num painel lateral (documentos, termos, membros, auditoria, chamado)" do ADR-0066.

## Contexto e problema

O ADR-0066 e o canvas aprovado em 17/10/2026 põem o detalhe do item escolhido numa coluna fixa à direita da lista. Na primeira tela que a usa, a Auditoria, Marcelo viu dois problemas:

- a coluna, de 340 px, tira espaço da tabela;
- ela é estreita demais para a tabela de mudanças, que quebra os valores no meio da palavra ("Administra dor").

Documentos, termos e membros vão ter o mesmo formato de lista com detalhe. Como mostrar o detalhe de um item escolhido numa lista?

## Fatores de decisão

- A lista com a largura toda, porque é onde a pessoa trabalha a maior parte do tempo.
- Espaço para o detalhe sem quebrar os valores.
- Um padrão só para todas as telas de lista com detalhe.
- Só cores e sombras do design system.

## Opções consideradas

- A. Coluna fixa à direita (ADR-0066 e canvas)
- B. Gaveta à direita, por cima da tela, com o fundo escurecido, aberta só ao escolher um item
- C. Página própria para o detalhe

## Resultado da decisão

Opção escolhida: **B. Gaveta sobreposta**, pedida por Marcelo na revisão da tela de Auditoria.

Regras:

- O detalhe de um item escolhido numa lista abre só quando a pessoa escolhe o item (clique ou Enter), numa gaveta à direita, por cima da tela. A lista continua com a largura toda.
- O fundo atrás da gaveta escurece com o token `overlay`. A gaveta usa `bg-elevated` e `shadow-dialog`, como os elementos que flutuam no design system.
- A gaveta tem até 640 px de largura e, no celular, a largura da tela. O conteúdo rola por dentro (ADR-0066).
- Ela fecha pelo botão Fechar, pela tecla Esc ou por um clique fora dela, e o foco volta para a lista. Enquanto está aberta, a linha do item fica marcada.
- Vale para documentos, termos, membros e auditoria.
- A página do chamado continua uma página própria, com o painel lateral da conversa (ADR-0069), porque não é o detalhe de uma linha de lista.

### Consequências

- Bom, porque a lista ganha a largura que era da coluna, e o detalhe ganha quase o dobro de largura.
- Bom, porque todas as telas de lista com detalhe se comportam igual, com um componente só.
- Ruim, porque, com a gaveta aberta, a lista fica coberta e é preciso fechar para escolher outro item.
- Neutro, porque o canvas aprovado continua valendo para o resto das telas. Só a coluna de detalhe é substituída por esta gaveta.

### Confirmação

Testes de tela na Auditoria cobrindo:

- a gaveta aberta só ao escolher um registro, com o fundo escurecido;
- o fechamento pelo botão, pelo Esc e pelo clique fora, com o foco de volta na lista;
- a largura da tela no celular.

## Prós e contras das opções

### A. Coluna fixa à direita

- Bom, porque mostra lista e detalhe ao mesmo tempo.
- Ruim, porque tira largura da lista o tempo todo e limita o detalhe a uma coluna estreita.

### B. Gaveta sobreposta

- Bom, porque a lista e o detalhe usam a largura toda, cada um no seu momento.
- Ruim, porque cobre a lista enquanto está aberta.

### C. Página própria

- Bom, porque o detalhe tem a tela inteira e um endereço próprio.
- Ruim, porque perde o contexto da lista e obriga a ir e voltar a cada item.
