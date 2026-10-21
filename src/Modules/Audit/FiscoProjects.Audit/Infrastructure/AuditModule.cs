using FiscoProjects.Audit.Application;
using FiscoProjects.Audit.Contracts;
using FiscoProjects.Audit.Infrastructure.Persistence;
using FiscoProjects.Persistence;
using FiscoProjects.Persistence.Auditing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FiscoProjects.Audit.Infrastructure;

/// <summary>
/// Services of the Audit module: the writer every module save calls, the security events, the listing for the Audit
/// screen and its export (ADR-0039, ADR-0099).
/// </summary>
public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddModuleDbContext<AuditDbContext>(configuration);
        services.AddScoped<IAuditStore, AuditStore>();
        services.AddScoped<IChangeAuditor, ChangeAuditor>();
        services.AddScoped<ISecurityEvents, SecurityEventLog>();
        services.AddScoped<ListAuditEntries>();
        services.AddScoped<ExportAuditEntries>();
        return services;
    }
}
