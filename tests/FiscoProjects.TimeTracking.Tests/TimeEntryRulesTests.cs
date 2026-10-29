using FiscoProjects.Projects.Contracts;
using FiscoProjects.TimeTracking.Domain;

namespace FiscoProjects.TimeTracking.Tests;

// Spec: horas-do-projeto (domain side): "Apontamento de horas", "Tarefa ou chamado do apontamento", "Horas de cada
// pessoa", "Horas de quem saiu do projeto" and "Permissões nas horas" (ADR-0128).
public sealed class TimeEntryRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 29);

    private static readonly Guid Project = Guid.CreateVersion7();

    private static readonly TimeEntryViewer Person = new(Guid.CreateVersion7(), IsAdministrator: false);

    private static ProjectAccess ProjectWith(ProjectMemberRole? role, bool closed = false) =>
        new(Project, "ERP-FIN", "Implantação do ERP financeiro", closed, role);

    private static (TimeEntryForm? Form, IReadOnlyList<TimeEntryField> Invalid) FormOf(
        decimal? hours = 2.5m, DateOnly? day = null, string? description = "Reunião de levantamento", Guid? task = null, Guid? ticket = null) =>
        TimeEntryForm.Of(Project, day ?? Today.AddDays(-1), hours, description, task, ticket, Today);

    private static TimeEntry EntryOf(Guid userId) => new(userId, FormOf().Form!, DateTime.UtcNow);

    [Fact(DisplayName = "Apontar horas no projeto")]
    public void Form_TrimsTheDescription()
    {
        var (form, invalid) = FormOf(description: "  Reunião de levantamento  ");

        invalid.Should().BeEmpty();
        form.Should().Be(new TimeEntryForm(Project, Today.AddDays(-1), 2.5m, "Reunião de levantamento", null, null));
    }

    [Fact(DisplayName = "Apontar horas de hoje")]
    public void Form_TakesToday() => FormOf(day: Today).Invalid.Should().BeEmpty();

    [Fact(DisplayName = "Dia depois de hoje")]
    public void Form_RefusesTomorrow() => FormOf(day: Today.AddDays(1)).Invalid.Should().Equal(TimeEntryField.Day);

    [Theory(DisplayName = "Horas fora dos quartos de hora")]
    [InlineData(1.1)]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(-1)]
    public void Form_RefusesHoursOutOfQuarters(double hours) =>
        FormOf(hours: (decimal)hours).Invalid.Should().Equal(TimeEntryField.Hours);

    [Theory(DisplayName = "Horas em quartos de hora")]
    [InlineData(0.25)]
    [InlineData(1.5)]
    [InlineData(7.75)]
    [InlineData(24)]
    public void Form_TakesQuarters(double hours) => FormOf(hours: (decimal)hours).Invalid.Should().BeEmpty();

    [Fact(DisplayName = "Apontamento sem descrição")]
    public void Form_RefusesNoDescription()
    {
        FormOf(description: "   ").Invalid.Should().Equal(TimeEntryField.Description);
        FormOf(description: new string('a', TimeEntry.DescriptionMaxLength + 1)).Invalid.Should().Equal(TimeEntryField.Description);
    }

    [Fact(DisplayName = "Tarefa e chamado juntos")]
    public void Form_RefusesTaskAndTicket() =>
        FormOf(task: Guid.CreateVersion7(), ticket: Guid.CreateVersion7()).Invalid.Should().Equal(TimeEntryField.Ticket);

    [Fact(DisplayName = "Mais de 24 horas no dia")]
    public void Day_TakesUpTo24Hours()
    {
        TimeEntryForm.FitsInDay(4m, 20m).Should().BeTrue();
        TimeEntryForm.FitsInDay(5m, 20m).Should().BeFalse();
    }

    [Theory(DisplayName = "Matriz das horas")]
    [InlineData(ProjectMemberRole.Manager, null)]
    [InlineData(ProjectMemberRole.TechLead, null)]
    [InlineData(ProjectMemberRole.Contributor, null)]
    [InlineData(ProjectMemberRole.Developer, null)]
    [InlineData(ProjectMemberRole.Viewer, TimeEntryRefusal.Forbidden)]
    public void Logging_FollowsTheRole(ProjectMemberRole role, TimeEntryRefusal? refusal) =>
        TimeEntryAccess.RefusalToLog(Person, ProjectWith(role)).Should().Be(refusal);

    [Fact(DisplayName = "Administrador fora do projeto")]
    public void Administrator_LogsWithoutARole() =>
        TimeEntryAccess.RefusalToLog(Person with { IsAdministrator = true }, ProjectWith(null)).Should().BeNull();

    [Fact(DisplayName = "Horas de um projeto de fora")]
    public void Outside_DoesNotSeeTheProject()
    {
        TimeEntryAccess.RefusalToLog(Person, ProjectWith(null)).Should().Be(TimeEntryRefusal.NotSeen);
        TimeEntryAccess.RefusalToLog(Person, null).Should().Be(TimeEntryRefusal.NotSeen);
    }

    [Fact(DisplayName = "Horas de projeto encerrado")]
    public void ClosedProject_TakesNoHours()
    {
        TimeEntryAccess.RefusalToLog(Person, ProjectWith(ProjectMemberRole.Contributor, closed: true)).Should().Be(TimeEntryRefusal.Closed);
        TimeEntryAccess.RefusalToLog(Person with { IsAdministrator = true }, ProjectWith(null, closed: true))
            .Should().Be(TimeEntryRefusal.Closed);
    }

    [Fact(DisplayName = "Horas de outra pessoa")]
    public void EntryOfOthers_IsSeenOnlyByTheAdministrator()
    {
        var entry = EntryOf(Guid.CreateVersion7());

        TimeEntryAccess.SeesEntry(Person, entry).Should().BeFalse();
        TimeEntryAccess.SeesEntry(Person with { IsAdministrator = true }, entry).Should().BeTrue();
        TimeEntryAccess.SeesEntry(Person, EntryOf(Person.UserId)).Should().BeTrue();
    }

    [Fact(DisplayName = "Quem saiu do projeto")]
    public void WhoLeft_NoLongerChangesTheEntries()
    {
        var entry = EntryOf(Person.UserId);

        TimeEntryAccess.RefusalToChange(Person, entry, ProjectWith(null)).Should().Be(TimeEntryRefusal.Forbidden);
        TimeEntryAccess.RefusalToChange(Person, entry, ProjectWith(ProjectMemberRole.Viewer)).Should().Be(TimeEntryRefusal.Forbidden);
        TimeEntryAccess.RefusalToChange(Person, entry, ProjectWith(ProjectMemberRole.Contributor)).Should().BeNull();
    }

    [Fact(DisplayName = "Corrigir o apontamento")]
    public void Change_CorrectsEveryField()
    {
        var entry = EntryOf(Person.UserId);
        var other = Guid.CreateVersion7();
        var task = Guid.CreateVersion7();

        entry.Change(new TimeEntryForm(other, Today, 6m, "Ajustes", task, null), DateTime.UtcNow);

        (entry.ProjectId, entry.Day, entry.Hours, entry.Description, entry.ScheduleItemId, entry.TicketId)
            .Should().Be((other, Today, 6m, "Ajustes", (Guid?)task, (Guid?)null));
    }
}
