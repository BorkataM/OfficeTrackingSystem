using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Application.Features.Attendance.Contracts;
using OfficeSystem.Domain.Attendance;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Attendance.SetAttendance;

internal sealed class SetAttendanceCommandHandler(
    IAttendanceRepository attendance,
    ITeamReadRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<SetAttendanceCommand, IReadOnlyList<DayPlanResponse>>
{
    public async Task<Result<IReadOnlyList<DayPlanResponse>>> HandleAsync(
        SetAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        Guid userId = currentUser.RequireUserId();

        TeamRole? role = await teams.GetRoleAsync(command.TeamId, userId, cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure<IReadOnlyList<DayPlanResponse>>(TeamErrors.NotMember);
        }

        DateOnly today = clock.Today;
        List<DateOnly> dates = command.Days.Select(d => d.Date).ToList();

        // One round trip for every day being touched, then upsert in memory.
        IReadOnlyList<AttendanceEntry> existing = await attendance
            .ListForUserAsync(command.TeamId, userId, dates, cancellationToken)
            .ConfigureAwait(false);

        Dictionary<DateOnly, AttendanceEntry> byDate = existing.ToDictionary(e => e.Date);
        List<AttendanceEntry> touched = new(command.Days.Count);

        foreach (DayPlanInput day in command.Days)
        {
            if (byDate.TryGetValue(day.Date, out AttendanceEntry? entry))
            {
                Result updated = entry.Update(day.Status, day.Note, today, clock.UtcNow);

                if (updated.IsFailure)
                {
                    return Result.Failure<IReadOnlyList<DayPlanResponse>>(updated.Error);
                }
            }
            else
            {
                Result<AttendanceEntry> created = AttendanceEntry.Create(
                    command.TeamId,
                    userId,
                    day.Date,
                    day.Status,
                    day.Note,
                    today,
                    clock.UtcNow);

                if (created.IsFailure)
                {
                    return Result.Failure<IReadOnlyList<DayPlanResponse>>(created.Error);
                }

                entry = created.Value;
                attendance.Add(entry);
            }

            touched.Add(entry);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<DayPlanResponse>>(
        [
            .. touched
                .OrderBy(e => e.Date)
                .Select(e => new DayPlanResponse(e.Date, e.Status, e.Note, e.UpdatedAtUtc))
        ]);
    }
}
