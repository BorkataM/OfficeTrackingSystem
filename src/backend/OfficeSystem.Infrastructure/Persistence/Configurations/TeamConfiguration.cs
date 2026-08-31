using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(Team.NameMaxLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(Team.DescriptionMaxLength);

        builder.Property(t => t.JoinCode)
            .HasConversion(code => code.Value, value => JoinCode.Create(value).Value)
            .HasMaxLength(JoinCode.Length)
            .IsRequired();

        builder.HasIndex(t => t.JoinCode).IsUnique();

        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.HasMany(t => t.Memberships)
            .WithOne()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Memberships).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
