using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Tests;

// Spec: entrada-e-sessao, requirement "Usuário no primeiro login".
public sealed class SignInWithIdentityTests(SqlServerFixture sqlServer)
{
    private static readonly ProviderIdentity Marina =
        new("local", "marina.costa@empresa.example", "Marina Costa", "marina.costa@empresa.example");

    [Fact(DisplayName = "Primeira entrada")]
    public async Task FirstSignIn_CreatesTheUserAndTheProviderLink()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        var result = await SignInAsync(database, Marina);

        var userId = result.Should().BeOfType<SignInResult.SignedIn>().Which.UserId;
        await using var db = database.NewContext();
        var user = await db.Users.SingleAsync(TestContext.Current.CancellationToken);
        user.Id.Should().Be(userId);
        user.Name.Should().Be("Marina Costa");
        user.Email.Should().Be("marina.costa@empresa.example");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().Be(database.Clock.GetUtcNow().UtcDateTime);
        user.LastSignInAt.Should().Be(user.CreatedAt);
        var link = await db.IdentityLinks.SingleAsync(TestContext.Current.CancellationToken);
        (link.UserId, link.Provider, link.ExternalId).Should().Be((userId, "local", "marina.costa@empresa.example"));
    }

    [Fact(DisplayName = "Entradas seguintes")]
    public async Task LaterSignIn_KeepsTheSameUserAndRefreshesTheName()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var first = await SignInAsync(database, Marina);
        database.Clock.Advance(TimeSpan.FromDays(2));

        var later = await SignInAsync(database, Marina with { Name = "Marina Costa Alves" });

        later.Should().Be(first);
        await using var db = database.NewContext();
        var user = await db.Users.SingleAsync(TestContext.Current.CancellationToken);
        user.Name.Should().Be("Marina Costa Alves");
        user.LastSignInAt.Should().Be(database.Clock.GetUtcNow().UtcDateTime);
        user.CreatedAt.Should().Be(user.LastSignInAt.AddDays(-2));
        (await db.IdentityLinks.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact(DisplayName = "Outra identidade cria outro usuário")]
    public async Task AnotherIdentity_CreatesAnotherUser()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var marina = await SignInAsync(database, Marina);

        var rafael = await SignInAsync(database,
            new ProviderIdentity("local", "rafael.souza@empresa.example", "Rafael Souza", "rafael.souza@empresa.example"));

        rafael.Should().NotBe(marina);
        await using var db = database.NewContext();
        (await db.Users.CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }

    [Fact(DisplayName = "Usuário inativo não entra")]
    public async Task InactiveUser_IsRefused()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var first = (SignInResult.SignedIn)await SignInAsync(database, Marina);
        await database.DeactivateAsync(first.UserId);

        var result = await SignInAsync(database, Marina);

        result.Should().BeOfType<SignInResult.Inactive>();
    }

    [Fact(DisplayName = "Acesso do usuário ativo e inativo")]
    public async Task UserAccess_FollowsTheActiveFlag()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var userId = ((SignInResult.SignedIn)await SignInAsync(database, Marina)).UserId;

        var beforeDeactivating = await IsActiveAsync(database, userId);
        await database.DeactivateAsync(userId);
        var afterDeactivating = await IsActiveAsync(database, userId);

        beforeDeactivating.Should().BeTrue();
        afterDeactivating.Should().BeFalse();
        (await IsActiveAsync(database, Guid.CreateVersion7())).Should().BeFalse();
        (await database.RunAsync<GetSignedInUser, SignedInUser?>(useCase =>
            useCase.ExecuteAsync(userId, TestContext.Current.CancellationToken))).Should().BeNull();
    }

    [Fact(DisplayName = "Pessoa da sessão")]
    public async Task SignedInUser_HasNameAndEmail()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var userId = ((SignInResult.SignedIn)await SignInAsync(database, Marina)).UserId;

        var user = await database.RunAsync<GetSignedInUser, SignedInUser?>(useCase =>
            useCase.ExecuteAsync(userId, TestContext.Current.CancellationToken));

        user.Should().Be(new SignedInUser(userId, "Marina Costa", "marina.costa@empresa.example"));
    }

    private static Task<SignInResult> SignInAsync(UsersDatabase database, ProviderIdentity identity) =>
        database.RunAsync<SignInWithIdentity, SignInResult>(useCase =>
            useCase.ExecuteAsync(identity, TestContext.Current.CancellationToken));

    private static Task<bool> IsActiveAsync(UsersDatabase database, Guid userId) =>
        database.RunAsync<IUserAccess, bool>(access => access.IsActiveAsync(userId, TestContext.Current.CancellationToken));
}
