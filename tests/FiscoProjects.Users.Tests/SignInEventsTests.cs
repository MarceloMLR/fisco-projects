using FiscoProjects.Testing;
using FiscoProjects.Users.Application;

namespace FiscoProjects.Users.Tests;

// Spec: auditoria, requirement "Eventos de segurança" (what the Users module reports to the audit).
public sealed class SignInEventsTests(SqlServerFixture sqlServer)
{
    private static readonly ProviderIdentity Marina =
        new("local", "marina.costa@empresa.example", "Marina Costa", "marina.costa@empresa.example");

    [Fact(DisplayName = "Entrada informada à auditoria")]
    public async Task SignIn_IsReportedWithThePersonAndTheProvider()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        var first = (SignInResult.SignedIn)await SignInAsync(database, Marina);
        await SignInAsync(database, Marina);

        database.SecurityEvents.Events.Should().Equal(
            $"SignedIn {first.UserId} local",
            $"SignedIn {first.UserId} local");
    }

    [Fact(DisplayName = "Recusa da conta desativada informada à auditoria")]
    public async Task DeactivatedUser_IsReportedAsRefused()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var first = (SignInResult.SignedIn)await SignInAsync(database, Marina);
        await database.DeactivateAsync(first.UserId);

        await SignInAsync(database, Marina);

        database.SecurityEvents.Events.Should().Equal(
            $"SignedIn {first.UserId} local",
            "SignInRefused marina.costa@empresa.example Inactive");
    }

    private static Task<SignInResult> SignInAsync(UsersDatabase database, ProviderIdentity identity) =>
        database.RunAsync<SignInWithIdentity, SignInResult>(useCase =>
            useCase.ExecuteAsync(identity, TestContext.Current.CancellationToken));
}
