using FiscoProjects.Credentials.Domain;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Credentials.Application;

/// <summary>
/// Protects the passwords of the credentials and reads them back (ADR-0033), so the use cases do not know how: a
/// password protected with an old or revoked key is still read.
/// </summary>
public interface IPasswordProtector
{
    string Protect(string password);

    string Unprotect(string protectedPassword);
}

/// <summary>Where the credentials are kept.</summary>
public interface ICredentialStore
{
    /// <summary>The credentials of the project, not tracked.</summary>
    Task<IReadOnlyList<PortalCredential>> ListAsync(Guid projectId, CancellationToken cancellationToken);

    /// <summary>The credential of the project, tracked.</summary>
    Task<PortalCredential?> FindAsync(Guid projectId, Guid id, CancellationToken cancellationToken);

    void Add(PortalCredential credential);

    void AddRange(IEnumerable<PortalCredential> credentials);

    void Remove(PortalCredential credential);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>An environment the credentials of the project use, with how many credentials it has.</summary>
public sealed record EnvironmentCount(string Name, int Count);

/// <summary>
/// The credentials of a project the person sees, without the passwords, after the search and the filter; the
/// environments, with how many credentials each has, and the total, without them; and what the person may do.
/// </summary>
public sealed record CredentialList(
    IReadOnlyList<PortalCredential> Items, IReadOnlyList<EnvironmentCount> Environments, int Total, CredentialPermissions Permissions);

/// <summary>A credential as the person fills it in; the password empty keeps the one a credential has.</summary>
public sealed record CredentialRequest(string? Portal, string? Environment, string? Url, string? Username, string? Password, string? Notes);

public enum CredentialOutcome
{
    Done = 1,
    NotFound = 2,
    Forbidden = 3,
    ProjectClosed = 4,
    Invalid = 5,
}

/// <summary>What happened to a credential: its id, or why nothing was saved, with the fields that are not valid.</summary>
public sealed record CredentialResult(CredentialOutcome Outcome, Guid? Id = null, IReadOnlyList<CredentialField>? Invalid = null)
{
    public static CredentialResult Of(CredentialOutcome outcome, Guid? id = null) => new(outcome, id);
}

/// <summary>The project and the permissions of a person in it, read once for each action (ADR-0045).</summary>
public sealed record CredentialArea(ProjectAccess Project, CredentialPermissions Permissions)
{
    /// <summary>The permissions, all false in a completed or cancelled project (ADR-0016).</summary>
    public CredentialPermissions Open => Project.IsClosed ? new(CanEdit: false, CanDelete: false) : Permissions;
}

/// <summary>Reads the project as the person sees it, with the permissions; null when the person does not see it (404).</summary>
public sealed class CredentialAreas(IProjectAccess projects)
{
    public async Task<CredentialArea?> OfAsync(Guid projectId, CredentialViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return await projects.FindAsync(projectId, viewer.UserId, viewer.IsAdministrator, cancellationToken) is { } project
            ? new CredentialArea(project, CredentialAccess.Of(viewer, project))
            : null;
    }

    /// <summary>
    /// Why the person may not change the credentials of the project: not seen (404), not allowed (403) or closed (409);
    /// null when the person may.
    /// </summary>
    public async Task<(CredentialArea? Area, CredentialResult? Refusal)> ForChangeAsync(
        Guid projectId, CredentialViewer viewer, Func<CredentialPermissions, bool> allowed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(allowed);
        if (await OfAsync(projectId, viewer, cancellationToken) is not { } area)
        {
            return (null, CredentialResult.Of(CredentialOutcome.NotFound));
        }

        if (!allowed(area.Permissions))
        {
            return (area, CredentialResult.Of(CredentialOutcome.Forbidden));
        }

        return area.Project.IsClosed ? (area, CredentialResult.Of(CredentialOutcome.ProjectClosed)) : (area, null);
    }
}

/// <summary>
/// The credentials of a project, without the passwords (ADR-0034): everyone in it and the Administrator sees them. The
/// search looks in the portal and in the user, without telling case or accents apart; a project has a few dozen
/// credentials, so they come all at once, in the order of the environment and of the portal.
/// </summary>
public sealed class ListCredentials(CredentialAreas areas, ICredentialStore credentials)
{
    public async Task<CredentialList?> ExecuteAsync(
        Guid projectId, string? search, string? environment, CredentialViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is not { } area)
        {
            return null;
        }

        var all = await credentials.ListAsync(projectId, cancellationToken);
        var text = SearchText.Simplify(search);
        var chosen = string.IsNullOrWhiteSpace(environment) ? null : environment.Trim();
        var found = all
            .Where(credential => chosen is null || string.Equals(credential.Environment, chosen, StringComparison.CurrentCultureIgnoreCase))
            .Where(credential => SearchText.Finds(credential.Portal, text) || SearchText.Finds(credential.Username, text))
            .OrderBy(credential => credential.Environment, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(credential => credential.Portal, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(credential => credential.Username, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(credential => credential.Id)
            .ToList();
        var environments = all
            .GroupBy(credential => credential.Environment, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => new EnvironmentCount(group.First().Environment, group.Count()))
            .OrderBy(count => count.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new(found, environments, all.Count, area.Open);
    }
}

/// <summary>
/// The password of a credential, for "Mostrar senha" and "Copiar senha" (ADR-0034): everyone who sees the project, also
/// a closed one; null when the person does not see it (404). Who asked is not recorded.
/// </summary>
public sealed class RevealPassword(CredentialAreas areas, ICredentialStore credentials, IPasswordProtector protector)
{
    public async Task<string?> ExecuteAsync(Guid projectId, Guid credentialId, CredentialViewer viewer, CancellationToken cancellationToken)
    {
        if (await areas.OfAsync(projectId, viewer, cancellationToken) is null
            || await credentials.FindAsync(projectId, credentialId, cancellationToken) is not { } credential)
        {
            return null;
        }

        return protector.Unprotect(credential.ProtectedPassword);
    }
}

/// <summary>Everyone in the project but the viewer includes a credential while the project is open (ADR-0016, ADR-0045).</summary>
public sealed class AddCredential(CredentialAreas areas, ICredentialStore credentials, IPasswordProtector protector, TimeProvider clock)
{
    public async Task<CredentialResult> ExecuteAsync(
        Guid projectId, CredentialRequest request, CredentialViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(viewer);
        var (_, refusal) = await areas.ForChangeAsync(projectId, viewer, permissions => permissions.CanEdit, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        var (form, invalid) = CredentialForm.Of(
            request.Portal, request.Environment, request.Url, request.Username, request.Password, request.Notes, passwordNeeded: true);
        if (form is null)
        {
            return new CredentialResult(CredentialOutcome.Invalid, null, invalid);
        }

        var credential = new PortalCredential(
            projectId, form, protector.Protect(form.Password!), viewer.UserId, clock.GetUtcNow().UtcDateTime);
        credentials.Add(credential);
        await credentials.SaveChangesAsync(cancellationToken);
        return CredentialResult.Of(CredentialOutcome.Done, credential.Id);
    }
}

/// <summary>
/// Everyone in the project but the viewer changes a credential while the project is open (ADR-0016, ADR-0045); the
/// password empty stays as it was.
/// </summary>
public sealed class ChangeCredential(CredentialAreas areas, ICredentialStore credentials, IPasswordProtector protector, TimeProvider clock)
{
    public async Task<CredentialResult> ExecuteAsync(
        Guid projectId, Guid credentialId, CredentialRequest request, CredentialViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(viewer);
        var (_, refusal) = await areas.ForChangeAsync(projectId, viewer, permissions => permissions.CanEdit, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        if (await credentials.FindAsync(projectId, credentialId, cancellationToken) is not { } credential)
        {
            return CredentialResult.Of(CredentialOutcome.NotFound);
        }

        var (form, invalid) = CredentialForm.Of(
            request.Portal, request.Environment, request.Url, request.Username, request.Password, request.Notes, passwordNeeded: false);
        if (form is null)
        {
            return new CredentialResult(CredentialOutcome.Invalid, null, invalid);
        }

        var password = form.Password is { } typed ? protector.Protect(typed) : null;
        credential.Change(form, password, viewer.UserId, clock.GetUtcNow().UtcDateTime);
        await credentials.SaveChangesAsync(cancellationToken);
        return CredentialResult.Of(CredentialOutcome.Done, credential.Id);
    }
}

/// <summary>The Administrator, the manager and the tech lead remove a credential while the project is open (ADR-0045).</summary>
public sealed class RemoveCredential(CredentialAreas areas, ICredentialStore credentials)
{
    public async Task<CredentialResult> ExecuteAsync(
        Guid projectId, Guid credentialId, CredentialViewer viewer, CancellationToken cancellationToken)
    {
        var (_, refusal) = await areas.ForChangeAsync(projectId, viewer, permissions => permissions.CanDelete, cancellationToken);
        if (refusal is not null)
        {
            return refusal;
        }

        if (await credentials.FindAsync(projectId, credentialId, cancellationToken) is not { } credential)
        {
            return CredentialResult.Of(CredentialOutcome.NotFound);
        }

        credentials.Remove(credential);
        await credentials.SaveChangesAsync(cancellationToken);
        return CredentialResult.Of(CredentialOutcome.Done, credential.Id);
    }
}
