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
}
