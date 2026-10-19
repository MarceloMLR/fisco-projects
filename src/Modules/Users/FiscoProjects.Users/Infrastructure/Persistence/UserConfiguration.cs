using FiscoProjects.Persistence.Auditing;
using FiscoProjects.Users.Contracts;
using FiscoProjects.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiscoProjects.Users.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> user)
    {
        user.ToTable("users");
        user.Property(u => u.Name).HasMaxLength(200);
        user.Property(u => u.Email).HasMaxLength(320);
        user.HasIndex(u => u.Email);
        // Stored as text; people who signed in before roles existed become User.
        user.Property(u => u.Role).HasConversion<string>().HasMaxLength(32).HasDefaultValue(SystemRole.User).HasSentinel(default(SystemRole));
        user.HasIndex(u => u.Name);
        // Stored as text; people who signed in before themes existed follow the operating system.
        user.Property(u => u.Theme).HasConversion<string>().HasMaxLength(16)
            .HasDefaultValue(ThemePreference.Automatic).HasSentinel(default(ThemePreference));

        // Audit (ADR-0039): the person is named by the name; the daily sign-in is a security event of its own,
        // and the creation date is the date of the creation record.
        user.HasAuditLabel(u => u.Name);
        user.Property(u => u.LastSignInAt).ExcludeFromAudit();
        user.Property(u => u.CreatedAt).ExcludeFromAudit();
        // A personal preference, not data of the business: changing it is no audit record.
        user.Property(u => u.Theme).ExcludeFromAudit();
    }
}

internal sealed class IdentityLinkConfiguration : IEntityTypeConfiguration<IdentityLink>
{
    public void Configure(EntityTypeBuilder<IdentityLink> link)
    {
        link.ToTable("identity_links");
        link.Property(l => l.Provider).HasMaxLength(32);
        link.Property(l => l.ExternalId).HasMaxLength(320);
        link.HasIndex(l => new { l.Provider, l.ExternalId }).IsUnique();
        link.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Cascade);
        // A technical link to the identity provider; the user creation is the record that matters.
        link.ExcludeFromAudit();
    }
}
