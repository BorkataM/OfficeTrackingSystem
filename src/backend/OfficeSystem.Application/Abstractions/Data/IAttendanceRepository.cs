using OfficeSystem.Domain.Attendance;

namespace OfficeSystem.Application.Abstractions.Data;

/// <summary>Write side: loads and stores attendance aggregates.</summary>
public interface IAttendanceRepository
{
    Task<IReadOnlyList<AttendanceEntry>> ListForUserAsync(
        Guid teamId,
        Guid userId,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken = default);

    void Add(AttendanceEntry entry);

    void Remove(AttendanceEntry entry);

    /// <summary>Used when a person leaves a team, so their plans do not linger in the grid.</summary>
    Task RemoveAllForMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);
}
