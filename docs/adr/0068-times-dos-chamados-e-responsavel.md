---
status: aceita
date: 2026-10-17
decision-makers: Marcelo Ribeiro
---

# ADR-0068: Chamados só para os times Funcional e Desenvolvimento, sem filas, com o responsável definido pela primeira resposta e trocado só pelos gestores

Substitui:

- no ADR-0023, os destinos com filas compartilhadas e a fila "TI", o responsável opcional na abertura e o "Assumir chamado"; o time "Técnico" passa a se chamar "Desenvolvimento";
- no ADR-0027, a troca do responsável por quem atende o destino;
- no ADR-0029, o responsável e a fila na abertura e o atalho "Abrir chamado" na tela inicial;
- no ADR-0040, a regra sobre quem atende numa fila;
- no ADR-0044, a relação "atendentes da fila", o "Assumir" na matriz e a troca do responsável por quem atende.

## Contexto e problema

O ADR-0023 previa que quem abre o chamado escolhe o destino (time funcional, time técnico ou uma fila compartilhada, como a TI) e, opcionalmente, o responsável; quem atende podia usar "Assumir chamado" e trocar o responsável dentro do destino (ADR-0027, ADR-0044). Na revisão das telas, Marcelo decidiu que quem abre o chamado não endereça o responsável: o chamado vai para um time, e nesse time alguém assume ou o gerente ou o líder técnico define o responsável. Decidiu também que, a princípio, não haverá equipe de TI nem filas, só os dois times do projeto, e que o "Assumir" é desnecessário, porque quem responder primeiro assume. Como os chamados chegam a quem atende?

## Fatores de decisão

- Abertura simples para quem pede ajuda, sem precisar saber quem vai atender.
- Nenhum chamado esquecido sem responsável.
- Controle da distribuição pelo gerente e pelo líder técnico.
- Menos regras na v1 (sem filas e sem atendentes de fora do projeto).

## Opções consideradas

- A. Manter o desenho atual: times e filas, responsável opcional na abertura, "Assumir" e troca por quem atende
- B. Dois times, sem filas, responsável definido pela primeira resposta do time e trocado só pelos gestores

## Resultado da decisão

Opção escolhida: **B**, por decisão de Marcelo.

Regras:

- Destinos na abertura: só Funcional (colaboradores) e Desenvolvimento (líder técnico e desenvolvedores), conforme o ADR-0040. Time sem ninguém para atender não aparece na abertura.
- O chamado entra sem responsável. A primeira resposta de alguém do time de destino torna essa pessoa responsável. Não existe botão "Assumir".
- A resposta só define o responsável se o chamado ainda não tiver um. Responder a um chamado que já tem responsável nunca o tira de quem está, e isso vale para todos, inclusive o líder técnico, que faz parte do time de Desenvolvimento.
- Respostas do solicitante, de quem só acompanha o chamado e do gerente do projeto não definem o responsável.
- Só o administrador, o gerente e o líder técnico trocam o responsável, um a um ou em lote. O novo responsável sai de quem atende o time de destino ou é o gerente do projeto.
- Transferir entre Funcional e Desenvolvimento continua permitido a quem atende e aos gestores (ADR-0044). A transferência deixa o chamado sem responsável, e a primeira resposta de alguém do novo time define o próximo.
- "Abrir chamado" só aparece na página Chamados e na aba Chamados do projeto (com o projeto já preenchido).
- A lista de chamados ganha as visões "Sem responsável nos meus times", com o número em destaque, e "Que acompanho".

Escopo:

| Item | Classificação |
| --- | --- |
| Destinos Funcional e Desenvolvimento, sem escolha de responsável na abertura | v1 |
| Responsável definido pela primeira resposta de alguém do time, só quando o chamado ainda não tem responsável | v1 |
| Troca do responsável só por administrador, gerente e líder técnico, um a um ou em lote | v1 |
| Transferência entre os dois times, deixando o chamado sem responsável | v1 |
| Visões "Sem responsável nos meus times" e "Que acompanho" | v1 |
| Filas compartilhadas (como a TI da empresa) e atendentes de fora do projeto | depois, se surgir a necessidade |
| Distribuição automática entre quem atende (rodízio) | depois (mantém o ADR-0023) |

### Consequências

- Bom, porque abrir um chamado fica mais simples e não há passo manual para assumir.
- Bom, porque a distribuição fica com o gerente e o líder técnico.
- Bom, porque sem filas some um conjunto de regras de acesso (atendentes de fora do projeto).
- Ruim, porque um chamado pode ficar sem responsável até alguém do time responder; a visão "Sem responsável nos meus times" e o prazo de primeira resposta (ADR-0025) cobrem esse risco.
- Neutro, porque empresas com uma equipe compartilhada (por exemplo, TI) precisarão das filas num ADR futuro.

### Confirmação

Testes cobrindo: a abertura oferece só os times com quem atenda e não tem campo de responsável; a primeira resposta de alguém do time define o responsável; uma resposta num chamado que já tem responsável não muda o responsável, também quando vem do líder técnico; respostas do solicitante, de quem só acompanha e do gerente não definem responsável; só administrador, gerente e líder técnico trocam o responsável; a transferência entre times deixa o chamado sem responsável; "Abrir chamado" não aparece na tela inicial.

## Prós e contras das opções

### A. Desenho atual

- Bom, porque atende equipes compartilhadas desde a v1.
- Ruim, porque quem abre precisa escolher destino e pessoa, e há mais regras de acesso e de troca.

### B. Dois times e primeira resposta

- Bom, porque é simples para quem abre e mantém o controle com os gestores.
- Ruim, porque depende de alguém do time responder para o chamado ganhar dono.
