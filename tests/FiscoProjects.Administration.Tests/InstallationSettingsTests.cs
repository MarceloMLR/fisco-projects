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
}
