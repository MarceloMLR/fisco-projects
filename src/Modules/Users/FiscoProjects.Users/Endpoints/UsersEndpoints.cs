using Microsoft.AspNetCore.Routing;

namespace FiscoProjects.Users.Endpoints;

/// <summary>Routes of the Users module.</summary>
public static class UsersEndpoints
{
    public static RouteGroupBuilder MapUsersEndpoints(this RouteGroupBuilder api)
    {
        api.MapSessionEndpoints();
        api.MapProfileEndpoints();
        api.MapUserAdministrationEndpoints();
        return api;
    }
}
