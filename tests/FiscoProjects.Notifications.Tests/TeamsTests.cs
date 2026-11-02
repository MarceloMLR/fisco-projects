using System.Text.Json;
using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Contracts;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Notifications.Infrastructure;

namespace FiscoProjects.Notifications.Tests;

// Spec: avisos, requirements "Canal dos avisos na hora", "Aviso na aba Atividade" and "Implementação local do Teams";
// configuracao-da-instalacao, "Aplicativo do Teams" (ADR-0149, ADR-0151).
public sealed class TeamsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "fiscoprojects-teams-" + Guid.NewGuid().ToString("N"));

    [Theory(DisplayName = "Canais de cada pessoa")]
    [InlineData(true, false, true, true, false)]
    [InlineData(false, true, true, false, true)]
    [InlineData(true, true, true, true, true)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(true, true, false, true, false)]
    public void EachChannelOn_Receives_TeamsOnlyWhenThere(bool email, bool teams, bool teamsThere, bool byEmail, bool byTeams)
    {
        NoticeRouting.RouteOf(new NoticeChannels(email, teams), teamsThere).Should().Be(new ImmediateRoute(byEmail, byTeams));
    }

    [Fact(DisplayName = "Canais de quem nunca escolheu no domínio")]
    public void WhoNeverChose_TheEmailOnAndTeamsOff()
    {
        NoticePreference.Default.Should().Be(new NoticeChannels(ByEmail: true, ByTeams: false));
    }

    [Theory(DisplayName = "Quem o Teams alcança")]
    [InlineData(TeamsProvider.Graph, "0f3c8a12", true)]
    [InlineData(TeamsProvider.Graph, null, false)]
    [InlineData(TeamsProvider.Local, null, true)]
    [InlineData(TeamsProvider.None, "0f3c8a12", false)]
    public void TeamsReaches_WhoSignsInByEntra_WithGraph(TeamsProvider provider, string? entraObjectId, bool reaches)
    {
        var routing = new NoticeRouting(new TeamsSettings(provider, "https://projetos.empresa.example", null), new NoApps());

        routing.Reaches(entraObjectId).Should().Be(reaches);
    }

    [Fact]
    public void ThePackage_NewVersionOnlyWhenTheContentChanged()
    {
        var app = TeamsApp.Create();

        app.Version.Should().BeNull();
        app.Package("A").Should().Be("1.0.1");
        app.Package("A").Should().Be("1.0.1");
        app.Package("B").Should().Be("1.0.2");
        app.Version.Should().Be("1.0.2");
    }

    [Fact]
    public void ATryAgain_WaitsAndFailsAfterTheLast()
    {
        var now = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var notice = Notice(now);

        for (var attempt = 1; attempt <= OutboxEmail.MaxAttempts; attempt++)
        {
            now = notice.NextAttemptAt;
            notice.Reserve(now);
            notice.MarkFailed(now, "Microsoft Graph answered 503.");
        }

        notice.Status.Should().Be(TeamsNoticeStatus.Failed);
    }

    [Fact]
    public void ARefusal_FailsWithoutAnotherTry()
    {
        var now = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var notice = Notice(now);
        notice.Reserve(now);

        notice.Refuse(now, "The application of Teams is not installed for the person.");

        notice.Status.Should().Be(TeamsNoticeStatus.Failed);
        notice.IsDue(now.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void TheTexts_HaveTheParametersOfTheirTemplate()
    {
        var texts = new TeamsTexts();

        var message = texts.MessageOf(
            NoticeKind.TicketDueSoon,
            new Dictionary<string, string>
            {
                [NoticeValues.Number] = "104",
                [NoticeValues.Title] = "Rejeição 539 ao emitir nota de devolução",
                [NoticeValues.Project] = "ERP-FIN",
                [NoticeValues.Deadline] = "Resolution",
                [NoticeValues.DueOn] = "2026-11-03",
            },
            actorName: null,
            "/tickets/104",
            "FiscoProjects");

        message.ActivityType.Should().Be("ticketDueSoon");
        message.Parameters.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["deadline"] = "resolução",
            ["ticket"] = "#104",
            ["dueOn"] = "03/11/2026",
        });
        texts.ActivityTypes().Select(type => type.Type).Should().HaveCount(EmailChannel.Immediate.Count);
    }

    [Fact(DisplayName = "Aviso gravado sem envio")]
    public async Task TheLocalSender_WritesTheNoticeToTheFolder()
    {
        var sender = new LocalTeamsSender(_folder);
        var manifestId = Guid.CreateVersion7();

        var result = await sender.SendAsync(
            new TeamsDelivery(
                manifestId,
                Guid.CreateVersion7(),
                "Diego Torres",
                null,
                new TeamsMessage(
                    "ticketAssigned",
                    [new("person", "Marina Costa"), new("ticket", "#104")],
                    "ERP-FIN",
                    "Rejeição 539 ao emitir nota de devolução",
                    "/tickets/104")),
            TestContext.Current.CancellationToken);

        result.Outcome.Should().Be(TeamsSendOutcome.Sent);
        var file = Directory.GetFiles(_folder, "*.json").Should().ContainSingle().Subject;
        var written = JsonDocument.Parse(await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken)).RootElement;
        written.GetProperty("to").GetProperty("name").GetString().Should().Be("Diego Torres");
        written.GetProperty("activityType").GetString().Should().Be("ticketAssigned");
        written.GetProperty("parameters").GetProperty("person").GetString().Should().Be("Marina Costa");
        written.GetProperty("preview").GetString().Should().Be("Rejeição 539 ao emitir nota de devolução");
        written.GetProperty("link").GetString().Should().StartWith($"https://teams.microsoft.com/l/entity/{manifestId}/home?context=");
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static TeamsNotice Notice(DateTime now) =>
        new(Guid.CreateVersion7(), NoticeKind.TicketAssigned, Guid.CreateVersion7(), Guid.CreateVersion7(), "/tickets/104", "{}", now);

    private sealed class NoApps : ITeamsAppStore
    {
        public Task<TeamsApp> GetAsync(CancellationToken cancellationToken) => Task.FromResult(TeamsApp.Create());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
