## 1. Servidor

- [x] 1.1 `ScheduleItem.IsDone` e `Schedule.Recalculate`, com os testes do domínio ("Feriado novo no meio de uma tarefa", "Marco num feriado novo", "Feriado desconsiderado não puxa as seguintes", "Concluídos não mudam", "Tarefa-resumo recalculada" e "Cronograma em dia não muda")
- [x] 1.2 `IScheduleStore.OpenSchedulesAsync`, `PreviewScheduleRecalculation`, `RecalculateSchedules` e os endpoints em `/api/admin/schedule-recalculation`
- [x] 1.3 Testes da API: "Ver antes de recalcular", "Projeto encerrado fora do recálculo", "Responsável avisado do recálculo", "Recálculo só do administrador", "Recalcular de novo" e "Desfazer depois do recálculo"

## 2. Tela

- [x] 2.1 `actions` no `Alert`
- [x] 2.2 `scheduleRecalculationQuery`, o aviso, o diálogo e a mensagem em `CalendarPage`, com os tipos da API e os textos
- [x] 2.3 Testes de tela: "Aviso depois de mudar o calendário", "Recalcular as datas pela tela" e "Cronogramas em dia"

## 3. Documentação e conferência

- [x] 3.1 ADR-0126, `CLAUDE.md`, roadmap (Projetos-1.1) e glossário
- [ ] 3.2 Build sem avisos, testes, `dotnet format`, lint, typecheck e `openspec validate --all --strict`

## Workflow follow-up

- PR, aprovação de Marcelo, archive na mesma branch e merge por Marcelo.
