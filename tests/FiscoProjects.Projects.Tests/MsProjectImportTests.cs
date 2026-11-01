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
    public void Read_HasTheLines_TheResources_AndTheAssignments()
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
        file.Resources.Should().Equal(
            new MsProjectResource(1, "Carla Mendes", "carla.mendes@empresa.example"),
            new MsProjectResource(2, "Servidor", null, IsWork: false),
            new MsProjectResource(3, "Licenças", null, IsWork: false));
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
        // At the limit, each task after the one before it: the schedule is made at once, without pushing each time.
        var chain = lines[..MsProjectImport.MaxTasks]
            .Select(line => line with { Links = line.Uid == 1 ? null : [new Link(line.Uid - 1)] })
            .ToList();
        var converted = Converted(Project("No limite", chain));
        var schedule = new Schedule(Guid.CreateVersion7());
        schedule.ApplyTemplate(converted.Outline, converted.FirstDay, Days, Today);
        (converted.Outline.Items.Count, converted.Outline.Dependencies.Count).Should().Be((MsProjectImport.MaxTasks, MsProjectImport.MaxTasks - 1));
        schedule.Items[^1].Start.Should().Be(Days.AddBusinessDays(D(10, 13), MsProjectImport.MaxTasks - 1));
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
        schedule.Counts.Should().Be(new MsProjectCounts(FileTasks: 3, Tasks: 3, FileResources: 1, Resources: 1));
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
                new Line(6, 3, "Massa de testes", D(10, 15), D(10, 15), Links: [new Link(2, Type: 2)]),
                new Line(7, 2, "Ajustar parâmetros", D(10, 19), D(10, 20), Links: [new Link(2, Lag: 500, LagFormat: 19)]),
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
        // "Start to finish" becomes "finish to start"; the lag of half of the 2 business days before becomes 1; the one of a
        // summary goes away (ADR-0137).
        schedule.Outline.Dependencies.Should().Equal(new OutlineDependency(0, 3), new OutlineDependency(0, 4, Lag: 1));
        WarningsOf(schedule, MsProjectWarningKind.LooseTasks).Should().Equal("Comunicar a diretoria");
        WarningsOf(schedule, MsProjectWarningKind.DeepTasks).Should().Equal("Roteiro de notas");
        WarningsOf(schedule, MsProjectWarningKind.DroppedSummaries).Should().Equal("Roteiros");
        WarningsOf(schedule, MsProjectWarningKind.OtherDependencies).Should().Equal("Levantar regras → Massa de testes");
        WarningsOf(schedule, MsProjectWarningKind.DependencyLags).Should().Equal("Levantar regras → Ajustar parâmetros");
        WarningsOf(schedule, MsProjectWarningKind.SummaryDependencies).Should().Equal("Testes → Revisar");
    }

    [Fact(DisplayName = "Tipos e intervalos do arquivo")]
    public void Convert_KeepsTheKindsAndTheLagsInDays()
    {
        var schedule = Converted(Project(
            "Implantação do ERP",
            [
                new Line(1, 1, "Configuração", D(10, 13), D(10, 23)),
                new Line(2, 2, "Parametrizar", D(10, 13), D(10, 16)),
                new Line(3, 2, "Testes", D(10, 14), D(10, 16), Links: [new Link(2, Type: 3, Lag: 4800)]),
                new Line(4, 2, "Treinamento", D(10, 15), D(10, 16), Links: [new Link(2, Type: 0)]),
                new Line(5, 2, "Virada", D(10, 23), D(10, 23), Milestone: true, Links: [new Link(4, Lag: 4 * 4800)]),
                new Line(6, 2, "Comunicado", D(10, 13), D(10, 13), Links: [new Link(2, Lag: -2400, LagFormat: 5)]),
            ]));

        schedule.Outline.Dependencies.Should().Equal(
            new OutlineDependency(0, 1, ScheduleLinkType.StartToStart, 1),
            new OutlineDependency(0, 2, ScheduleLinkType.FinishToFinish),
            new OutlineDependency(2, 3, Lag: 4),
            new OutlineDependency(0, 4, Lag: -1));
        // Half a day before in hours is not a whole day: it becomes one, with the warning.
        WarningsOf(schedule, MsProjectWarningKind.DependencyLags).Should().Equal("Parametrizar → Comunicado");
        WarningsOf(schedule, MsProjectWarningKind.OtherDependencies).Should().BeEmpty();
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
    public void Convert_LeavesOutTheInactiveBlankAndNamelessLines_WithAWarning()
    {
        var schedule = Converted(Project(
            "Projeto",
            [
                new Line(1, 1, "Configuração", D(10, 13), D(10, 15)),
                new Line(2, 2, "Fica", D(10, 13), D(10, 13)),
                new Line(3, 2, "Cancelada", D(10, 14), D(10, 15), Active: false),
                new Line(4, 3, "Dentro da cancelada", D(10, 14), D(10, 15)),
                new Line(5, 2, string.Empty, D(10, 14), D(10, 14), Blank: true),
                new Line(6, 2, "Depende da cancelada", D(10, 16), D(10, 16), Links: [new Link(4)]),
                new Line(7, 2, string.Empty, D(10, 16), D(10, 16)),
                new Line(8, 3, "Dentro da sem nome", D(10, 16), D(10, 16)),
            ]));

        schedule.Outline.Items.Select(item => item.Name).Should().Equal("Fica", "Depende da cancelada");
        schedule.Outline.Dependencies.Should().BeEmpty();
        schedule.Warnings.Select(warning => (warning.Kind, warning.Names, warning.Count)).Should().BeEquivalentTo(
            [
                (MsProjectWarningKind.InactiveTasks, new[] { "Cancelada", "Dentro da cancelada" }, 2),
                (MsProjectWarningKind.BlankLines, Array.Empty<string>(), 3),
            ],
            options => options.WithStrictOrdering());
        // The blank line goes alone, and the nameless one with the line under it; neither is a task of the file.
        schedule.Counts.Should().Be(new MsProjectCounts(FileTasks: 6, Tasks: 3, FileResources: 0, Resources: 0));
    }

    [Fact(DisplayName = "Recursos que não vêm do arquivo")]
    public void Convert_LeavesOutTheResourcesOfMaterialAndCost_AndTheWorkResourcesWithoutTasks_WithAWarning()
    {
        var schedule = Converted(Project(
            "Recursos",
            [
                new Line(1, 1, "Fase", D(10, 13), D(10, 20)),
                new Line(2, 2, "Com dois recursos", D(10, 13), D(10, 14)),
                new Line(3, 2, "Inativa", D(10, 15), D(10, 16), Active: false),
                new Line(4, 2, "Marco", D(10, 20), D(10, 20), Milestone: true),
            ],
            [
                new Resource(1, "Carla Mendes"),
                new Resource(2, "Segunda da tarefa"),
                new Resource(3, "Só na inativa"),
                new Resource(4, "Só no marco"),
                new Resource(5, "Sem tarefas"),
                new Resource(6, "Servidor", Type: 0),
                new Resource(7, "Licenças", Type: 2),
            ],
            [(2, 1), (2, 2), (2, 6), (3, 3), (4, 4), (2, 7)]));

        schedule.Resources.Should().Equal(new MsProjectImportedResource("Carla Mendes", null, 1));
        schedule.ItemResources.Should().Equal(0, null);
        schedule.Warnings.Select(warning => (warning.Kind, warning.Names, warning.Count)).Should().BeEquivalentTo(
            [
                (MsProjectWarningKind.SeveralResources, new[] { "Com dois recursos" }, 1),
                (MsProjectWarningKind.NoOwner, new[] { "Marco" }, 1),
                (MsProjectWarningKind.OtherResources, new[] { "Servidor", "Licenças" }, 2),
                (MsProjectWarningKind.UnusedResources, new[] { "Segunda da tarefa", "Só na inativa", "Só no marco", "Sem tarefas" }, 4),
                (MsProjectWarningKind.InactiveTasks, new[] { "Inativa" }, 1),
            ],
            options => options.WithStrictOrdering());
        schedule.Counts.Should().Be(new MsProjectCounts(FileTasks: 4, Tasks: 3, FileResources: 7, Resources: 1));
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
        var tests = Add(source, ScheduleItemKind.Task, configuration, "Testes integrados", D(10, 16), 4, Rafael, 0, [survey], deadline: D(10, 21));
        var script = Add(source, ScheduleItemKind.Task, configuration, "Roteiro", D(10, 16), 2, null, 50, [survey], tests);
        Add(source, ScheduleItemKind.Task, configuration, "Execução", D(10, 20), 2, null, 0, [script], tests, ownedByClient: true);
        var acceptance = Add(source, ScheduleItemKind.Milestone, go, "Aceite", D(10, 22), 0, null, 0, [script], deadline: D(10, 9));
        source.SetReached(acceptance.Id, true, Today);
        var document = MsProjectExport.Of(
            "ERP-FIN", "Implantação do ERP", source, source.Figures(Today), Days,
            new Dictionary<Guid, MsProjectOwner> { [Rafael] = new("Rafael Souza", "rafael.souza@empresa.example") },
            new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc),
            "Cliente Exemplo");

        var imported = Converted(Bytes(document));
        var made = new Schedule(Guid.CreateVersion7());
        made.ApplyTemplate(imported.Outline, imported.FirstDay, Days, Today);

        imported.Warnings.Should().BeEmpty();
        made.Phases.Select(phase => phase.Name).Should().Equal(source.Phases.Select(phase => phase.Name));
        Shape(made).Should().BeEquivalentTo(Shape(source));
        imported.Resources.Should().Equal(
            new MsProjectImportedResource("Rafael Souza", "rafael.souza@empresa.example", 2),
            new MsProjectImportedResource("Cliente Exemplo", null, 1));
    }

    [Fact(DisplayName = "Data-limite no MS Project")]
    public void Deadline_GoesInTheXml_AndMovesWithTheStart()
    {
        var source = new Schedule(Guid.CreateVersion7());
        var configuration = source.AddPhase("Configuração");
        var survey = Add(source, ScheduleItemKind.Task, configuration, "Levantar regras", D(10, 21), 3, null, 0);
        var tests = Add(source, ScheduleItemKind.Task, configuration, "Testes integrados", D(10, 26), 4, null, 0, [survey], deadline: D(10, 30));
        var document = MsProjectExport.Of(
            "ERP-FIN", "Implantação do ERP", source, source.Figures(Today), Days, new Dictionary<Guid, MsProjectOwner>(),
            new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc));
        var imported = Converted(Bytes(document));
        var made = new Schedule(Guid.CreateVersion7());

        // Five business days after the first day of the file, 21/10: 28/10.
        made.ApplyTemplate(imported.Outline, Days.AddBusinessDays(imported.FirstDay, 5), Days, Today);

        var task = document.Descendants(MsProjectExport.Ns + "Task")
            .Single(element => (string?)element.Element(MsProjectExport.Ns + "Name") == tests.Name);
        ((string?)task.Element(MsProjectExport.Ns + "Deadline")).Should().Be("2026-10-30T17:00:00");
        // 30/10 plus five business days, without the holiday of 02/11: 09/11, and the task moves the same.
        var moved = made.Items.Single(item => item.Name == tests.Name);
        (moved.Start, moved.End, moved.Deadline).Should().Be((D(11, 3), D(11, 6), D(11, 9)));
        made.Items.Single(item => item.Name == survey.Name).Deadline.Should().BeNull();
    }

    [Fact(DisplayName = "Data-limite do arquivo do MS Project")]
    public void DeadlineOfTheFile_IsCountedInBusinessDays_FromTheFirstDay_EvenBeforeIt()
    {
        var schedule = Converted(Project("Cronograma", [
            new Line(1, 1, "Configuração", D(10, 14), D(10, 16)),
            new Line(2, 2, "Antes do início", D(10, 14), D(10, 16), Deadline: D(10, 9)),
            new Line(3, 2, "Num sábado", D(10, 14), D(10, 16), Deadline: D(10, 17)),
            new Line(4, 2, "Sem data-limite", D(10, 14), D(10, 16)),
        ]));
        var made = new Schedule(Guid.CreateVersion7());

        made.ApplyTemplate(schedule.Outline, schedule.FirstDay, Days, Today);

        schedule.Outline.Items.Select(item => item.DeadlineOffset).Should().Equal(-2, 3, null);
        // Before the first day, back in business days without the holiday of 12/10; on a Saturday, to the next business day.
        made.Items.Select(item => item.Deadline).Should().Equal(D(10, 9), D(10, 19), null);
    }

    private static ScheduleItem Add(
        Schedule schedule, ScheduleItemKind kind, SchedulePhase phase, string name, DateOnly start, int duration, Guid? owner, int progress,
        ScheduleItem[]? after = null, ScheduleItem? parent = null, bool ownedByClient = false, DateOnly? deadline = null) =>
        schedule.AddItem(kind, new ScheduleItemPlan(
            phase.Id, name, start, duration, owner, progress, [.. (after ?? []).Select(item => item.Id)], parent?.Id, ownedByClient, deadline),
            Days, Today);

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
        item.Deadline,
        Before = schedule.PredecessorsOf(item.Id).Select(predecessor => predecessor.Name).Order().ToList(),
    });
}
