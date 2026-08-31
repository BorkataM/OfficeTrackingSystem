using Microsoft.EntityFrameworkCore;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Domain.Attendance;
using OfficeSystem.Domain.Teams;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Infrastructure.Persistence;

public sealed class OfficeSystemDbContext(DbContextOptions<OfficeSystemDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();

    public DbSet<AttendanceEntry> AttendanceEntries => Set<AttendanceEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OfficeSystemDbContext).Assembly);
        modelBuilder.UseDomainGeneratedKeys();
        modelBuilder.ApplySnakeCaseNames();
    }
}
