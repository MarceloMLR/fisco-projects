using FiscoProjects.Administration.Application;
using FiscoProjects.Administration.Domain;

namespace FiscoProjects.Administration.Tests;

public sealed class ReplaceLogoFailureTests
{
    [Fact(DisplayName = "Arquivo excluído quando o banco falha")]
    public async Task WhenTheRecordIsNotSaved_TheNewFileIsRemoved()
    {
        var files = new MemoryFileStorage();
        var replace = new ReplaceInstallationLogo(new FailingStore(), files, TimeProvider.System);

        var send = () => replace.ExecuteAsync("marca.png", new MemoryStream(Logos.Png()), TestContext.Current.CancellationToken);

        await send.Should().ThrowAsync<InvalidOperationException>();
        files.Files.Should().BeEmpty();
    }

    private sealed class FailingStore : IInstallationSettingsStore
    {
        private readonly InstallationSettings _settings =
            (InstallationSettings)Activator.CreateInstance(typeof(InstallationSettings), nonPublic: true)!;

        public Task<InstallationSettings> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_settings);

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The database is out.");
    }
}
