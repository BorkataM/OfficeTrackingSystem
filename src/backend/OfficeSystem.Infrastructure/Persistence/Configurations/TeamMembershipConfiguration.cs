using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OfficeSystem.Domain.Teams;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Infrastructure.Persistence.Configurations;

internal sealed class TeamMembershipConfiguration : IEntityTypeConfiguration<TeamMembership>
{
    public void Configure(EntityTypeBuilder<TeamMembership> builder)
    {
        builder.ToTable("team_memberships");

        builder.HasKey(m => m.Id);

        // A person holds at most one role per team.
        builder.HasIndex(m => new { m.TeamId, m.UserId }).IsUnique();

        builder.Property(m => m.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.JoinedAtUtc).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
