using FiscoProjects.Credentials.Domain;
using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Credentials.Infrastructure.Persistence;

// The credentials of the projects (ADR-0034): in the audit by the portal, with the project in every record; the
// password only as changed, never its value (ADR-0033, ADR-0039); who included or changed it and when stay out, as
// the audit records them.
internal sealed class PortalCredentialConfiguration : IEntityTypeConfiguration<PortalCredential>
{
    public void Configure(EntityTypeBuilder<PortalCredential> credential)
    {
        credential.ToTable("portal_credentials");
        credential.Property(c => c.Portal).HasMaxLength(PortalCredential.PortalMaxLength);
        credential.Property(c => c.Environment).HasMaxLength(PortalCredential.EnvironmentMaxLength);
        credential.Property(c => c.Url).HasMaxLength(PortalCredential.UrlMaxLength);
        credential.Property(c => c.Username).HasMaxLength(PortalCredential.UsernameMaxLength);
        credential.Property(c => c.Notes).HasMaxLength(PortalCredential.NotesMaxLength);
        credential.HasIndex(c => c.ProjectId);

        credential.HasAuditLabel(c => c.Portal);
        credential.Property(c => c.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        credential.Property(c => c.ProtectedPassword).IsAuditSecret();
        credential.Property(c => c.CreatedById).ExcludeFromAudit();
        credential.Property(c => c.CreatedAt).ExcludeFromAudit();
        credential.Property(c => c.UpdatedById).ExcludeFromAudit();
        credential.Property(c => c.UpdatedAt).ExcludeFromAudit();
    }
}

/// <summary>A key of the Data Protection of the credentials, as the XML it keeps (ADR-0033); never removed.</summary>
internal sealed class CredentialKey
{
    public int Id { get; init; }

    public string? FriendlyName { get; init; }

    public string Xml { get; init; } = string.Empty;
}

// The keys: technical, out of the audit, with nothing that tells a person.
internal sealed class CredentialKeyConfiguration : IEntityTypeConfiguration<CredentialKey>
{
    public void Configure(EntityTypeBuilder<CredentialKey> key)
    {
        key.ToTable("data_protection_keys");
        key.Property(k => k.FriendlyName).HasMaxLength(200);
        key.ExcludeFromAudit();
    }
}
