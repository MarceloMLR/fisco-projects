---
status: aceita
date: 2026-10-19
decision-makers: Marcelo Ribeiro
---

# ADR-0086: "Iniciar" pede o percentual, e o marco não atingido depois da data fica atrasado

Complementa o ADR-0020, que definiu o progresso em percentual com os atalhos "Iniciar" e "Concluir" e a tarefa atrasada.

## Contexto e problema

O ADR-0020 escolheu o percentual com os atalhos "Iniciar" e "Concluir", que ajustam o percentual, e destacou a tarefa vencida abaixo de 100%. O marco tem só os estados atingido ou não. Faltam duas regras para a Cronograma-2: quanto "Iniciar" põe no progresso de uma tarefa a 0%, e se um marco cuja data passou sem ser atingido aparece como atrasado.

## Fatores de decisão

- O progresso do projeto, ponderado pela duração (ADR-0020), fiel ao andamento.
- Atualização rápida no dia a dia, inclusive no celular (ADR-0012).
- Atraso sempre visível por ícone, cor e texto (ADR-0065), também nas entregas.

## Opções consideradas

Para "Iniciar":

- A. Pôr 10%
- B. Pôr 1%
- C. Pedir o percentual

Para o marco:

- D. Marco não atingido depois da data fica atrasado
- E. Marco só atingido ou pendente, sem atraso

## Resultado da decisão

Opções escolhidas: **C** e **D**, por escolha de Marcelo.

Regras:

- "Iniciar" aparece numa tarefa a 0% e pede o percentual, que a pessoa digita; nada é gravado sem o número.
- "Concluir" põe 100% e grava a data real de conclusão (ADR-0020).
- Um marco é atingido ou não; ao ser atingido, guarda a data. Quem marca e desmarca é o administrador ou o gerente (ADR-0043); o termo de aceite marcará o marco no bloco de termos (ADR-0035).
- Um marco não atingido cuja data é anterior a hoje, no fuso da instalação, fica atrasado, com ícone, cor e texto, como a tarefa atrasada. A fase com um marco atrasado fica atrasada, e as listas contam os marcos atrasados junto das tarefas atrasadas.

### Consequências

- Bom, porque o percentual de uma tarefa iniciada é sempre o que a pessoa informou, sem um valor inventado.
- Bom, porque uma entrega que passou da data sem aceite aparece como as tarefas atrasadas.
- Ruim, porque "Iniciar" pede um passo a mais do que um valor fixo.

### Confirmação

Testes cobrindo: "Iniciar" grava só o percentual digitado; "Concluir" grava 100% e a data de conclusão; marcar e desmarcar um marco grava e apaga a data; o marco vencido e não atingido aparece atrasado, e o atingido ou do dia não.

## Prós e contras das opções

### A. Pôr 10%

- Bom, porque é um toque só.
- Ruim, porque o valor não vem de quem faz a tarefa.

### B. Pôr 1%

- Bom, porque quase não mexe no progresso do projeto.
- Ruim, porque o valor também é inventado.

### C. Pedir o percentual

- Bom, porque o progresso é sempre o informado.
- Ruim, porque pede um campo a mais.

### D. Marco atrasado

- Bom, porque a entrega vencida aparece nas listas.
- Ruim, porque o marco ganha um estado a mais.

### E. Só atingido ou pendente

- Bom, porque é o mais simples.
- Ruim, porque uma entrega vencida sem aceite passa despercebida.
