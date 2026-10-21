## Context

Na tabela padrão, as linhas se escolhem pelo `onRowClick` do Data Grid, e várias colunas têm um link para o mesmo endereço (o título do chamado, o nome do projeto). O clique no link dispara a navegação do link e, ao subir até a linha, a da linha: duas entradas no histórico. A gaveta do termo grava `?term=` com uma entrada nova ao abrir e outra ao fechar. Depois de salvar ou cancelar um formulário, a navegação acrescenta a página seguinte, e o formulário fica atrás dela.

## Goals / Non-Goals

**Goals:**
- Um Voltar volta uma página, nas tabelas, nas gavetas e depois dos formulários.

**Non-Goals:**
- Tirar as visões da página Chamados do histórico.

## Decisions

- **Tabela**: `fromControl(event)` olha se o clique ou o Enter vieram de dentro de um link, botão, campo ou item de menu (`closest`), e então a linha não é escolhida. Vale para todas as tabelas de uma vez.
- **Gaveta**: `setParams(..., { replace: true })` ao abrir e ao fechar o termo, como o `?item=` do cronograma.
- **Formulários**: `navigate(..., { replace: true })` depois de criar o projeto ou o chamado, ao cancelar, e quando a pessoa sai do próprio projeto.

## Risks / Trade-offs

- [Alguém conta com o Voltar para fechar a gaveta] → ela fecha pelo X, pelo Esc e por um clique fora, como as outras gavetas.
