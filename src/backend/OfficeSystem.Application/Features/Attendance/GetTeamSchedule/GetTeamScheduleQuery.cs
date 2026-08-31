using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Application.Features.Attendance.Contracts;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Attendance.GetTeamSchedule;

/// <summary>
/// The grid behind the main view. Omitting the range yields the ISO week that
/// contains today.
/// </summary>
public sealed record GetTeamScheduleQuery(Guid TeamId, DateOnly? From, DateOnly? To)
    : IQuery<TeamScheduleResponse>;

internal sealed class GetTeamScheduleQueryHandler(
    ITeamReadRepository teams,
    IAttendanceReadRepository attendance,
    ICurrentUser currentUser,
    IClock clock) : IQueryHandler<GetTeamScheduleQuery, TeamScheduleResponse>
{
    public async Task<Result<TeamScheduleResponse>> HandleAsync(GetTeamScheduleQuery query, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.RequireUserId();

        TeamRole? role = await teams.GetRoleAsync(query.TeamId, userId, cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure<TeamScheduleResponse>(TeamErrors.NotMember);
        }

        Result<DateRange> range = ResolveRange(query, clock.Today);

        if (range.IsFailure)
        {
            return Result.Failure<TeamScheduleResponse>(range.Error);
        }

        IReadOnlyList<TeamMemberResponse> members = await teams
            .ListMembersAsync(query.TeamId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AttendanceRecord> records = await attendance
            .ListForTeamAsync(query.TeamId, range.Value, cancellationToken)
            .ConfigureAwait(false);

        Dictionary<Guid, List<AttendanceRecord>> byMember = records
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Date).ToList());

        List<ScheduleRowResponse> rows = [];

        foreach (TeamMemberResponse member in members)
        {
            List<DayPlanResponse> days = byMember.TryGetValue(member.UserId, out List<AttendanceRecord>? own)
                ? [.. own.Select(r => new DayPlanResponse(r.Date, r.Status, r.Note, r.UpdatedAtUtc))]
                : [];

            rows.Add(new ScheduleRowResponse(member, days));
        }

        Dictionary<DateOnly, List<AttendanceRecord>> byDate = records
            .GroupBy(r => r.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        List<DateOnly> allDays = [.. range.Value.Days()];

        List<DayTotalsResponse> totals =
        [
            .. allDays.Select(day => AttendanceTotals.For(
                day,
                members.Count,
                byDate.TryGetValue(day, out List<AttendanceRecord>? onDay)
                    ? onDay.Select(r => r.Status)
                    : []))
        ];

        return new TeamScheduleResponse(
            query.TeamId,
            range.Value.Start,
            range.Value.End,
            clock.Today,
            allDays,
            rows,
            totals);
    }

    private static Result<DateRange> ResolveRange(GetTeamScheduleQuery query, DateOnly today)
        => (query.From, query.To) switch
        {
            (null, null) => DateRange.WeekOf(today),
            ({ } from, null) => DateRange.WeekOf(from),
            (null, { } to) => DateRange.WeekOf(to),
            ({ } from, { } to) => DateRange.Create(from, to)
        };
}
