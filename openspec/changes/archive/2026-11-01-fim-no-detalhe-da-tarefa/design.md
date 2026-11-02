## Context

O detalhe da tarefa e "Nova tarefa" usam o mesmo `ScheduleItemFields`, com o formulário de `scheduleItemValues.ts` (React Hook Form e Zod): o início digitado como dd/mm/aaaa e a duração de 1 a 999, obrigatória na tarefa com datas próprias; o fim aparece como ajuda da duração ("Termina em", `endsOn`), do item como gravado. O pedido de item (`ScheduleItemRequest`) leva `start` e `duration`, e o `ScheduleItemChecks.Check` monta o `ScheduleItemPlan`; o domínio põe o início no dia útil e calcula o fim (`ScheduleItem.Place`). O `PUT .../dates` (`Schedule.Reschedule`) já aceita o fim, pela duração `Schedule.DurationBetween(início, fim)`. O `GET /schedule` traz os dias não úteis só em volta das datas do cronograma, para o sombreado.

## Goals / Non-Goals

**Goals:**

- O fim e a duração calculados um pelo outro enquanto a pessoa digita, pela conta do servidor.
- Salvar pelo que a pessoa digitou por último, sem depender da conta da tela.

**Non-Goals:**

- O fim do marco e da tarefa-resumo; o empurrão das dependências antes de salvar; a edição na tabela.

## Decisions

- **Domínio.** `Schedule.DatesOf(start, duration, days)`: o início no dia útil (`OnABusinessDay`) e o fim (`AddBusinessDays(início, duração - 1)`), ou nulo com o início ou a duração inválidos; a duração pelo fim continua no `Schedule.DurationBetween`.
- **Pedido.** `ScheduleItemRequest` e `ScheduleItemForm` ganham `End` (opcional). O `ScheduleItemChecks.Check` recebe o calendário: com o fim, a tarefa com datas próprias tem a duração `DurationBetween(início, fim)`, e a `Duration` do pedido não é conferida; o fim no marco, na tarefa-resumo, antes do início ou longe demais vira `ScheduleField.End` (400 em `end`).
- **Datas antes de salvar.** `PreviewScheduleItemDates` (o projeto pelo `IProjectStore`, quem vê pelo `ProjectPermissions.CanView`, o calendário pelo `IWorkCalendar`): com a duração, o `DatesOf`; com o fim, o `DurationBetween` e depois o `DatesOf`, que leva o fim ao último dia útil; 400 em `start`, `duration` ou `end`, e em `duration` sem a duração nem o fim. Rota `GET /api/projects/{id}/schedule/item-dates` (`TaskDatesResponse`: `start`, `end`, `duration`), 404 para quem não vê o projeto.
- **Formulário.** `ScheduleItemValues` ganha `end` (dd/mm/aaaa) e `datesFrom` (`duration` ou `end`, o último digitado; o fim apagado volta à duração). O Zod confere a duração quando ela vale e o fim (data válida, não antes do início) quando ele vale. O `bodyOf` manda `end` só quando ele vale; senão, o pedido fica como era, sem a chave. O `valuesOfItem` traz o fim gravado; o `showRefusal` põe o `end` recusado no campo.
- **Conta na tela.** `itemDatesQuery(projectId, start, { duration | end })` em `queries.ts`; o `ScheduleItemFields` a pede só depois de a pessoa mexer no início, no fim ou na duração, com o que vale completo, e põe o resultado no outro campo (a duração pelo fim, o fim pela duração), sem mudar o `datesFrom`. O fim recusado pela conta aparece no campo. O `endsOn` e a ajuda "Termina em" saem; a ajuda da duração diz que um calcula o outro.
- **Visual.** Na linha das datas, "Início", "Fim" e "Duração em dias úteis", lado a lado no computador e um embaixo do outro no celular; o fim com a máscara de dia, como o início.

## Risks / Trade-offs

- [Uma chamada por data ou duração completa] → Só com o valor completo; a consulta guarda as respostas pela chave, e a resposta é pequena.
- [A conta chega depois de a pessoa salvar] → O pedido leva o que vale, e o servidor faz a mesma conta.
- [As dependências empurram a tarefa ao salvar] → O detalhe volta com as datas gravadas, como hoje.
