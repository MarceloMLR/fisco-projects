---
status: aceita
date: 2026-11-03
decision-makers: Marcelo Ribeiro
---

# ADR-0153: Abrir pelo Teams direto na entrada da Microsoft

Complementa o ADR-0152: os links da página do FiscoProjects no Teams ("Responder no FiscoProjects" e "Abrir no FiscoProjects") passam a abrir o item no navegador sem passar pela página de entrada do FiscoProjects quando não há sessão. A sessão por cookie do ADR-0054, com as 8 horas sem uso e o fim ao fechar o navegador, continua como está.

## Contexto e problema

Na página do Teams, "Responder no FiscoProjects" abre o chamado no navegador (ADR-0152). O cookie da sessão não é persistente e expira depois de 8 horas sem uso (ADR-0054), então, quase sempre que a pessoa chega pelo Teams, não há sessão: aparece a página de entrada, a pessoa confirma o e-mail (já preenchido com o último usado, ADR-0009) e só então vai à Microsoft, que muitas vezes nem pede senha, porque o navegador já tem a conta da empresa.

Em 03/11/2026, ao testar o chamado no Teams, Marcelo perguntou se a pessoa não poderia ficar conectada, para "Responder" abrir direto o chamado, sem entrar toda vez. Das três saídas apresentadas, escolheu ir direto à entrada da Microsoft.

Como o link do Teams leva ao item sem a pessoa passar pela página de entrada, sem enfraquecer a sessão?

## Fatores de decisão

- Menos passos para quem chega pelo Teams, que já está na conta da empresa.
- A sessão não muda: nada de cookie que sobrevive ao navegador nem sessão mais longa (ADR-0054).
- Num computador compartilhado, quem escolheu Sair não deve ser levado de volta à conta pela Microsoft sem ver a página de entrada.
- Nenhum dado pessoal no endereço (ADR-0057) e nenhum caminho para outro site (ADR-0149).
- Sem permissão nova no Entra ID e sem mudança na API.

## Opções consideradas

- A. Link do Teams direto na entrada da Microsoft, salvo depois de Sair
- B. "Manter conectado": cookie persistente por alguns dias
- C. Sessão mais longa para todos

## Resultado da decisão

Escolhida a opção A, por Marcelo, porque resolve quem chega pelo Teams sem mudar a sessão de ninguém.

- Os links da página do Teams levam a uma página de passagem do próprio FiscoProjects (`/teams/open?item=`), fora da estrutura das telas e sem sessão, com o caminho do item conferido como hoje (`safeItemPath`).
- Com sessão, ela abre o item.
- Sem sessão e com a entrada pelo Entra ligada, ela pede o endereço da Microsoft pelo `POST /api/session/entra` que a página de entrada já usa, com o último e-mail usado no navegador como dica (sem ele, sem dica) e o item como volta, e vai direto para lá. Quando o navegador já tem a conta da empresa, a Microsoft devolve a pessoa sem pedir nada, e o item abre.
- Depois de "Sair" neste navegador, ou sem a entrada pelo Entra, ela mostra a página de entrada, que leva ao item depois de entrar, como hoje.
- "Sair" passa a ficar guardado no navegador, ao lado do último e-mail (ADR-0009), até a próxima entrada; só a marca de que a pessoa saiu, nunca um token (ADR-0054).
- A recusa da Microsoft (convidado, sem atribuição, desativado, falha) volta à página de entrada com o motivo, como em toda entrada pelo Entra (ADR-0117).

### Consequências

- Bom, porque quem chega pelo Teams com a conta da empresa aberta no navegador vai do aviso ao chamado sem digitar nada.
- Bom, porque a sessão, as 8 horas e o fim ao fechar o navegador ficam como estão.
- Bom, porque não há rota nova na API nem permissão nova no Entra ID.
- Neutro, porque sem a conta aberta no navegador a Microsoft pede a conta, como já pediria depois da página de entrada.
- Neutro, porque a marca de Sair fica no navegador: limpar os dados do site a esquece, e então o link do Teams volta a ir direto à Microsoft.

### Confirmação

Testes de tela da página de passagem (com sessão, sem sessão e com o Entra, depois de Sair, sem o Entra, caminho de fora) e dos links da página do Teams; teste da marca de Sair guardada e esquecida na entrada seguinte.

## Prós e contras das opções

### A. Link do Teams direto na entrada da Microsoft, salvo depois de Sair

- Bom, porque tira a página de entrada do caminho de quem já está na conta da empresa.
- Bom, porque não muda a sessão.
- Ruim, porque só vale para quem chega pelo Teams; quem abre o FiscoProjects pelo favorito ainda vê a página de entrada.

### B. "Manter conectado": cookie persistente por alguns dias

- Bom, porque vale para qualquer entrada.
- Ruim, porque muda o ADR-0054: a sessão sobrevive ao navegador, o que pede cuidado com computadores compartilhados e uma escolha a mais na entrada.

### C. Sessão mais longa para todos

- Bom, porque é só uma configuração.
- Ruim, porque não resolve o navegador fechado e aumenta o tempo de uma sessão esquecida aberta.
