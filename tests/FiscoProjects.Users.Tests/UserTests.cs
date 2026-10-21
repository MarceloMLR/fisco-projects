using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;

namespace FiscoProjects.Users.Tests;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact(DisplayName = "Usuário novo ativo, sem espaços sobrando")]
    public void Create_TrimsAndStartsActive()
    {
        var user = User.Create("  Marina Costa ", " marina.costa@empresa.example ", Now);

        user.Name.Should().Be("Marina Costa");
        user.Email.Should().Be("marina.costa@empresa.example");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().Be(Now);
        user.LastSignInAt.Should().Be(Now);
        user.Id.Version.Should().Be(7);
    }

    [Fact(DisplayName = "Entrada atualiza nome, e-mail e última entrada")]
    public void RecordSignIn_RefreshesNameEmailAndLastSignIn()
    {
        var user = User.Create("Marina Costa", "marina.costa@empresa.example", Now);

        user.RecordSignIn("Marina Alves", "marina.alves@empresa.example", Now.AddHours(1));

        user.Name.Should().Be("Marina Alves");
        user.Email.Should().Be("marina.alves@empresa.example");
        user.LastSignInAt.Should().Be(Now.AddHours(1));
        user.CreatedAt.Should().Be(Now);
    }

    [Fact(DisplayName = "Usuário novo com o tema automático")]
    public void Create_FollowsTheOperatingSystemTheme()
    {
        var user = User.Create("Marina Costa", "marina.costa@empresa.example", Now);

        user.Theme.Should().Be(ThemePreference.Automatic);
    }

    [Fact(DisplayName = "Escolher o tema no domínio")]
    public void ChooseTheme_KeepsTheTheme_AndRefusesAnUnknownOne()
    {
        var user = User.Create("Marina Costa", "marina.costa@empresa.example", Now);

        user.ChooseTheme(ThemePreference.Dark);
        var unknown = () => user.ChooseTheme((ThemePreference)7);

        user.Theme.Should().Be(ThemePreference.Dark);
        unknown.Should().Throw<ArgumentOutOfRangeException>();
        user.Theme.Should().Be(ThemePreference.Dark);
    }

    [Fact(DisplayName = "Usuário novo com o papel Usuário")]
    public void Create_StartsWithTheUserRole()
    {
        var user = User.Create("Marina Costa", "marina.costa@empresa.example", Now);

        user.Role.Should().Be(SystemRole.User);
        user.IsActiveAdministrator.Should().BeFalse();
    }

    [Fact(DisplayName = "Papel desconhecido recusado")]
    public void ChangeRole_RefusesAnUnknownRole()
    {
        var user = User.Create("Marina Costa", "marina.costa@empresa.example", Now);

        var change = () => user.ChangeRole((SystemRole)99);

        change.Should().Throw<ArgumentOutOfRangeException>();
        user.Role.Should().Be(SystemRole.User);
    }

    [Fact(DisplayName = "Administrador ativo só enquanto ativo")]
    public void ActiveAdministrator_FollowsRoleAndActivity()
    {
        var user = User.Create("Paulo Nogueira", "paulo.nogueira@empresa.example", Now);
        user.ChangeRole(SystemRole.Administrator);

        var beforeDeactivating = user.IsActiveAdministrator;
        user.Deactivate();
        var afterDeactivating = user.IsActiveAdministrator;
        user.Reactivate();

        (beforeDeactivating, afterDeactivating, user.IsActiveAdministrator).Should().Be((true, false, true));
        user.Role.Should().Be(SystemRole.Administrator);
    }
    [Fact(DisplayName = "Anonimizar pessoa desativada")]
    public void Anonymize_TakesTheNameAndTheEmailAway()
    {
        var user = User.Create("Carla Mendes", "carla.mendes@empresa.example", Now);
        user.ChangeRole(SystemRole.ProjectManager);
        user.Deactivate();

        user.Anonymize("Pessoa removida nº 3", 3, Now.AddDays(1));

        (user.Name, user.Email, user.Role).Should().Be(("Pessoa removida nº 3", string.Empty, SystemRole.User));
        (user.AnonymousNumber, user.AnonymizedAt, user.IsAnonymized).Should().Be((3, Now.AddDays(1), true));
        user.ReceivesDailySummary.Should().BeFalse();
        user.IsActive.Should().BeFalse();
    }

    [Fact(DisplayName = "Pessoa ativa não é anonimizada")]
    public void Anonymize_RefusesAnActivePerson()
    {
        var user = User.Create("Carla Mendes", "carla.mendes@empresa.example", Now);

        user.Invoking(u => u.Anonymize("Pessoa removida nº 1", 1, Now)).Should().Throw<InvalidOperationException>();
        user.Name.Should().Be("Carla Mendes");
    }

    [Fact(DisplayName = "Anonimização definitiva")]
    public void AnAnonymizedPerson_IsNotReactivatedNorAnonymizedAgain()
    {
        var user = User.Create("Carla Mendes", "carla.mendes@empresa.example", Now);
        user.Deactivate();
        user.Anonymize("Pessoa removida nº 1", 1, Now);

        user.Invoking(u => u.Reactivate()).Should().Throw<InvalidOperationException>();
        user.Invoking(u => u.Anonymize("Pessoa removida nº 2", 2, Now)).Should().Throw<InvalidOperationException>();
        user.IsActive.Should().BeFalse();
    }
    [Theory(DisplayName = "Identificador da anonimização")]
    [InlineData("Pessoa removida nº {0}", true)]
    [InlineData("Pessoa removida", false)]
    [InlineData("Pessoa {1} removida nº {0}", false)]
    [InlineData(" ", false)]
    public void TheIdentifier_HasTheNumber(string anonymousName, bool valid)
    {
        var options = new FiscoProjects.Users.Application.AnonymizationOptions { AnonymousName = anonymousName };

        options.IsValid.Should().Be(valid);
        if (valid)
        {
            options.IdentifierFor(12).Should().Be("Pessoa removida nº 12");
        }
    }
}
