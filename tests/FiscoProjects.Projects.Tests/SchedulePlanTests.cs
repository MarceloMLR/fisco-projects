using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: visao-geral-do-projeto, "Andamento previsto" (domain side) (ADR-0020, ADR-0021, ADR-0073). The calendar of
// 2026 has 12/10 and 02/11 as holidays.
public sealed class SchedulePlanTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static (Schedule Schedule, SchedulePhase Phase) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        return (schedule, schedule.AddPhase("Configuração"));
    }

    private static ScheduleItem Task(Schedule schedule, SchedulePhase phase, string name, DateOnly start, int duration, Guid? parent = null) =>
        schedule.AddItem(
            ScheduleItemKind.Task, new ScheduleItemPlan(phase.Id, name, start, duration, null, 0, [], parent), Days, D(10, 1));

    [Fact(DisplayName = "Previsto pelo cronograma")]
    public void Planned_ByTheSchedule()
    {
        var (schedule, phase) = NewSchedule();
        Task(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 5);
        Task(schedule, phase, "Configurar plano de contas", D(10, 26), 5);
        schedule.AddItem(
            ScheduleItemKind.Milestone, new ScheduleItemPlan(phase.Id, "Configuração aprovada", D(10, 30), 0, null, 0, []), Days, D(10, 1));

        SchedulePlan.PlannedProgress(schedule.Items, null, D(10, 19), Days).Should().Be(50);
        SchedulePlan.PlannedProgress(schedule.Items, null, D(10, 16), Days).Should().Be(40);
        SchedulePlan.PlannedProgress(schedule.Items, null, D(10, 9), Days).Should().Be(0);
        SchedulePlan.PlannedProgress(schedule.Items, null, D(11, 30), Days).Should().Be(100);
    }

    [Fact(DisplayName = "Previsto pela linha de base")]
    public void Planned_ByTheBaseline()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 5);
        schedule.SaveBaseline(D(10, 13));
        schedule.ChangeItem(task.Id, new ScheduleItemPlan(phase.Id, task.Name, D(10, 21), 5, null, null, []), Days, D(10, 13));
        // Included after the baseline: out of the planned.
        Task(schedule, phase, "Configurar plano de contas", D(10, 13), 5);

        task.End.Should().Be(D(10, 27));
        SchedulePlan.PlannedProgress(schedule.Items, schedule.Baseline, D(10, 19), Days).Should().Be(100);
        SchedulePlan.PlannedProgress(schedule.Items, null, D(10, 19), Days).Should().Be(50);
    }

    [Fact(DisplayName = "Previsto conta só dias úteis")]
    public void Planned_CountsBusinessDaysOnly()
    {
        var (schedule, phase) = NewSchedule();
        Task(schedule, phase, "Migrar saldos", D(10, 29), 4);

        SchedulePlan.PlannedProgress(schedule.Items, null, D(11, 2), Days).Should().Be(50);
        SchedulePlan.PlannedProgress(schedule.Items, null, D(11, 1), Days).Should().Be(50);
        SchedulePlan.PlannedProgress(schedule.Items, null, D(11, 3), Days).Should().Be(75);
    }

    [Fact(DisplayName = "Previsto sem tarefas e com tarefa-resumo")]
    public void Planned_IsNoneWithoutTasks_AndCountsTheSubtasks()
    {
        var (schedule, phase) = NewSchedule();
        SchedulePlan.PlannedProgress(schedule.Items, null, D(10, 19), Days).Should().BeNull();

        var summary = Task(schedule, phase, "Testes integrados", D(10, 13), 5);
        Task(schedule, phase, "Roteiro de testes", D(10, 13), 2, summary.Id);
        Task(schedule, phase, "Executar testes", D(10, 26), 2, summary.Id);

        SchedulePlan.PlannedProgress(schedule.Items, null, D(10, 19), Days).Should().Be(50);
    }
}
