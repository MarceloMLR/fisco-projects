using GerenWare.Audit.Application;
using GerenWare.Audit.Contracts;
using GerenWare.Audit.Infrastructure.Persistence;
using GerenWare.Persistence;
using GerenWare.Persistence.Auditing;
using GerenWare.Persistence.PersonalData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GerenWare.Audit.Infrastructure;

/// <summary>
/// Services of the Audit module: the writer every module save calls, the security events, the listing for the Audit
/// screen and its export (ADR-0039, ADR-0099), the erasure of an anonymized person and the monthly cleanup (ADR-0110).
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
        // LGPD (ADR-0058, ADR-0110): the erasure of an anonymized person and the monthly cleanup.
        services.AddScoped<IPersonalDataEraser, AuditPersonalData>();
        services.AddScoped<CleanUpAudit>();
        services.AddHostedService<AuditCleanup>();
        return services;
    }
}
