using FiscoProjects.Credentials.Domain;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Credentials.Tests;

// Spec: acessos (domain side): "Acessos do projeto" and "Permissões nos acessos" (ADR-0034, ADR-0045).
public sealed class CredentialRulesTests
{
    private static readonly CredentialViewer Person = new(Guid.CreateVersion7(), IsAdministrator: false);

    private static ProjectAccess ProjectWith(ProjectMemberRole? role) =>
        new(Guid.CreateVersion7(), "ERP-FIN", "Implantação do ERP financeiro", IsClosed: false, role);

    [Theory(DisplayName = "Matriz dos acessos")]
    [InlineData(ProjectMemberRole.Manager, true, true)]
    [InlineData(ProjectMemberRole.TechLead, true, true)]
    [InlineData(ProjectMemberRole.Contributor, true, false)]
    [InlineData(ProjectMemberRole.Developer, true, false)]
    [InlineData(ProjectMemberRole.Viewer, false, false)]
    public void Permissions_FollowTheRole(ProjectMemberRole role, bool edits, bool deletes) =>
        CredentialAccess.Of(Person, ProjectWith(role)).Should().Be(new CredentialPermissions(edits, deletes));

    [Fact(DisplayName = "Administrador em qualquer projeto nos acessos")]
    public void Administrator_DoesEverything_WithoutARole() =>
        CredentialAccess.Of(Person with { IsAdministrator = true }, ProjectWith(null)).Should().Be(new CredentialPermissions(true, true));

    [Fact(DisplayName = "Acesso completo")]
    public void Form_TrimsAllButThePassword()
    {
        var (form, invalid) = CredentialForm.Of(
            " ERP ", " Homologação ", " https://hml-erp.cliente.example ", " consultoria.fiscal ", " Kx7#pa2V!q ", "  ", passwordNeeded: true);

        invalid.Should().BeEmpty();
        form.Should().Be(new CredentialForm("ERP", "Homologação", "https://hml-erp.cliente.example", "consultoria.fiscal", " Kx7#pa2V!q ", null));
    }

    [Fact(DisplayName = "Acesso sem endereço")]
    public void Form_TakesNoAddress() =>
        CredentialForm.Of("SEFAZ", "Produção", " ", "12.345.678/0001-90", "segredo", null, passwordNeeded: true).Form!.Url.Should().BeNull();

    [Theory(DisplayName = "Campos do acesso")]
    [InlineData("", "Produção", "", "usuario", "senha", CredentialField.Portal)]
    [InlineData("ERP", " ", "", "usuario", "senha", CredentialField.Environment)]
    [InlineData("ERP", "Produção", "ftp://erp.cliente.example", "usuario", "senha", CredentialField.Url)]
    [InlineData("ERP", "Produção", "", "", "senha", CredentialField.Username)]
    [InlineData("ERP", "Produção", "", "usuario", "", CredentialField.Password)]
    public void Form_TellsTheFieldNotValid(string portal, string environment, string url, string username, string password, CredentialField field)
    {
        var (form, invalid) = CredentialForm.Of(portal, environment, url, username, password, null, passwordNeeded: true);

        form.Should().BeNull();
        invalid.Should().Equal(field);
    }

    [Fact(DisplayName = "Senha em branco mantém a senha na edição")]
    public void Change_WithoutPassword_KeepsIt()
    {
        var (form, invalid) = CredentialForm.Of("ERP", "Produção", null, "usuario", "", "Nova observação", passwordNeeded: false);
        var credential = new PortalCredential(Guid.CreateVersion7(), form!, "protegida", Guid.CreateVersion7(), DateTime.UtcNow);

        credential.Change(form!, null, Guid.CreateVersion7(), DateTime.UtcNow);

        invalid.Should().BeEmpty();
        form!.Password.Should().BeNull();
        (credential.ProtectedPassword, credential.Notes).Should().Be(("protegida", "Nova observação"));
    }

    [Fact(DisplayName = "Tamanhos do acesso")]
    public void Form_TakesTheLongestFields()
    {
        CredentialForm.Of(new string('p', 120), new string('a', 60), null, new string('u', 200), new string('s', 500), new string('o', 500), true)
            .Invalid.Should().BeEmpty();
        CredentialForm.Of(new string('p', 121), new string('a', 61), null, new string('u', 201), new string('s', 501), new string('o', 501), true)
            .Invalid.Should().Equal(CredentialField.Portal, CredentialField.Environment, CredentialField.Username, CredentialField.Password, CredentialField.Notes);
    }
}
