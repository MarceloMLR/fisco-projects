---
status: aceita
date: 2026-10-30
decision-makers: Marcelo Ribeiro
---

# ADR-0132: Tempo mínimo do botão em andamento

Complementa o ADR-0131 (botões em andamento, com o ícone de carregamento no lugar do ícone).

## Contexto e problema

O ADR-0131 pôs o botão em andamento do clique até a resposta e aceitou, como consequência ruim, que uma resposta rápida faz o ícone aparecer por um instante. No teste no `dev`, em 30/10/2026, o servidor respondeu em pouco mais de 100 ms, o ícone só piscou, e Marcelo não chegou a ver que o clique foi aceito; num botão que fica desabilitado depois da resposta, como Salvar sem mais alterações, sobrou só o cinza.

Pergunta: o ícone fica só enquanto a resposta não chega, ou por um tempo mínimo?

## Fatores de decisão

- A pessoa vê que o clique foi aceito, mesmo com o servidor rápido.
- A ação não fica mais lenta.
- Uma regra num lugar só, para todo botão em andamento.

## Opções consideradas

- E1. Só enquanto a resposta não chega (como no ADR-0131)
- E2. Pelo menos meio segundo, mesmo quando a resposta chega antes

## Resultado da decisão

Opção escolhida: **E2**, por decisão de Marcelo em 30/10/2026.

- O botão em andamento mostra o ícone de carregamento por pelo menos meio segundo a partir do clique quando a ação vai ao servidor; se a resposta chega antes, continua em andamento até completar, sem aceitar novo clique. A ação que não vai ao servidor, como o formulário recusado pelas próprias regras da tela, termina na hora, para os erros aparecerem e o novo clique funcionar.
- A ação não espera: a resposta é aplicada na hora, e o diálogo que fecha com ela fecha na hora; o tempo mínimo vale para o botão que continua na tela.
- A regra fica no `Button` e no `IconButton` de `web/src/components`, sobre todas as fontes do andamento (a promessa do clique, o envio do formulário e o `loading`).

### Consequências

- Bom, porque o sinal aparece sempre, e a pessoa sabe que o clique foi aceito.
- Bom, porque nenhuma tela precisa fazer nada.
- Ruim, porque, depois de uma resposta rápida, o botão fica até meio segundo sem aceitar um novo clique.

### Confirmação

Testes cobrindo: a resposta rápida mantém o botão em andamento até meio segundo do clique, sem aceitar novo clique, e o botão volta ao normal depois; o formulário recusado sem ir ao servidor não fica em andamento.

## Prós e contras das opções

### E1. Só enquanto espera

- Bom, porque o botão volta assim que pode.
- Ruim, porque, com o servidor rápido, o ícone pisca e passa despercebido.

### E2. Pelo menos meio segundo

- Bom, porque o sinal sempre se vê.
- Ruim, porque segura o botão um pouco depois da resposta.
