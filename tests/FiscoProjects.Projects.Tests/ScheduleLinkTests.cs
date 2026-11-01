using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using static FiscoProjects.Testing.MsProjectFiles;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma, requirements "Dependências", "Dependências que empurram" and "Tipo e intervalo no MS Project";
// modelos-de-projeto, "Salvar como modelo" (ADR-0137). The calendar of 2026 has 12/10 and 02/11 as holidays, on Mondays.
public sealed class ScheduleLinkTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 1);

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static ScheduleItem Add(
        Schedule schedule, SchedulePhase phase, string name, DateOnly start, int duration, params ScheduleLink[] links) =>
        schedule.AddItem(
            duration == 0 ? ScheduleItemKind.Milestone : ScheduleItemKind.Task,
            new ScheduleItemPlan(phase.Id, name, start, duration, null, 0, [.. links.Select(link => link.PredecessorId)], Links: links),
            Days,
            Today);

    private static (Schedule Schedule, SchedulePhase Phase) NewSchedule()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        return (schedule, schedule.AddPhase("Homologação"));
    }

    private static ScheduleLink Link(ScheduleItem before, ScheduleLinkType type = ScheduleLinkType.FinishToStart, int lag = 0) =>
        new(before.Id, type, lag);

    [Fact(DisplayName = "Intervalo entre as tarefas")]
    public void Lag_WaitsOrLeads_InBusinessDays()
    {
        var (schedule, phase) = NewSchedule();
        var delivery = Add(schedule, phase, "Entrega", D(10, 7), 3);

        var waiting = Add(schedule, phase, "Homologação do cliente", D(10, 1), 2, Link(delivery, lag: 2));
        var leading = Add(schedule, phase, "Preparar o ambiente", D(10, 1), 2, Link(delivery, lag: -1));
        var milestone = Add(schedule, phase, "Entrega aceita", D(10, 1), 0, Link(delivery, lag: 1));

        // The delivery ends on Friday 09/10; Monday 12/10 is a holiday.
        delivery.End.Should().Be(D(10, 9));
        (waiting.Start, waiting.End).Should().Be((D(10, 15), D(10, 16)));
        leading.Start.Should().Be(D(10, 9));
        milestone.Start.Should().Be(D(10, 13));
    }

    [Fact(DisplayName = "Começa junto")]
    public void StartToStart_StartsWithTheItemBefore_PlusTheLag()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 8);

        var tests = Add(schedule, phase, "Testes", D(10, 1), 3, Link(setup, ScheduleLinkType.StartToStart, 1));

        (tests.Start, tests.End).Should().Be((D(10, 15), D(10, 19)));
        setup.End.Should().Be(D(10, 23));
    }

    [Fact(DisplayName = "Termina junto")]
    public void FinishToFinish_EndsWithTheItemBefore()
    {
        var (schedule, phase) = NewSchedule();
        var migration = Add(schedule, phase, "Migração", D(10, 14), 5);

        var training = Add(schedule, phase, "Treinamento", D(10, 1), 2, Link(migration, ScheduleLinkType.FinishToFinish));
        var lateTraining = Add(schedule, phase, "Treinamento tardio", D(10, 1), 2, Link(migration, ScheduleLinkType.FinishToFinish, 2));

        migration.End.Should().Be(D(10, 20));
        (training.Start, training.End).Should().Be((D(10, 19), D(10, 20)));
        lateTraining.End.Should().Be(D(10, 22));
    }

    [Fact(DisplayName = "Empurrar pelo início")]
    public void LaterStart_PushesTheItemThatStartsWithIt()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 3);
        var tests = Add(schedule, phase, "Testes", D(10, 14), 3, Link(setup, ScheduleLinkType.StartToStart));

        var pushed = schedule.Reschedule(setup.Id, D(10, 16), null, Days, Today);

        pushed.Should().Be(1);
        (tests.Start, tests.End, tests.Duration).Should().Be((D(10, 16), D(10, 20), 3));
    }

    [Fact(DisplayName = "Antecipar não puxa pelo início")]
    public void EarlierStart_PullsNobody()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 3);
        var tests = Add(schedule, phase, "Testes", D(10, 15), 3, Link(setup, ScheduleLinkType.StartToStart, 1));

        schedule.Reschedule(setup.Id, D(10, 7), null, Days, Today).Should().Be(0);

        tests.Start.Should().Be(D(10, 15));
    }

    [Fact(DisplayName = "Tipo e intervalo mantidos sem os links")]
    public void ChangeWithoutLinks_KeepsTheKindsAndLags_AndANewDependencyFinishesThenStarts()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 3);
        var survey = Add(schedule, phase, "Levantamento", D(10, 1), 2);
        var tests = Add(schedule, phase, "Testes", D(10, 14), 3, Link(setup, ScheduleLinkType.StartToStart, 1));

        schedule.ChangeItem(tests.Id, new ScheduleItemPlan(phase.Id, tests.Name, tests.Start, 3, null, null, [setup.Id, survey.Id]), Days, Today);

        schedule.Dependencies.Where(d => d.SuccessorId == tests.Id).Select(d => (d.PredecessorId, d.Type, d.Lag)).Should().BeEquivalentTo(
            [(setup.Id, ScheduleLinkType.StartToStart, 1), (survey.Id, ScheduleLinkType.FinishToStart, 0)]);
    }

    [Theory(DisplayName = "Tipo e intervalo inválidos no domínio")]
    [InlineData(1000)]
    [InlineData(-100)]
    public void LagOutOfTheRange_IsRefused(int lag)
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 3);

        FluentActions.Invoking(() => Add(schedule, phase, "Testes", D(10, 14), 3, Link(setup, lag: lag)))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Add(schedule, phase, "Testes", D(10, 14), 3, Link(setup, (ScheduleLinkType)9)))
            .Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Caminho crítico com os tipos")]
    public void CriticalPath_FollowsTheKindsAndLags()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 5);
        // Starts with it, two business days later, and ends last: the project ends with it.
        var tests = Add(schedule, phase, "Testes", D(10, 1), 5, Link(setup, ScheduleLinkType.StartToStart, 2));
        // Ends with the setup: one business day before the end of the project, so it has float.
        var training = Add(schedule, phase, "Treinamento", D(10, 1), 2, Link(setup, ScheduleLinkType.FinishToFinish));
        var other = Add(schedule, phase, "Documentação", D(10, 14), 2);

        var critical = ScheduleCriticalPath.Of(schedule.Items, schedule.Dependencies, Days);

        tests.End.Should().Be(D(10, 22));
        critical.Should().BeEquivalentTo([setup.Id, tests.Id]);
        critical.Should().NotContain([training.Id, other.Id]);
    }

    [Fact(DisplayName = "Recálculo com os tipos")]
    public void Recalculate_KeepsTheKindsAndLags()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 3);
        var tests = Add(schedule, phase, "Testes", D(10, 14), 3, Link(setup, ScheduleLinkType.StartToStart, 1));

        schedule.Recalculate(Days).Should().Be(0);

        (tests.Start, tests.End).Should().Be((D(10, 15), D(10, 19)));
    }

    [Fact(DisplayName = "Tipo e intervalo no desfazer")]
    public void ChangeOfTheKind_GoesBackWithTheUndo()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 3);
        var tests = Add(schedule, phase, "Testes", D(10, 19), 3, Link(setup));
        var before = schedule.Snapshot();
        schedule.ChangeItem(
            tests.Id,
            new ScheduleItemPlan(phase.Id, tests.Name, tests.Start, 3, null, null, [setup.Id], Links: [Link(setup, ScheduleLinkType.StartToStart, 2)]),
            Days,
            Today);
        var diff = ScheduleDiff.Between(before, schedule.Snapshot());

        schedule.ApplyChange(diff.Inverse()).Should().BeTrue();

        schedule.Dependencies.Single().Should().Match<ScheduleDependency>(d => d.Type == ScheduleLinkType.FinishToStart && d.Lag == 0);
        schedule.ApplyChange(diff).Should().BeTrue();
        schedule.Dependencies.Single().Should().Match<ScheduleDependency>(d => d.Type == ScheduleLinkType.StartToStart && d.Lag == 2);
    }

    [Fact(DisplayName = "Modelo com o tipo e o intervalo")]
    public void Template_KeepsTheKindsAndLags()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 3);
        Add(schedule, phase, "Testes", D(10, 14), 3, Link(setup, ScheduleLinkType.StartToStart, 1));
        var template = ProjectTemplate.From(schedule, "Implantação", null, Days)!;
        var made = new Schedule(Guid.CreateVersion7());

        made.ApplyTemplate(template, D(11, 3), Days, Today);

        var tests = made.Items.Single(item => item.Name == "Testes");
        made.Dependencies.Single().Should().Match<ScheduleDependency>(d => d.Type == ScheduleLinkType.StartToStart && d.Lag == 1);
        (tests.Start, tests.End).Should().Be((D(11, 4), D(11, 6)));
    }

    [Fact(DisplayName = "Tipos no MS Project")]
    public void Kinds_GoInTheXml_AndComeBack()
    {
        var (schedule, phase) = NewSchedule();
        var setup = Add(schedule, phase, "Parametrização", D(10, 14), 5);
        var tests = Add(schedule, phase, "Testes", D(10, 1), 3, Link(setup, ScheduleLinkType.StartToStart, 2));
        Add(schedule, phase, "Treinamento", D(10, 1), 2, Link(setup, ScheduleLinkType.FinishToFinish));
        Add(schedule, phase, "Aceite", D(10, 1), 0, Link(tests, lag: -1));
        var document = MsProjectExport.Of(
            "ERP-FIN", "Implantação do ERP", schedule, schedule.Figures(Today), Days, new Dictionary<Guid, MsProjectOwner>(),
            new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

        var links = document.Descendants(MsProjectExport.Ns + "PredecessorLink")
            .Select(link => ((string?)link.Element(MsProjectExport.Ns + "Type"), (string?)link.Element(MsProjectExport.Ns + "LinkLag")))
            .ToList();
        var imported = MsProjectImport.Convert(MsProjectImport.Read(new MemoryStream(Bytes(document))), "Cronograma", Days);
        var made = new Schedule(Guid.CreateVersion7());
        made.ApplyTemplate(imported.Outline, imported.FirstDay, Days, Today);

        links.Should().BeEquivalentTo([("3", "9600"), ("0", "0"), ("1", "-4800")]);
        imported.Warnings.Should().BeEmpty();
        made.Items.Select(item => (item.Name, item.Start, item.End)).Should().BeEquivalentTo(schedule.Items.Select(item => (item.Name, item.Start, item.End)));
        made.Dependencies.Select(d => (d.Type, d.Lag)).Should().BeEquivalentTo(schedule.Dependencies.Select(d => (d.Type, d.Lag)));
    }
}
