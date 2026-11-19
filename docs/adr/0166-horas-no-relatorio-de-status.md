---
status: aceita
date: 2026-11-19
decision-makers: Marcelo Ribeiro
---

# ADR-0166: Horas do projeto no relatório de status

Substitui, no ADR-0128, a linha "Horas que o gerente vê ou cadastra" (fora) só para o relatório de status (ADR-0165), e, no ADR-0144, a regra de que o saldo do projeto continua só do administrador, também só para o relatório. O resto continua: só o administrador cadastra as horas contratadas, as adicionais e as previstas, e as horas de cada pessoa continuam só dela e do administrador. Aprovado por Marcelo em 19/11/2026.

## Contexto e problema

O saldo de horas do projeto (as contratadas e as adicionais contra as apontadas) é só do administrador (ADR-0128), e o gerente vê só as horas das tarefas dos projetos que gerencia (ADR-0144). Em 19/11/2026, ao pedir o relatório de status, Marcelo decidiu que, como o relatório só é gerado pela gestão e pelo administrador, quem gera tem a opção "Mostrar horas". Quando o gerente gera, ele passa a ver o saldo do projeto que gerencia. O que entra, quem pode marcar a opção e o que muda na regra das horas?

## Fatores de decisão

- O cliente que contratou um banco de horas quer ver o consumo no status report.
- As horas de cada pessoa são dado pessoal e de gestão (ADR-0058, ADR-0128): não vão para um arquivo que sai da empresa.
- Uma regra só para quem vê o quê, no módulo das horas (ADR-0128).

## Opções consideradas

- H1. "Mostrar horas" para quem gera (o administrador e o gerente), só com os totais do projeto e das tarefas, no relatório.
- H2. Como H1, e o gerente também vê o saldo do projeto na tela, numa seção da Visão geral.
- H3. Só o administrador marca "Mostrar horas".

## Resultado da decisão

Opção escolhida: **H1**, pela decisão de Marcelo. H2 abre uma tela nova sem pedido; H3 contraria o que ele decidiu.

Regras:

| Item | Definição |
| --- | --- |
| Quem marca | Quem gera o relatório: o administrador e o gerente do projeto (ADR-0165). Desmarcado de início |
| O que entra | As horas contratadas, as adicionais, as apontadas, o saldo e o estado (acima, perto do limite, dentro ou sem horas contratadas, ADR-0128) e, por tarefa, as previstas e as apontadas que o gerente já vê (ADR-0144) |
| O que nunca entra | As horas de cada pessoa e os apontamentos, mesmo para o administrador: o arquivo vai para o cliente |
| Sem horas contratadas | O slide diz "Sem horas contratadas" e mostra só as apontadas |
| A regra | Fica no módulo das horas, ao lado do `SeesTaskHours` (ADR-0144), e o módulo entrega ao relatório só os totais |

### Consequências

- Bom, porque o cliente vê o consumo do banco de horas sem uma planilha à parte.
- Bom, porque as horas de cada pessoa continuam fora do que sai da empresa.
- Ruim, porque, na prática, o gerente passa a saber o saldo do projeto que gerencia: basta gerar o relatório. Se a empresa não quiser isso, a opção H3 volta a ser discutida.

### Confirmação

Testes da API: o administrador e o gerente gerando com e sem "Mostrar horas"; os totais do projeto e das tarefas no relatório; nenhum nome de pessoa nas horas do arquivo; o projeto sem horas contratadas. A matriz de quem vê as horas continua coberta por inteiro (ADR-0056).

## Prós e contras das opções

### H1. Opção no relatório, para quem gera

- Bom, porque atende o pedido sem tela nova.
- Ruim, porque o gerente vê o saldo pelo relatório, e não pela tela.

### H2. Também na tela

- Bom, porque o gerente acompanha o saldo sem gerar o relatório.
- Ruim, porque é uma tela nova sem pedido.

### H3. Só o administrador

- Bom, porque o saldo continua só dele.
- Ruim, porque o gerente, que gera o relatório toda semana, teria de pedir ao administrador.
