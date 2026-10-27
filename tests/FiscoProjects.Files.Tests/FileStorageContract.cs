using System.Text;
using FiscoProjects.Files.Contracts;

namespace FiscoProjects.Files.Tests;

/// <summary>
/// What every implementation of the file storage does (ADR-0079): the local one now, SharePoint later. An
/// implementation is checked by a test class that inherits these tests and gives its storage.
/// </summary>
// Spec: armazenamento-de-arquivos, requirements "Pastas", "Enviar e abrir arquivos", "Mesmo nome na mesma pasta",
// "Excluir arquivos e pastas", "Pastas vazias acima" and "Nomes e caminhos seguros".
public abstract class FileStorageContract
{
    protected static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    protected abstract IFileStorage Storage { get; }

    [Fact(DisplayName = "Pastas criadas no caminho")]
    public async Task MissingFolders_AreCreatedAlongThePath()
    {
        var ticket = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN/Chamados/#101", Cancellation);

        ticket.Name.Should().Be("#101");
        (await NamesAsync(null)).Should().Equal("Projetos");
        var projects = (await Storage.ListFoldersAsync(null, Cancellation)).Single();
        var project = (await Storage.ListFoldersAsync(projects.Id, Cancellation)).Should().ContainSingle().Subject;
        project.Name.Should().Be("ERP-FIN");
        var tickets = (await Storage.ListFoldersAsync(project.Id, Cancellation)).Should().ContainSingle().Subject;
        (await Storage.ListFoldersAsync(tickets.Id, Cancellation)).Should().ContainSingle().Which.Should().Be(ticket);
    }

    [Fact(DisplayName = "Pasta que já existe")]
    public async Task ExistingFolder_IsTheSameFolder()
    {
        var project = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN", Cancellation);

        var again = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN", Cancellation);
        var fromParent = await Storage.EnsureFolderAsync(
            (await Storage.EnsureFolderAsync(null, "Projetos", Cancellation)).Id, "ERP-FIN", Cancellation);

        again.Should().Be(project);
        fromParent.Should().Be(project);
        (await NamesAsync(null)).Should().Equal("Projetos");
    }

    [Fact(DisplayName = "Listar subpastas")]
    public async Task Listing_ShowsOnlyTheFoldersDirectlyInside_Alphabetically()
    {
        var project = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN", Cancellation);
        await Storage.EnsureFolderAsync(project.Id, "Documentos/Atas", Cancellation);
        await Storage.EnsureFolderAsync(project.Id, "Chamados", Cancellation);
        await Storage.EnsureFolderAsync(project.Id, "Área do cliente", Cancellation);
        await UploadAsync(project.Id, "Leia-me.txt", "texto", SameNameAction.NewVersion);
        await UploadAsync(project.Id, "Leia-me.txt", "novo texto", SameNameAction.NewVersion);

        (await NamesAsync(project.Id)).Should().Equal("Área do cliente", "Chamados", "Documentos");
        (await NamesAsync(null)).Should().Equal("Projetos");
    }

    [Fact(DisplayName = "Enviar um arquivo")]
    public async Task Upload_StoresTheFileInTheFolder()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN/Documentos", Cancellation);

        var stored = await UploadAsync(folder.Id, "Especificação.pdf", "%PDF-1.7 conteúdo", SameNameAction.NewVersion);

        stored.Name.Should().Be("Especificação.pdf");
        stored.Size.Should().Be(Encoding.UTF8.GetByteCount("%PDF-1.7 conteúdo"));
        stored.Id.Should().NotBeNullOrWhiteSpace();
        stored.VersionId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "Abrir um arquivo")]
    public async Task Open_GivesBackTheContentNameTypeAndSize()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN/Documentos", Cancellation);
        var stored = await UploadAsync(folder.Id, "Especificação.pdf", "%PDF-1.7 conteúdo", SameNameAction.NewVersion);

        await using var download = await Storage.OpenAsync(stored.Id, null, Cancellation);

        download.Should().NotBeNull();
        (await ReadAsync(download!)).Should().Be("%PDF-1.7 conteúdo");
        download!.Name.Should().Be("Especificação.pdf");
        download.ContentType.Should().Be("application/pdf");
        download.Size.Should().Be(stored.Size);
        download.VersionId.Should().Be(stored.VersionId);
    }

    [Fact(DisplayName = "Arquivo inexistente")]
    public async Task UnknownFile_IsNotFound()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos", Cancellation);
        var stored = await UploadAsync(folder.Id, "Contrato.pdf", "contrato", SameNameAction.NewVersion);
        await Storage.DeleteFileAsync(stored.Id, Cancellation);

        (await Storage.OpenAsync(stored.Id, null, Cancellation)).Should().BeNull();
        (await Storage.DeleteFileAsync(stored.Id, Cancellation)).Should().BeFalse();
        await Storage.DeleteFolderIfEmptyAsync(folder.Id, Cancellation);
        var upload = () => UploadAsync(folder.Id, "Contrato.pdf", "contrato", SameNameAction.NewVersion);
        await upload.Should().ThrowAsync<StorageFolderNotFoundException>();
    }

    [Fact(DisplayName = "Nova versão de um arquivo")]
    public async Task NewVersion_KeepsTheIdAndThePreviousVersion()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN/Documentos", Cancellation);
        var first = await UploadAsync(folder.Id, "Contrato.pdf", "primeira", SameNameAction.NewVersion);

        var second = await UploadAsync(folder.Id, "Contrato.pdf", "segunda versão", SameNameAction.NewVersion);

        second.Id.Should().Be(first.Id);
        second.VersionId.Should().NotBe(first.VersionId);
        await using (var current = await Storage.OpenAsync(first.Id, null, Cancellation))
        {
            (await ReadAsync(current!)).Should().Be("segunda versão");
            current!.VersionId.Should().Be(second.VersionId);
        }

        await using (var previous = await Storage.OpenAsync(first.Id, first.VersionId, Cancellation))
        {
            (await ReadAsync(previous!)).Should().Be("primeira");
            previous!.VersionId.Should().Be(first.VersionId);
            previous.Size.Should().Be(first.Size);
        }

        (await Storage.OpenAsync(first.Id, "999", Cancellation)).Should().BeNull();
    }

    [Fact(DisplayName = "Manter os dois arquivos")]
    public async Task KeepBoth_StoresTheNewFileWithAnotherName()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN/Chamados/#101", Cancellation);
        var first = await UploadAsync(folder.Id, "print.png", "primeiro", SameNameAction.KeepBoth);

        var second = await UploadAsync(folder.Id, "print.png", "segundo", SameNameAction.KeepBoth);
        var third = await UploadAsync(folder.Id, "print.png", "terceiro", SameNameAction.KeepBoth);

        first.Name.Should().Be("print.png");
        second.Name.Should().Be("print (1).png");
        third.Name.Should().Be("print (2).png");
        new[] { first.Id, second.Id, third.Id }.Should().OnlyHaveUniqueItems();
        await using var unchanged = await Storage.OpenAsync(first.Id, null, Cancellation);
        (await ReadAsync(unchanged!)).Should().Be("primeiro");
    }

    [Fact(DisplayName = "Excluir um arquivo")]
    public async Task DeletingAFile_RemovesItsVersionsToo()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN/Documentos", Cancellation);
        var first = await UploadAsync(folder.Id, "Contrato.pdf", "primeira", SameNameAction.NewVersion);
        await UploadAsync(folder.Id, "Contrato.pdf", "segunda", SameNameAction.NewVersion);

        (await Storage.DeleteFileAsync(first.Id, Cancellation)).Should().BeTrue();

        (await Storage.OpenAsync(first.Id, null, Cancellation)).Should().BeNull();
        (await Storage.OpenAsync(first.Id, first.VersionId, Cancellation)).Should().BeNull();
        var again = await UploadAsync(folder.Id, "Contrato.pdf", "nova", SameNameAction.NewVersion);
        (await Storage.OpenAsync(again.Id, first.VersionId, Cancellation)).Should().NotBeNull("a new file starts its versions again");
    }

    [Fact(DisplayName = "Excluir uma pasta vazia")]
    public async Task EmptyFolder_IsDeleted()
    {
        var project = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN", Cancellation);
        var tickets = await Storage.EnsureFolderAsync(project.Id, "Chamados", Cancellation);

        (await Storage.DeleteFolderIfEmptyAsync(tickets.Id, Cancellation)).Should().BeTrue();

        (await NamesAsync(project.Id)).Should().BeEmpty();
        (await Storage.DeleteFolderIfEmptyAsync(tickets.Id, Cancellation)).Should().BeFalse();
    }

    [Fact(DisplayName = "Pasta com conteúdo não é excluída")]
    public async Task FolderWithContent_IsKept()
    {
        var project = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN", Cancellation);
        var documents = await Storage.EnsureFolderAsync(project.Id, "Documentos", Cancellation);
        var file = await UploadAsync(documents.Id, "Ata.docx", "ata", SameNameAction.NewVersion);
        await Storage.EnsureFolderAsync(project.Id, "Chamados/#101", Cancellation);

        (await Storage.DeleteFolderIfEmptyAsync(documents.Id, Cancellation)).Should().BeFalse();
        (await Storage.DeleteFolderIfEmptyAsync(project.Id, Cancellation)).Should().BeFalse();

        (await NamesAsync(project.Id)).Should().Equal("Chamados", "Documentos");
        await using var kept = await Storage.OpenAsync(file.Id, null, Cancellation);
        kept.Should().NotBeNull();
    }

    [Fact(DisplayName = "Pasta de um arquivo")]
    public async Task TheFolderOfAnItem_IsTheOneAboveIt()
    {
        var project = await Storage.EnsureFolderAsync(null, "ERP-FIN - Projeto", Cancellation);
        var ticket = await Storage.EnsureFolderAsync(project.Id, "Chamados/#104", Cancellation);
        var file = await UploadAsync(ticket.Id, "nota.xml", "<nfe/>", SameNameAction.KeepBoth);
        var tickets = await Storage.FolderOfAsync(ticket.Id, Cancellation);

        (await Storage.FolderOfAsync(file.Id, Cancellation)).Should().Be(ticket.Id);
        tickets.Should().NotBeNull();
        (await Storage.FolderOfAsync(tickets!, Cancellation)).Should().Be(project.Id);
        (await Storage.FolderOfAsync(project.Id, Cancellation)).Should().BeNull("the project folder is at the root");
        await Storage.DeleteFileAsync(file.Id, Cancellation);
        (await Storage.FolderOfAsync(file.Id, Cancellation)).Should().BeNull("the file does not exist");
    }

    [Fact(DisplayName = "Pastas vazias acima")]
    public async Task RemovingWhatAModuleStored_TakesTheFoldersLeftEmpty()
    {
        var project = await Storage.EnsureFolderAsync(null, "ERP-FIN - Projeto", Cancellation);
        var first = await Storage.EnsureFolderAsync(project.Id, "Chamados/#104", Cancellation);
        var second = await Storage.EnsureFolderAsync(project.Id, "Chamados/#105", Cancellation);
        var files = new[]
        {
            await UploadAsync(first.Id, "nota.xml", "<nfe/>", SameNameAction.KeepBoth),
            await UploadAsync(first.Id, "print.png", "png", SameNameAction.KeepBoth),
        };
        var other = await Storage.EnsureFolderAsync(null, "Outro projeto", Cancellation);

        var removed = await Storage.RemoveAsync(files.Select(file => file.Id), [second.Id, "ERP-FIN - Projeto/Chamados/#999"], Cancellation);
        var again = await Storage.RemoveAsync(files.Select(file => file.Id), [first.Id], Cancellation);

        removed.Should().Be(2);
        again.Should().Be(0, "what is gone is skipped");
        (await NamesAsync(null)).Should().Equal("Outro projeto");
        (await Storage.FolderOfAsync(other.Id, Cancellation)).Should().BeNull();
    }

    [Fact(DisplayName = "Até a pasta indicada")]
    public async Task RemovingUpToAFolder_KeepsThatFolderAndTheOnesAbove()
    {
        var project = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN - Projeto", Cancellation);
        var ticket = await Storage.EnsureFolderAsync(project.Id, "Chamados/#104", Cancellation);
        var file = await UploadAsync(ticket.Id, "nota.xml", "<nfe/>", SameNameAction.KeepBoth);
        var projects = await Storage.FolderOfAsync(project.Id, Cancellation);

        await Storage.RemoveAsync([file.Id], [], project.Id, Cancellation);

        (await NamesAsync(null)).Should().Equal("Projetos");
        (await NamesAsync(projects)).Should().Equal("ERP-FIN - Projeto");
        (await NamesAsync(project.Id)).Should().BeEmpty("the folders of the tickets were left empty");
    }

    [Fact(DisplayName = "Renomear pasta")]
    public async Task RenamingAFolder_KeepsTheIdsOfEverythingInIt()
    {
        var project = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN - Implantação", Cancellation);
        var documents = await Storage.EnsureFolderAsync(project.Id, "Documentos/01 Gestão", Cancellation);
        var first = await UploadAsync(documents.Id, "Contrato.pdf", "versão 1", SameNameAction.NewVersion);
        var second = await UploadAsync(documents.Id, "Contrato.pdf", "versão 2", SameNameAction.NewVersion);
        var projects = await Storage.FolderOfAsync(project.Id, Cancellation);

        var renamed = await Storage.RenameFolderAsync(project.Id, "ERP-FIN: Implantação do ERP", Cancellation);

        renamed.Should().Be(new StoredFolder(project.Id, "ERP-FIN_ Implantação do ERP"));
        (await NamesAsync(projects)).Should().Equal("ERP-FIN_ Implantação do ERP");
        (await Storage.EnsureFolderAsync(project.Id, "Documentos/01 Gestão", Cancellation)).Id.Should().Be(documents.Id);
        (await Storage.FolderOfAsync(second.Id, Cancellation)).Should().Be(documents.Id);
        second.Id.Should().Be(first.Id);
        await using (var current = await Storage.OpenAsync(second.Id, null, Cancellation))
        {
            (await ReadAsync(current!)).Should().Be("versão 2");
        }

        await using var previous = await Storage.OpenAsync(first.Id, first.VersionId, Cancellation);
        (await ReadAsync(previous!)).Should().Be("versão 1");
        var next = await UploadAsync(documents.Id, "Contrato.pdf", "versão 3", SameNameAction.NewVersion);
        next.Id.Should().Be(first.Id, "a new version after the rename is still the same file");
    }

    [Fact(DisplayName = "Nome já usado ao lado")]
    public async Task RenamingToANameInUse_ChangesNothing()
    {
        var first = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN - Implantação", Cancellation);
        var second = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN - Suporte", Cancellation);
        var projects = await Storage.FolderOfAsync(first.Id, Cancellation);
        await UploadAsync(projects!, "Leia-me.txt", "de fora", SameNameAction.KeepBoth);

        var toFolder = await Storage.RenameFolderAsync(first.Id, "erp-fin - suporte", Cancellation);
        var toFile = await Storage.RenameFolderAsync(second.Id, "Leia-me.txt", Cancellation);

        toFolder.Should().BeNull();
        toFile.Should().BeNull();
        (await NamesAsync(projects)).Should().Equal("ERP-FIN - Implantação", "ERP-FIN - Suporte");
    }

    [Fact(DisplayName = "Pasta que não existe")]
    public async Task RenamingAFolderThatIsNotThere_IsRefused()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN - Implantação", Cancellation);
        await Storage.DeleteFolderIfEmptyAsync(folder.Id, Cancellation);

        var gone = () => Storage.RenameFolderAsync(folder.Id, "Outro nome", Cancellation);
        var root = () => Storage.RenameFolderAsync(string.Empty, "Outro nome", Cancellation);

        await gone.Should().ThrowAsync<StorageFolderNotFoundException>();
        await root.Should().ThrowAsync<ArgumentException>();
        (await NamesAsync(null)).Should().Equal("Projetos");
    }

    [Fact(DisplayName = "Arquivo de fora fica")]
    public async Task AFileTheModuleDidNotStore_KeepsItsFolders()
    {
        var ticket = await Storage.EnsureFolderAsync(null, "ERP-FIN - Projeto/Chamados/#104", Cancellation);
        var stored = await UploadAsync(ticket.Id, "nota.xml", "<nfe/>", SameNameAction.KeepBoth);
        var project = await Storage.FolderOfAsync((await Storage.FolderOfAsync(ticket.Id, Cancellation))!, Cancellation);
        var outside = await UploadAsync(project!, "Planilha antiga.xlsx", "de fora", SameNameAction.KeepBoth);

        await Storage.RemoveAsync([stored.Id], [], Cancellation);

        (await NamesAsync(null)).Should().Equal("ERP-FIN - Projeto");
        (await NamesAsync(project)).Should().BeEmpty("the folders of the tickets were left empty");
        await using var kept = await Storage.OpenAsync(outside.Id, null, Cancellation);
        kept.Should().NotBeNull();
    }

    [Fact(DisplayName = "Nome com caracteres não aceitos no envio")]
    public async Task UploadedName_IsAdjusted()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos: 2026", Cancellation);

        var stored = await UploadAsync(folder.Id, "Relatório: março?.pdf ", "relatório", SameNameAction.NewVersion);
        var escaping = await UploadAsync(folder.Id, "../fora.txt", "fora", SameNameAction.NewVersion);

        folder.Name.Should().Be("Projetos_ 2026");
        stored.Name.Should().Be("Relatório_ março_.pdf");
        escaping.Name.Should().Be(".._fora.txt", "a name never leaves its folder");
        await using var download = await Storage.OpenAsync(escaping.Id, null, Cancellation);
        (await ReadAsync(download!)).Should().Be("fora");
    }

    [Fact(DisplayName = "Caminho que sai da raiz")]
    public async Task PathGoingOutOfTheRoot_IsRefused()
    {
        var ensure = () => Storage.EnsureFolderAsync(null, "../outra-pasta", Cancellation);
        var ensureInside = async () =>
            await Storage.EnsureFolderAsync((await Storage.EnsureFolderAsync(null, "Projetos", Cancellation)).Id, "x/../../y", Cancellation);

        await ensure.Should().ThrowAsync<ArgumentException>();
        await ensureInside.Should().ThrowAsync<ArgumentException>();
        (await NamesAsync(null)).Should().Equal("Projetos");
    }

    protected Task<StoredFile> UploadAsync(string folderId, string name, string text, SameNameAction sameName) =>
        Storage.UploadAsync(folderId, name, new MemoryStream(Encoding.UTF8.GetBytes(text)), sameName, Cancellation);

    protected async Task<List<string>> NamesAsync(string? folderId) =>
        [.. (await Storage.ListFoldersAsync(folderId, Cancellation)).Select(folder => folder.Name)];

    protected static async Task<string> ReadAsync(FileDownload download)
    {
        using var reader = new StreamReader(download.Content, Encoding.UTF8);
        return await reader.ReadToEndAsync(Cancellation);
    }
}
