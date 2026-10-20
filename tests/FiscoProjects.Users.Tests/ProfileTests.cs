using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;

namespace FiscoProjects.Users.Tests;

// Spec: perfil, requirements "Tema no perfil" and "Tela Meu perfil" (use cases).
public sealed class ProfileTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Tema guardado no perfil")]
    public async Task ChosenTheme_IsInTheProfileAndTheSession()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var marina = await SignInAsync(database);

        var chosen = await database.RunAsync<ChooseTheme, bool>(useCase =>
            useCase.ExecuteAsync(marina, ThemePreference.Dark, Cancellation));
        var profile = await database.RunAsync<GetProfile, Profile?>(useCase => useCase.ExecuteAsync(marina, Cancellation));
        var session = await database.RunAsync<GetSignedInUser, SignedInUser?>(useCase =>
            useCase.ExecuteAsync(marina, Cancellation));

        chosen.Should().BeTrue();
        profile.Should().BeEquivalentTo(new Profile(
            marina, "Marina Costa", "marina.costa@empresa.example", SystemRole.User, ThemePreference.Dark,
            new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc), ["local"], ReceivesDailySummary: true));
        session!.Theme.Should().Be(ThemePreference.Dark);
    }

    [Fact(DisplayName = "Perfil de quem não pode mais usar o sistema")]
    public async Task InactivePerson_HasNoProfile_AndCannotChooseATheme()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);
        var marina = await SignInAsync(database);
        await database.DeactivateAsync(marina);

        var chosen = await database.RunAsync<ChooseTheme, bool>(useCase =>
            useCase.ExecuteAsync(marina, ThemePreference.Dark, Cancellation));
        var profile = await database.RunAsync<GetProfile, Profile?>(useCase => useCase.ExecuteAsync(marina, Cancellation));
        var unknown = await database.RunAsync<GetProfile, Profile?>(useCase =>
            useCase.ExecuteAsync(Guid.CreateVersion7(), Cancellation));

        chosen.Should().BeFalse();
        profile.Should().BeNull();
        unknown.Should().BeNull();
    }

    private static async Task<Guid> SignInAsync(UsersDatabase database)
    {
        var result = await database.RunAsync<SignInWithIdentity, SignInResult>(useCase => useCase.ExecuteAsync(
            new ProviderIdentity("local", "marina.costa@empresa.example", "Marina Costa", "marina.costa@empresa.example"),
            Cancellation));
        return ((SignInResult.SignedIn)result).UserId;
    }
}
