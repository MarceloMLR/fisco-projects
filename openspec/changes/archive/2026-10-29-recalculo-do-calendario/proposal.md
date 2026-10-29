## Why

As datas guardadas das tarefas e dos marcos não mudam quando o calendário muda: um feriado novo, um nacional desmarcado ou um ponto facultativo só valem para uma tarefa na próxima alteração dela, enquanto o atraso, o caminho crítico e o sombreado já usam o calendário novo. Em 28/10/2026, Marcelo perguntou se a mudança vale para todos os cronogramas e escolheu a opção de, ao salvar o calendário, o sistema mostrar quantas tarefas de projetos em aberto mudariam de datas e oferecer "Recalcular as datas" (Projetos-1.1, fora do roadmap). O ADR-0126 registra a escolha.

## What Changes

- O cronograma ganha o recálculo pelo calendário: cada tarefa e marco não concluído mantém o início (no dia útil seguinte, quando deixou de ser útil) e a duração em dias úteis, e as dependências empurram os seguintes; as tarefas a 100%, os marcos atingidos, a linha de base e os projetos encerrados não mudam, e as tarefas-resumo voltam a seguir as subtarefas.
- `GET /api/admin/schedule-recalculation` diz, só ao administrador, quantas tarefas e marcos de quais projetos em aberto mudam com o calendário atual, sem mudar nada; `POST` recalcula, projeto por projeto, avisa os responsáveis das tarefas que mudam de datas e devolve o que recalculou. As mudanças entram na auditoria como as do cronograma e ficam fora do desfazer.
- A tela Calendário e feriados confere o recálculo ao abrir e depois de cada mudança gravada, mostra o aviso no topo com a quantidade e "Recalcular as datas", e pede confirmação com os projetos antes de recalcular.
- O `Alert` das telas ganha as ações, como o do design system.

## Capabilities

### New Capabilities

### Modified Capabilities
- `cronograma`: o recálculo das datas pelo calendário e a regra das datas guardadas.
- `calendario`: o aviso e o recálculo pela tela Calendário e feriados.

## Non-goals

- Recalcular sozinho a cada mudança do calendário, sem o administrador ver antes.
- Puxar para mais cedo as tarefas seguintes quando uma tarefa anterior termina antes: o recálculo segue a regra "antecipar não puxa".
- Mudar a linha de base, as tarefas concluídas, os marcos atingidos ou os cronogramas de projetos encerrados.
- Recalcular os prazos dos chamados, que continuam calculados na abertura (ADR-0025).
- Desfazer o recálculo pelo cronograma.

## Impact

- `Projects`: `Schedule.Recalculate`, `ScheduleItem.IsDone`, `IScheduleStore.OpenSchedulesAsync`, os casos de uso `PreviewScheduleRecalculation` e `RecalculateSchedules` e os endpoints em `/api/admin/schedule-recalculation`.
- Telas: `CalendarPage`, `Alert` (ações), a consulta `scheduleRecalculationQuery`, textos em `pt-BR.json` e tipos da API.
- `docs/adr/0126-recalculo-das-datas-pelo-calendario.md`, `docs/roadmap.md`, `CLAUDE.md`, `docs/glossario.md`.
- ADRs: 0018, 0019, 0025, 0037, 0039, 0043, 0046, 0083, 0088, 0091, 0126.
