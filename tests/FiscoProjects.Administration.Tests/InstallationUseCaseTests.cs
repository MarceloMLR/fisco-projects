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

        installation.Should().Be(new Installation("FiscoProjects", null, null));
        info.Should().Be(new InstallationInfo("FiscoProjects", "America/Sao_Paulo", new TimeOnly(7, 30), 90));
    }

    [Fact(DisplayName = "Fuso padrão")]
    public async Task NewInstallation_ShowsDatesInTheTimeZoneOfBrasilia()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var rules = await database.RunAsync<GetGeneralRules, GeneralRules>(get => get.ExecuteAsync(Cancellation));

        rules.Should().Be(new GeneralRules("America/Sao_Paulo", new TimeOnly(7, 30), 90));
    }

    [Fact(DisplayName = "Trocar o nome e o fuso")]
    public async Task Update_KeepsTheNameWithoutTheSpacesAtTheEnds_AndTheTimeZone()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var invalid = await UpdateAsync(database, "  Consultoria Fictícia ", "America/Manaus");

        invalid.Should().BeEmpty();
        var settings = await SettingsAsync(database);
        settings.Name.Should().Be("Consultoria Fictícia");
        settings.TimeZone.Should().Be("America/Manaus");
        (await database.RunAsync<IInstallationSettings, InstallationInfo>(get => get.GetAsync(Cancellation)))
            .Should().Be(new InstallationInfo("Consultoria Fictícia", "America/Manaus", new TimeOnly(7, 30), 90));
    }

    [Fact(DisplayName = "Horário padrão")]
    public async Task NewInstallation_SendsTheDailySummaryAtHalfPastSeven()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var settings = await SettingsAsync(database);

        settings.DailySummaryAt.Should().Be(new TimeOnly(7, 30));
    }

    [Fact(DisplayName = "Trocar o horário")]
    public async Task Update_KeepsTheTimeOfTheDailySummary_WithoutSeconds()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var invalid = await UpdateAsync(database, "FiscoProjects", "America/Sao_Paulo", dailySummaryAt: new TimeOnly(8, 0, 42));

        invalid.Should().BeEmpty();
        (await SettingsAsync(database)).DailySummaryAt.Should().Be(new TimeOnly(8, 0));
        (await database.RunAsync<IInstallationSettings, InstallationInfo>(get => get.GetAsync(Cancellation)))
            .DailySummaryAt.Should().Be(new TimeOnly(8, 0));
    }

    [Fact(DisplayName = "Horário inválido")]
    public async Task UpdateWithoutTheTime_ChangesNothing()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var invalid = await database.RunAsync<UpdateInstallation, IReadOnlyList<InstallationField>>(update =>
            update.ExecuteAsync(new InstallationChanges("Outro nome", "America/Sao_Paulo", null, null, 90), Cancellation));

        invalid.Should().Equal(InstallationField.DailySummaryAt);
        (await SettingsAsync(database)).Name.Should().Be("FiscoProjects");
    }

    [Fact(DisplayName = "Guarda padrão")]
    public async Task NewInstallation_KeepsTheNotificationsForNinetyDays()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        (await SettingsAsync(database)).NotificationRetentionDays.Should().Be(90);
    }

    [Theory(DisplayName = "Guarda inválida")]
    [InlineData(0)]
    [InlineData(366)]
    [InlineData(null)]
    public async Task RetentionOutOfRange_ChangesNothing(int? days)
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var invalid = await database.RunAsync<UpdateInstallation, IReadOnlyList<InstallationField>>(update =>
            update.ExecuteAsync(new InstallationChanges("Outro nome", "America/Sao_Paulo", null, new TimeOnly(7, 30), days), Cancellation));

        invalid.Should().Equal(InstallationField.NotificationRetentionDays);
        (await SettingsAsync(database)).Name.Should().Be("FiscoProjects");
    }

    [Theory(DisplayName = "Nome inválido")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Nome com mais de sessenta caracteres para passar do limite aceito")]
    public async Task InvalidName_ChangesNothing(string name)
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var invalid = await UpdateAsync(database, name, "America/Manaus");

        invalid.Should().Equal(InstallationField.Name);
        var settings = await SettingsAsync(database);
        settings.Name.Should().Be(InstallationSettings.DefaultName);
        settings.TimeZone.Should().Be(InstallationSettings.DefaultTimeZone);
    }

    [Theory(DisplayName = "Fuso inválido")]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("E. South America Standard Time")]
    [InlineData("")]
    [InlineData(null)]
    public async Task InvalidTimeZone_ChangesNothing(string? timeZone)
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var invalid = await UpdateAsync(database, "", timeZone);

        invalid.Should().Equal(InstallationField.Name, InstallationField.TimeZone);
        (await SettingsAsync(database)).TimeZone.Should().Be(InstallationSettings.DefaultTimeZone);
    }

    [Fact(DisplayName = "Trocar a cor principal")]
    public async Task MainColor_IsKeptInCapitals_AndBackToTheDefault()
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        (await UpdateAsync(database, "FiscoProjects", "America/Sao_Paulo", "#0f766e")).Should().BeEmpty();
        (await SettingsAsync(database)).MainColor.Should().Be("#0F766E");
        (await database.RunAsync<GetInstallation, Installation>(get => get.ExecuteAsync(Cancellation)))
            .MainColor.Should().Be("#0F766E");

        (await UpdateAsync(database, "FiscoProjects", "America/Sao_Paulo", null)).Should().BeEmpty();
        (await SettingsAsync(database)).MainColor.Should().BeNull();
    }

    [Theory(DisplayName = "Cor sem contraste ou fora do formato")]
    [InlineData("#FFD400")]
    [InlineData("#9FD3FF")]
    [InlineData("0F766E")]
    [InlineData("#0F766")]
    [InlineData("#GGGGGG")]
    public async Task InvalidMainColor_ChangesNothing(string mainColor)
    {
        await using var database = await AdministrationDatabase.CreateAsync(sqlServer);

        var invalid = await UpdateAsync(database, "Consultoria Fictícia", "America/Sao_Paulo", mainColor);

        invalid.Should().Equal(InstallationField.MainColor);
        var settings = await SettingsAsync(database);
        settings.MainColor.Should().BeNull();
        settings.Name.Should().Be(InstallationSettings.DefaultName);
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

    private static Task<IReadOnlyList<InstallationField>> UpdateAsync(
        AdministrationDatabase database, string? name, string? timeZone, string? mainColor = null, TimeOnly? dailySummaryAt = null) =>
        database.RunAsync<UpdateInstallation, IReadOnlyList<InstallationField>>(update =>
            update.ExecuteAsync(
                new InstallationChanges(name, timeZone, mainColor, dailySummaryAt ?? new TimeOnly(7, 30), 90), Cancellation));

    private static Task<LogoRefusal?> SendAsync(AdministrationDatabase database, string fileName, byte[] content) =>
        database.RunAsync<ReplaceInstallationLogo, LogoRefusal?>(replace =>
            replace.ExecuteAsync(fileName, new MemoryStream(content), Cancellation));

    private static async Task<InstallationSettings> SettingsAsync(AdministrationDatabase database)
    {
        await using var db = database.NewContext();
        return await db.InstallationSettings.AsNoTracking().SingleAsync(Cancellation);
    }
}
