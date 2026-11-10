## Why

O cronograma do FiscoProjects tem tarefas com responsável, datas, progresso e atraso, mas só em duas formas: a Linha do tempo e a Lista. Quem compara com o Jira e o monday.com espera ver as tarefas num quadro, em colunas, e mover o cartão quando o trabalho anda; numa demonstração, a falta do quadro passa a impressão de produto incompleto. Tarefas-1, do grupo 16 (Tarefas), a pedido de Marcelo em 10/11/2026; as decisões ficam no ADR-0155.

## What Changes

- Uma terceira visualização na aba Cronograma, "Quadro", ao lado de "Linha do tempo" e "Lista", no endereço como `?view=board`.
- Três colunas pelo progresso: A fazer (0%), Em andamento (de 1% a 99%) e Concluída (100%), cada uma com a quantidade de tarefas.
- Um cartão por tarefa com datas próprias (a tarefa sem subtarefas e a subtarefa): o nome, a fase (e a tarefa-mãe da subtarefa), o responsável, o fim ou o atraso, o progresso em andamento e o dia da conclusão. A tarefa-resumo e os marcos ficam fora.
- O cartão abre o detalhe da tarefa, como a linha da Lista.
- Quem atualiza o progresso da tarefa move o cartão: arrastando, no computador, ou por "Mover", no cartão, também pelo teclado e no celular. Para Concluída, conclui na hora; para A fazer, volta a 0% na hora; para Em andamento, pede o percentual, como "Iniciar" e "Atualizar progresso".
- Os filtros do cronograma valem também no quadro.
- Sem mudança na API: o quadro usa o cronograma e o pedido do progresso que já existem.

## Capabilities

### New Capabilities

Nenhuma.

### Modified Capabilities

- `cronograma`: a visualização Quadro, as colunas, o cartão, mover o cartão e quem move.

## Non-goals

- Tarefas avulsas, sem datas, fora do cronograma.
- Colunas configuráveis, status próprio da tarefa e limite de tarefas por coluna.
- Raias por responsável ou por fase e ordem escolhida à mão dentro da coluna.
- Os marcos e a tarefa-resumo no quadro.

## Impact

- `web/src`: a aba Cronograma (`ProjectSchedulePage`), o quadro (`ScheduleBoard`) e as regras dele (`scheduleBoard.ts`), os textos e os testes.
- Documentação: o ADR-0155, `CLAUDE.md`, `docs/glossario.md` e o roadmap.
- Demonstração: sem dados novos; as tarefas do plano já cobrem as três colunas.
