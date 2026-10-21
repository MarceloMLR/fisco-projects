using FiscoProjects.Testing;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Tests;

// Spec: usuarios-e-papeis, requirements "Pessoas da empresa para escolher" and "Usuário criado pelo diretório";
// entrada-e-sessao, requirement "Usuário no primeiro login".
public sealed class PeopleDirectoryTests(SqlServerFixture sqlServer)
{
    private static readonly LocalPerson[] Directory =
    [
        new() { Name = "Marina Costa", Email = "marina.costa@empresa.example" },
        new() { Name = "Carla Mendes", Email = "Carla.Mendes@empresa.example" },
        new() { Name = "Lívia Prado", Email = "livia.prado@empresa.example" },
        new() { Name = "Diego Torres", Email = "diego.torres@empresa.example" },
    ];

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Pessoas do sistema e do diretório")]
    public async Task People_ComeFromTheUsersAndTheDirectory()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);
        var marina = await SignInAsync(database, "Marina Costa", "marina.costa@empresa.example");
        await SignInAsync(database, "Paulo Nogueira", "paulo.nogueira@empresa.example", SystemRole.Administrator);

        var people = await ListAsync(database, new PeopleQuery(null, ManagersOnly: false, Limit: 20));

        people.Should().Equal(
            new PersonItem(null, "local:carla.mendes@empresa.example", "Carla Mendes", "Carla.Mendes@empresa.example", false),
            new PersonItem(null, "local:diego.torres@empresa.example", "Diego Torres", "diego.torres@empresa.example", false),
            new PersonItem(null, "local:livia.prado@empresa.example", "Lívia Prado", "livia.prado@empresa.example", false),
            new PersonItem(marina, null, "Marina Costa", "marina.costa@empresa.example", false),
            people[^1]);
        people[^1].Should().Match<PersonItem>(person => person.Name == "Paulo Nogueira" && person.Id != null && person.CanBeManager);
    }

    [Fact(DisplayName = "Busca sem acentos no diretório, até o limite")]
    public async Task People_AreSearchedWithoutAccents_UpToTheLimit()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);

        var livia = await ListAsync(database, new PeopleQuery("LIVIA", ManagersOnly: false, Limit: 20));
        var two = await ListAsync(database, new PeopleQuery(null, ManagersOnly: false, Limit: 2));

        livia.Select(person => person.Name).Should().Equal("Lívia Prado");
        two.Select(person => person.Name).Should().Equal("Carla Mendes", "Diego Torres");
    }

    [Fact(DisplayName = "Pessoa do diretório que já entrou")]
    public async Task DirectoryPersonWhoSignedIn_ComesOnce_AsAUser()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);
        var marina = await SignInAsync(database, "Marina Costa", "marina.costa@empresa.example");

        var people = await ListAsync(database, new PeopleQuery("marina", ManagersOnly: false, Limit: 20));

        people.Should().Equal(new PersonItem(marina, null, "Marina Costa", "marina.costa@empresa.example", false));
    }

    [Fact(DisplayName = "Pessoa do diretório com o usuário inativo")]
    public async Task DirectoryPersonWithAnInactiveUser_IsLeftOut()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);
        var diego = await SignInAsync(database, "Diego Torres", "diego.torres@empresa.example");
        await database.DeactivateAsync(diego);

        var people = await ListAsync(database, new PeopleQuery("diego", ManagersOnly: false, Limit: 20));

        people.Should().BeEmpty();
    }

    [Fact(DisplayName = "Só gerentes")]
    public async Task ManagersOnly_LeaveTheDirectoryOut()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);
        await SignInAsync(database, "Marina Costa", "marina.costa@empresa.example", SystemRole.ProjectManager);

        var people = await ListAsync(database, new PeopleQuery(null, ManagersOnly: true, Limit: 20));

        people.Select(person => (person.Name, person.CanBeManager)).Should().Equal(("Marina Costa", true));
    }

    [Fact(DisplayName = "Só líderes técnicos")]
    public async Task TechLeadsOnly_LeaveTheDirectoryOut()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);
        await SignInAsync(database, "Marina Costa", "marina.costa@empresa.example", SystemRole.ProjectManager);
        await SignInAsync(database, "Rafael Souza", "rafael.souza@empresa.example", SystemRole.TechLead);

        var people = await ListAsync(database, new PeopleQuery(null, ManagersOnly: false, Limit: 20, TechLeadsOnly: true));

        people.Select(person => (person.Name, person.CanBeManager, person.CanBeTechLead))
            .Should().Equal(("Rafael Souza", false, true));
    }

    [Fact(DisplayName = "Diretório vazio sem a entrada local")]
    public async Task WithoutTheLocalSignIn_TheDirectoryHasNobody()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer);

        var people = await ListAsync(database, new PeopleQuery(null, ManagersOnly: false, Limit: 20));
        var carla = await FromDirectoryAsync(database, "local:carla.mendes@empresa.example");

        people.Should().BeEmpty();
        carla.Should().BeNull();
    }

    [Fact(DisplayName = "Usuário criado ao incluir")]
    public async Task UserFromDirectory_IsCreatedActive_AsUser_WithoutSignIn()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);

        var carla = await FromDirectoryAsync(database, "local:carla.mendes@empresa.example");

        await using var db = database.NewContext();
        var user = await db.Users.SingleAsync(Cancellation);
        user.Id.Should().Be(carla!.Value);
        (user.Name, user.Email, user.Role, user.IsActive, user.LastSignInAt)
            .Should().Be(("Carla Mendes", "Carla.Mendes@empresa.example", SystemRole.User, true, (DateTime?)null));
        var link = await db.IdentityLinks.SingleAsync(Cancellation);
        (link.UserId, link.Provider, link.ExternalId).Should().Be((carla.Value, "local", "carla.mendes@empresa.example"));
    }

    [Fact(DisplayName = "Pessoa do diretório incluída de novo")]
    public async Task UserFromDirectory_AgainGivesTheSameUser()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);

        var first = await FromDirectoryAsync(database, "local:carla.mendes@empresa.example");
        var again = await FromDirectoryAsync(database, "local:carla.mendes@empresa.example");

        again.Should().Be(first);
        await using var db = database.NewContext();
        (await db.Users.CountAsync(Cancellation)).Should().Be(1);
    }

    [Fact(DisplayName = "Pessoa fora do diretório ou inativa")]
    public async Task UserFromDirectory_RefusesWhoIsNotInTheDirectory_OrIsInactive()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);
        var diego = await SignInAsync(database, "Diego Torres", "diego.torres@empresa.example");
        await database.DeactivateAsync(diego);

        (await FromDirectoryAsync(database, "local:joao.silva@empresa.example")).Should().BeNull();
        (await FromDirectoryAsync(database, "entra:carla.mendes@empresa.example")).Should().BeNull();
        (await FromDirectoryAsync(database, "carla.mendes@empresa.example")).Should().BeNull();
        (await FromDirectoryAsync(database, "local:diego.torres@empresa.example")).Should().BeNull();
    }

    [Fact(DisplayName = "Primeira entrada de quem foi incluído pelo diretório")]
    public async Task FirstSignIn_OfAPersonFromTheDirectory_UsesTheSameUser()
    {
        await using var database = await UsersDatabase.CreateAsync(sqlServer, directory: Directory);
        var carla = await FromDirectoryAsync(database, "local:carla.mendes@empresa.example");
        database.Clock.Advance(TimeSpan.FromHours(3));

        var signedIn = await database.RunAsync<SignInLocally, LocalSignInResult>(
            signIn => signIn.ExecuteAsync("carla.mendes@empresa.example", Cancellation));

        signedIn.Should().Be(new LocalSignInResult.SignedIn(carla!.Value));
        await using var db = database.NewContext();
        var user = await db.Users.SingleAsync(Cancellation);
        user.LastSignInAt.Should().Be(database.Clock.GetUtcNow().UtcDateTime);
    }

    private static Task<IReadOnlyList<PersonItem>> ListAsync(UsersDatabase database, PeopleQuery query) =>
        database.RunAsync<ListPeople, IReadOnlyList<PersonItem>>(people => people.ExecuteAsync(query, Cancellation));

    private static Task<Guid?> FromDirectoryAsync(UsersDatabase database, string directoryId) =>
        database.RunAsync<IUserProvisioning, Guid?>(provisioning => provisioning.UserFromDirectoryAsync(directoryId, Cancellation));

    private static async Task<Guid> SignInAsync(
        UsersDatabase database, string name, string email, SystemRole role = SystemRole.User)
    {
        var result = await database.RunAsync<SignInWithIdentity, SignInResult>(signIn => signIn.ExecuteAsync(
            new ProviderIdentity("local", email.ToLowerInvariant(), name, email), Cancellation));
        var userId = ((SignInResult.SignedIn)result).UserId;
        if (role != SystemRole.User)
        {
            await database.SetRoleAsync(userId, role);
        }

        return userId;
    }
}
