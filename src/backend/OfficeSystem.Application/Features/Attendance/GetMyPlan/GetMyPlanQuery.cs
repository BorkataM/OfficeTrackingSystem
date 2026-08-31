using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Application.Features.Attendance.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Attendance.GetMyPlan;

/// <summary>The caller's own plan across a range — feeds the month calendar.</summary>
public sealed record GetMyPlanQuery(Guid TeamId, DateOnly? From, DateOnly? To)
    : IQuery<IReadOnlyList<DayPlanResponse>>;

internal sealed class GetMyPlanQueryHandler(
    ITeamReadRepository teams,
    IAttendanceReadRepository attendance,
    ICurrentUser currentUser,
    IClock clock) : IQueryHandler<GetMyPlanQuery, IReadOnlyList<DayPlanResponse>>
{
    public async Task<Result<IReadOnlyList<DayPlanResponse>>> HandleAsync(GetMyPlanQuery query, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.RequireUserId();

        TeamRole? role = await teams.GetRoleAsync(query.TeamId, userId, cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure<IReadOnlyList<DayPlanResponse>>(TeamErrors.NotMember);
        }

        Result<DateRange> range = query is { From: { } from, To: { } to }
            ? DateRange.Create(from, to)
            : DateRange.MonthOf(query.From ?? query.To ?? clock.Today);

        if (range.IsFailure)
        {
            return Result.Failure<IReadOnlyList<DayPlanResponse>>(range.Error);
        }

        IReadOnlyList<AttendanceRecord> records = await attendance
            .ListForUserAsync(query.TeamId, userId, range.Value, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<DayPlanResponse>>(
        [
            .. records
                .OrderBy(r => r.Date)
                .Select(r => new DayPlanResponse(r.Date, r.Status, r.Note, r.UpdatedAtUtc))
        ]);
    }
}
