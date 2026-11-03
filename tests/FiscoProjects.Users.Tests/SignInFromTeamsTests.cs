using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Tests;

// Spec: entrada-e-sessao, requirement "Entrada pelo Teams" (ADR-0007, ADR-0008, ADR-0152): the person of a validated token
// of Teams, by the link "entra", without creating nor writing anything.
public sealed class SignInFromTeamsTests(SqlServerFixture sqlServer)
{
    private const string MarinaObjectId = "0B5F7C1E-2D3A-4F6B-8C9D-0E1F2A3B4C5D";

    private static readonly EntraIdentity Marina =
        new(MarinaObjectId, "Marina Costa", "marina.costa@empresa.example", "marina.costa@empresa.example", "0", null);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Pessoa do vínculo pelo Teams")]
    public async Task ThePersonOfTheLink_IsRecognized_WithoutAnySignIn()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var signedIn = (EntraSignInResult.SignedIn)await database.RunAsync<SignInWithEntra, EntraSignInResult>(
            useCase => useCase.ExecuteAsync(Marina, Cancellation));
        database.SecurityEvents.Events.Clear();

        var result = await RecognizeAsync(database, Marina with { ObjectId = MarinaObjectId.ToLowerInvariant(), Name = "Outro nome" });

        result.Should().Be(new TeamsSignInResult.Recognized(signedIn.UserId));
        database.SecurityEvents.Events.Should().BeEmpty();
        await using var db = database.NewContext();
        (await db.Users.SingleAsync(Cancellation)).Name.Should().Be("Marina Costa");
    }

    [Fact(DisplayName = "Convidado pelo Teams")]
    public async Task AGuest_IsRefused_WithoutAnything()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        var byStatus = await RecognizeAsync(database, Marina with { AccountStatus = "1" });
        var byProvider = await RecognizeAsync(database, Marina with { AccountStatus = null, IdentityProvider = "live.com" });

        byStatus.Should().Be(new TeamsSignInResult.Refused(TeamsRefusal.Guest));
        byProvider.Should().Be(new TeamsSignInResult.Refused(TeamsRefusal.Guest));
        database.SecurityEvents.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sem vínculo pelo Teams")]
    public async Task APersonWithoutTheLink_IsUnknown_AndNoUserIsCreated()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        var unknown = await RecognizeAsync(database, Marina);
        var withoutObject = await RecognizeAsync(database, Marina with { ObjectId = " " });

        unknown.Should().Be(new TeamsSignInResult.Refused(TeamsRefusal.Unknown));
        withoutObject.Should().Be(new TeamsSignInResult.Refused(TeamsRefusal.Unrecognized));
        await using var db = database.NewContext();
        (await db.Users.CountAsync(Cancellation)).Should().Be(0);
    }

    [Fact(DisplayName = "Desativado pelo Teams")]
    public async Task ADeactivatedPerson_IsRefused()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var signedIn = (EntraSignInResult.SignedIn)await database.RunAsync<SignInWithEntra, EntraSignInResult>(
            useCase => useCase.ExecuteAsync(Marina, Cancellation));
        await database.DeactivateAsync(signedIn.UserId);

        (await RecognizeAsync(database, Marina)).Should().Be(new TeamsSignInResult.Refused(TeamsRefusal.Inactive));
    }

    private static Task<TeamsSignInResult> RecognizeAsync(UsersDatabase database, EntraIdentity identity) =>
        database.RunAsync<SignInFromTeams, TeamsSignInResult>(useCase => useCase.ExecuteAsync(identity, Cancellation));
}
