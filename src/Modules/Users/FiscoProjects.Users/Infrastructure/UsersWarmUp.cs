using FiscoProjects.SharedKernel;
using FiscoProjects.Users.Application;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FiscoProjects.Users.Infrastructure;

/// <summary>
/// The reads of the session (ADR-0106), on every request and screen, as the person who signed in last sees them: who is
/// in the session, the access of the routes and the profile.
/// </summary>
internal sealed class UsersWarmUp(UsersDbContext db, GetSignedInUser signedIn, IUserAccess access, GetProfile profile) : IWarmUp
{
    public string Module => "Users";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var userId = await db.Users.AsNoTracking()
            .Where(user => user.IsActive)
            .OrderByDescending(user => user.LastSignInAt)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId is not { } person)
        {
            return;
        }

        await signedIn.ExecuteAsync(person, cancellationToken);
        await access.FindAsync(person, cancellationToken);
        await profile.ExecuteAsync(person, cancellationToken);
    }
}
