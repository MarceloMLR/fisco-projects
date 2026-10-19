using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Projects.Infrastructure.Persistence;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public const string CnpjIndex = "IX_clients_Cnpj";

    public void Configure(EntityTypeBuilder<Client> client)
    {
        client.ToTable("clients");
        client.Property(c => c.Name).HasMaxLength(Client.NameMaxLength);
        client.Property(c => c.Cnpj).HasMaxLength(Cnpj.Length).IsFixedLength();
        client.Property(c => c.Observation).HasMaxLength(Client.ObservationMaxLength);
        client.Property(c => c.LogoFileId).HasMaxLength(400);
        client.Property(c => c.LogoFileName).HasMaxLength(200);
        client.Property(c => c.LogoContentType).HasMaxLength(64);
        client.Ignore(c => c.HasLogo);
        client.HasIndex(c => c.Name);
        // One client per CNPJ, active or inactive (ADR-0015); clients without one do not count.
        client.HasIndex(c => c.Cnpj).IsUnique().HasFilter("[Cnpj] IS NOT NULL").HasDatabaseName(CnpjIndex);

        // Audit (ADR-0039): the client is named by its name, and the logo by the name of the file sent; ids, type and
        // date of the logo are technical.
        client.HasAuditLabel(c => c.Name);
        client.Property(c => c.LogoFileId).ExcludeFromAudit();
        client.Property(c => c.LogoContentType).ExcludeFromAudit();
        client.Property(c => c.LogoChangedAt).ExcludeFromAudit();
    }
}
