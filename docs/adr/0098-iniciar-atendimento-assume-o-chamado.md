---
status: aceita
date: 2026-10-20
decision-makers: Marcelo Ribeiro
---

# ADR-0098: "Iniciar atendimento" torna responsável quem atende o chamado sem responsável

Complementa o ADR-0024 (o fluxo de status) e o ADR-0068 (o responsável pela primeira resposta).

## Contexto e problema

Pelo ADR-0068, o chamado ganha responsável na primeira mensagem de quem atende o time, ou quando alguém o atribui. As ações "Iniciar triagem" (Aberto → Em triagem) e "Iniciar atendimento" (Em triagem → Em andamento) só mudam o status: quem começa a atender pelo botão não aparece como responsável até escrever. Marcelo quer que "Iniciar atendimento" torne responsável quem clica, e que "Iniciar triagem" continue sem mexer no responsável. O que acontece quando o chamado já tem responsável?

## Fatores de decisão

- Quem começa a atender aparece como responsável, sem precisar escrever.
- Ninguém perde um chamado sem querer.
- As regras de quem pode ser responsável continuam num lugar só (`TicketAccess.CanBeAssignee`).

## Opções consideradas

- A. Assume só se o chamado não tiver responsável; com responsável, nada muda
- B. Sempre assume, trocando o responsável e avisando o anterior
- C. Nada muda: o responsável continua vindo da primeira mensagem ou da atribuição

## Resultado da decisão

Opção escolhida: **A**, por escolha de Marcelo, porque quem começa a atender passa a ser o responsável sem tirar o chamado de outra pessoa.

Regras:

- "Iniciar atendimento" num chamado sem responsável torna responsável quem fez a ação, se essa pessoa pode ser responsável pelo chamado (quem atende o time ou o gerente do projeto, ADR-0068); ela passa a acompanhar o chamado, e a conversa registra o evento, como na atribuição.
- Com responsável, o responsável continua o mesmo.
- O administrador que não está no projeto não pode ser responsável: o chamado continua sem responsável.
- "Iniciar triagem" não mexe no responsável.
- A regra da primeira mensagem (ADR-0068) continua.

### Consequências

- Bom, porque o responsável aparece assim que o atendimento começa.
- Bom, porque um chamado com responsável nunca troca de mãos por um clique.
- Ruim, porque quem quiser assumir o chamado de outra pessoa continua precisando de "Atribuir".

### Confirmação

Testes cobrindo: "Iniciar atendimento" sem responsável torna responsável quem atende e o faz acompanhar; com responsável, nada muda; o administrador de fora não vira responsável; "Iniciar triagem" não define responsável.

## Prós e contras das opções

### A. Assume só sem responsável

- Bom, porque não tira chamados de ninguém.
- Ruim, porque não serve para assumir o chamado de outra pessoa.

### B. Sempre assume

- Bom, porque o botão sempre leva o chamado para quem clica.
- Ruim, porque um clique tira o chamado de quem já o atendia.

### C. Nada muda

- Bom, porque não mexe no fluxo.
- Ruim, porque o chamado em andamento pode ficar sem responsável.
