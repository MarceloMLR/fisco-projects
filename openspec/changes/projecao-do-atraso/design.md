## Context

O agregado `Schedule` já empurra os itens que dependem de um item mudado (`Push`, pelas regras do `ScheduleLinks`, ADR-0137) e já é lido inteiro a cada `GET /schedule` e a cada visão geral, com o calendário da instalação (`IBusinessDays`) e o dia de hoje (`InstallationClock`). A Status-1 pôs as regras do que depende de um item num lugar só (`ScheduleImpacts`), sem mudar data nenhuma. A projeção precisa empurrar como o `Push`, mas sem gravar e uma vez por tarefa atrasada, cada uma a partir das datas atuais.

## Goals / Non-Goals

**Goals:**

- A regra da projeção num lugar só, no domínio, testável sem banco.
- A mesma projeção no `GET /schedule` (e, por ele, no Minhas tarefas) e na visão geral, e pronta para o relatório de status (Status-4).

**Non-Goals:**

- Guardar a projeção, cachear entre leituras ou avisar quando ela muda.
- Mudar o `IsLate` (ADR-0020) ou o que as outras telas contam como atrasado.

## Decisions

1. **`ScheduleProjections`, estático no domínio, sobre cópias dos itens.** Para cada tarefa atrasada, copia os itens (`ScheduleItem` pelo construtor que recebe o `ItemState`, sem rastreio), põe as datas projetadas na tarefa e empurra, em ordem topológica, só os itens que vêm depois dela, com o `ScheduleDependency.EarliestStart` de cada dependência; o item concluído fica como está. Alternativa: rodar o `Schedule.ChangeItem` numa cópia do agregado lida de novo do banco (como o recálculo, ADR-0126), descartada porque seria uma leitura por tarefa atrasada; e um mapa só de datas, descartado porque repetiria as regras do `ScheduleLinks`, que recebem itens.
2. **A tarefa projetada.** Do dia de partida (hoje, ou o próximo dia útil), o que falta é `ceil(duração × (100 − progresso) / 100)`, no mínimo 1. Com progresso, o início fica e o fim vai para o último dia útil do que falta; em 0%, a tarefa começa no dia de partida, com a duração inteira. Assim, a dependência "começa junto" só anda quando a tarefa ainda não começou.
3. **O resultado** (`ScheduleProjection`): o início e o fim projetados, os dias úteis depois do fim planejado, os marcos não atingidos que mudaram (o dia de hoje, o projetado e os dias úteis, em ordem do dia e do nome) e o fim do projeto (o maior fim de todos os itens), quando muda. Os dias úteis contam do dia seguinte ao antigo até o novo, como o atraso (`DaysLate`).
4. **Onde roda.** O `GetSchedule` calcula as projeções de todas as tarefas atrasadas (`ScheduleView.Projections`), menos num projeto encerrado, e o `ScheduleEndpoints.ItemOf` as entrega em `projection`. O `GetProjectOverview` usa a mesma regra: a lista de tarefas da atenção passa a ser as atrasadas (`IsLate`) e as que têm projeção, cada uma com `Start` e a `Projection` no lugar do `Blocks`, na mesma ordem de hoje (as mais atrasadas primeiro).
5. **Respostas.** `ScheduleProjectionResponse`, `ProjectedMilestoneResponse` e `ProjectedEndResponse`, nos `ScheduleEndpoints`, usados também pelos `OverviewEndpoints`; o `blocks` da atenção sai (o contrato é só da tela, gerado de novo).
6. **Tela.** A seção "Projeção do atraso" (`ScheduleProjection.tsx`) entra nos dois detalhes da tarefa, logo depois do aviso de ausência, com os textos montados em `schedule.ts` (`projectionTexts`), e a linha da atenção usa o `pushText` do mesmo lugar. A tarefa que devia ter começado é a que tem o fim em hoje ou depois (`end >= today`), e o prazo dela diz "Devia ter começado em 13/10" com o estado de atraso.

## Risks / Trade-offs

- [Custo por leitura: uma projeção por tarefa atrasada] → cada uma só visita os itens que vêm depois da tarefa; um cronograma tem até 2.000 tarefas (o limite da importação), e as atrasadas costumam ser poucas.
- [A projeção supõe o ritmo planejado] → o texto diz que é projeção e que as datas só mudam quando quem edita decidir (ADR-0164).
- [A tarefa que devia ter começado entra na atenção, mas não na contagem de atrasadas] → é a regra do ADR-0164 para a projeção; as outras telas continuam pelo ADR-0020, e a linha diz "Devia ter começado", não "atrasada".
