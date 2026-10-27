using System.Text;
using FiscoProjects.Files.Contracts;
using FiscoProjects.Files.Infrastructure;

namespace FiscoProjects.Files.Tests;

/// <summary>The local implementation, in a folder of its own for each test, against the contract and the disk.</summary>
public sealed class LocalFileStorageTests : FileStorageContract, IDisposable
{
    private readonly string _parent = Path.Combine(Path.GetTempPath(), $"fiscoprojects-files-{Guid.NewGuid():N}");
    private readonly string _root;

    public LocalFileStorageTests()
    {
        _root = Path.Combine(_parent, "root");
        Storage = new LocalFileStorage(_root);
    }

    protected override IFileStorage Storage { get; }

    [Fact(DisplayName = "Id que sai da raiz no disco")]
    public async Task IdsOutsideTheRoot_AreRefused_AndNothingOutsideIsTouched()
    {
        var outside = Path.Combine(_parent, "fora.txt");
        await File.WriteAllTextAsync(outside, "fora da raiz", Cancellation);

        string[] ids = ["../fora.txt", "Projetos/../../fora.txt", outside, @"..\fora.txt", ".fiscoprojects/versions/x"];
        foreach (var id in ids)
        {
            var open = () => Storage.OpenAsync(id, null, Cancellation);
            var delete = () => Storage.DeleteFileAsync(id, Cancellation);
            var deleteFolder = () => Storage.DeleteFolderIfEmptyAsync(id, Cancellation);
            var upload = () => UploadAsync(id, "x.txt", "x", SameNameAction.NewVersion);
            await open.Should().ThrowAsync<ArgumentException>(id);
            await delete.Should().ThrowAsync<ArgumentException>(id);
            await deleteFolder.Should().ThrowAsync<ArgumentException>(id);
            await upload.Should().ThrowAsync<ArgumentException>(id);
        }

        (await File.ReadAllTextAsync(outside, Cancellation)).Should().Be("fora da raiz");
        Directory.EnumerateFileSystemEntries(_parent).Select(Path.GetFileName).Should().BeEquivalentTo(["root", "fora.txt"]);
    }

    [Fact(DisplayName = "Ids antigos da implementação local")]
    public async Task IdsGivenBefore_StillWork_EvenAfterARename()
    {
        // As the storage left them before the ids were written down: the id of each item was its path.
        Directory.CreateDirectory(Path.Combine(_root, "ERP-FIN - Implantação", "Documentos"));
        await File.WriteAllTextAsync(Path.Combine(_root, "ERP-FIN - Implantação", "Documentos", "Contrato.pdf"), "antes", Cancellation);
        const string project = "ERP-FIN - Implantação";
        const string documents = "ERP-FIN - Implantação/Documentos";
        const string contract = "ERP-FIN - Implantação/Documentos/Contrato.pdf";

        (await Storage.EnsureFolderAsync(null, project, Cancellation)).Id.Should().Be(project);
        (await UploadAsync(documents, "Contrato.pdf", "depois", SameNameAction.NewVersion)).Id.Should().Be(contract);
        await Storage.RenameFolderAsync(project, "ERP-FIN - Implantação do ERP", Cancellation);
        var restarted = new LocalFileStorage(_root);

        await using (var renamed = await restarted.OpenAsync(contract, null, Cancellation))
        {
            (await ReadAsync(renamed!)).Should().Be("depois");
        }

        await using (var previous = await restarted.OpenAsync(contract, "1", Cancellation))
        {
            (await ReadAsync(previous!)).Should().Be("antes");
        }

        (await restarted.FolderOfAsync(contract, Cancellation)).Should().Be(documents);
        (await restarted.EnsureFolderAsync(project, "Documentos", Cancellation)).Id.Should().Be(documents);
        Directory.Exists(Path.Combine(_root, "ERP-FIN - Implantação do ERP", "Documentos")).Should().BeTrue();
        Directory.Exists(Path.Combine(_root, "ERP-FIN - Implantação")).Should().BeFalse();
    }

    [Fact(DisplayName = "Arquivos na pasta configurada")]
    public async Task Files_LiveInTheConfiguredFolder()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos/ERP-FIN", Cancellation);

        await UploadAsync(folder.Id, "Contrato.pdf", "contrato", SameNameAction.NewVersion);

        (await File.ReadAllTextAsync(Path.Combine(_root, "Projetos", "ERP-FIN", "Contrato.pdf"), Cancellation)).Should().Be("contrato");
    }

    [Fact(DisplayName = "Envio interrompido não deixa arquivo")]
    public async Task InterruptedUpload_LeavesNoFile()
    {
        var folder = await Storage.EnsureFolderAsync(null, "Projetos", Cancellation);

        var upload = () => Storage.UploadAsync(folder.Id, "Grande.zip", new FailingStream(), SameNameAction.NewVersion, Cancellation);

        await upload.Should().ThrowAsync<IOException>();
        Directory.EnumerateFiles(Path.Combine(_root, "Projetos")).Should().BeEmpty();
        Directory.EnumerateFiles(Path.Combine(_root, ".fiscoprojects"), "*", SearchOption.AllDirectories).Select(Path.GetFileName)
            .Should().Equal(["ids.json"], "the temporary file is removed too, and only the ids stay");
    }

    public void Dispose()
    {
        if (Directory.Exists(_parent))
        {
            Directory.Delete(_parent, recursive: true);
        }
    }

    /// <summary>A content that breaks after the first bytes, as a connection that drops.</summary>
    private sealed class FailingStream : Stream
    {
        private bool _sent;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_sent)
            {
                throw new IOException("Connection lost.");
            }

            _sent = true;
            Encoding.UTF8.GetBytes("começo").CopyTo(buffer, offset);
            return Encoding.UTF8.GetByteCount("começo");
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
