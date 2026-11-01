---
status: aceita
date: 2026-11-01
decision-makers: Marcelo Ribeiro
---

# ADR-0138: Horas previstas por tarefa, só do administrador

Muda, no ADR-0128, o item "Horas planejadas por tarefa e cronômetro", que era "depois": as horas previstas por tarefa entram agora; o cronômetro continua "depois".

## Contexto e problema

O administrador vê o saldo de cada projeto contra as horas contratadas (ADR-0128), mas não sabe em que tarefa as horas passaram do combinado: a parametrização do ICMS foi orçada em 40 horas e já tem 52 apontadas, enquanto a integração da NF-e ainda tem folga. Cada pessoa já pode apontar as horas numa tarefa do cronograma; falta dizer quantas horas cada tarefa deveria levar. Marcelo pediu as horas previstas por tarefa no grupo 14 (GanttA-5), e elas são a base da carga da equipe (GanttB-5). Quem cadastra e quem vê as previstas, e como a tarefa-resumo as conta?

## Fatores de decisão

- Saber, por tarefa, se as horas apontadas passaram das previstas.
- Manter as horas como dado de gestão só do administrador, como o saldo (ADR-0128).
- Uma regra só para as horas: em horas, em quartos de hora, sem valores em reais (ADR-0016).
- Cálculo simples e previsível, que a carga da equipe possa usar depois.

## Opções consideradas

Quem cadastra e vê as previstas:

- A1. Só o administrador cadastra e vê as previstas e as apontadas de cada tarefa.
- A2. O administrador e o gerente do projeto cadastram e veem.
- A3. O administrador cadastra; todos do projeto veem as previstas da tarefa, sem as apontadas.

Como a tarefa-resumo conta as horas:

- B1. Cada tarefa, também a resumo e a subtarefa, tem as próprias previstas, comparadas com as apontadas nela.
- B2. A tarefa-resumo soma as previstas e as apontadas das subtarefas, e só as tarefas com datas próprias recebem previstas.

## Resultado da decisão

Opções escolhidas: **A1** e **B1**. A1 por decisão de Marcelo em 01/11/2026, que seguiu a recomendação: as horas previstas e apontadas por tarefa ficam com o administrador, como o saldo, e o ADR-0128 continua valendo sem mudança para o gerente e o líder técnico. B1 porque o apontamento já é feito na tarefa escolhida, também na tarefa-resumo, sem somar as subtarefas: comparar as previstas da tarefa com as apontadas nela não pede regra nova, e nenhuma hora some quando uma tarefa ganha subtarefas.

Regras:

- **Horas previstas.** Cada tarefa do cronograma (também a resumo e a subtarefa; o marco e a fase não) tem, opcionalmente, as horas previstas, de 0,25 a 9.999, em quartos de hora. Só o administrador as põe, troca e tira, em qualquer situação do projeto, como as contratadas.
- **Comparação.** As apontadas da tarefa são as dos apontamentos que escolheram aquela tarefa, de qualquer pessoa. O estado de cada tarefa segue a regra do saldo do projeto, num lugar só (`HoursBalance`): acima das previstas, perto das previstas a partir de 90% numa tarefa abaixo de 100%, dentro e sem horas previstas.
- **Onde aparece.** Só para o administrador: no cronograma (a coluna Horas da Lista e a seção Horas do detalhe da tarefa, com "Alterar") e no detalhe de Horas dos projetos (a seção "Por tarefa", com as previstas nas tarefas, as apontadas nelas e as apontadas fora das tarefas do cronograma).
- **Fora do cronograma.** As previstas ficam no módulo das horas, fora do desfazer do cronograma (ADR-0091), da linha de base, dos modelos (ADR-0017) e do XML do MS Project (ADR-0136, que a gestão também baixa). A tarefa excluída leva as previstas para fora da tela; o desfazer que a traz de volta as traz também.
- **Auditoria.** Toda mudança das previstas vai para a auditoria, com a tarefa pelo nome atual e o projeto como contexto (ADR-0039, ADR-0084).
- **Exclusão do projeto.** As previstas saem com ele (ADR-0111).

Fora:

| Item | Classificação |
| --- | --- |
| Previstas por pessoa numa tarefa com várias pessoas | fora: a tarefa tem um responsável (ADR-0019) |
| Previstas no MS Project (`Work`), nos modelos e na linha de base | depois |
| Previstas que o gerente ou o responsável veem | fora |
| Cronômetro | depois |

### Consequências

- Bom, porque o administrador encontra a tarefa que estourou as horas sem abrir os apontamentos.
- Bom, porque as previstas ficam no mesmo módulo e na mesma regra do saldo, sem nada novo no cronograma.
- Ruim, porque a tarefa-resumo não soma as subtarefas: quem quer o total de uma etapa olha o detalhe de Horas dos projetos.
- Ruim, porque o gerente, que planeja o cronograma, não vê nem põe as previstas.

### Confirmação

Testes cobrindo: as previstas postas, trocadas e tiradas pelo administrador; as horas fora dos quartos de hora e acima de 9.999 recusadas; a tarefa de outro projeto, o marco e a fase recusados; o estado de cada tarefa (acima, perto, dentro e sem previstas) com as apontadas nela; as apontadas fora das tarefas; as previstas saindo com o projeto excluído; a auditoria; e a matriz de permissões das rotas novas, só do administrador (ADR-0056).

## Prós e contras das opções

### A1. Só o administrador

- Bom, porque segue o ADR-0128 e a decisão de Marcelo.
- Ruim, porque o gerente não planeja as horas do cronograma que monta.

### A2. O administrador e o gerente

- Bom, porque quem planeja o cronograma planeja as horas.
- Ruim, porque abre ao gerente o consumo, que o ADR-0128 deixou só com o administrador.

### A3. Todos veem as previstas

- Bom, porque a pessoa sabe quanto a tarefa deveria levar.
- Ruim, porque espalha um número de gestão e convida a comparar com as próprias horas, que só a pessoa e o administrador veem.

### B1. Cada tarefa com as próprias horas

- Bom, porque é a mesma regra do apontamento e não perde horas quando a tarefa ganha subtarefas.
- Ruim, porque a tarefa-resumo não mostra o total das subtarefas.

### B2. A tarefa-resumo soma as subtarefas

- Bom, porque a tarefa-resumo mostra o total, como as datas e o progresso (ADR-0088).
- Ruim, porque as horas apontadas na própria tarefa-resumo e as previstas dela antes das subtarefas pedem uma regra à parte.
