---
status: aceita
date: 2026-10-30
decision-makers: Marcelo Ribeiro
---

# ADR-0131: Botões em andamento, com o ícone de carregamento no lugar do ícone

Complementa o ADR-0062 (direção visual), o ADR-0065 (forma sóbria, status por ícone, cor e texto) e o ADR-0080 (leituras pelo TanStack Query, com o indicador das listas depois de 300 ms).

## Contexto e problema

Em 30/10/2026, Marcelo notou que, quando um botão demora a responder, não dá para saber se o clique foi aceito, se está processando ou se falhou, e que isso vale para todos os botões que mandam algo ao servidor: salvar, enviar, lembrar, concluir. Hoje o design system não tem estado em andamento para o botão: os formulários desabilitam o botão de envio enquanto gravam, sem sinal visível além do cinza, e muitas ações não mudam nada até a resposta, o que também permite o clique repetido.

Perguntas:

- Como o botão mostra que está processando?
- Quando o sinal aparece?
- Quais botões entram?
- Como garantir que todo botão novo siga a regra, sem depender de cada tela lembrar?

## Fatores de decisão

- A pessoa sabe, na hora, que o clique foi aceito.
- O botão não muda de largura nem de lugar, e o rótulo continua dizendo o que acontece (design system).
- O clique repetido não manda a ação duas vezes.
- Uma regra num lugar só, que vale para os botões que existem e os que vierem.

## Opções consideradas

Visual:

- A1. O ícone de carregamento no lugar do ícone do botão (antes do texto, quando não há ícone), com o rótulo visível e o botão inativo
- A2. Só o ícone de carregamento, no meio, sem o rótulo
- A3. O ícone de carregamento e o rótulo no gerúndio ("Salvando…")

Quando aparece:

- B1. Na hora do clique
- B2. Depois de 300 ms, como o indicador das listas

Alcance:

- C1. Botões e botões de ícone que mandam algo ao servidor
- C2. Também os itens dos menus "⋮"

Como cada botão fica sabendo:

- D1. Cada tela passa o estado em andamento a cada botão
- D2. Os componentes de botão e de formulário do próprio sistema percebem o andamento pela ação que disparam, e o lint recusa o botão do MUI usado direto

## Resultado da decisão

Opções escolhidas: **A1, B1 e C1**, por decisão de Marcelo em 30/10/2026, e **D2**.

- **Visual:** enquanto a ação processa, o ícone de carregamento do tamanho do ícone (16px, na cor do texto do botão) fica no lugar do ícone do começo, ou antes do texto quando o botão não tem ícone; no botão de ícone, no lugar do ícone. O rótulo continua o mesmo, o botão mantém a largura e as cores da variante, fica inativo (sem hover e sem novo clique) e avisa às tecnologias assistivas que está ocupado (`aria-busy`).
- **Quando:** na hora do clique, até a resposta, com sucesso ou erro.
- **Alcance:** todo botão e botão de ícone que manda algo ao servidor, nos formulários, nos diálogos e nas ações das telas. Os itens dos menus "⋮" ficam para depois.
- **Como:** o `Button` e o `IconButton` de `web/src/components` ficam em andamento enquanto a promessa devolvida pelo clique não termina, e o `Form` (também usado pelo `Dialog` com `onSubmit`) põe em andamento o botão de envio enquanto a gravação roda; o estado também pode ser passado à mão (`loading`). O lint recusa o `Button` e o `IconButton` do MUI fora desses componentes.
- **Design system:** o Button ganha o estado em andamento, no guia e na prévia.

### Consequências

- Bom, porque a pessoa sabe que o clique foi aceito, e o clique repetido não manda a ação de novo.
- Bom, porque a regra fica nos componentes, e todo botão novo já nasce com ela.
- Ruim, porque toda tela troca o botão do MUI pelo do sistema, numa mudança grande, ainda que mecânica.
- Ruim, porque uma resposta rápida faz o ícone aparecer por um instante.

### Confirmação

Testes cobrindo: o botão em andamento até a resposta, com o ícone no lugar do ícone, o rótulo e a largura mantidos; o botão sem ícone; o botão de ícone; o clique repetido sem segunda ação; o formulário e o diálogo com o botão de envio em andamento; o fim do andamento com erro; e o lint recusando o botão do MUI usado direto.

## Prós e contras das opções

### A1. Ícone no lugar do ícone

- Bom, porque o rótulo continua dizendo o que acontece, e a largura não muda.
- Ruim, porque o botão sem ícone ganha um elemento a mais no começo.

### A2. Só o ícone

- Bom, porque é o sinal mais forte.
- Ruim, porque o rótulo some, e a pessoa perde o que clicou.

### A3. Rótulo no gerúndio

- Bom, porque o texto diz o que está acontecendo.
- Ruim, porque cada botão precisa de um segundo texto, e a largura muda.

### B1 e B2. Na hora ou depois de 300 ms

- B1 confirma o clique na hora, mas pisca nas respostas rápidas.
- B2 não pisca, mas deixa um intervalo sem sinal.

### C2. Itens dos menus

- Bom, porque cobre as ações do "⋮".
- Ruim, porque o menu teria de ficar aberto até a resposta, um comportamento novo.

### D1. Cada tela passa o estado

- Bom, porque é explícito.
- Ruim, porque depende de cada tela lembrar, e um botão novo nasce sem o sinal.
