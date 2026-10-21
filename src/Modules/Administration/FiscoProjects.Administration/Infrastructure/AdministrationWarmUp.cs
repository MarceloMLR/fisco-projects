using FiscoProjects.Administration.Application;
using FiscoProjects.SharedKernel;

namespace FiscoProjects.Administration.Infrastructure;

/// <summary>The reads of the brand and of the rules of the installation (ADR-0106), which every screen asks for.</summary>
internal sealed class AdministrationWarmUp(GetInstallation installation, GetGeneralRules rules) : IWarmUp
{
    public string Module => "Administration";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await installation.ExecuteAsync(cancellationToken);
        await rules.ExecuteAsync(cancellationToken);
    }
}
