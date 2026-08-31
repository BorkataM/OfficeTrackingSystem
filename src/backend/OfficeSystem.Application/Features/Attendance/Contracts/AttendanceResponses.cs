using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Attendance;

namespace OfficeSystem.Application.Features.Attendance.Contracts;

public sealed record DayPlanResponse(
    DateOnly Date,
    AttendanceStatus Status,
    string? Note,
    DateTimeOffset UpdatedAtUtc);

/// <summary>One row of the schedule grid: a person and their plan for each day in range.</summary>
public sealed record ScheduleRowResponse(
    TeamMemberResponse Member,
    IReadOnlyList<DayPlanResponse> Days);

/// <summary>Per-day totals, so the UI can show occupancy without recomputing it.</summary>
public sealed record DayTotalsResponse(
    DateOnly Date,
    bool IsWeekend,
    int InOffice,
    int Remote,
    int Travelling,
    int Away,
    int NotPlanned);

public sealed record TeamScheduleResponse(
    Guid TeamId,
    DateOnly Start,
    DateOnly End,
    DateOnly Today,
    IReadOnlyList<DateOnly> Days,
    IReadOnlyList<ScheduleRowResponse> Rows,
    IReadOnlyList<DayTotalsResponse> Totals);

/// <summary>Who is in on one specific day, grouped by status.</summary>
public sealed record DayRosterEntryResponse(
    TeamMemberResponse Member,
    AttendanceStatus? Status,
    string? Note);

public sealed record DayRosterResponse(
    Guid TeamId,
    DateOnly Date,
    DayTotalsResponse Totals,
    IReadOnlyList<DayRosterEntryResponse> Entries);
