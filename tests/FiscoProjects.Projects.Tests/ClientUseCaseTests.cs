using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using FiscoProjects.Testing;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Tests;

// Spec: clientes (module side): "Dados do cliente", "CNPJ do cliente", "Logo do cliente", "Situação e exclusão do
// cliente" and the search of "Tela Clientes".
public sealed class ClientUseCaseTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Incluir cliente")]
    public async Task Client_IsIncluded_Active()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);

        var added = await AddAsync(database, " Alfa Distribuidora ", "11.222.333/0001-81", " Matriz em Campinas ");

        added.Outcome.Should().Be(ClientSaveOutcome.Saved);
        (await ListAsync(database)).Items.Should().ContainSingle().Which.Should().Be(new ClientListItem(
            added.Id!.Value, "Alfa Distribuidora", "11222333000181", "Matriz em Campinas", true, null));
    }

    [Fact(DisplayName = "Cliente só com o nome")]
    public async Task Client_WithOnlyAName_IsIncluded()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);

        var added = await AddAsync(database, "Kappa Energia", "", null);

        added.Outcome.Should().Be(ClientSaveOutcome.Saved);
        (await ListAsync(database)).Items.Single().Cnpj.Should().BeNull();
    }

    [Fact(DisplayName = "Cliente inválido")]
    public async Task InvalidClient_IsNotIncluded()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);

        var empty = await AddAsync(database, " ", "11.222.333/0001-80", new string('a', 501));
        var tooLong = await AddAsync(database, new string('a', 151), null, null);

        empty.Should().BeEquivalentTo(ClientSaveResult.InvalidFields(
            [ClientField.Name, ClientField.Cnpj, ClientField.Observation]));
        tooLong.Invalid.Should().Equal(ClientField.Name);
        (await ListAsync(database)).Total.Should().Be(0);
    }

    [Fact(DisplayName = "CNPJ repetido")]
    public async Task RepeatedCnpj_IsRefused_EvenOfAnInactiveClient()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var alfa = await AddAsync(database, "Alfa Distribuidora", "11.222.333/0001-81", null);
        await database.RunAsync<DeactivateClient, bool>(deactivate => deactivate.ExecuteAsync(alfa.Id!.Value, Cancellation));
        var beta = await AddAsync(database, "Beta Alimentos", null, null);

        var added = await AddAsync(database, "Alfa Nova", "11222333000181", null);
        var changed = await database.RunAsync<ChangeClient, ClientSaveResult>(change => change.ExecuteAsync(
            beta.Id!.Value, new ClientChanges("Beta Alimentos", "11.222.333/0001-81", null), Cancellation));
        var same = await database.RunAsync<ChangeClient, ClientSaveResult>(change => change.ExecuteAsync(
            alfa.Id!.Value, new ClientChanges("Alfa Distribuidora", "11222333000181", "Matriz"), Cancellation));

        added.Should().BeEquivalentTo(ClientSaveResult.CnpjInUse("Alfa Distribuidora"));
        changed.Should().BeEquivalentTo(ClientSaveResult.CnpjInUse("Alfa Distribuidora"));
        same.Outcome.Should().Be(ClientSaveOutcome.Saved);
        (await ListAsync(database)).Total.Should().Be(2);
    }

    [Fact(DisplayName = "CNPJ repetido no último momento")]
    public async Task CnpjTakenMeanwhile_IsRefusedByTheDatabase()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        await AddAsync(database, "Alfa Distribuidora", "11.222.333/0001-81", null);
        await using var db = database.NewContext();
        var store = new Infrastructure.Persistence.ClientStore(db);

        // As if two requests checked the CNPJ at the same time: the second gets past the check.
        var client = Client.Create("Alfa Nova", "11222333000181", null);
        store.Add(client);
        var save = () => store.SaveChangesAsync(Cancellation);

        await save.Should().ThrowAsync<ClientCnpjInUseException>();
    }

    [Fact(DisplayName = "Alterar cliente")]
    public async Task Client_IsChanged()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Alfa Distribuidora", null, null);

        var changed = await database.RunAsync<ChangeClient, ClientSaveResult>(change => change.ExecuteAsync(
            added.Id!.Value, new ClientChanges("Alfa Distribuidora S.A.", "12.abc.345/01de-35", "Filiais SP e RS"), Cancellation));

        changed.Outcome.Should().Be(ClientSaveOutcome.Saved);
        (await ListAsync(database)).Items.Single().Should().Match<ClientListItem>(client =>
            client.Name == "Alfa Distribuidora S.A." && client.Cnpj == "12ABC34501DE35" && client.Observation == "Filiais SP e RS");
    }

    [Fact(DisplayName = "Cliente que não existe")]
    public async Task MissingClient_IsNotFound()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var id = Guid.CreateVersion7();

        var changed = await database.RunAsync<ChangeClient, ClientSaveResult>(change =>
            change.ExecuteAsync(id, new ClientChanges("Alfa", null, null), Cancellation));

        changed.Outcome.Should().Be(ClientSaveOutcome.NotFound);
        (await database.RunAsync<DeactivateClient, bool>(use => use.ExecuteAsync(id, Cancellation))).Should().BeFalse();
        (await database.RunAsync<ReactivateClient, bool>(use => use.ExecuteAsync(id, Cancellation))).Should().BeFalse();
        (await database.RunAsync<RemoveClient, bool>(use => use.ExecuteAsync(id, Cancellation))).Should().BeFalse();
        (await database.RunAsync<RemoveClientLogo, bool>(use => use.ExecuteAsync(id, Cancellation))).Should().BeFalse();
        (await ReplaceLogoAsync(database, id, Logos.Png, "logo.png")).Should().Be(ClientLogoOutcome.NotFound);
    }

    [Fact(DisplayName = "Inativar e reativar cliente")]
    public async Task Client_IsDeactivated_ThenReactivated()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Épsilon Logística", null, null);

        await database.RunAsync<DeactivateClient, bool>(use => use.ExecuteAsync(added.Id!.Value, Cancellation));
        var inactive = await ListAsync(database, isActive: false);
        await database.RunAsync<ReactivateClient, bool>(use => use.ExecuteAsync(added.Id!.Value, Cancellation));

        inactive.Items.Should().ContainSingle().Which.IsActive.Should().BeFalse();
        (await ListAsync(database, isActive: true)).Items.Should().ContainSingle();
    }

    [Fact(DisplayName = "Buscar cliente")]
    public async Task Search_FindsByNameWithoutAccents_OrByCnpj()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        await AddAsync(database, "Épsilon Logística", null, null);
        await AddAsync(database, "Alfa Distribuidora", "11.222.333/0001-81", null);
        await AddAsync(database, "Beta Alimentos", "12ABC34501DE35", null);

        (await ListAsync(database, "epsilon")).Items.Select(client => client.Name).Should().Equal("Épsilon Logística");
        (await ListAsync(database, "ALFA")).Items.Select(client => client.Name).Should().Equal("Alfa Distribuidora");
        (await ListAsync(database, "222.333")).Items.Select(client => client.Name).Should().Equal("Alfa Distribuidora");
        (await ListAsync(database, "abc")).Items.Select(client => client.Name).Should().Equal("Beta Alimentos");
        (await ListAsync(database)).Items.Select(client => client.Name)
            .Should().Equal("Alfa Distribuidora", "Beta Alimentos", "Épsilon Logística");
    }

    [Fact(DisplayName = "Enviar o logo do cliente")]
    public async Task Logo_IsStoredInTheFolderOfTheSystem()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Alfa Distribuidora", null, null);

        var outcome = await ReplaceLogoAsync(database, added.Id!.Value, Logos.Png, "C:\\logos\\alfa.png");

        outcome.Should().Be(ClientLogoOutcome.Saved);
        var (fileId, file) = database.Files.Files.Single();
        file.Name.Should().Be($"client-{added.Id}.png");
        database.Files.FolderNameOf(fileId).Should().Be("FiscoProjects");
        await using var db = database.NewContext();
        var client = await db.Clients.SingleAsync(Cancellation);
        client.LogoFileName.Should().Be("alfa.png");
        client.LogoChangedAt.Should().Be(database.Clock.GetUtcNow().UtcDateTime);
        var logo = await database.RunAsync<GetClientLogo, ClientLogoDownload?>(get => get.ExecuteAsync(added.Id!.Value, Cancellation));
        logo!.ContentType.Should().Be("image/png");
        await logo.File.DisposeAsync();
    }

    [Fact(DisplayName = "Trocar o logo por outro formato")]
    public async Task LogoOfAnotherFormat_ReplacesTheFile()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Alfa Distribuidora", null, null);
        await ReplaceLogoAsync(database, added.Id!.Value, Logos.Png, "alfa.png");

        await ReplaceLogoAsync(database, added.Id!.Value, Logos.Jpg, "alfa.jpg");

        database.Files.Files.Values.Should().ContainSingle().Which.Name.Should().Be($"client-{added.Id}.jpg");
    }

    [Fact(DisplayName = "Logo do cliente recusado")]
    public async Task RefusedLogo_KeepsThePreviousOne()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Alfa Distribuidora", null, null);
        await ReplaceLogoAsync(database, added.Id!.Value, Logos.Png, "alfa.png");
        var large = new byte[ClientLogo.MaxBytes + 1];
        Logos.Png.CopyTo(large, 0);

        (await ReplaceLogoAsync(database, added.Id!.Value, Logos.Svg, "alfa.svg")).Should().Be(ClientLogoOutcome.WrongFormat);
        (await ReplaceLogoAsync(database, added.Id!.Value, large, "grande.png")).Should().Be(ClientLogoOutcome.TooLarge);

        database.Files.Files.Values.Should().ContainSingle().Which.Name.Should().EndWith(".png");
        await using var db = database.NewContext();
        (await db.Clients.SingleAsync(Cancellation)).LogoFileName.Should().Be("alfa.png");
    }

    [Fact(DisplayName = "Remover o logo do cliente")]
    public async Task Logo_IsRemoved()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Alfa Distribuidora", null, null);
        await ReplaceLogoAsync(database, added.Id!.Value, Logos.Png, "alfa.png");

        (await database.RunAsync<RemoveClientLogo, bool>(remove => remove.ExecuteAsync(added.Id!.Value, Cancellation))).Should().BeTrue();

        database.Files.Files.Should().BeEmpty();
        (await ListAsync(database)).Items.Single().LogoChangedAt.Should().BeNull();
    }

    [Fact(DisplayName = "Excluir cliente")]
    public async Task Client_IsRemoved_WithItsLogo()
    {
        await using var database = await ProjectsDatabase.CreateAsync(sqlServer);
        var added = await AddAsync(database, "Alfa Distribuidora", null, null);
        await ReplaceLogoAsync(database, added.Id!.Value, Logos.Png, "alfa.png");

        (await database.RunAsync<RemoveClient, bool>(remove => remove.ExecuteAsync(added.Id!.Value, Cancellation))).Should().BeTrue();

        (await ListAsync(database)).Total.Should().Be(0);
        database.Files.Files.Should().BeEmpty();
    }

    private static Task<ClientSaveResult> AddAsync(ProjectsDatabase database, string name, string? cnpj, string? observation) =>
        database.RunAsync<AddClient, ClientSaveResult>(add =>
            add.ExecuteAsync(new ClientChanges(name, cnpj, observation), Cancellation));

    private static Task<ClientListPage> ListAsync(ProjectsDatabase database, string? search = null, bool? isActive = null) =>
        database.RunAsync<ListClients, ClientListPage>(list =>
            list.ExecuteAsync(new ClientListQuery(search, isActive, 1, 25), Cancellation));

    private static Task<ClientLogoOutcome> ReplaceLogoAsync(ProjectsDatabase database, Guid id, byte[] content, string fileName) =>
        database.RunAsync<ReplaceClientLogo, ClientLogoOutcome>(async replace =>
        {
            await using var stream = new MemoryStream(content);
            return await replace.ExecuteAsync(id, fileName, stream, Cancellation);
        });
}
