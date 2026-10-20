using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma (domain side): "Desfazer no cronograma" and "Refazer no cronograma" (ADR-0091). The calendar of
// 2026 has 12/10 and 02/11 as holidays.
public sealed class ScheduleHistoryTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);
    private static readonly Guid Marina = Guid.CreateVersion7();

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static (Schedule Schedule, SchedulePhase Phase) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        return (schedule, schedule.AddPhase("Configuração"));
    }

    private static ScheduleItem Task(Schedule schedule, SchedulePhase phase, string name, DateOnly start, int duration, params ScheduleItem[] after) =>
        schedule.AddItem(
            ScheduleItemKind.Task,
            new ScheduleItemPlan(phase.Id, name, start, duration, null, 0, [.. after.Select(item => item.Id)]),
            Days,
            Today);

    private static ScheduleItem Milestone(Schedule schedule, SchedulePhase phase, string name, DateOnly day, params ScheduleItem[] after) =>
        schedule.AddItem(
            ScheduleItemKind.Milestone,
            new ScheduleItemPlan(phase.Id, name, day, 0, null, 0, [.. after.Select(item => item.Id)]),
            Days,
            Today);

    /// <summary>A change as the use cases record it: the schedule before and after it.</summary>
    private static ScheduleChange Recorded(Schedule schedule, ScheduleAction action, string name, Action change)
    {
        var before = schedule.Snapshot();
        change();
        return new ScheduleChange(schedule.Id, Marina, new ScheduleChangeLabel(action, name), ScheduleDiff.Between(before, schedule.Snapshot()));
    }

    [Fact(DisplayName = "Desfazer volta a cadeia")]
    public void Undo_PutsTheTaskAndThePushedMilestoneBack_AndRedoMovesThemAgain()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 3);
        var milestone = Milestone(schedule, phase, "Aceite da configuração", D(10, 15), task);
        var before = schedule.Snapshot();

        var moved = Recorded(schedule, ScheduleAction.DatesChanged, task.Name, () => schedule.Reschedule(task.Id, D(10, 15), null, Days, Today));

        (task.End, milestone.Start).Should().Be((D(10, 19), D(10, 19)));
        // The task and the milestone it pushed, in one change.
        moved.Diff.Items.Should().HaveCount(2);
        moved.UndoIn(schedule).Should().BeTrue();
        moved.Undone.Should().BeTrue();
        (task.Start, task.End, milestone.Start).Should().Be((D(10, 13), D(10, 15), D(10, 15)));
        schedule.Snapshot().Should().BeEquivalentTo(before);

        moved.RedoIn(schedule).Should().BeTrue();
        moved.Undone.Should().BeFalse();
        (task.End, milestone.Start).Should().Be((D(10, 19), D(10, 19)));
    }

    [Fact(DisplayName = "Desfazer uma exclusão")]
    public void UndoingARemoval_BringsTheItemBackWithItsIdDatesAndDependencies()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 3);
        var removed = Task(schedule, phase, "Testes integrados", D(10, 16), 2, task);
        var milestone = Milestone(schedule, phase, "Aceite da configuração", D(10, 19), removed);
        var before = schedule.Snapshot();

        var removal = Recorded(schedule, ScheduleAction.TaskRemoved, removed.Name, () => schedule.RemoveItem(removed.Id, Days));
        schedule.ItemOf(removed.Id).Should().BeNull();

        removal.UndoIn(schedule).Should().BeTrue();

        var back = schedule.ItemOf(removed.Id)!;
        (back.Name, back.Start, back.End).Should().Be(("Testes integrados", D(10, 16), D(10, 19)));
        schedule.PredecessorsOf(removed.Id).Should().ContainSingle().Which.Id.Should().Be(task.Id);
        schedule.PredecessorsOf(milestone.Id).Should().ContainSingle().Which.Id.Should().Be(removed.Id);
        schedule.Snapshot().Should().BeEquivalentTo(before);
    }

    [Fact(DisplayName = "Desfazer bloqueado")]
    public void SomeoneChangingTheSameItemsAfter_BlocksTheUndo_AndNothingChanges()
    {
        var (schedule, phase) = NewSchedule();
        var task = Task(schedule, phase, "Parametrizar regras de ICMS", D(10, 13), 3);
        var moved = Recorded(schedule, ScheduleAction.DatesChanged, task.Name, () => schedule.Reschedule(task.Id, D(10, 15), null, Days, Today));
        // The Administrator changes the same task after it.
        schedule.UpdateProgress(task.Id, 50, Today);
        var now = schedule.Snapshot();

        moved.UndoIn(schedule).Should().BeFalse();

        moved.Undone.Should().BeFalse();
        schedule.Snapshot().Should().BeEquivalentTo(now);
    }

    [Fact(DisplayName = "Desfazer sem quebrar o cronograma")]
    public void UndoingAnInclusion_ThatAnotherItemDependsOnNow_IsBlocked()
    {
        var (schedule, phase) = NewSchedule();
        ScheduleItem? included = null;
        var inclusion = Recorded(schedule, ScheduleAction.TaskAdded, "Mapear processos", () => included = Task(schedule, phase, "Mapear processos", D(10, 13), 2));
        // Someone else makes a new task depend on it, without changing it.
        var later = Task(schedule, phase, "Validar cadastro", D(10, 13), 2, included!);

        inclusion.UndoIn(schedule).Should().BeFalse();

        schedule.ItemOf(included!.Id).Should().NotBeNull();
        schedule.PredecessorsOf(later.Id).Should().ContainSingle();
    }

    [Fact(DisplayName = "Desfazer fase renomeada")]
    public void UndoingARenamedPhase_PutsTheNameBack_AndAnEmptyChangeIsEmpty()
    {
        var (schedule, phase) = NewSchedule();
        var renaming = Recorded(schedule, ScheduleAction.PhaseRenamed, "Parametrização", () => schedule.RenamePhase(phase.Id, "Parametrização"));

        renaming.UndoIn(schedule).Should().BeTrue();
        phase.Name.Should().Be("Configuração");
        renaming.RedoIn(schedule).Should().BeTrue();
        phase.Name.Should().Be("Parametrização");
        ScheduleDiff.Between(schedule.Snapshot(), schedule.Snapshot()).IsEmpty.Should().BeTrue();
    }
}
