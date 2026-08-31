using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Attendance.Contracts;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Attendance.GetDayRoster;

/// <summary>Who is in on a single day — the detail panel next to the grid.</summary>
public sealed record GetDayRosterQuery(Guid TeamId, DateOnly Date) : IQuery<DayRosterResponse>;

internal sealed class GetDayRosterQueryHandler(
    ITeamReadRepository teams,
    IAttendanceReadRepository attendance,
    ICurrentUser currentUser) : IQueryHandler<GetDayRosterQuery, DayRosterResponse>
{
    public async Task<Result<DayRosterResponse>> HandleAsync(GetDayRosterQuery query, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.RequireUserId();

        TeamRole? role = await teams.GetRoleAsync(query.TeamId, userId, cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure<DayRosterResponse>(TeamErrors.NotMember);
        }

        Result<DateRange> range = DateRange.Create(query.Date, query.Date);

        if (range.IsFailure)
        {
            return Result.Failure<DayRosterResponse>(range.Error);
        }

        IReadOnlyList<TeamMemberResponse> members = await teams
            .ListMembersAsync(query.TeamId, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AttendanceRecord> records = await attendance
            .ListForTeamAsync(query.TeamId, range.Value, cancellationToken)
            .ConfigureAwait(false);

        Dictionary<Guid, AttendanceRecord> byMember = records.ToDictionary(r => r.UserId);

        List<DayRosterEntryResponse> entries =
        [
            .. members.Select(member => byMember.TryGetValue(member.UserId, out AttendanceRecord? record)
                ? new DayRosterEntryResponse(member, record.Status, record.Note)
                : new DayRosterEntryResponse(member, null, null))
        ];

        DayTotalsResponse totals = AttendanceTotals.For(query.Date, members.Count, records.Select(r => r.Status));

        return new DayRosterResponse(query.TeamId, query.Date, totals, entries);
    }
}
