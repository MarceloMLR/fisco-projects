using FiscoProjects.Notifications.Application;
using FiscoProjects.Notifications.Domain;
using FiscoProjects.Notifications.Infrastructure.Persistence;
using FiscoProjects.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FiscoProjects.Notifications.Tests;

public sealed class DispatchEmailsTests(SqlServerFixture sqlServer)
{
    [Fact(DisplayName = "Envio e marcação")]
    public async Task SendsAndMarks()
    {
        await using var database = await NotificationsDatabase.CreateAsync(sqlServer);
        await database.EnqueueAsync();

        var result = await database.RunAsync<DispatchEmails, DispatchResult>(
            dispatch => dispatch.ExecuteAsync(TestContext.Current.CancellationToken));

        result.Sent.Should().Be(1);
        var sent = database.Sender.Sent.Should().ContainSingle().Subject;
        sent.ToAddress.Should().Be("marina.costa@empresa.example");
        sent.Subject.Should().Be("Teste de envio do FiscoProjects");
        sent.TextBody.Should().Contain("Olá, Marina Costa.");
        var email = await database.NewContext().OutboxEmails.SingleAsync(TestContext.Current.CancellationToken);
        email.Status.Should().Be(OutboxEmailStatus.Sent);
        email.SentAt.Should().Be(database.Clock.GetUtcNow().UtcDateTime);
        email.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task AFailureIsTriedAgainAfterTheWait()
    {
        await using var database = await NotificationsDatabase.CreateAsync(sqlServer);
        await database.EnqueueAsync();
        database.Sender.Failing = true;

        var failed = await database.RunAsync<DispatchEmails, DispatchResult>(
            dispatch => dispatch.ExecuteAsync(TestContext.Current.CancellationToken));
        database.Sender.Failing = false;
        var tooSoon = await database.RunAsync<DispatchEmails, DispatchResult>(
            dispatch => dispatch.ExecuteAsync(TestContext.Current.CancellationToken));
        database.Clock.Advance(TimeSpan.FromMinutes(1));
        var later = await database.RunAsync<DispatchEmails, DispatchResult>(
            dispatch => dispatch.ExecuteAsync(TestContext.Current.CancellationToken));

        failed.Failures.Should().ContainSingle().Which.Should().Match<EmailFailure>(f => f.Attempts == 1 && !f.Final);
        failed.Failures[0].Error.Should().Contain("The mailbox is out.");
        tooSoon.Sent.Should().Be(0);
        later.Sent.Should().Be(1);
        var email = await database.NewContext().OutboxEmails.SingleAsync(TestContext.Current.CancellationToken);
        email.Status.Should().Be(OutboxEmailStatus.Sent);
        email.Attempts.Should().Be(2);
        email.LastError.Should().BeNull();
    }

    [Fact]
    public async Task OnlyOneProcessSendsAnEmail()
    {
        await using var database = await NotificationsDatabase.CreateAsync(sqlServer);
        await database.EnqueueAsync();

        // Two processes read the same e-mail; the first reservation wins, the second is refused by the row version.
        await using var first = database.NewScope();
        await using var second = database.NewScope();
        var firstStore = first.ServiceProvider.GetRequiredService<IOutboxStore>();
        var secondStore = second.ServiceProvider.GetRequiredService<IOutboxStore>();
        var now = database.Clock.GetUtcNow().UtcDateTime;
        var mine = (await firstStore.DueAsync(now, 10, TestContext.Current.CancellationToken)).Single();
        var theirs = (await secondStore.DueAsync(now, 10, TestContext.Current.CancellationToken)).Single();
        mine.Reserve(now);
        theirs.Reserve(now);

        var firstSaved = await firstStore.TrySaveAsync(mine, TestContext.Current.CancellationToken);
        var secondSaved = await secondStore.TrySaveAsync(theirs, TestContext.Current.CancellationToken);

        firstSaved.Should().BeTrue();
        secondSaved.Should().BeFalse();
        var email = await database.NewContext().OutboxEmails.SingleAsync(TestContext.Current.CancellationToken);
        email.Attempts.Should().Be(1);
    }

    [Fact(DisplayName = "Conteúdo apagado depois de 30 dias")]
    public async Task ForgetsTheContentAfterTheDays()
    {
        await using var database = await NotificationsDatabase.CreateAsync(sqlServer);
        await database.EnqueueAsync();
        await database.RunAsync<DispatchEmails, DispatchResult>(dispatch => dispatch.ExecuteAsync(TestContext.Current.CancellationToken));
        database.Clock.Advance(TimeSpan.FromDays(29));
        await database.EnqueueAsync("rafael.souza@empresa.example", "Rafael Souza");
        await database.RunAsync<DispatchEmails, DispatchResult>(dispatch => dispatch.ExecuteAsync(TestContext.Current.CancellationToken));

        database.Clock.Advance(TimeSpan.FromDays(2));
        var forgotten = await database.RunAsync<ForgetOldEmails, int>(forget => forget.ExecuteAsync(TestContext.Current.CancellationToken));

        forgotten.Should().Be(1);
        var emails = await database.NewContext().OutboxEmails.OrderBy(e => e.CreatedAt).ToListAsync(TestContext.Current.CancellationToken);
        emails[0].Should().BeEquivalentTo(new
        {
            Kind = EmailKinds.Test,
            Status = OutboxEmailStatus.Sent,
            ToAddress = (string?)null,
            Subject = (string?)null,
            HtmlBody = (string?)null,
            TextBody = (string?)null,
        });
        emails[0].ForgottenAt.Should().NotBeNull();
        emails[1].ToAddress.Should().Be("rafael.souza@empresa.example");
    }

    [Fact]
    public async Task AnEmailStillInTheQueueIsNotForgotten()
    {
        await using var database = await NotificationsDatabase.CreateAsync(sqlServer);
        await database.EnqueueAsync();
        database.Clock.Advance(TimeSpan.FromDays(40));

        var forgotten = await database.RunAsync<ForgetOldEmails, int>(forget => forget.ExecuteAsync(TestContext.Current.CancellationToken));

        forgotten.Should().Be(0);
        (await database.NewContext().OutboxEmails.SingleAsync(TestContext.Current.CancellationToken)).ToAddress.Should().NotBeNull();
    }

    [Fact]
    public async Task TheQueueIsOutOfTheAudit()
    {
        await using var database = await NotificationsDatabase.CreateAsync(sqlServer);
        var context = database.NewContext();

        var entity = context.Model.FindEntityType(typeof(OutboxEmail))!;

        entity.FindAnnotation("FiscoProjects:Audit:Excluded")?.Value.Should().Be(true);
        entity.GetTableName().Should().Be("outbox_emails");
        entity.GetSchema().Should().Be(NotificationsDbContext.Schema);
    }
}
