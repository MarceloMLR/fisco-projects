using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Tests;

// Spec: usuarios-e-papeis, requirements "Último administrador ativo" and "Desativar e reativar".
public sealed class UserAdministrationTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Trocar o papel do último administrador")]
    public async Task ChangingTheRoleOfTheLastAdministrator_IsRefused()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var paulo = await AdministratorAsync(database, "Paulo Nogueira");

        var result = await ChangeRoleAsync(database, paulo, SystemRole.User);

        result.Should().Be(UserChangeResult.LastAdministrator);
        (await database.RoleOfAsync(paulo)).Should().Be(SystemRole.Administrator);
    }

    [Fact(DisplayName = "Desativar o último administrador")]
    public async Task DeactivatingTheLastAdministrator_IsRefused()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var paulo = await AdministratorAsync(database, "Paulo Nogueira");

        var result = await DeactivateAsync(database, paulo);

        result.Should().Be(UserChangeResult.LastAdministrator);
        (await IsActiveAsync(database, paulo)).Should().BeTrue();
    }

    [Fact(DisplayName = "Outro administrador ativo")]
    public async Task WithAnotherActiveAdministrator_TheChangeIsAccepted()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        await AdministratorAsync(database, "Paulo Nogueira");
        var marina = await AdministratorAsync(database, "Marina Costa");

        var result = await ChangeRoleAsync(database, marina, SystemRole.User);

        result.Should().Be(UserChangeResult.Done);
        (await database.RoleOfAsync(marina)).Should().Be(SystemRole.User);
    }

    [Fact(DisplayName = "Inativo não conta como administrador ativo")]
    public async Task InactiveAdministrator_DoesNotKeepTheInstallationAdministered()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var paulo = await AdministratorAsync(database, "Paulo Nogueira");
        var marina = await AdministratorAsync(database, "Marina Costa");
        (await DeactivateAsync(database, marina)).Should().Be(UserChangeResult.Done);

        var result = await DeactivateAsync(database, paulo);

        result.Should().Be(UserChangeResult.LastAdministrator);
    }

    [Fact(DisplayName = "Duas trocas simultâneas entre os dois últimos administradores")]
    public async Task ConcurrentChangesOfTheLastTwoAdministrators_KeepOneOfThem()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        for (var round = 0; round < 5; round++)
        {
            var paulo = await AdministratorAsync(database, $"Paulo Nogueira {round}");
            var marina = await AdministratorAsync(database, $"Marina Costa {round}");
            await KeepOnlyTheseAdministratorsAsync(database, paulo, marina);

            var results = await Task.WhenAll(
                ChangeRoleAsync(database, paulo, SystemRole.User),
                DeactivateAsync(database, marina));

            results.Should().ContainSingle(result => result == UserChangeResult.Done)
                .And.ContainSingle(result => result == UserChangeResult.LastAdministrator);
            var stillAdministrators = await Task.WhenAll(
                IsActiveAdministratorAsync(database, paulo), IsActiveAdministratorAsync(database, marina));
            stillAdministrators.Should().ContainSingle(isAdministrator => isAdministrator);
        }
    }

    [Fact(DisplayName = "Desativar e reativar mantém o papel")]
    public async Task DeactivatingAndReactivating_KeepsTheRole()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        await AdministratorAsync(database, "Paulo Nogueira");
        var marina = await SignInAsync(database, "Marina Costa");
        await ChangeRoleAsync(database, marina, SystemRole.ProjectManager);

        var deactivated = await DeactivateAsync(database, marina);
        var activeAfterDeactivating = await IsActiveAsync(database, marina);
        var reactivated = await database.RunAsync<ReactivateUser, UserChangeResult>(useCase =>
            useCase.ExecuteAsync(marina, Cancellation));

        (deactivated, reactivated).Should().Be((UserChangeResult.Done, UserChangeResult.Done));
        activeAfterDeactivating.Should().BeFalse();
        (await IsActiveAsync(database, marina)).Should().BeTrue();
        (await database.RoleOfAsync(marina)).Should().Be(SystemRole.ProjectManager);
    }

    [Fact(DisplayName = "Usuário inexistente")]
    public async Task UnknownUser_IsNotFound()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var unknown = Guid.CreateVersion7();

        var results = new[]
        {
            await ChangeRoleAsync(database, unknown, SystemRole.User),
            await DeactivateAsync(database, unknown),
            await database.RunAsync<ReactivateUser, UserChangeResult>(useCase => useCase.ExecuteAsync(unknown, Cancellation)),
        };

        results.Should().AllBeEquivalentTo(UserChangeResult.NotFound);
    }

    private static async Task<Guid> SignInAsync(UsersDatabase database, string name)
    {
        var email = $"{name.ToLowerInvariant().Replace(' ', '.')}@empresa.example";
        var result = await database.RunAsync<SignInWithIdentity, SignInResult>(useCase =>
            useCase.ExecuteAsync(new ProviderIdentity("local", email, name, email), Cancellation));
        return ((SignInResult.SignedIn)result).UserId;
    }

    private static async Task<Guid> AdministratorAsync(UsersDatabase database, string name)
    {
        var userId = await SignInAsync(database, name);
        await database.SetRoleAsync(userId, SystemRole.Administrator);
        return userId;
    }

    /// <summary>Earlier rounds leave administrators behind; only the two of this round may count.</summary>
    private static async Task KeepOnlyTheseAdministratorsAsync(UsersDatabase database, params Guid[] keep)
    {
        await using var db = database.NewContext();
        var others = await db.Users
            .Where(user => user.Role == SystemRole.Administrator && !keep.Contains(user.Id))
            .Select(user => user.Id)
            .ToListAsync(Cancellation);
        foreach (var other in others)
        {
            await database.SetRoleAsync(other, SystemRole.User);
        }
    }

    private static Task<UserChangeResult> ChangeRoleAsync(UsersDatabase database, Guid userId, SystemRole role) =>
        database.RunAsync<ChangeSystemRole, UserChangeResult>(useCase => useCase.ExecuteAsync(userId, role, Cancellation));

    private static Task<UserChangeResult> DeactivateAsync(UsersDatabase database, Guid userId) =>
        database.RunAsync<DeactivateUser, UserChangeResult>(useCase => useCase.ExecuteAsync(userId, Cancellation));

    private static Task<bool> IsActiveAsync(UsersDatabase database, Guid userId) =>
        database.RunAsync<IUserAccess, bool>(async access => (await access.FindAsync(userId, Cancellation))!.IsActive);

    private static Task<bool> IsActiveAdministratorAsync(UsersDatabase database, Guid userId) =>
        database.RunAsync<IUserAccess, bool>(async access =>
            (await access.FindAsync(userId, Cancellation)) is { IsActive: true, Role: SystemRole.Administrator });
}
