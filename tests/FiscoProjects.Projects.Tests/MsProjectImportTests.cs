using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using static FiscoProjects.Testing.MsProjectFiles;

namespace FiscoProjects.Projects.Tests;

// Spec: cronograma, requirements "Importar do MS Project" and "Progresso e responsáveis do MS Project", the reading and
// the conversion (ADR-0135). The calendar of 2026 has 12/10 and 02/11 as holidays, on Mondays.
public sealed class MsProjectImportTests
{
    private static readonly TestBusinessDays Days = TestBusinessDays.Of2026;
    private static readonly DateOnly Today = D(10, 16);
    private static readonly Guid Rafael = Guid.CreateVersion7();

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static MsProjectFile Read(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return MsProjectImport.Read(stream);
    }

    private static MsProjectSchedule Converted(byte[] bytes) => MsProjectImport.Convert(Read(bytes), "Cronograma", Days);

    private static MsProjectRefusal RefusalOf(byte[] bytes) =>
        FluentActions.Invoking(() => Converted(bytes)).Should().Throw<MsProjectFileRefusedException>().Which.Reason;

    private static IReadOnlyList<string> WarningsOf(MsProjectSchedule schedule, MsProjectWarningKind kind) =>
        schedule.Warnings.SingleOrDefault(warning => warning.Kind == kind)?.Names ?? [];

    [Fact(DisplayName = "Leitura do XML do MS Project")]
    public void Read_HasTheLines_TheWorkResources_AndTheAssignments()
    {
        var file = Read(Project(
            "Implantação do ERP",
            [
                new Line(1, 1, "Configuração", D(10, 13), D(10, 15)),
                new Line(2, 2, "Parametrizar regras de ICMS", D(10, 13), D(10, 15), Percent: 40),
                new Line(3, 2, "Aceite", D(10, 15), D(10, 15), Milestone: true, Links: [new Link(2, Type: 1, Lag: 4800)]),
            ],
            [new Resource(1, "Carla Mendes", "carla.mendes@empresa.example"), new Resource(2, "Servidor", Type: 0), new Resource(3, "Licenças", Type: 2)],
            [(2, 1), (2, 2)]));

        file.Title.Should().Be("Implantação do ERP");
        // The summary of the whole project, at level 0, is not a line.
        file.Rows.Select(row => (row.Uid, row.Level, row.Name, row.Start, row.Finish, row.PercentComplete, row.Milestone)).Should().Equal(
            (1, 1, "Configuração", D(10, 13), D(10, 15), 0, false),
            (2, 2, "Parametrizar regras de ICMS", D(10, 13), D(10, 15), 40, false),
            (3, 2, "Aceite", D(10, 15), D(10, 15), 0, true));
        file.Rows[2].Links.Should().Equal(new MsProjectLink(2, 1, 4800, false));
        file.Resources.Should().Equal(new MsProjectResource(1, "Carla Mendes", "carla.mendes@empresa.example"));
        file.Assignments.Should().Equal(new MsProjectAssignment(2, 1), new MsProjectAssignment(2, 2));
    }

    [Fact(DisplayName = "Arquivo com DTD")]
    public void AFileWithADtd_IsRefused_WithoutReadingTheEntities()
    {
        var file = Bytes("""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE Project [ <!ENTITY secret SYSTEM "file:///etc/passwd"> ]>
            <Project xmlns="http://schemas.microsoft.com/project">
              <Tasks><Task><UID>1</UID><OutlineLevel>1</OutlineLevel><Name>&secret;</Name><Start>2026-10-13T08:00:00</Start></Task></Tasks>
            </Project>
            """);

        RefusalOf(file).Should().Be(MsProjectRefusal.NotMsProjectXml);
    }

    [Theory(DisplayName = "Arquivo que não é XML do MS Project")]
    [InlineData("<Project><Tasks><Task><UID>1</UID><OutlineLevel>1</OutlineLevel><Name>A</Name></Task></Tasks></Project>")]
    [InlineData("<Planilha xmlns=\"http://schemas.microsoft.com/project\" />")]
    [InlineData("D0CF11E0A1B11AE1, o começo de um .mpp")]
    public void AFileOfAnotherProgram_IsRefused(string text)
    {
        RefusalOf(Bytes(text)).Should().Be(MsProjectRefusal.NotMsProjectXml);
    }

    [Fact(DisplayName = "Arquivo grande demais")]
    public void AFileAbove5Megabytes_IsRefused()
    {
        var file = new byte[MsProjectImport.MaxBytes + 1];
        Array.Fill(file, (byte)' ');

        RefusalOf(file).Should().Be(MsProjectRefusal.TooLarge);
    }

    [Fact(DisplayName = "Tarefas demais")]
    public void AFileWithMoreThan2000Tasks_IsRefused()
    {
        var lines = Enumerable.Range(1, MsProjectImport.MaxTasks + 1).Select(uid => new Line(uid, 1, $"Tarefa {uid}", D(10, 13), D(10, 13))).ToList();

        RefusalOf(Project("Grande", lines)).Should().Be(MsProjectRefusal.TooManyTasks);
        Converted(Project("No limite", lines[..MsProjectImport.MaxTasks])).Outline.Items.Should().HaveCount(MsProjectImport.MaxTasks);
    }

    [Fact(DisplayName = "Arquivo sem tarefas")]
    public void AFileWithoutTasks_OrOnlyWithLinesThatAreNotBrought_IsRefused()
    {
        RefusalOf(Project("Vazio", [])).Should().Be(MsProjectRefusal.NoTasks);
        RefusalOf(Project("Inativo", [new Line(1, 1, "Fora", D(10, 13), D(10, 13), Active: false)])).Should().Be(MsProjectRefusal.NoTasks);
    }

    [Fact(DisplayName = "Conversão do arquivo do MS Project")]
    public void Convert_HasThePhase_TheTaskInBusinessDays_TheMilestoneAfterIt_AndTheResource()
    {
        var schedule = Converted(Configuration("carla.mendes@empresa.example"));

        (schedule.Title, schedule.FirstDay).Should().Be(("Implantação do ERP", D(10, 13)));
        schedule.Outline.Phases.Should().Equal(new OutlinePhase("Configuração"));
        schedule.Outline.Items.Should().Equal(
            new OutlineItem(0, null, ScheduleItemKind.Task, "Parametrizar regras de ICMS", 0, 3, 40),
            new OutlineItem(0, null, ScheduleItemKind.Milestone, "Aceite da configuração", 2, 0));
        schedule.Outline.Dependencies.Should().Equal(new OutlineDependency(0, 1));
        schedule.Resources.Should().Equal(new MsProjectImportedResource("Carla Mendes", "carla.mendes@empresa.example", 1));
        schedule.ItemResources.Should().Equal(0, null);
        schedule.Warnings.Should().BeEmpty();
    }

    [Fact(DisplayName = "Conversão do que não cabe no FiscoProjects")]
    public void Convert_MovesTheLevels_AndDropsTheDependenciesTheScheduleHasNot_WithAWarningForEach()
    {
        var schedule = Converted(Project(
            "Implantação do ERP",
            [
                new Line(1, 1, "Configuração", D(10, 13), D(10, 22)),
                new Line(2, 2, "Levantar regras", D(10, 13), D(10, 14)),
                new Line(3, 2, "Testes", D(10, 15), D(10, 16)),
                new Line(4, 3, "Roteiros", D(10, 15), D(10, 16)),
                new Line(5, 4, "Roteiro de notas", D(10, 15), D(10, 16)),
                new Line(6, 3, "Massa de testes", D(10, 15), D(10, 15), Links: [new Link(2, Type: 3)]),
                new Line(7, 2, "Ajustar parâmetros", D(10, 19), D(10, 20), Links: [new Link(2, Lag: 4800)]),
                new Line(8, 2, "Revisar", D(10, 21), D(10, 21), Links: [new Link(3)]),
                new Line(9, 1, "Comunicar a diretoria", D(10, 22), D(10, 22)),
            ]));

        schedule.Outline.Phases.Select(phase => phase.Name).Should().Equal("Configuração", "Implantação do ERP");
        schedule.Outline.Items.Select(item => (item.Phase, item.Parent, item.Name)).Should().Equal(
            (0, null, "Levantar regras"),
            (0, null, "Testes"),
            (0, 1, "Roteiro de notas"),
            (0, 1, "Massa de testes"),
            (0, null, "Ajustar parâmetros"),
            (0, null, "Revisar"),
            (1, null, "Comunicar a diretoria"));
        // The dependency with a lag stays without it; the others go away.
        schedule.Outline.Dependencies.Should().Equal(new OutlineDependency(0, 4));
        WarningsOf(schedule, MsProjectWarningKind.LooseTasks).Should().Equal("Comunicar a diretoria");
        WarningsOf(schedule, MsProjectWarningKind.DeepTasks).Should().Equal("Roteiro de notas");
        WarningsOf(schedule, MsProjectWarningKind.DroppedSummaries).Should().Equal("Roteiros");
        WarningsOf(schedule, MsProjectWarningKind.OtherDependencies).Should().Equal("Levantar regras → Massa de testes");
        WarningsOf(schedule, MsProjectWarningKind.DependencyLags).Should().Equal("Levantar regras → Ajustar parâmetros");
        WarningsOf(schedule, MsProjectWarningKind.SummaryDependencies).Should().Equal("Testes → Revisar");
    }

    [Fact(DisplayName = "Datas em dias úteis do calendário da instalação")]
    public void Convert_CountsTheBusinessDaysOfTheCalendar_AndKeepsTheDurationsFrom1To999()
    {
        var schedule = Converted(Project(
            "Datas",
            [
                new Line(1, 1, "Fase", D(10, 9), D(10, 14)),
                // From Friday to Wednesday, over the holiday of Monday 12/10: 3 business days.
                new Line(2, 2, "Sobre o feriado", D(10, 9), D(10, 14)),
                // On a Saturday: it starts on the next business day.
                new Line(3, 2, "No sábado", D(10, 17), D(10, 17)),
                new Line(4, 2, "Muito longa", D(10, 13), new DateOnly(2031, 1, 31)),
                new Line(5, 2, new string('N', 210), D(10, 13), D(10, 13)),
            ]));

        schedule.Outline.Items.Select(item => (item.StartOffset, item.Duration)).Should().Equal((0, 3), (5, 1), (1, 999), (1, 1));
        WarningsOf(schedule, MsProjectWarningKind.LongTasks).Should().Equal("Muito longa");
        schedule.Outline.Items[3].Name.Should().HaveLength(Schedule.ItemNameMaxLength);
        WarningsOf(schedule, MsProjectWarningKind.CutNames).Should().Equal(new string('N', Schedule.ItemNameMaxLength));
    }

    [Fact(DisplayName = "Linhas que não vêm do arquivo")]
    public void Convert_LeavesOutTheInactiveAndEmptyLines_WithTheLinesUnderThem()
    {
        var schedule = Converted(Project(
            "Projeto",
            [
                new Line(1, 1, "Configuração", D(10, 13), D(10, 15)),
                new Line(2, 2, "Fica", D(10, 13), D(10, 13)),
                new Line(3, 2, "Cancelada", D(10, 14), D(10, 15), Active: false),
                new Line(4, 3, "Dentro da cancelada", D(10, 14), D(10, 15)),
                new Line(5, 2, "Depende da cancelada", D(10, 16), D(10, 16), Links: [new Link(4)]),
            ]));

        schedule.Outline.Items.Select(item => item.Name).Should().Equal("Fica", "Depende da cancelada");
        schedule.Outline.Dependencies.Should().BeEmpty();
        schedule.Warnings.Should().BeEmpty();
    }

    [Fact(DisplayName = "Dependência que formaria ciclo")]
    public void Convert_DropsTheDependencyThatWouldMakeACycle()
    {
        var schedule = Converted(Project(
            "Ciclo",
            [
                new Line(1, 1, "Fase", D(10, 13), D(10, 14)),
                new Line(2, 2, "Primeira", D(10, 13), D(10, 13), Links: [new Link(3)]),
                new Line(3, 2, "Segunda", D(10, 14), D(10, 14), Links: [new Link(2)]),
            ]));

        schedule.Outline.Dependencies.Should().Equal(new OutlineDependency(0, 1));
        WarningsOf(schedule, MsProjectWarningKind.DependencyCycles).Should().Equal("Segunda → Primeira");
    }

    [Fact(DisplayName = "Progresso e responsáveis na conversão")]
    public void Convert_KeepsTheProgress_AndTheFirstResourceOfEachTask()
    {
        var schedule = Converted(Project(
            "Responsáveis",
            [
                new Line(1, 1, "Fase", D(10, 13), D(10, 20)),
                new Line(2, 2, "Concluída", D(10, 13), D(10, 14), Percent: 100),
                new Line(3, 2, "Com dois recursos", D(10, 15), D(10, 16), Percent: 30),
                new Line(4, 2, "Marco atingido", D(10, 20), D(10, 20), Percent: 100, Milestone: true),
            ],
            [new Resource(1, "Carla Mendes", "carla.mendes@empresa.example"), new Resource(2, "Rafael Souza")],
            [(2, 2), (3, 1), (3, 2), (4, 1)]));

        schedule.Outline.Items.Select(item => item.Progress).Should().Equal(100, 30, 100);
        schedule.Resources.Should().Equal(
            new MsProjectImportedResource("Rafael Souza", null, 1),
            new MsProjectImportedResource("Carla Mendes", "carla.mendes@empresa.example", 1));
        schedule.ItemResources.Should().Equal(0, 1, null);
        WarningsOf(schedule, MsProjectWarningKind.SeveralResources).Should().Equal("Com dois recursos");
        WarningsOf(schedule, MsProjectWarningKind.NoOwner).Should().Equal("Marco atingido");
    }

    [Fact(DisplayName = "Ida e volta pelo XML do MS Project")]
    public void TheFileTheExportSaves_ComesBackAsTheSameSchedule()
    {
        var source = new Schedule(Guid.CreateVersion7());
        var configuration = source.AddPhase("Configuração");
        var go = source.AddPhase("Entrada em produção");
        var survey = Add(source, ScheduleItemKind.Task, configuration, "Levantar regras", D(10, 13), 3, Rafael, 100);
        var tests = Add(source, ScheduleItemKind.Task, configuration, "Testes integrados", D(10, 16), 4, Rafael, 0, [survey]);
        var script = Add(source, ScheduleItemKind.Task, configuration, "Roteiro", D(10, 16), 2, null, 50, [survey], tests);
        Add(source, ScheduleItemKind.Task, configuration, "Execução", D(10, 20), 2, null, 0, [script], tests, ownedByClient: true);
        var acceptance = Add(source, ScheduleItemKind.Milestone, go, "Aceite", D(10, 22), 0, null, 0, [script]);
        source.SetReached(acceptance.Id, true, Today);
        var document = MsProjectExport.Of(
            "ERP-FIN", "Implantação do ERP", source, source.Figures(Today), Days,
            new Dictionary<Guid, string> { [Rafael] = "Rafael Souza" },
            new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc),
            "Cliente Exemplo");

        var imported = Converted(Bytes(document));
        var made = new Schedule(Guid.CreateVersion7());
        made.ApplyTemplate(imported.Outline, imported.FirstDay, Days, Today);

        imported.Warnings.Should().BeEmpty();
        made.Phases.Select(phase => phase.Name).Should().Equal(source.Phases.Select(phase => phase.Name));
        Shape(made).Should().BeEquivalentTo(Shape(source));
        imported.Resources.Select(resource => (resource.Name, resource.Tasks)).Should().Equal(("Rafael Souza", 2), ("Cliente Exemplo", 1));
    }

    private static ScheduleItem Add(
        Schedule schedule, ScheduleItemKind kind, SchedulePhase phase, string name, DateOnly start, int duration, Guid? owner, int progress,
        ScheduleItem[]? after = null, ScheduleItem? parent = null, bool ownedByClient = false) =>
        schedule.AddItem(kind, new ScheduleItemPlan(
            phase.Id, name, start, duration, owner, progress, [.. (after ?? []).Select(item => item.Id)], parent?.Id, ownedByClient), Days, Today);

    /// <summary>The items by name, with the phase, the task, the kind, the dates, the progress and the items before them.</summary>
    private static IEnumerable<object> Shape(Schedule schedule) => schedule.Items.Select(item => new
    {
        item.Name,
        Phase = schedule.PhaseOf(item.PhaseId)!.Name,
        Parent = item.ParentId is { } parent ? schedule.ItemOf(parent)!.Name : null,
        item.Kind,
        item.Start,
        item.End,
        item.Duration,
        item.Progress,
        item.IsDone,
        Before = schedule.PredecessorsOf(item.Id).Select(predecessor => predecessor.Name).Order().ToList(),
    });
}
