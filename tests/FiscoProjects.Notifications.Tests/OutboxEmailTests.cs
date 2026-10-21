using FiscoProjects.Notifications.Domain;

namespace FiscoProjects.Notifications.Tests;

public sealed class OutboxEmailTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    private static OutboxEmail NewEmail() =>
        new("Test", "marina.costa@empresa.example", "Marina Costa", "Assunto", "<p>Olá</p>", "Olá", Now);

    [Fact(DisplayName = "Falha e nova tentativa")]
    public void FailureWaitsForTheNextTry()
    {
        var email = NewEmail();
        email.Reserve(Now);

        email.MarkFailed(Now, "The mailbox is out.");

        email.Status.Should().Be(OutboxEmailStatus.Pending);
        email.Attempts.Should().Be(1);
        email.NextAttemptAt.Should().Be(Now.AddMinutes(1));
        email.LastError.Should().Be("The mailbox is out.");
        email.IsDue(Now.AddSeconds(59)).Should().BeFalse();
        email.IsDue(Now.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void TheWaitsGrowAfterEachFailure()
    {
        var email = NewEmail();
        var at = Now;
        var waits = new List<TimeSpan>();
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            email.Reserve(at);
            email.MarkFailed(at, "error");
            waits.Add(email.NextAttemptAt - at);
            at = email.NextAttemptAt;
        }

        waits.Should().Equal(
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1), TimeSpan.FromHours(4));
    }

    [Fact(DisplayName = "Falha definitiva")]
    public void TheSixthFailureIsFinal()
    {
        var email = NewEmail();
        var at = Now;
        for (var attempt = 1; attempt <= OutboxEmail.MaxAttempts; attempt++)
        {
            email.Reserve(at);
            email.MarkFailed(at, $"error {attempt}");
            at = email.NextAttemptAt;
        }

        email.Status.Should().Be(OutboxEmailStatus.Failed);
        email.Attempts.Should().Be(6);
        email.FailedAt.Should().NotBeNull();
        email.LastError.Should().Be("error 6");
        email.IsDue(at.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void AReservationKeepsOthersFromTakingTheEmail()
    {
        var email = NewEmail();

        email.Reserve(Now);

        email.IsDue(Now.AddMinutes(9)).Should().BeFalse();
        email.IsDue(Now.Add(OutboxEmail.Reservation)).Should().BeTrue();
        var reserveAgain = () => email.Reserve(Now.AddMinutes(1));
        reserveAgain.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ALongErrorIsCut()
    {
        var email = NewEmail();
        email.Reserve(Now);

        email.MarkFailed(Now, new string('x', 2000));

        email.LastError.Should().HaveLength(OutboxEmail.ErrorMaxLength);
    }

    [Fact]
    public void ForgettingKeepsTheDayTheKindAndTheStatus()
    {
        var email = NewEmail();
        email.Reserve(Now);
        email.MarkSent(Now);

        email.Forget(Now.AddDays(31));

        email.Should().BeEquivalentTo(new
        {
            Kind = "Test",
            Status = OutboxEmailStatus.Sent,
            CreatedAt = Now,
            SentAt = (DateTime?)Now,
            ToAddress = (string?)null,
            ToName = (string?)null,
            Subject = (string?)null,
            HtmlBody = (string?)null,
            TextBody = (string?)null,
        });
    }
    [Fact(DisplayName = "E-mail esperando envio para pessoa anonimizada")]
    public void Withdraw_LeavesAPendingEmailFailed_AndForgotten()
    {
        var email = NewEmail();

        email.Withdraw(Now.AddHours(1));

        email.Status.Should().Be(OutboxEmailStatus.Failed);
        email.FailedAt.Should().Be(Now.AddHours(1));
        email.ForgottenAt.Should().Be(Now.AddHours(1));
        (email.ToAddress, email.ToName, email.Subject, email.HtmlBody, email.TextBody).Should().Be((null, null, null, null, null));
    }

    [Fact(DisplayName = "E-mail enviado para pessoa anonimizada")]
    public void Withdraw_KeepsTheStatusOfASentEmail()
    {
        var email = NewEmail();
        email.Reserve(Now);
        email.MarkSent(Now);

        email.Withdraw(Now.AddHours(1));

        email.Status.Should().Be(OutboxEmailStatus.Sent);
        email.ToAddress.Should().BeNull();
        email.ForgottenAt.Should().Be(Now.AddHours(1));
    }

    [Fact(DisplayName = "Nome de pessoa anonimizada no e-mail de outra")]
    public void Rename_PutsTheIdentifierInTheContent()
    {
        var email = new OutboxEmail(
            "Test", "marina.costa@empresa.example", "Marina Costa", "Carla Mendes mencionou você",
            "<p>Carla Mendes &amp; D&#39;Ávila</p>", "Carla Mendes", Now);

        email.Rename("Carla Mendes", "Pessoa removida nº 1", "Carla Mendes", "Pessoa removida nº 1");

        email.Subject.Should().Be("Pessoa removida nº 1 mencionou você");
        email.HtmlBody.Should().Be("<p>Pessoa removida nº 1 &amp; D&#39;Ávila</p>");
        email.TextBody.Should().Be("Pessoa removida nº 1");
        email.ToAddress.Should().Be("marina.costa@empresa.example");
    }
}
