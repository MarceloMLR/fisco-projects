using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Users.Tests;

// Spec: usuarios-e-papeis, requirements "Papel no sistema" and "Primeiro administrador".
public sealed class SystemRoleTests(SqlServerFixture sqlServer)
{
    private static readonly ProviderIdentity Paulo =
        new("local", "paulo.nogueira@empresa.example", "Paulo Nogueira", "paulo.nogueira@empresa.example");

    private static readonly ProviderIdentity Marina =
        new("local", "marina.costa@empresa.example", "Marina Costa", "marina.costa@empresa.example");

    [Fact(DisplayName = "Papel na primeira entrada")]
    public async Task FirstSignIn_GivesTheUserRole()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, firstAdministrator: Paulo);

        var marina = await SignInAsync(database, Marina);

        (await database.RoleOfAsync(marina)).Should().Be(SystemRole.User);
    }

    [Fact(DisplayName = "Primeiro administrador configurado")]
    public async Task ConfiguredIdentity_BecomesAdministratorOnTheFirstSignIn()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, firstAdministrator: Paulo);

        var paulo = await SignInAsync(database, Paulo with { ExternalId = "PAULO.NOGUEIRA@empresa.example" });

        (await database.RoleOfAsync(paulo)).Should().Be(SystemRole.Administrator);
    }

    [Fact(DisplayName = "Primeira entrada da pessoa configurada com outro administrador ativo")]
    public async Task ConfiguredIdentity_IsAdministratorOnTheFirstSignInEvenWithAnotherOne()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, firstAdministrator: Paulo);
        var marina = await SignInAsync(database, Marina);
        await database.SetRoleAsync(marina, SystemRole.Administrator);

        var paulo = await SignInAsync(database, Paulo);

        (await database.RoleOfAsync(paulo)).Should().Be(SystemRole.Administrator);
    }

    [Fact(DisplayName = "Configuração depois da primeira entrada")]
    public async Task ConfiguredLater_BecomesAdministratorWhileThereIsNone()
    {
        await using var before = await UsersDatabase.CreateAsync(sqlServer);
        var paulo = await SignInAsync(before, Paulo);
        (await before.RoleOfAsync(paulo)).Should().Be(SystemRole.User);
        await using var after = await before.WithFirstAdministratorAsync(Paulo);

        var again = await SignInAsync(after, Paulo);

        again.Should().Be(paulo);
        (await after.RoleOfAsync(paulo)).Should().Be(SystemRole.Administrator);
    }

    [Fact(DisplayName = "Troca feita na tela respeitada")]
    public async Task RoleChosenOnTheScreen_IsKeptWhileAnotherAdministratorIsActive()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, firstAdministrator: Paulo);
        var paulo = await SignInAsync(database, Paulo);
        var marina = await SignInAsync(database, Marina);
        await database.SetRoleAsync(marina, SystemRole.Administrator);
        await database.SetRoleAsync(paulo, SystemRole.ProjectManager);

        await SignInAsync(database, Paulo);

        (await database.RoleOfAsync(paulo)).Should().Be(SystemRole.ProjectManager);
    }

    [Fact(DisplayName = "Acesso traz o papel")]
    public async Task UserAccess_HasTheRole()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, firstAdministrator: Paulo);
        var paulo = await SignInAsync(database, Paulo);

        var access = await database.RunAsync<IUserAccess, UserAccess?>(userAccess =>
            userAccess.FindAsync(paulo, TestContext.Current.CancellationToken));

        access.Should().Be(new UserAccess(paulo, IsActive: true, SystemRole.Administrator));
    }

    private static async Task<Guid> SignInAsync(UsersDatabase database, ProviderIdentity identity)
    {
        var result = await database.RunAsync<SignInWithIdentity, SignInResult>(useCase =>
            useCase.ExecuteAsync(identity, TestContext.Current.CancellationToken));
        return result.Should().BeOfType<SignInResult.SignedIn>().Which.UserId;
    }
}
