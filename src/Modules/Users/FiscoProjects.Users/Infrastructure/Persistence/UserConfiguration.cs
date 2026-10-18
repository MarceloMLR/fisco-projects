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
    }
}
