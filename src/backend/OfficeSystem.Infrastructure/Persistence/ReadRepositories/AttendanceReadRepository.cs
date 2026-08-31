using Microsoft.EntityFrameworkCore;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Infrastructure.Persistence.ReadRepositories;

internal sealed class AttendanceReadRepository(OfficeSystemDbContext context) : IAttendanceReadRepository
{
    public async Task<IReadOnlyList<AttendanceRecord>> ListForTeamAsync(
        Guid teamId,
        DateRange range,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(range);

        DateOnly start = range.Start;
        DateOnly end = range.End;

        return await context.AttendanceEntries
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.Date >= start && e.Date <= end)
            .Select(e => new AttendanceRecord(e.UserId, e.Date, e.Status, e.Note, e.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AttendanceRecord>> ListForUserAsync(
        Guid teamId,
        Guid userId,
        DateRange range,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(range);

        DateOnly start = range.Start;
        DateOnly end = range.End;

        return await context.AttendanceEntries
            .AsNoTracking()
            .Where(e => e.TeamId == teamId && e.UserId == userId && e.Date >= start && e.Date <= end)
            .Select(e => new AttendanceRecord(e.UserId, e.Date, e.Status, e.Note, e.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
