## Context

O `Schedule.Recalculate` (ADR-0126) já recoloca cada item em aberto, em ordem de dependência, pelo `EarliestStart`, com a mesma duração, e chama o `Summarize` para as tarefas-resumo. O `ScheduleEditing.RunAsync` cuida da permissão, do projeto encerrado, da transação serializável, do desfazer (pelo rótulo do `ScheduleResult`) e dos avisos das datas.

## Goals / Non-Goals

**Goals:**

- Um movimento em dias úteis no domínio, reaproveitando o `EarliestStart` e o `Summarize`.
- O desfazer, a auditoria e os avisos pelo caminho de toda mudança.

**Non-Goals:**

- Mover parte do cronograma; mover pela data de início.

## Decisions

- **Domínio.** `Schedule.Move(businessDays, days)`: de -999 a 999, sem zero (`Schedule.MaxMove`); cada item com datas próprias e em aberto vai para `AddBusinessDays(início, n)` e, em ordem de dependência, é colocado no maior entre esse dia e o `EarliestStart` pelas dependências; o `Summarize` refaz as tarefas-resumo. Nada muda se algum novo início sair de 2000 a 2099 (`ArgumentOutOfRangeException`). Devolve quantos itens mudaram. A data-limite e a linha de base não são tocadas.
- **Caso de uso.** `MoveSchedule` pelo `ScheduleEditing.RunAsync`, com `ScheduleField.Move` para os dias fora do intervalo ou um início fora do calendário, e o rótulo `ScheduleAction.ScheduleMoved` com os dias com sinal ("+5", "-3") no nome.
- **API.** `POST /api/projects/{id}/schedule/move` (`MoveSchedule`) com `ScheduleMoveRequest(days)`; 200 com `ScheduleMovedResponse(moved)`, 400 em `days`, 403, 404 e 409 como as outras mudanças.
- **Tela.** "Mover o cronograma" no `ScheduleMoreMenu`, só com `canEditSchedule` e itens em aberto, abre o `MoveScheduleDialog` (`ScheduleMove.tsx`): a direção no `SegmentedControl` ("Para depois", "Para antes"), os dias úteis (de 1 a 999) e o texto do que anda e do que fica; ao confirmar, invalida o cronograma, os projetos e a auditoria e avisa quantos itens mudaram. O `stepText` ganha `ScheduleMoved` ("o movimento do cronograma em +5 dias úteis").

## Risks / Trade-offs

- [Muitos avisos de uma vez] → Os avisos das datas já vão para o resumo diário, não na hora (ADR-0037).
- [Item segurado pelas dependências num movimento para antes] → É a regra de toda mudança; o diálogo diz que as dependências continuam valendo.
