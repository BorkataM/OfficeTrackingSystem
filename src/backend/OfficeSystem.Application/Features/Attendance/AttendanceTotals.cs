using OfficeSystem.Application.Features.Attendance.Contracts;
using OfficeSystem.Domain.Attendance;

namespace OfficeSystem.Application.Features.Attendance;

internal static class AttendanceTotals
{
    /// <summary>
    /// Rolls a day's statuses into the counters the UI shows. Members without an
    /// entry land in <c>NotPlanned</c> rather than being silently dropped.
    /// </summary>
    public static DayTotalsResponse For(DateOnly date, int memberCount, IEnumerable<AttendanceStatus> statuses)
    {
        int inOffice = 0;
        int remote = 0;
        int travelling = 0;
        int away = 0;

        foreach (AttendanceStatus status in statuses)
        {
            switch (status)
            {
                case AttendanceStatus.Office:
                    inOffice++;
                    break;
                case AttendanceStatus.Remote:
                    remote++;
                    break;
                case AttendanceStatus.Travelling:
                    travelling++;
                    break;
                case AttendanceStatus.Away:
                    away++;
                    break;
                default:
                    break;
            }
        }

        int planned = inOffice + remote + travelling + away;

        return new DayTotalsResponse(
            date,
            IsWeekend(date),
            inOffice,
            remote,
            travelling,
            away,
            Math.Max(0, memberCount - planned));
    }

    public static bool IsWeekend(DateOnly date)
        => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
