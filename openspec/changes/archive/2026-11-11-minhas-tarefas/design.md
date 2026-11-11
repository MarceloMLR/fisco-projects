## Context

O `Projects` é dono dos cronogramas. O `GetSchedule` monta a visão do cronograma de um projeto (as datas, o atraso em dias úteis, a data-limite, a folga, o caminho crítico, as ausências e a contagem de comentários), e o `ScheduleEndpoints` a transforma em `ScheduleItemResponse`. O Meu trabalho lê as tarefas da pessoa pelo `IScheduleStore.OwnedTasksAsync`, só até 7 dias à frente (ADR-0096). Na tela, o Quadro (`ScheduleBoard`, ADR-0155) recebe um cronograma de um projeto, e o detalhe do item (`TaskProgress`, `CommentSection`) usa só o id do projeto.

## Goals / Non-Goals

**Goals:** uma página com todas as tarefas da pessoa de todos os projetos em aberto, nos dois agrupamentos, em Lista e Quadro, com o detalhe ali mesmo.

**Non-Goals:** mudar permissões, mostrar tarefas de outras pessoas, marcos ou a tarefa-resumo, mudar o Meu trabalho.

## Decisions

- **Os números do cronograma.** O `ListMyTasks` pede ao store os projetos não concluídos nem cancelados de que a pessoa participa com alguma tarefa dela com datas próprias (`ProjectsWithTasksOfAsync`), e monta o cronograma de cada um pelo `GetSchedule`, para cada tarefa vir no mesmo `ScheduleItemResponse` da aba (atraso, data-limite, folga, `canUpdateProgress`, comentários) e a tela reaproveitar os componentes. Fica com as tarefas da pessoa: sem subtarefas (`Summaries`), abaixo de 100% ou, com `done`, concluídas desde 30 dias antes de hoje.
- **A rota.** `GET /api/my-tasks?done=` devolve `today` e os projetos pelo código, cada um com o id, o código, o nome e as tarefas (o item, a fase e a tarefa-mãe), pelo fim e, nas concluídas, pela conclusão mais recente. O mapeamento do item fica num lugar só (`ScheduleEndpoints.ItemOf`, agora `internal`).
- **O Quadro de vários projetos.** O `ScheduleBoard` passa a ser um `TaskBoard` que recebe os cartões (`BoardCard` com o `projectId` e, em Minhas tarefas, o código do projeto); o cronograma monta os dele (`cardsOf`) e mostra o responsável, e Minhas tarefas mostra o código do projeto no lugar. As colunas, a ordem e o mover continuam no `scheduleBoard.ts` (`columnsOf`), e o progresso vai para o projeto de cada cartão.
- **O detalhe.** Uma gaveta (`MyTaskDrawer`) com o código e o nome do projeto, a fase e a tarefa-mãe, as datas, o atraso e a data-limite, o progresso (`TaskProgress`), a nota, os comentários (`CommentSection`) e "Abrir no cronograma" (`/projects/{id}/schedule?item=`); o `TaskProgress` e o `CommentSection` passam a pedir só o id do projeto.
- **O endereço.** `?view=board`, `?group=date`, `?done=true` e `?item=` ficam no endereço, como no cronograma; a gaveta troca o endereço sem entrar no histórico (ADR-0105).
- **Por data.** As regras ficam em `myTasks.ts` (`dateGroupOf`, `tasksByDate`, `tasksByProject`), testadas sem tela, com o dia de hoje da API e a semana de segunda a domingo; a tarefa atrasada é a que a API diz atrasada (`late`).
- **A consulta.** `myTasksQuery(done)` fica sob a chave dos projetos, que toda mudança do cronograma pede de novo.
- **O aquecimento.** O `ListMyTasks` entra no aquecimento do `Projects`, como as outras telas principais (ADR-0106).

## Risks / Trade-offs

- [Um cronograma montado por projeto] → a pessoa costuma estar em poucos projetos em aberto, e a consulta inicial só traz os que têm tarefas dela; se pesar, a visão do cronograma pode ganhar uma forma enxuta depois.
- [Mais um item na barra lateral] → é o caminho do Jira, do Monday e do Planner, e a estrutura comporta (ADR-0066).
