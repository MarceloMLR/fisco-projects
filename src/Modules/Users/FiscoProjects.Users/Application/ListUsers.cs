using FiscoProjects.Users.Contracts;

namespace FiscoProjects.Users.Application;

/// <summary>Where a person stands: with access, deactivated (ADR-0008) or anonymized, for good (ADR-0110).</summary>
public enum UserState
{
    Active,
    Inactive,
    Anonymized,
}

/// <summary>Filters of the Users screen (ADR-0008). Page starts at 1.</summary>
public sealed record UserListQuery(string? Search, SystemRole? Role, UserState? State, int Page, int PageSize)
{
    public const int MaxPageSize = 100;
}

public sealed record UserListItem(
    Guid Id, string Name, string Email, SystemRole Role, bool IsActive, bool IsAnonymized, DateTime? LastSignInAt)
{
    public UserState State => IsAnonymized ? UserState.Anonymized : IsActive ? UserState.Active : UserState.Inactive;
}

public sealed record UserListPage(IReadOnlyList<UserListItem> Items, int Total);

/// <summary>The people of the installation, in alphabetical order, for the Users screen.</summary>
public sealed class ListUsers(IUserStore store)
{
    public Task<UserListPage> ExecuteAsync(UserListQuery query, CancellationToken cancellationToken) =>
        store.ListAsync(query with { Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim() }, cancellationToken);
}
