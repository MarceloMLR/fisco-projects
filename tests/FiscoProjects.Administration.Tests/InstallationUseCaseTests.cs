using FiscoProjects.Administration.Application;
using FiscoProjects.Administration.Contracts;
using FiscoProjects.Administration.Domain;
using FiscoProjects.Testing;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Administration.Tests;

// Spec: configuracao-da-instalacao, requirements "Nome da instalação" and "Logo da instalação" (module side).
public sealed class InstallationUseCaseTests(SqlServerFixture sqlServer)
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact(DisplayName = "Nome padrão")]
    public async Task NewInstallation_IsCalledFiscoProjects_WithoutALogo()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var installation = await database.RunAsync<GetInstallation, Installation>(get => get.ExecuteAsync(Cancellation));
        var info = await database.RunAsync<IInstallationSettings, InstallationInfo>(settings => settings.GetAsync(Cancellation));

        installation.Should().Be(new Installation("FiscoProjects", null));
        info.Should().Be(new InstallationInfo("FiscoProjects"));
    }

    [Fact(DisplayName = "Trocar o nome")]
    public async Task Rename_KeepsTheNameWithoutTheSpacesAtTheEnds()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var renamed = await database.RunAsync<RenameInstallation, bool>(rename =>
            rename.ExecuteAsync("  Consultoria Fictícia ", Cancellation));

        renamed.Should().BeTrue();
        (await NameAsync(database)).Should().Be("Consultoria Fictícia");
    }

    [Theory(DisplayName = "Nome inválido")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Nome com mais de sessenta caracteres para passar do limite aceito")]
    public async Task InvalidName_ChangesNothing(string name)
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var renamed = await database.RunAsync<RenameInstallation, bool>(rename => rename.ExecuteAsync(name, Cancellation));

        renamed.Should().BeFalse();
        (await NameAsync(database)).Should().Be(InstallationSettings.DefaultName);
    }

    [Fact(DisplayName = "Enviar o logo")]
    public async Task ReplaceLogo_StoresTheFileInTheFolderOfTheSystem_AndRecordsIt()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var refusal = await SendAsync(database, "Marca Fictícia.png", Logos.Png());

        refusal.Should().BeNull();
        var settings = await SettingsAsync(database);
        settings.LogoFileName.Should().Be("Marca Fictícia.png");
        settings.LogoContentType.Should().Be("image/png");
        settings.LogoChangedAt.Should().Be(new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc));
        var file = database.Files.Files[settings.LogoFileId!];
        file.Name.Should().Be("installation-logo.png");
        file.Content.Should().Equal(Logos.Png());
        database.Files.FolderNameOf(settings.LogoFileId!).Should().Be("FiscoProjects");

        var logo = await database.RunAsync<GetInstallationLogo, InstallationLogo?>(get => get.ExecuteAsync(Cancellation));
        await using var logoFile = logo!.File;
        logo.ContentType.Should().Be("image/png");
        (await database.RunAsync<GetInstallation, Installation>(get => get.ExecuteAsync(Cancellation)))
            .LogoChangedAt.Should().Be(settings.LogoChangedAt);
    }

    [Fact(DisplayName = "Trocar PNG por SVG")]
    public async Task ReplacingAPngByAnSvg_RemovesThePng()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);
        await SendAsync(database, "marca.png", Logos.Png());
        database.Clock.Advance(TimeSpan.FromMinutes(5));

        await SendAsync(database, "marca.svg", Logos.Svg());

        var settings = await SettingsAsync(database);
        settings.LogoContentType.Should().Be("image/svg+xml");
        settings.LogoChangedAt.Should().Be(new DateTime(2026, 10, 19, 12, 5, 0, DateTimeKind.Utc));
        database.Files.Files.Values.Select(file => file.Name).Should().Equal("installation-logo.svg");
    }

    [Fact(DisplayName = "Logo recusado")]
    public async Task RefusedLogo_KeepsThePreviousOne()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);
        await SendAsync(database, "marca.png", Logos.Png());
        var before = await SettingsAsync(database);

        (await SendAsync(database, "foto.jpg", Logos.Jpg())).Should().Be(LogoRefusal.Format);
        (await SendAsync(database, "grande.png", Logos.Png(LogoFile.MaxBytes + 1))).Should().Be(LogoRefusal.TooLarge);
        (await SendAsync(database, "marca.svg", Logos.Svg("<script>alert(1)</script>"))).Should().Be(LogoRefusal.Unsafe);

        var after = await SettingsAsync(database);
        after.LogoFileName.Should().Be(before.LogoFileName);
        after.LogoChangedAt.Should().Be(before.LogoChangedAt);
        database.Files.Files.Should().ContainSingle().Which.Value.Content.Should().Equal(Logos.Png());
    }

    [Fact(DisplayName = "Remover o logo")]
    public async Task RemoveLogo_ClearsTheRecordAndTheFile()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);
        await SendAsync(database, "marca.png", Logos.Png());

        await database.RunAsync<RemoveInstallationLogo>(remove => remove.ExecuteAsync(Cancellation));

        var settings = await SettingsAsync(database);
        settings.HasLogo.Should().BeFalse();
        settings.LogoFileName.Should().BeNull();
        settings.LogoChangedAt.Should().BeNull();
        database.Files.Files.Should().BeEmpty();
        (await database.RunAsync<GetInstallationLogo, InstallationLogo?>(get => get.ExecuteAsync(Cancellation))).Should().BeNull();
    }

    private static Task<LogoRefusal?> SendAsync(AdministrationDatabase database, string fileName, byte[] content) =>
        database.RunAsync<ReplaceInstallationLogo, LogoRefusal?>(replace =>
            replace.ExecuteAsync(fileName, new MemoryStream(content), Cancellation));

    private static async Task<InstallationSettings> SettingsAsync(AdministrationDatabase database)
    {
        await using var db = database.NewContext();
        return await db.InstallationSettings.AsNoTracking().SingleAsync(Cancellation);
    }

    private static async Task<string> NameAsync(AdministrationDatabase database) => (await SettingsAsync(database)).Name;
}
