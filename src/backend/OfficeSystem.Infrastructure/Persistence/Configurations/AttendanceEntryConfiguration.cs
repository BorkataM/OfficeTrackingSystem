using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OfficeSystem.Domain.Attendance;
using OfficeSystem.Domain.Teams;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceEntryConfiguration : IEntityTypeConfiguration<AttendanceEntry>
{
    public void Configure(EntityTypeBuilder<AttendanceEntry> builder)
    {
        builder.ToTable("attendance_entries");

        builder.HasKey(e => e.Id);

        // The domain rule "one plan per person per day per team", enforced by the schema.
        builder.HasIndex(e => new { e.TeamId, e.UserId, e.Date }).IsUnique();

        // Covers the schedule grid query, which always filters team + date range.
        builder.HasIndex(e => new { e.TeamId, e.Date });

        builder.Property(e => e.Date).IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Note)
            .HasMaxLength(AttendanceEntry.NoteMaxLength);

        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
