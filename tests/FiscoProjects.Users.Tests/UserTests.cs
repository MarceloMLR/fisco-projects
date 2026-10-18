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
}
