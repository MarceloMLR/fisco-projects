---
status: aceita
date: 2026-10-31
decision-makers: Marcelo Ribeiro
---

# ADR-0133: Avisos e movimento das telas

Complementa o ADR-0062 (direção visual), o ADR-0065 (forma sóbria), o ADR-0109 (avisos no alto, no meio) e o ADR-0131 (botões em andamento). Troca a regra do design system "movimento curto e discreto, sem animações de entrada".

## Contexto e problema

Em 30/10/2026, Marcelo achou os avisos feios: nem sempre centralizados, maiores do que deviam, brancos no tema escuro, aparecendo e sumindo do nada. O mesmo nos diálogos, nas gavetas, na troca de página e nas abas do projeto, que vão aparecendo uma a uma conforme as contagens chegam. No código, cada tela tinha o próprio aviso e apagava o texto antes da animação de saída (a caixa encolhia vazia e sumia); o aviso aberto dentro de outra janela era posicionado a partir dela; os diálogos e as gavetas saíam da tela antes de poder animar a saída; e cada contagem das abas chegava num pedido diferente, alargando a aba e empurrando as outras.

Marcelo comparou as opções numa página de prévia com a tela do produto (avisos, diálogos, gaveta, troca de página e contagens, nos dois temas, em câmera lenta e com "reduzir movimento") e escolheu em 31/10/2026.

## Fatores de decisão

- A pessoa vê o que mudou, sem a tela parecer brusca nem lenta.
- Movimento curto, que não atrasa a ação nem a navegação.
- Uma regra num lugar só, para os avisos, as janelas e as páginas que existem e as que vierem.
- Respeito a quem pede ao sistema para reduzir o movimento.

## Opções consideradas

Aviso:

- A1. Como hoje, cada tela com o seu, cores invertidas
- A2. Um só para todas as telas, no alto e no meio do conteúdo, um de cada vez
- A3. Um só, no canto de baixo, à esquerda, empilhando até três

Visual do aviso:

- V1. Cores invertidas, mais compacto
- V2. Fundo de janela, com borda, sombra leve e o ícone de concluído

Diálogo: D1 como hoje (esmaece ao abrir e some ao fechar); D2 só esmaecer; D3 esmaecer e crescer.

Gaveta: G1 como hoje (desliza ao abrir e some ao fechar); G2 desliza nos dois sentidos.

Troca de página: P1 como hoje; P2 esmaecer; P3 esmaecer e subir.

Contagens das abas: C1 como hoje, uma a uma; C2 juntas, no lugar já guardado.

## Resultado da decisão

Opções escolhidas por Marcelo em 31/10/2026: **A2, V2, D3, G2, P3 e C2**.

- **Aviso:** um só para todas as telas, na estrutura delas, no alto, no meio da área de conteúdo (na faixa do cabeçalho no computador, logo abaixo da barra do topo no celular, ADR-0109), um de cada vez, em fila. Fundo de janela (`bg-elevated`), borda, sombra de menu e o ícone de concluído em `status-success-text`; o aviso de uma falha leva o ícone de erro em `status-danger-text`. Do tamanho do texto, até 420px. Entra descendo 8px e esmaecendo em 200 ms e sai esmaecendo em 150 ms, com o texto. Fica 5 segundos, 8 quando tem ação, parado enquanto o ponteiro ou o foco está nele, e tem o botão de fechar.
- **Diálogo:** o fundo esmaece e a janela cresce de 98% subindo 8px em 200 ms; ao fechar, o caminho de volta em 150 ms. O diálogo fica na tela até a saída terminar.
- **Gaveta:** desliza da direita ao abrir (250 ms) e ao fechar (200 ms), com o fundo esmaecendo.
- **Troca de página:** só o conteúdo da página, ou só a aba do projeto, esmaece subindo 4px em 200 ms; a barra lateral e o cabeçalho ficam parados, e a página anterior sai na hora.
- **Contagens das abas:** o lugar de cada contagem aparece com as abas, e os números entram juntos, esmaecendo em 150 ms, quando a última chega.
- **Curvas:** entrada `cubic-bezier(0.2, 0, 0, 1)`, saída `cubic-bezier(0.4, 0, 1, 1)`.
- **Reduzir movimento:** com a opção ligada no sistema, nada desliza nem cresce; tudo só esmaece, em 150 ms.

Os avisos entram na Fundação-5.6; as janelas, a troca de página e as contagens, na Fundação-5.7.

### Consequências

- Bom, porque os avisos ficam sempre no mesmo lugar e com o mesmo tamanho, e uma confirmação continua visível mesmo quando a tela que a deu fecha ou muda.
- Bom, porque abrir e fechar janelas e trocar de página fica suave sem esperar.
- Ruim, porque toda tela troca o aviso próprio pelo da estrutura, numa mudança grande, ainda que mecânica.
- Ruim, porque as contagens das abas esperam a mais lenta.

### Confirmação

Testes cobrindo: o aviso único, a fila, o tempo com e sem ação, parado com o ponteiro, o fechar e o de falha; o diálogo e a gaveta até o fim da saída; a troca de página e de aba; as contagens juntas; e o movimento reduzido.

## Prós e contras das opções

### A3. No canto de baixo

- Bom, porque fica perto de onde a pessoa costuma estar e empilha vários.
- Ruim, porque sai do lugar que o ADR-0109 escolheu e disputa espaço com as tabelas.

### V1. Invertido

- Bom, porque se destaca mais.
- Ruim, porque fica branco no tema escuro, que foi o incômodo.

### D2 e P2. Só esmaecer

- Bom, porque é o mais discreto.
- Ruim, porque a janela e a página parecem só acender, sem direção.
