---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0144: Horas das tarefas também para o gerente do projeto

Substitui, no ADR-0138, a regra "Onde aparece", que deixava as horas das tarefas só para o administrador, e muda, no ADR-0128, a regra de que o apontamento de outra pessoa só é visto pelo administrador. O resto do ADR-0138 continua valendo: só o administrador põe, troca e tira as horas previstas.

## Contexto e problema

Com as horas previstas por tarefa (ADR-0138), o administrador vê, no cronograma, quantas horas cada tarefa já consumiu. O gerente, que monta e acompanha o cronograma, não vê nada disso, e nem o administrador sabe, pelo cronograma, quem apontou as horas de uma tarefa: a seção "Horas" do detalhe diz "Apontadas 15 h", sem as pessoas nem os dias. Marcelo pediu, em 01/11/2026, que o gerente do projeto também veja as horas das tarefas nos projetos que gerencia e que a seção tenha "Ver apontamentos", com quem apontou, em que dia e quantas horas. Quem vê, o que o gerente pode fazer e quanto dos apontamentos ele vê?

## Fatores de decisão

- O gerente acompanha o consumo das tarefas que planejou, sem pedir ao administrador.
- Quem vê as horas de uma tarefa vê também quem as apontou, para entender o número.
- As horas previstas continuam um dado de gestão do administrador, como as contratadas (ADR-0128, ADR-0138).
- Uma regra só para quem vê as horas das tarefas, no módulo das horas (ADR-0128).

## Opções consideradas

- C1. O gerente do projeto vê as horas das tarefas e os apontamentos de cada tarefa; só o administrador põe as previstas.
- C2. O gerente do projeto vê e também põe, troca e tira as previstas.
- C3. O gerente vê as previstas, as apontadas e o saldo, sem os apontamentos de cada pessoa.

## Resultado da decisão

Opção escolhida: **C1**, pelo pedido de Marcelo, que falou em o gerente ver as horas: o gerente entende o consumo de cada tarefa e quem o fez, e as previstas continuam com o administrador, como decidido no ADR-0138. Pôr as previstas pelo gerente (C2) fica para quando Marcelo pedir.

Regras:

- **Quem vê as horas das tarefas.** O administrador, em qualquer projeto, e quem é gerente do projeto (o papel no projeto, ADR-0040), nos projetos que gerencia, em qualquer situação, como a exportação para o MS Project (ADR-0136). O líder técnico, os colaboradores, os desenvolvedores e os leitores não veem (403); quem não está no projeto recebe 404. A regra fica num lugar só, no módulo das horas (`TimeEntryAccess.SeesTaskHours`).
- **O que aparece.** No cronograma, para quem vê: a coluna "Horas" da Lista e a seção "Horas" do detalhe da tarefa, com as previstas, as apontadas, o saldo e o consumo. "Alterar" as previstas só aparece para o administrador.
- **Ver apontamentos.** Ao lado das apontadas, na seção "Horas", "Ver apontamentos" abre os apontamentos daquela tarefa, de todas as pessoas: a pessoa, o dia, as horas e a descrição, do dia mais recente para o mais antigo, com o total. Quem vê os apontamentos da tarefa é quem vê as horas das tarefas. Corrigir e apagar seguem o ADR-0128: o administrador corrige os de qualquer pessoa, e cada pessoa, inclusive o gerente, só os próprios.
- **O resto das horas.** O saldo do projeto, as horas contratadas e as adicionais, os apontamentos fora das tarefas e a carga da equipe (ADR-0142) continuam só do administrador.

Fora:

| Item | Classificação |
| --- | --- |
| O gerente pôr, trocar e tirar as previstas | depois, se Marcelo pedir |
| O líder técnico ver as horas das tarefas | fora |
| O gerente ver os apontamentos fora das tarefas ou de todo o projeto | fora: são do saldo, do administrador (ADR-0128) |

### Consequências

- Bom, porque o gerente vê, no próprio cronograma, a tarefa que consumiu mais horas do que o previsto e quem as apontou.
- Bom, porque o administrador também passa a ver quem apontou cada tarefa sem sair do cronograma.
- Ruim, porque o gerente vê o consumo, mas depende do administrador para ajustar as previstas.

### Confirmação

Testes cobrindo: o gerente do projeto vendo as horas das tarefas e os apontamentos de uma tarefa, também com o projeto encerrado; o gerente de outro projeto, o líder técnico, o colaborador, o desenvolvedor e o leitor recusados com 403 e quem está fora com 404; o marco, a fase e a tarefa de outro projeto com 404; as previstas mudadas só pelo administrador; o "⋮" de cada apontamento só para quem pode corrigi-lo; e, na tela, a coluna e a seção para o gerente, sem "Alterar", e "Ver apontamentos" com as pessoas, os dias e as horas (ADR-0056).

## Prós e contras das opções

### C1. O gerente vê as horas e os apontamentos da tarefa

- Bom, porque atende ao pedido e mantém as previstas com quem já as cuida.
- Ruim, porque o gerente não ajusta as previstas da tarefa que replanejou.

### C2. O gerente também põe as previstas

- Bom, porque quem planeja o cronograma planeja as horas.
- Ruim, porque muda o ADR-0138 além do pedido.

### C3. Sem os apontamentos de cada pessoa

- Bom, porque mostra menos sobre o trabalho de cada pessoa.
- Ruim, porque o número não se explica, e o pedido foi justamente ver quem apontou.
