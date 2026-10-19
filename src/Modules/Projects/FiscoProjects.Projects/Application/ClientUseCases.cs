using FiscoProjects.Files.Contracts;
using FiscoProjects.Projects.Domain;

namespace FiscoProjects.Projects.Application;

/// <summary>Filters of the list of clients. Page starts at 1.</summary>
public sealed record ClientListQuery(string? Search, bool? IsActive, int Page, int PageSize)
{
    public const int MaxPageSize = 100;
}

public sealed record ClientListItem(
    Guid Id, string Name, string? Cnpj, string? Observation, bool IsActive, DateTime? LogoChangedAt);

public sealed record ClientListPage(IReadOnlyList<ClientListItem> Items, int Total);

/// <summary>Another client already has the CNPJ: the unique index refused the save.</summary>
public sealed class ClientCnpjInUseException(Exception inner) : Exception("Another client has the CNPJ.", inner);

/// <summary>Where the clients are kept.</summary>
public interface IClientStore
{
    /// <summary>
    /// The clients in alphabetical order: the name without telling case or accents apart, or the CNPJ without its
    /// punctuation.
    /// </summary>
    Task<ClientListPage> ListAsync(ClientListQuery query, CancellationToken cancellationToken);

    /// <summary>The client, tracked, so a change is saved by <see cref="SaveChangesAsync"/>.</summary>
    Task<Client?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The name of the client with the CNPJ, other than the one given.</summary>
    Task<string?> NameOfClientWithCnpjAsync(string cnpj, Guid? except, CancellationToken cancellationToken);

    void Add(Client client);

    void Remove(Client client);

    /// <exception cref="ClientCnpjInUseException">Another client got the same CNPJ meanwhile.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>The clients of the installation, in alphabetical order (ADR-0015).</summary>
public sealed class ListClients(IClientStore store)
{
    public Task<ClientListPage> ExecuteAsync(ClientListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListAsync(
            query with { Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim() },
            cancellationToken);
    }
}

/// <summary>A field of the client form.</summary>
public enum ClientField
{
    Name = 1,
    Cnpj = 2,
    Observation = 3,
}

/// <summary>The client form: the name, the CNPJ (with or without punctuation) and the observation.</summary>
public sealed record ClientChanges(string? Name, string? Cnpj, string? Observation);

public enum ClientSaveOutcome
{
    Saved = 1,
    Invalid = 2,
    CnpjInUse = 3,
    NotFound = 4,
}

/// <summary>What happened to the form: the id of the client saved, the invalid fields or who has the CNPJ.</summary>
public sealed record ClientSaveResult(
    ClientSaveOutcome Outcome, Guid? Id, IReadOnlyList<ClientField> Invalid, string? CnpjInUseBy)
{
    public static ClientSaveResult Saved(Guid id) => new(ClientSaveOutcome.Saved, id, [], null);

    public static ClientSaveResult NotFound() => new(ClientSaveOutcome.NotFound, null, [], null);

    public static ClientSaveResult InvalidFields(IReadOnlyList<ClientField> fields) =>
        new(ClientSaveOutcome.Invalid, null, fields, null);

    public static ClientSaveResult CnpjInUse(string clientName) =>
        new(ClientSaveOutcome.CnpjInUse, null, [], clientName);
}

/// <summary>The form checked: the values as they are kept, or the invalid fields.</summary>
internal sealed record CheckedClient(string Name, string? Cnpj, string? Observation, IReadOnlyList<ClientField> Invalid)
{
    public static CheckedClient Of(ClientChanges changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        var invalid = new List<ClientField>();
        var name = Client.ValidName(changes.Name);
        if (name is null)
        {
            invalid.Add(ClientField.Name);
        }

        if (!Domain.Cnpj.TryNormalize(changes.Cnpj, out var cnpj))
        {
            invalid.Add(ClientField.Cnpj);
        }

        if (!Client.TryNormalizeObservation(changes.Observation, out var observation))
        {
            invalid.Add(ClientField.Observation);
        }

        return new CheckedClient(name ?? string.Empty, cnpj, observation, invalid);
    }
}

/// <summary>Saves the client, or tells who has the CNPJ when the database refuses it at the last moment.</summary>
internal static class ClientSaving
{
    public static async Task<ClientSaveResult> SaveAsync(
        IClientStore store, Client client, CancellationToken cancellationToken)
    {
        try
        {
            await store.SaveChangesAsync(cancellationToken);
            return ClientSaveResult.Saved(client.Id);
        }
        catch (ClientCnpjInUseException)
        {
            var owner = client.Cnpj is null ? null : await store.NameOfClientWithCnpjAsync(client.Cnpj, client.Id, cancellationToken);
            return ClientSaveResult.CnpjInUse(owner ?? string.Empty);
        }
    }
}

/// <summary>
/// Includes a client, active and without a logo (ADR-0015). The Administrator and the Project manager do it, the
/// latter when creating a project (ADR-0046).
/// </summary>
public sealed class AddClient(IClientStore store)
{
    public async Task<ClientSaveResult> ExecuteAsync(ClientChanges changes, CancellationToken cancellationToken)
    {
        var form = CheckedClient.Of(changes);
        if (form.Invalid.Count > 0)
        {
            return ClientSaveResult.InvalidFields(form.Invalid);
        }

        if (form.Cnpj is { } cnpj && await store.NameOfClientWithCnpjAsync(cnpj, null, cancellationToken) is { } owner)
        {
            return ClientSaveResult.CnpjInUse(owner);
        }

        var client = Client.Create(form.Name, form.Cnpj, form.Observation);
        store.Add(client);
        return await ClientSaving.SaveAsync(store, client, cancellationToken);
    }
}

/// <summary>Changes the name, the CNPJ and the observation of a client; only the Administrator (ADR-0046).</summary>
public sealed class ChangeClient(IClientStore store)
{
    public async Task<ClientSaveResult> ExecuteAsync(Guid id, ClientChanges changes, CancellationToken cancellationToken)
    {
        var client = await store.FindAsync(id, cancellationToken);
        if (client is null)
        {
            return ClientSaveResult.NotFound();
        }

        var form = CheckedClient.Of(changes);
        if (form.Invalid.Count > 0)
        {
            return ClientSaveResult.InvalidFields(form.Invalid);
        }

        if (form.Cnpj is { } cnpj && await store.NameOfClientWithCnpjAsync(cnpj, id, cancellationToken) is { } owner)
        {
            return ClientSaveResult.CnpjInUse(owner);
        }

        client.Change(form.Name, form.Cnpj, form.Observation);
        return await ClientSaving.SaveAsync(store, client, cancellationToken);
    }
}

/// <summary>Makes a client inactive; it stays in the list, dimmed.</summary>
public sealed class DeactivateClient(IClientStore store)
{
    /// <returns>False for a client that does not exist.</returns>
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await store.FindAsync(id, cancellationToken);
        if (client is null)
        {
            return false;
        }

        client.Deactivate();
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>Makes an inactive client active again.</summary>
public sealed class ReactivateClient(IClientStore store)
{
    /// <returns>False for a client that does not exist.</returns>
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await store.FindAsync(id, cancellationToken);
        if (client is null)
        {
            return false;
        }

        client.Reactivate();
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>
/// Removes a client: the record first, then the file of the logo. A file that cannot be removed stays behind in the
/// folder of the system, with no effect on the application.
/// </summary>
public sealed class RemoveClient(IClientStore store, IFileStorage files)
{
    /// <returns>False for a client that does not exist.</returns>
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await store.FindAsync(id, cancellationToken);
        if (client is null)
        {
            return false;
        }

        var logo = client.LogoFileId;
        store.Remove(client);
        await store.SaveChangesAsync(cancellationToken);
        if (logo is not null)
        {
            await files.DeleteFileAsync(logo, CancellationToken.None);
        }

        return true;
    }
}

/// <summary>The logo to serve: the file, its type and when it changed.</summary>
public sealed record ClientLogoDownload(FileDownload File, string ContentType, DateTime ChangedAt);

public sealed class GetClientLogo(IClientStore store, IFileStorage files)
{
    /// <returns>Null for a client without a logo, one that does not exist, or a file gone from the storage.</returns>
    public async Task<ClientLogoDownload?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await store.FindAsync(id, cancellationToken);
        if (client is not { LogoFileId: { } fileId, LogoContentType: { } contentType, LogoChangedAt: { } changedAt })
        {
            return null;
        }

        var file = await files.OpenAsync(fileId, null, cancellationToken);
        return file is null ? null : new ClientLogoDownload(file, contentType, changedAt);
    }
}

public enum ClientLogoOutcome
{
    Saved = 1,
    NotFound = 2,
    WrongFormat = 3,
    TooLarge = 4,
}

/// <summary>
/// Keeps a new logo of a client (ADR-0015, ADR-0030): checked by its content, stored in the folder of the system, and
/// only then recorded. The file of a logo of another format is removed after the record is saved.
/// </summary>
public sealed class ReplaceClientLogo(IClientStore store, IFileStorage files, TimeProvider clock)
{
    /// <summary>Folder of the files of the system itself, under the root of the storage (as the installation logo).</summary>
    public const string Folder = "FiscoProjects";

    private const int FileNameMaxLength = 200;

    public async Task<ClientLogoOutcome> ExecuteAsync(
        Guid id, string? fileName, Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var client = await store.FindAsync(id, cancellationToken);
        if (client is null)
        {
            return ClientLogoOutcome.NotFound;
        }

        var bytes = await ReadAtMostAsync(content, ClientLogo.MaxBytes + 1, cancellationToken);
        var check = ClientLogo.Check(bytes);
        if (check.Format is not { } format)
        {
            return check.Refusal == ClientLogoRefusal.TooLarge ? ClientLogoOutcome.TooLarge : ClientLogoOutcome.WrongFormat;
        }

        var previous = client.LogoFileId;
        var folder = await files.EnsureFolderAsync(null, Folder, cancellationToken);
        await using var stream = new MemoryStream(bytes, writable: false);
        var stored = await files.UploadAsync(
            folder.Id, $"client-{client.Id}.{format.Extension}", stream, SameNameAction.NewVersion, cancellationToken);

        client.ReplaceLogo(stored.Id, SentName(fileName, stored.Name), format.ContentType, clock.GetUtcNow().UtcDateTime);
        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Nothing recorded points to the new file: remove it, unless it is the same file with a new version.
            if (stored.Id != previous)
            {
                await files.DeleteFileAsync(stored.Id, CancellationToken.None);
            }

            throw;
        }

        if (previous is not null && previous != stored.Id)
        {
            await files.DeleteFileAsync(previous, cancellationToken);
        }

        return ClientLogoOutcome.Saved;
    }

    private static string SentName(string? fileName, string storedName)
    {
        // Some browsers send the whole path, with the separators of the computer of the person.
        var name = (fileName ?? string.Empty).Split('/', '\\').Last().Trim();
        if (name.Length == 0)
        {
            return storedName;
        }

        return name.Length <= FileNameMaxLength ? name : name[..FileNameMaxLength];
    }

    private static async Task<byte[]> ReadAtMostAsync(Stream content, int limit, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while (buffer.Length < limit
               && (read = await content.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, limit - buffer.Length)), cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}

/// <summary>Removes the logo of a client: the record first, then the file.</summary>
public sealed class RemoveClientLogo(IClientStore store, IFileStorage files)
{
    /// <returns>False for a client that does not exist.</returns>
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await store.FindAsync(id, cancellationToken);
        if (client is null)
        {
            return false;
        }

        if (client.LogoFileId is { } fileId)
        {
            client.RemoveLogo();
            await store.SaveChangesAsync(cancellationToken);
            await files.DeleteFileAsync(fileId, cancellationToken);
        }

        return true;
    }
}
