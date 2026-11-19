using GerenWare.Projects.Domain;

namespace GerenWare.Projects.Tests;

// Spec: chamados, requirement "O que depende do chamado" (ADR-0164). The calendar of 2026 has 12/10 and 02/11 as
// holidays, on Mondays.
public sealed class ScheduleImpactTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 1);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static ScheduleItem Add(
        Schedule schedule, SchedulePhase phase, string name, DateOnly start, int duration, Guid? parentId = null, params ScheduleLink[] links) =>
        schedule.AddItem(
            duration == 0 ? ScheduleItemKind.Milestone : ScheduleItemKind.Task,
            new ScheduleItemPlan(
                phase.Id, name, start, duration, null, 0, [.. links.Select(link => link.PredecessorId)], parentId, Links: links),
            Days,
            Today);

    private static ScheduleLink After(ScheduleItem before, ScheduleLinkType type = ScheduleLinkType.FinishToStart) => new(before.Id, type, 0);

    private static IReadOnlySet<Guid> Critical(Schedule schedule) =>
        ScheduleCriticalPath.Of(schedule.Items, schedule.Dependencies, Days);

    /// <summary>Testes integrados, then the milestones of the acceptance and of the go-live, and a task outside the path.</summary>
    private static (Schedule Schedule, ScheduleItem Tests, ScheduleItem Acceptance, ScheduleItem GoLive, ScheduleItem Training) Homologation()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Homologação");
        var tests = Add(schedule, phase, "Testes integrados", D(10, 5), 5);
        // The dependencies push the milestones to the first day they may be: 13/10 and 14/10, on the critical path.
        var acceptance = Add(schedule, phase, "Homologação concluída", D(10, 5), 0, null, After(tests));
        var goLive = Add(schedule, phase, "Go-live", D(10, 5), 0, null, After(acceptance));
        var training = Add(schedule, phase, "Treinamento", D(10, 5), 2);
        return (schedule, tests, acceptance, goLive, training);
    }

    [Fact(DisplayName = "Marcos ameaçados pelas dependências")]
    public void LinkedItem_ThreatensTheMilestonesAfterIt_ByTheirDay()
    {
        var (schedule, tests, acceptance, goLive, _) = Homologation();

        var reach = ScheduleImpacts.Of(schedule, [tests.Id], Critical(schedule));

        reach.Items.Should().ContainSingle().Which.Should().Be(tests);
        reach.Milestones.Should().BeEquivalentTo([acceptance, goLive]);
        reach.Milestones.Select(milestone => milestone.Start).Should().BeInAscendingOrder();
    }

    [Fact(DisplayName = "Marcos pelas dependências de qualquer tipo")]
    public void Milestones_AreReached_ThroughDependenciesOfAnyKind()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Migração");
        var load = Add(schedule, phase, "Carga dos saldos", D(10, 5), 3);
        var check = Add(schedule, phase, "Conferência", D(10, 5), 2, null, After(load, ScheduleLinkType.StartToStart));
        var report = Add(schedule, phase, "Relatório", D(10, 5), 2, null, After(check, ScheduleLinkType.FinishToFinish));
        var cutover = Add(schedule, phase, "Virada", D(10, 20), 0, null, After(report));

        var reach = ScheduleImpacts.Of(schedule, [load.Id], Critical(schedule));

        reach.Milestones.Should().ContainSingle().Which.Should().Be(cutover);
    }

    [Fact(DisplayName = "Marco atingido não é ameaçado")]
    public void ReachedMilestone_IsNotThreatened()
    {
        var (schedule, tests, acceptance, goLive, _) = Homologation();
        schedule.SetReached(acceptance.Id, reached: true, Today);

        var reach = ScheduleImpacts.Of(schedule, [tests.Id], Critical(schedule));

        // The milestone after the reached one is still threatened.
        reach.Milestones.Should().ContainSingle().Which.Should().Be(goLive);
    }

    [Fact(DisplayName = "Item no caminho crítico")]
    public void LinkedItem_OnTheCriticalPath_IsSaid()
    {
        var (schedule, tests, _, _, training) = Homologation();

        ScheduleImpacts.Of(schedule, [tests.Id], Critical(schedule)).OnCriticalPath.Should().BeTrue();
        ScheduleImpacts.Of(schedule, [training.Id], Critical(schedule)).OnCriticalPath.Should().BeFalse();
    }

    [Fact(DisplayName = "Tarefa-resumo ligada")]
    public void LinkedSummary_ReachesThroughItsSubtasks()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Testes");
        var summary = Add(schedule, phase, "Testes integrados", D(10, 5), 5);
        Add(schedule, phase, "Roteiro", D(10, 5), 2, summary.Id);
        var run = Add(schedule, phase, "Execução", D(10, 7), 3, summary.Id);
        var goLive = Add(schedule, phase, "Go-live", D(10, 5), 0, null, After(run));

        var reach = ScheduleImpacts.Of(schedule, [summary.Id], Critical(schedule));

        reach.Items.Should().ContainSingle().Which.Should().Be(summary);
        reach.Milestones.Should().ContainSingle().Which.Should().Be(goLive);
        reach.OnCriticalPath.Should().BeTrue();
    }

    [Fact(DisplayName = "Item ligado excluído")]
    public void ItemThatDoesNotExist_IsLeftOut()
    {
        var (schedule, tests, _, goLive, _) = Homologation();
        schedule.RemoveItem(tests.Id, Days);

        ScheduleImpacts.Of(schedule, [tests.Id], Critical(schedule)).Should().Be(ScheduleReach.None);
        // A milestone linked right to the ticket is in the items, not among the ones threatened.
        var reach = ScheduleImpacts.Of(schedule, [goLive.Id], Critical(schedule));
        reach.Items.Should().ContainSingle().Which.Should().Be(goLive);
        reach.Milestones.Should().BeEmpty();
    }

    [Fact(DisplayName = "Itens na ordem do cronograma")]
    public void Items_AreInTheOrderOfTheSchedule()
    {
        var (schedule, tests, _, goLive, training) = Homologation();

        var reach = ScheduleImpacts.Of(schedule, [goLive.Id, training.Id, tests.Id], Critical(schedule));

        reach.Items.Should().Equal(tests, training, goLive);
    }

    [Fact(DisplayName = "Marcos depois de um item")]
    public void MilestonesAfter_OneItem()
    {
        var (schedule, tests, acceptance, goLive, training) = Homologation();

        ScheduleImpacts.MilestonesAfter(schedule, tests.Id).Should().BeEquivalentTo([acceptance.Id, goLive.Id]);
        ScheduleImpacts.MilestonesAfter(schedule, acceptance.Id).Should().BeEquivalentTo([goLive.Id]);
        ScheduleImpacts.MilestonesAfter(schedule, training.Id).Should().BeEmpty();
        ScheduleImpacts.MilestonesAfter(schedule, Guid.CreateVersion7()).Should().BeEmpty();
    }
}
