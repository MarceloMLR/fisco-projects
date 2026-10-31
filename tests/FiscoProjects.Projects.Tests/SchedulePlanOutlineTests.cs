using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma, requirement "Progresso e responsáveis do MS Project", the domain side: the outline a new schedule is
// made from, by a model or a file of the MS Project (ADR-0017, ADR-0135). The calendar of 2026 has 12/10 as a holiday,
// on a Monday.
public sealed class SchedulePlanOutlineTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);
    private static readonly Guid Carla = Guid.CreateVersion7();

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static SchedulePlanOutline Outline(IReadOnlyList<OutlineItem> items, params OutlineDependency[] dependencies) =>
        new([new OutlinePhase("Configuração")], items, dependencies);

    [Fact(DisplayName = "Cronograma de um plano com responsáveis e progresso")]
    public void AnOutline_MakesTheItems_WithTheOwners_TheClient_AndTheProgress()
    {
        var schedule = new Schedule(Guid.CreateVersion7());

        // Friday 09/10, before the holiday of Monday 12/10.
        schedule.ApplyTemplate(
            Outline(
                [
                    new OutlineItem(0, null, ScheduleItemKind.Task, "Levantar regras", 0, 2, 100, Carla),
                    new OutlineItem(0, null, ScheduleItemKind.Task, "Parametrizar", 2, 3, 40, OwnedByClient: true),
                    new OutlineItem(0, null, ScheduleItemKind.Milestone, "Aceite", 6, 0, 100),
                    new OutlineItem(0, null, ScheduleItemKind.Milestone, "Entrada em produção", 20, 0),
                ],
                new OutlineDependency(0, 1),
                new OutlineDependency(1, 2)),
            D(10, 9),
            Days,
            Today);

        var items = schedule.Items.ToDictionary(item => item.Name);
        // The task at 100% that ended before today is completed on its end.
        (items["Levantar regras"].Start, items["Levantar regras"].End, items["Levantar regras"].OwnerId, items["Levantar regras"].CompletedOn)
            .Should().Be((D(10, 9), D(10, 13), Carla, D(10, 13)));
        (items["Parametrizar"].Start, items["Parametrizar"].End, items["Parametrizar"].Progress, items["Parametrizar"].OwnedByClient)
            .Should().Be((D(10, 14), D(10, 16), 40, true));
        // The milestone at 100% after today is reached today.
        (items["Aceite"].Start, items["Aceite"].ReachedOn).Should().Be((D(10, 20), Today));
        items["Entrada em produção"].ReachedOn.Should().BeNull();
    }

    [Fact(DisplayName = "Tarefa-resumo de um plano")]
    public void TheProgressOfATaskWithSubtasks_ComesFromThem()
    {
        var schedule = new Schedule(Guid.CreateVersion7());

        schedule.ApplyTemplate(
            Outline(
            [
                new OutlineItem(0, null, ScheduleItemKind.Task, "Testes", 0, 1, 90, Carla),
                new OutlineItem(0, 0, ScheduleItemKind.Task, "Roteiro", 0, 2, 100),
                new OutlineItem(0, 0, ScheduleItemKind.Task, "Execução", 2, 2, 0),
            ]),
            D(10, 13),
            Days,
            Today);

        var tests = schedule.Items.Single(item => item.Name == "Testes");
        (tests.Start, tests.End, tests.Progress, tests.OwnerId).Should().Be((D(10, 13), D(10, 16), 50, Carla));
    }

    private static SchedulePlanOutline Invalid(string reason) => reason switch
    {
        "fase fora do plano" => new([new OutlinePhase("A")], [new OutlineItem(1, null, ScheduleItemKind.Task, "T", 0, 1)], []),
        "tarefa depois da subtarefa" => Outline([new OutlineItem(0, 1, ScheduleItemKind.Task, "S", 0, 1), new OutlineItem(0, null, ScheduleItemKind.Task, "T", 0, 1)]),
        "dependência de um item seguinte" => Outline(
            [new OutlineItem(0, null, ScheduleItemKind.Task, "A", 0, 1), new OutlineItem(0, null, ScheduleItemKind.Task, "B", 0, 1)],
            new OutlineDependency(1, 0)),
        "dependência de uma tarefa-resumo" => Outline(
            [
                new OutlineItem(0, null, ScheduleItemKind.Task, "T", 0, 1),
                new OutlineItem(0, 0, ScheduleItemKind.Task, "S", 0, 1),
                new OutlineItem(0, null, ScheduleItemKind.Task, "B", 1, 1),
            ],
            new OutlineDependency(0, 2)),
        "marco a 50%" => Outline([new OutlineItem(0, null, ScheduleItemKind.Milestone, "M", 0, 0, 50)]),
        "tarefa a 101%" => Outline([new OutlineItem(0, null, ScheduleItemKind.Task, "T", 0, 1, 101)]),
        "duração de 1.000 dias" => Outline([new OutlineItem(0, null, ScheduleItemKind.Task, "T", 0, 1000)]),
        "marco com responsável" => Outline([new OutlineItem(0, null, ScheduleItemKind.Milestone, "M", 0, 0, OwnerId: Carla)]),
        _ => Outline([new OutlineItem(0, null, ScheduleItemKind.Task, " ", 0, 1)]),
    };

    [Theory(DisplayName = "Plano que não se sustenta")]
    [InlineData("fase fora do plano")]
    [InlineData("tarefa depois da subtarefa")]
    [InlineData("dependência de um item seguinte")]
    [InlineData("dependência de uma tarefa-resumo")]
    [InlineData("marco a 50%")]
    [InlineData("tarefa a 101%")]
    [InlineData("duração de 1.000 dias")]
    [InlineData("marco com responsável")]
    [InlineData("nome vazio")]
    public void AnOutlineThatDoesNotHoldTogether_IsRefused(string reason)
    {
        var schedule = new Schedule(Guid.CreateVersion7());

        FluentActions.Invoking(() => schedule.ApplyTemplate(Invalid(reason), D(10, 13), Days, Today))
            .Should().Throw<ArgumentException>(reason);
    }
}
