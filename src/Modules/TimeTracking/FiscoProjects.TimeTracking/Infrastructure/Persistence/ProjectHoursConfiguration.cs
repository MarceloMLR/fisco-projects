using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Projects.Contracts;
using FiscoProjects.TimeTracking.Domain;
using FiscoProjects.Users.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.TimeTracking.Infrastructure.Persistence;

// The contracted hours of a project (ADR-0128): one per project, in the audit by the current name of the project, which
// is also where the change happened; when they changed stays out, as the audit records it.
internal sealed class ContractedHoursConfiguration : IEntityTypeConfiguration<ContractedHours>
{
    public void Configure(EntityTypeBuilder<ContractedHours> contracted)
    {
        contracted.ToTable("contracted_hours");
        contracted.Property(c => c.Hours).HasPrecision(7, 2);
        contracted.HasIndex(c => c.ProjectId).IsUnique();

        contracted.HasAuditLabelReference(c => c.ProjectId, ProjectsAudit.ProjectReference);
        contracted.Property(c => c.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        contracted.Property(c => c.UpdatedAt).ExcludeFromAudit();
    }
}

// The additional hours of a project (ADR-0128): in the audit by the reason, with the project in every record and who
// registered them by the current name (ADR-0084). Only the id of the person is kept, so the anonymization has nothing
// to erase here.
internal sealed class AdditionalHoursConfiguration : IEntityTypeConfiguration<AdditionalHours>
{
    public void Configure(EntityTypeBuilder<AdditionalHours> additional)
    {
        additional.ToTable("additional_hours");
        additional.Property(a => a.Hours).HasPrecision(7, 2);
        additional.Property(a => a.Reason).HasMaxLength(AdditionalHours.ReasonMaxLength);
        additional.HasIndex(a => a.ProjectId);

        additional.HasAuditLabel(a => a.Reason);
        additional.Property(a => a.ProjectId).IsAuditReference(ProjectsAudit.ProjectReference).IsAuditContext();
        additional.Property(a => a.CreatedBy).IsAuditReference(UserAudit.Reference);
        additional.Property(a => a.CreatedAt).ExcludeFromAudit();
        additional.Property(a => a.UpdatedAt).ExcludeFromAudit();
    }
}
