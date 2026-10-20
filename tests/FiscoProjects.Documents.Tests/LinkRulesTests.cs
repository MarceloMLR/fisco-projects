using FiscoProjects.Documents.Domain;
using FiscoProjects.Projects.Contracts;

namespace FiscoProjects.Documents.Tests;

// Spec: links (domain side): "Categorias de links", "Links do projeto" and "Permissões nos links" (ADR-0032, ADR-0045).
public sealed class LinkRulesTests
{
    private static readonly DocumentViewer Person = new(Guid.CreateVersion7(), IsAdministrator: false);

    private static ProjectAccess ProjectWith(ProjectMemberRole? role) =>
        new(Guid.CreateVersion7(), "ERP-FIN", "Implantação do ERP financeiro", IsClosed: false, role);

    [Theory(DisplayName = "Matriz dos links")]
    [InlineData(ProjectMemberRole.Manager, true)]
    [InlineData(ProjectMemberRole.TechLead, true)]
    [InlineData(ProjectMemberRole.Contributor, true)]
    [InlineData(ProjectMemberRole.Developer, true)]
    [InlineData(ProjectMemberRole.Viewer, false)]
    public void EditingLinks_FollowsTheRole(ProjectMemberRole role, bool edits) =>
        DocumentAccess.MayEditLinks(Person, ProjectWith(role)).Should().Be(edits);

    [Fact(DisplayName = "Administrador edita os links de qualquer projeto")]
    public void Administrator_EditsLinks_WithoutARole() =>
        DocumentAccess.MayEditLinks(Person with { IsAdministrator = true }, ProjectWith(null)).Should().BeTrue();

    [Fact(DisplayName = "Link completo")]
    public void Form_TrimsTheFields()
    {
        var category = Guid.CreateVersion7();

        var (form, invalid) = LinkForm.Of("  ERP · Homologação ", " https://hml-erp.cliente.example ", category, "   ");

        invalid.Should().BeEmpty();
        form.Should().Be(new LinkForm("ERP · Homologação", "https://hml-erp.cliente.example", category, null));
    }

    [Theory(DisplayName = "Campos do link")]
    [InlineData("", "https://erp.cliente.example", true, 10, LinkField.Title)]
    [InlineData("ERP", "ftp://arquivos.example", true, 10, LinkField.Url)]
    [InlineData("ERP", "erp.cliente.example", true, 10, LinkField.Url)]
    [InlineData("ERP", "mailto:time@empresa.example", true, 10, LinkField.Url)]
    [InlineData("ERP", "https://erp.cliente.example", false, 10, LinkField.Category)]
    [InlineData("ERP", "https://erp.cliente.example", true, 501, LinkField.Description)]
    public void Form_TellsTheFieldNotValid(string title, string url, bool withCategory, int description, LinkField field)
    {
        var (form, invalid) = LinkForm.Of(title, url, withCategory ? Guid.CreateVersion7() : null, new string('a', description));

        form.Should().BeNull();
        invalid.Should().Equal(field);
    }

    [Fact(DisplayName = "Tamanhos do link")]
    public void Form_TakesTheLongestFields()
    {
        var url = "https://erp.cliente.example/" + new string('a', ProjectLink.UrlMaxLength - 28);

        LinkForm.Of(new string('t', ProjectLink.TitleMaxLength), url, Guid.CreateVersion7(), null).Invalid.Should().BeEmpty();
        LinkForm.Of(new string('t', ProjectLink.TitleMaxLength + 1), url + "a", Guid.CreateVersion7(), null).Invalid
            .Should().Equal(LinkField.Title, LinkField.Url);
    }

    [Theory(DisplayName = "Nome da categoria de link")]
    [InlineData(" Documentação ", "Documentação")]
    [InlineData("   ", null)]
    [InlineData("Linha\nquebrada", null)]
    public void CategoryName_IsTrimmedAndChecked(string name, string? valid) =>
        LinkCategory.NameOf(name).Should().Be(valid);

    [Fact(DisplayName = "Nome de categoria até 80 caracteres")]
    public void CategoryName_HasUpTo80Characters()
    {
        LinkCategory.NameOf(new string('c', 80)).Should().HaveLength(80);
        LinkCategory.NameOf(new string('c', 81)).Should().BeNull();
    }
}
