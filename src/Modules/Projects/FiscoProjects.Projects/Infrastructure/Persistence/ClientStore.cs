using FiscoProjects.Projects.Application;
using FiscoProjects.Projects.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

internal sealed class ClientStore(ProjectsDbContext db) : IClientStore
{
    private const string AccentInsensitive = "Latin1_General_100_CI_AI";

    public async Task<ClientListPage> ListAsync(ClientListQuery query, CancellationToken cancellationToken)
    {
        var clients = db.Clients.AsNoTracking();
        if (query.Search is { } search)
        {
            // Without telling uppercase, lowercase and accents apart: "epsilon" finds "Épsilon". The CNPJ is found with
            // or without its punctuation.
            var cnpj = Cnpj.Clean(search);
            clients = cnpj.Length == 0
                ? clients.Where(client => EF.Functions.Collate(client.Name, AccentInsensitive).Contains(search))
                : clients.Where(client =>
                    EF.Functions.Collate(client.Name, AccentInsensitive).Contains(search)
                    || (client.Cnpj != null && client.Cnpj.Contains(cnpj)));
        }

        if (query.IsActive is { } isActive)
        {
            clients = clients.Where(client => client.IsActive == isActive);
        }

        var total = await clients.CountAsync(cancellationToken);
        var items = await clients
            .OrderBy(client => client.Name).ThenBy(client => client.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(client => new ClientListItem(
                client.Id, client.Name, client.Cnpj, client.Observation, client.IsActive,
                client.LogoFileId == null ? null : client.LogoChangedAt))
            .ToListAsync(cancellationToken);
        return new ClientListPage(items, total);
    }

    public async Task<IReadOnlyList<ClientChoice>> ChoicesAsync(CancellationToken cancellationToken) =>
        await db.Clients.AsNoTracking()
            .OrderBy(client => client.Name).ThenBy(client => client.Id)
            .Select(client => new ClientChoice(
                client.Id, client.Name, client.IsActive, client.LogoFileId == null ? null : client.LogoChangedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.Clients.AsNoTracking()
            .Where(client => ids.Contains(client.Id))
            .ToDictionaryAsync(client => client.Id, client => client.Name, cancellationToken);

    public Task<Client?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Clients.SingleOrDefaultAsync(client => client.Id == id, cancellationToken);

    public Task<string?> NameOfClientWithCnpjAsync(string cnpj, Guid? except, CancellationToken cancellationToken) =>
        db.Clients.AsNoTracking()
            .Where(client => client.Cnpj == cnpj && client.Id != except)
            .Select(client => client.Name)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(Client client) => db.Clients.Add(client);

    public void Remove(Client client) => db.Clients.Remove(client);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 } sql
            && sql.Message.Contains(ClientConfiguration.CnpjIndex, StringComparison.Ordinal))
        {
            // The changes stay as they were tried; the request ends here, with its own context.
            throw new ClientCnpjInUseException(exception);
        }
    }
}
