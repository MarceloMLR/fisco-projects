## Context

O `Calendar` é dono do calendário da instalação, e os outros módulos contam dias úteis pelo `IWorkCalendar` (ADR-0018). O `Projects` guarda, em cada tarefa e marco, o início, a duração em dias úteis e o fim, calculados quando o item é incluído, alterado ou empurrado (`ScheduleItem.Place`, `Schedule.Push`). O que é calculado na leitura (atraso, caminho crítico, previsto, sombreado, Meu trabalho) já segue o calendário atual; as datas guardadas, não. A tela Calendário e feriados é só do administrador e grava cada mudança na hora.

## Goals / Non-Goals

**Goals:**
- O administrador vê, depois de mudar o calendário, quantos itens de quais projetos em aberto mudam, e recalcula as datas com um passo, sem mexer no que já foi concluído.

**Non-Goals:**
- Recálculo automático, prazos dos chamados e desfazer do recálculo (proposta).

## Decisions

- **Regra no domínio.** `Schedule.Recalculate(IBusinessDays)` passa pelas tarefas e marcos com datas próprias na ordem das dependências (cada um depois dos anteriores) e põe cada item não concluído no `EarliestStart` do próprio início, que já é o dia útil seguinte quando o início deixou de ser útil e nunca antes do que as anteriores permitem; o `Place` mantém a duração. As tarefas-resumo voltam ao `Summarize`. Devolve quantos itens mudaram o início, o fim ou a duração. Concluído é a tarefa a 100% e o marco atingido (`ScheduleItem.IsDone`, que o `IsLate` passa a usar). Aplicado num cronograma feito com o calendário atual, não muda nada, então "quantos mudam" é o próprio recálculo numa cópia.
- **Antecipar não puxa.** Como em toda mudança (ADR-0019), uma tarefa que termina antes não traz as seguintes para mais cedo: cada item mantém o próprio início.
- **Sem estado guardado.** Não há marca de "calendário mudou": o aviso é o recálculo feito numa cópia sem rastreio (`IScheduleStore.OpenSchedulesAsync`, os cronogramas dos projetos não concluídos nem cancelados com itens, com o código e o nome, numa leitura só). Assim o aviso aparece depois de várias mudanças seguidas e some depois do recálculo, sem regra a manter no `Calendar`.
- **Onde fica.** O recálculo é do `Projects`, em `/api/admin/schedule-recalculation` (`GetScheduleRecalculation` e `RecalculateSchedules`, só `SystemPolicies.Administrator`), e a tela do calendário o chama: o `Calendar` não passa a conhecer o `Projects`.
- **Gravação.** `RecalculateSchedules` recalcula a cópia de cada cronograma e, para os que mudam, abre a transação serializável do projeto (`InSerializableTransactionAsync`), confere de novo que o projeto está em aberto, recalcula o cronograma rastreado e grava. Um projeto por transação: uma falha no meio deixa os anteriores gravados, e pedir de novo completa os outros. A auditoria registra a alteração de cada item pelo administrador (ADR-0039).
- **Avisos.** Depois de gravar cada projeto, o `ProjectNotices.DatesChangedAsync` avisa os responsáveis das tarefas cujas datas mudaram, como numa mudança do cronograma (ADR-0037).
- **Fora do desfazer.** O recálculo não grava `ScheduleChange`. A mudança de alguém que o recálculo alterou depois fica bloqueada no desfazer, como quando outra pessoa muda os mesmos itens (ADR-0091).
- **Resposta.** As duas rotas devolvem `items` (o total) e `projects` (o id, o código, o nome e a quantidade de cada um, pelo código); o `POST`, o que de fato recalculou.
- **Tela.** O `scheduleRecalculationQuery` fica sob a chave do calendário, então a invalidação que a tela já faz depois de cada mudança confere de novo o recálculo. Com itens, o `Alert` de tom `warning` aparece no topo, com o título "O calendário mudou", a quantidade e a ação "Recalcular as datas" (o `Alert` ganha `actions`, como no design system). A ação abre o `Dialog` de confirmação com os projetos (`CodeChip`, nome e quantidade) e o que muda e o que não muda; ao confirmar, o `Snackbar` diz em quantos projetos recalculou, e as consultas do calendário, dos projetos, do Meu trabalho e da auditoria são invalidadas.

## Risks / Trade-offs

- [Muitos projetos abertos tornam a tela do calendário mais lenta] → a leitura é uma só, em três consultas, e o cálculo é em memória; a tela mostra o calendário sem esperar o aviso.
- [Um responsável com muitas tarefas recebe muitos avisos] → cada tarefa tem a chave dela no resumo diário, que guarda só a última data de cada uma.
- [O desfazer de uma mudança anterior fica bloqueado] → é a regra do desfazer quando outro muda os mesmos itens; o diálogo avisa que o recálculo não entra no desfazer.
- [Mudança retroativa do calendário move tarefas em andamento que começaram no passado] → o início só muda quando o próprio dia deixou de ser útil, o fim só quando o calendário mudou dentro do período, e as concluídas ficam como estão.
