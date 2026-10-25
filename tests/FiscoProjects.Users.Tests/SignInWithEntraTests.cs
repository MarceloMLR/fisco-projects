using System.Security.Claims;
using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FiscoProjects.Users.Tests;

// Spec: entrada-e-sessao, requirements "Entrada pelo Entra ID" and "Recusas na entrada pelo Entra" (ADR-0007, ADR-0117):
// the person of a validated ID token.
public sealed class SignInWithEntraTests(SqlServerFixture sqlServer)
{
    private const string MarinaObjectId = "0B5F7C1E-2D3A-4F6B-8C9D-0E1F2A3B4C5D";

    private static readonly EntraIdentity Marina =
        new(MarinaObjectId, "Marina Costa", "marina.costa@empresa.example", "marina.costa@empresa.example", "0", null);

    [Fact(DisplayName = "Pessoa da empresa pelo Entra")]
    public async Task Member_IsSignedInWithTheEntraLinkByTheObjectId()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        var result = (EntraSignInResult.SignedIn)await SignInAsync(database, Marina);
        var again = (EntraSignInResult.SignedIn)await SignInAsync(database, Marina with { Name = "Marina C. Costa" });

        again.UserId.Should().Be(result.UserId);
        database.SecurityEvents.Events.Should().Equal($"SignedIn {result.UserId} entra", $"SignedIn {result.UserId} entra");
        await using var db = database.NewContext();
        (await db.IdentityLinks.CountAsync(link => link.Provider == "entra" && link.ExternalId == MarinaObjectId.ToLowerInvariant(), Cancellation))
            .Should().Be(1);
    }

    [Fact(DisplayName = "Pessoa sem caixa de correio pelo Entra")]
    public async Task WithoutEmail_TheSignInNameIsTheEmail()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        var result = (EntraSignInResult.SignedIn)await SignInAsync(
            database, Marina with { Email = null, PreferredUsername = "marina.costa@empresa.example", Name = null });

        await using var db = database.NewContext();
        var user = await db.Users.SingleAsync(user => user.Id == result.UserId, Cancellation);
        user.Email.Should().Be("marina.costa@empresa.example");
        user.Name.Should().Be("marina.costa@empresa.example");
    }

    [Fact(DisplayName = "Convidado recusado pelo Entra")]
    public async Task Guest_IsRefusedBeforeAnythingIsCreated()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        var byStatus = await SignInAsync(database, Marina with { AccountStatus = "1" });
        var byProvider = await SignInAsync(database, Marina with { AccountStatus = null, IdentityProvider = "live.com" });

        byStatus.Should().BeOfType<EntraSignInResult.Guest>();
        byProvider.Should().BeOfType<EntraSignInResult.Guest>();
        await using var db = database.NewContext();
        (await db.Users.CountAsync(Cancellation)).Should().Be(0);
        database.SecurityEvents.Events.Should().Equal(
            "SignInRefused marina.costa@empresa.example Guest", "SignInRefused marina.costa@empresa.example Guest");
    }

    [Fact(DisplayName = "Desativado recusado pelo Entra")]
    public async Task DeactivatedUser_IsRefused()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var first = (EntraSignInResult.SignedIn)await SignInAsync(database, Marina);
        await database.DeactivateAsync(first.UserId);

        (await SignInAsync(database, Marina)).Should().BeOfType<EntraSignInResult.Inactive>();
    }

    [Fact(DisplayName = "Token sem o identificador do objeto")]
    public async Task WithoutObjectId_ThePersonIsNotRecognized()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        (await SignInAsync(database, Marina with { ObjectId = " " })).Should().BeOfType<EntraSignInResult.Unrecognized>();
        await using var db = database.NewContext();
        (await db.Users.CountAsync(Cancellation)).Should().Be(0);
    }

    [Fact(DisplayName = "Declarações do token do Entra")]
    public void Identity_ComesFromTheClaimsByTheirNamesInTheToken()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("oid", MarinaObjectId),
            new Claim("name", "Marina Costa"),
            new Claim("preferred_username", "marina.costa@empresa.example"),
            new Claim("acct", "0"),
        ], "entra"));

        var identity = EntraIdentity.From(principal);

        identity.Should().Be(new EntraIdentity(MarinaObjectId, "Marina Costa", null, "marina.costa@empresa.example", "0", null));
        identity.IsGuest.Should().BeFalse();
    }

    [Fact(DisplayName = "Entrada pelo Entra conferida ao subir")]
    public void Validation_RefusesTheSignInHalfConfigured()
    {
        var validation = new EntraSignInValidation();

        validation.Validate(null, new EntraSignInOptions()).Succeeded.Should().BeTrue("it is off");
        var failed = validation.Validate(null, new EntraSignInOptions { Enabled = true, Instance = "http://login.example/" });
        failed.Failed.Should().BeTrue();
        failed.Failures.Should().HaveCount(3);
        validation.Validate(null, new EntraSignInOptions
        {
            Enabled = true,
            TenantId = "8c0a9f3e-1b2d-4e5f-9a6b-7c8d9e0f1a2b",
            ClientId = "4d3c2b1a-0f9e-4d8c-b7a6-5e4d3c2b1a0f",
        }).Should().Be(ValidateOptionsResult.Success);
    }

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private static Task<EntraSignInResult> SignInAsync(UsersDatabase database, EntraIdentity identity) =>
        database.RunAsync<SignInWithEntra, EntraSignInResult>(useCase => useCase.ExecuteAsync(identity, Cancellation));
}
