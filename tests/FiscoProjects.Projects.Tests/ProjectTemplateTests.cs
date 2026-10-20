using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Tests;

// Spec: modelos-de-projeto (domain side): "Salvar como modelo" and "Projeto a partir de um modelo" (ADR-0017). The
// calendar of 2026 has 12/10 as a holiday, on a Monday.
public sealed class ProjectTemplateTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);
    private static readonly Guid Rafael = Guid.CreateVersion7();

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static ScheduleItem Add(Schedule schedule, SchedulePhase phase, ScheduleItemKind kind, string name, DateOnly start, int duration,
        Guid? owner = null, Guid? parent = null, params ScheduleItem[] after) =>
        schedule.AddItem(kind, new ScheduleItemPlan(phase.Id, name, start, duration, owner, kind == ScheduleItemKind.Milestone ? 0 : 40, [.. after.Select(item => item.Id)], parent), Days, Today);

    private static Schedule Source()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var configuration = schedule.AddPhase("Configuração");
        schedule.AddPhase("Homologação");
        var task = Add(schedule, configuration, ScheduleItemKind.Task, "Parametrizar regras de ICMS", D(10, 13), 3, Rafael);
        Add(schedule, configuration, ScheduleItemKind.Milestone, "Aceite da configuração", D(10, 15), 0, after: task);
        // Two business days after the start, with nothing before it.
        Add(schedule, configuration, ScheduleItemKind.Task, "Treinar usuários", D(10, 15), 2, Rafael);
        return schedule;
    }

    [Fact(DisplayName = "Salvar o cronograma como modelo")]
    public void Template_KeepsPhasesItemsDependenciesAndDurations_WithoutDatesNorOwners()
    {
        var template = ProjectTemplate.From(Source(), " Implantação de ERP ", "  ", Days)!;

        (template.Name, template.Description, template.IsActive).Should().Be(("Implantação de ERP", null, true));
        template.Phases.Select(phase => phase.Name).Should().Equal("Configuração", "Homologação");
        template.Items.OrderBy(item => item.Position)
            .Select(item => (item.Name, item.Kind, item.StartOffset, item.Duration))
            .Should().Equal(
                ("Parametrizar regras de ICMS", ScheduleItemKind.Task, 0, 3),
                ("Aceite da configuração", ScheduleItemKind.Milestone, 2, 0),
                ("Treinar usuários", ScheduleItemKind.Task, 2, 2));
        var dependency = template.Dependencies.Single();
        template.Items.Single(item => item.Id == dependency.SuccessorId).Name.Should().Be("Aceite da configuração");
        ProjectTemplate.From(new Schedule(Guid.CreateVersion7()), "Vazio", null, Days).Should().BeNull();
    }

    [Fact(DisplayName = "Criar projeto a partir de um modelo")]
    public void ApplyingATemplate_MakesTheScheduleInBusinessDaysFromTheStart()
    {
        var template = ProjectTemplate.From(Source(), "Implantação de ERP", null, Days)!;
        var schedule = new Schedule(Guid.CreateVersion7());

        // Friday 09/10, before the holiday of Monday 12/10.
        schedule.ApplyTemplate(template, D(10, 9), Days, Today);

        schedule.Phases.Select(phase => phase.Name).Should().Equal("Configuração", "Homologação");
        var items = schedule.Items.ToDictionary(item => item.Name);
        (items["Parametrizar regras de ICMS"].Start, items["Parametrizar regras de ICMS"].End).Should().Be((D(10, 9), D(10, 14)));
        items["Aceite da configuração"].Start.Should().Be(D(10, 14));
        schedule.PredecessorsOf(items["Aceite da configuração"].Id).Single().Name.Should().Be("Parametrizar regras de ICMS");
        (items["Treinar usuários"].Start, items["Treinar usuários"].End).Should().Be((D(10, 14), D(10, 15)));
        schedule.Items.Should().OnlyContain(item => item.OwnerId == null && item.Progress == 0);
    }

    [Fact(DisplayName = "Modelo com subtarefas")]
    public void ATemplateWithSubtasks_MakesTheSummaryAndItsSubtasksAgain()
    {
        var schedule = new Schedule(Guid.CreateVersion7());
        var phase = schedule.AddPhase("Configuração");
        var tests = Add(schedule, phase, ScheduleItemKind.Task, "Testes integrados", D(10, 13), 5);
        var script = Add(schedule, phase, ScheduleItemKind.Task, "Roteiro de testes", D(10, 13), 2, parent: tests.Id);
        Add(schedule, phase, ScheduleItemKind.Task, "Executar testes", D(10, 15), 3, parent: tests.Id, after: script);
        var template = ProjectTemplate.From(schedule, "Testes", null, Days)!;
        var made = new Schedule(Guid.CreateVersion7());

        made.ApplyTemplate(template, D(11, 3), Days, Today);

        var items = made.Items.ToDictionary(item => item.Name);
        made.SubtasksOf(items["Testes integrados"].Id).Select(item => item.Name).Should().BeEquivalentTo(["Roteiro de testes", "Executar testes"]);
        (items["Testes integrados"].Start, items["Testes integrados"].End).Should().Be((D(11, 3), D(11, 9)));
        made.PredecessorsOf(items["Executar testes"].Id).Single().Name.Should().Be("Roteiro de testes");
    }
}
