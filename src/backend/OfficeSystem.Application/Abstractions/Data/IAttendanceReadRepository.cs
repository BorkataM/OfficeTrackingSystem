using OfficeSystem.Domain.Attendance;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Application.Abstractions.Data;

/// <summary>A single stored plan, flattened for the read side.</summary>
public sealed record AttendanceRecord(
    Guid UserId,
    DateOnly Date,
    AttendanceStatus Status,
    string? Note,
    DateTimeOffset UpdatedAtUtc);

public interface IAttendanceReadRepository
{
    Task<IReadOnlyList<AttendanceRecord>> ListForTeamAsync(
        Guid teamId,
        DateRange range,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceRecord>> ListForUserAsync(
        Guid teamId,
        Guid userId,
        DateRange range,
        CancellationToken cancellationToken = default);
}
