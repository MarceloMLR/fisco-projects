using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma, requirement "Data-limite de tarefas e marcos" (GanttA-2). The calendar of 2026 has 12/10 and 02/11
// as holidays, on Mondays.
public sealed class ScheduleDeadlineTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static ScheduleItemPlan Plan(
        SchedulePhase phase, string name, DateOnly start, int duration, ScheduleItem[]? after = null, DateOnly? deadline = null) =>
        new(phase.Id, name, start, duration, null, 0, [.. (after ?? []).Select(item => item.Id)], Deadline: deadline);

    /// <summary>
    /// Levantar regras from 21/10 to 23/10/2026, Testes integrados after it until 29/10/2026 and Treinamento after the
    /// tests, from 30/10/2026.
    /// </summary>
    private static (Schedule Schedule, SchedulePhase Phase, ScheduleItem Survey, ScheduleItem Tests, ScheduleItem Training) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Homologação");
        var survey = schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "Levantar regras", D(10, 21), 3), Days, Today);
        var tests = schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "Testes integrados", D(10, 26), 4, [survey]), Days, Today);
        var training = schedule.AddItem(ScheduleItemKind.Task, Plan(phase, "Treinamento", D(10, 30), 2, [tests]), Days, Today);
        return (schedule, phase, survey, tests, training);
    }

    private static void SetDeadline(Schedule schedule, SchedulePhase phase, ScheduleItem item, DateOnly? deadline, ScheduleItem[] after) =>
        schedule.ChangeItem(item.Id, Plan(phase, item.Name, item.Start, item.Duration, after, deadline), Days, Today);

    [Fact(DisplayName = "Data-limite cumprida")]
    public void Deadline_AfterTheEnd_ChangesNoDate_AndIsNotMissed()
    {
        var (schedule, phase, survey, tests, training) = NewSchedule();
        var before = schedule.Items.Select(item => (item.Name, item.Start, item.End)).ToList();

        SetDeadline(schedule, phase, tests, D(10, 30), [survey]);

        tests.Deadline.Should().Be(D(10, 30));
        schedule.Items.Select(item => (item.Name, item.Start, item.End)).Should().Equal(before);
        (tests.End, training.Start).Should().Be((D(10, 29), D(10, 30)));
        tests.DeadlineDeviation(Today, Days).Should().Be(-1);
        tests.MissesDeadline(Today, Days).Should().BeFalse();
        ScheduleFigures.MissedDeadlinesOf(schedule.Items, Today, Days).Should().Be(0);
        ScheduleFigures.DeadlineDeviationsOf(schedule.Items, Today, Days).Should().Equal(new Dictionary<Guid, int> { [tests.Id] = -1 });
    }

    [Fact(DisplayName = "Data-limite estourada")]
    public void Deadline_BeforeThePushedEnd_IsMissedByTheBusinessDays()
    {
        var (schedule, phase, survey, tests, _) = NewSchedule();
        SetDeadline(schedule, phase, tests, D(10, 30), [survey]);

        // Levantar regras takes two more days: the tests are pushed to end on 03/11, after the holiday of 02/11.
        schedule.ChangeItem(survey.Id, Plan(phase, survey.Name, survey.Start, 5), Days, Today);

        (tests.End, tests.Deadline).Should().Be((D(11, 3), D(10, 30)));
        tests.DeadlineDeviation(Today, Days).Should().Be(1);
        tests.MissesDeadline(Today, Days).Should().BeTrue();
        ScheduleFigures.MissedDeadlinesOf(schedule.Items, Today, Days).Should().Be(1);
    }

    [Fact(DisplayName = "Data-limite do marco")]
    public void MilestoneDeadline_IsComparedWithToday_UntilReached_AndThenWithTheDayItWasReached()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Entrada em produção");
        var milestone = schedule.AddItem(
            ScheduleItemKind.Milestone, Plan(phase, "Virada", D(11, 4), 0, deadline: D(11, 4)), Days, Today);
        var dayAfter = D(11, 5);

        milestone.DeadlineDeviation(dayAfter, Days).Should().Be(1);
        milestone.MissesDeadline(dayAfter, Days).Should().BeTrue();

        schedule.SetReached(milestone.Id, true, dayAfter);

        milestone.DeadlineDeviation(D(11, 9), Days).Should().Be(1);
        milestone.MissesDeadline(D(11, 9), Days).Should().BeFalse();
        ScheduleFigures.MissedDeadlinesOf(schedule.Items, D(11, 9), Days).Should().Be(0);
    }

    [Fact(DisplayName = "Data-limite da tarefa concluída")]
    public void DoneTask_IsComparedWithTheDayItWasDone()
    {
        var (schedule, phase, survey, tests, _) = NewSchedule();
        SetDeadline(schedule, phase, tests, D(10, 28), [survey]);

        // Done on 27/10, before the end: a business day before the deadline, and never missed.
        schedule.UpdateProgress(tests.Id, 100, D(10, 27));

        tests.DeadlineDeviation(D(11, 10), Days).Should().Be(-1);
        tests.MissesDeadline(D(11, 10), Days).Should().BeFalse();
    }

    [Fact(DisplayName = "Item atrasado passa da data-limite por hoje")]
    public void LateItem_IsComparedWithToday()
    {
        var (schedule, phase, survey, tests, _) = NewSchedule();
        SetDeadline(schedule, phase, tests, D(10, 29), [survey]);

        // It ends on 29/10 and is still open on 04/11: three business days past it, without the holiday of 02/11.
        tests.DeadlineDeviation(D(11, 4), Days).Should().Be(3);
        tests.MissesDeadline(D(11, 4), Days).Should().BeTrue();
        tests.DeadlineDeviation(D(10, 29), Days).Should().Be(0);
        tests.MissesDeadline(D(10, 29), Days).Should().BeFalse();
    }

    [Fact(DisplayName = "Data-limite tirada")]
    public void RemovedDeadline_LeavesTheItemWithoutIt()
    {
        var (schedule, phase, survey, tests, _) = NewSchedule();
        SetDeadline(schedule, phase, tests, D(10, 30), [survey]);

        SetDeadline(schedule, phase, tests, null, [survey]);

        tests.Deadline.Should().BeNull();
        tests.DeadlineDeviation(Today, Days).Should().BeNull();
        ScheduleFigures.DeadlineDeviationsOf(schedule.Items, Today, Days).Should().BeEmpty();
    }

    [Fact(DisplayName = "Data-limite no desfazer")]
    public void Deadline_GoesBackWithTheChange()
    {
        var (schedule, phase, survey, tests, _) = NewSchedule();
        var before = schedule.Snapshot();
        SetDeadline(schedule, phase, tests, D(10, 30), [survey]);
        var diff = ScheduleDiff.Between(before, schedule.Snapshot());

        schedule.ApplyChange(diff.Inverse()).Should().BeTrue();

        tests.Deadline.Should().BeNull();
        schedule.ApplyChange(diff).Should().BeTrue();
        tests.Deadline.Should().Be(D(10, 30));
    }
}
