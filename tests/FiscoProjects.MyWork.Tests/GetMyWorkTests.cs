using FiscoProjects.Administration.Contracts;
using FiscoProjects.Calendar.Contracts;
using FiscoProjects.MyWork.Application;
using FiscoProjects.MyWork.Contracts;
using FiscoProjects.MyWork.Domain;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.MyWork.Tests;

// Spec: meu-trabalho, "Fila pela API" (the items of every module, today of the installation) (ADR-0013, ADR-0096).
public sealed class GetMyWorkTests
{
    private static readonly Guid Marina = Guid.CreateVersion7();
    private static readonly Guid Diego = Guid.CreateVersion7();

    private sealed class Installation : IWorkCalendar, IInstallationSettings
    {
        public Task<IBusinessDays> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IBusinessDays>(TestBusinessDays.Of2026);

        Task<InstallationInfo> IInstallationSettings.GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new InstallationInfo("Empresa de demonstração", "America/Sao_Paulo", new TimeOnly(7, 30), 90));
    }

    private sealed class Clock(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utc, TimeSpan.Zero);
    }

    private sealed class Source(params MyWorkItem[] items) : IMyWorkSource
    {
        public MyWorkQuery? Asked { get; private set; }

        public Task<IReadOnlyList<MyWorkItem>> ItemsAsync(MyWorkQuery query, CancellationToken cancellationToken)
        {
            Asked = query;
            return Task.FromResult<IReadOnlyList<MyWorkItem>>(items);
        }
    }

    private sealed class Directory : IUserDirectory
    {
        public Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                new Dictionary<Guid, string> { [Marina] = "Marina Costa", [Diego] = "Diego Torres" }
                    .Where(pair => userIds.Contains(pair.Key))
                    .ToDictionary());

        public Task<IReadOnlyList<PersonInfo>> PeopleAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [Fact(DisplayName = "Itens de todos os módulos no dia da instalação")]
    public async Task Items_OfEveryModule_OnTheDayOfTheInstallation()
    {
        var ticket = new MyWorkItem(MyWorkKind.Ticket, Guid.CreateVersion7(), Guid.CreateVersion7(), "ERP-FIN", "Liberar ambiente", MyWorkViews.ForMe)
        {
            WaitsForYou = true,
            WaitingSince = new DateTime(2026, 10, 19, 20, 0, 0, DateTimeKind.Utc),
            Ticket = new MyWorkTicket(107, MyWorkTicketStatus.WaitingForRequester, MyWorkTeam.Development, Marina, Diego, true, 4, null),
        };
        var task = new MyWorkItem(MyWorkKind.Task, Guid.CreateVersion7(), Guid.CreateVersion7(), "FISC-SP", "Revisar CFOP", MyWorkViews.ForMe)
        {
            DueOn = new DateOnly(2026, 10, 19),
            Task = new MyWorkTask("Diagnóstico", 0),
        };
        var tickets = new Source(ticket);
        var tasks = new Source(task);
        var installation = new Installation();
        // 23:00 of Monday, 19/10, in São Paulo: still Monday for the installation.
        var myWork = new GetMyWork(
            [tickets, tasks], installation, installation, new Clock(new DateTime(2026, 10, 20, 2, 0, 0, DateTimeKind.Utc)), new Directory());

        var result = await myWork.ExecuteAsync(Marina, isAdministrator: false, TestContext.Current.CancellationToken);

        tickets.Asked.Should().Be(new MyWorkQuery(Marina, false, new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 26)));
        result.Window.NextBusinessDay.Should().Be(new DateOnly(2026, 10, 20));
        var forMe = result.Views.Single(view => view.View == MyWorkView.ForMe);
        forMe.Groups.Select(group => (group.Group, group.Entries.Single().Action))
            .Should().Equal((MyWorkGroup.Waiting, MyWorkAction.Reply), (MyWorkGroup.Soon, MyWorkAction.Start));
        (result.NameOf(Marina), result.NameOf(Diego)).Should().Be(("Marina Costa", "Diego Torres"));
    }
}
