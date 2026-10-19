using FiscoProjects.Administration.Domain;

namespace FiscoProjects.Administration.Tests;

public sealed class InstallationSettingsTests
{
    [Theory(DisplayName = "Nome válido sem os espaços das pontas")]
    [InlineData("  Consultoria Fictícia  ", "Consultoria Fictícia")]
    [InlineData("A", "A")]
    public void ValidName_TrimsTheEnds(string name, string expected) =>
        InstallationSettings.ValidName(name).Should().Be(expected);

    [Theory(DisplayName = "Nome inválido")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidName_IsNull_ForAnEmptyName(string? name) =>
        InstallationSettings.ValidName(name).Should().BeNull();

    [Fact(DisplayName = "Nome com até 60 caracteres")]
    public void ValidName_AcceptsUpTo60Characters()
    {
        InstallationSettings.ValidName(new string('a', 60)).Should().HaveLength(60);
        InstallationSettings.ValidName(new string('a', 61)).Should().BeNull();
    }

    [Theory(DisplayName = "Fuso IANA conhecido")]
    [InlineData("America/Sao_Paulo")]
    [InlineData("America/Manaus")]
    [InlineData("Europe/Lisbon")]
    public void KnownIanaTimeZone_IsValid(string timeZone) =>
        InstallationSettings.IsValidTimeZone(timeZone).Should().BeTrue();

    [Theory(DisplayName = "Fuso desconhecido ou do Windows")]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("E. South America Standard Time")]
    [InlineData("")]
    [InlineData(null)]
    public void UnknownOrWindowsTimeZone_IsInvalid(string? timeZone) =>
        InstallationSettings.IsValidTimeZone(timeZone).Should().BeFalse();
}
