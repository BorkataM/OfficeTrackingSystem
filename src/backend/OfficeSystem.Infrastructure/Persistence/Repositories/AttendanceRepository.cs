using Microsoft.EntityFrameworkCore;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Domain.Attendance;

namespace OfficeSystem.Infrastructure.Persistence.Repositories;

internal sealed class AttendanceRepository(OfficeSystemDbContext context) : IAttendanceRepository
{
    public async Task<IReadOnlyList<AttendanceEntry>> ListForUserAsync(
        Guid teamId,
        Guid userId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dates);

        if (dates.Count == 0)
        {
            return [];
        }

        List<DateOnly> requested = [.. dates];

        return await context.AttendanceEntries
            .Where(e => e.TeamId == teamId && e.UserId == userId && requested.Contains(e.Date))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(AttendanceEntry entry) => context.AttendanceEntries.Add(entry);

    public void Remove(AttendanceEntry entry) => context.AttendanceEntries.Remove(entry);

    public async Task RemoveAllForMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        List<AttendanceEntry> entries = await context.AttendanceEntries
            .Where(e => e.TeamId == teamId && e.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        context.AttendanceEntries.RemoveRange(entries);
    }
}
