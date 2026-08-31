using FluentAssertions;
using OfficeSystem.Application.Features.Attendance.ClearAttendance;
using OfficeSystem.Application.Features.Attendance.Contracts;
using OfficeSystem.Application.Features.Attendance.GetDayRoster;
using OfficeSystem.Application.Features.Attendance.GetMyPlan;
using OfficeSystem.Application.Features.Attendance.GetTeamSchedule;
using OfficeSystem.Application.Features.Attendance.SetAttendance;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Application.Tests.Authentication;
using OfficeSystem.Application.Tests.Harness;
using OfficeSystem.Application.Tests.Teams;
using OfficeSystem.Domain.Attendance;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Tests.Attendance;

public sealed class AttendanceUseCaseTests
{
    private static readonly DateOnly Monday = new(2026, 8, 24);
    private static readonly DateOnly Wednesday = new(2026, 8, 26);
    private static readonly DateOnly Sunday = new(2026, 8, 30);

    [Fact]
    public async Task SetAttendance_StoresEveryDayInOneCall()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        Result<IReadOnlyList<DayPlanResponse>> result = await SetAsync(
            harness,
            team.Id,
            new DayPlanInput(Monday, AttendanceStatus.Office, null),
            new DayPlanInput(Monday.AddDays(1), AttendanceStatus.Remote, "  Focus day  "),
            new DayPlanInput(Wednesday, AttendanceStatus.Travelling, "Customer visit"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(d => d.Date).Should().BeInAscendingOrder();
        result.Value.Single(d => d.Date == Monday.AddDays(1)).Note.Should().Be("Focus day");
    }

    [Fact]
    public async Task SetAttendance_OverwritesAnExistingDayRatherThanDuplicatingIt()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        await SetAsync(harness, team.Id, new DayPlanInput(Monday, AttendanceStatus.Office, "First"));
        await SetAsync(harness, team.Id, new DayPlanInput(Monday, AttendanceStatus.Away, "Second"));

        Result<IReadOnlyList<DayPlanResponse>> plan = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetMyPlanQuery(team.Id, Monday, Sunday), token));

        plan.Value.Should().HaveCount(1);
        plan.Value[0].Status.Should().Be(AttendanceStatus.Away);
        plan.Value[0].Note.Should().Be("Second");
    }

    [Fact]
    public async Task SetAttendance_RejectsTheWholeBatchWhenOneDayIsOutOfRange()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        Result<IReadOnlyList<DayPlanResponse>> result = await SetAsync(
            harness,
            team.Id,
            new DayPlanInput(Monday, AttendanceStatus.Office, null),
            new DayPlanInput(harness.Clock.Today.AddDays(AttendanceEntry.MaxDaysInTheFuture + 1), AttendanceStatus.Office, null));

        result.Error.Should().Be(AttendanceErrors.TooFarInTheFuture);

        // Nothing was committed: the batch is all-or-nothing.
        Result<IReadOnlyList<DayPlanResponse>> plan = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetMyPlanQuery(team.Id, Monday, Sunday), token));

        plan.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task SetAttendance_RejectsADuplicatedDayAtTheValidationStage()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        Result<IReadOnlyList<DayPlanResponse>> result = await SetAsync(
            harness,
            team.Id,
            new DayPlanInput(Monday, AttendanceStatus.Office, null),
            new DayPlanInput(Monday, AttendanceStatus.Remote, null));

        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).Failures.Should().ContainKey("days");
    }

    [Fact]
    public async Task SetAttendance_IsRefusedForSomeoneOutsideTheTeam()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        await AuthenticationUseCaseTests.RegisterAsync(harness, "stranger@example.com", "Stranger");

        Result<IReadOnlyList<DayPlanResponse>> result = await SetAsync(
            harness,
            team.Id,
            new DayPlanInput(Monday, AttendanceStatus.Office, null));

        result.Error.Should().Be(TeamErrors.NotMember);
    }

    [Fact]
    public async Task ClearAttendance_RemovesOnlyTheDaysAsked()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        await SetAsync(
            harness,
            team.Id,
            new DayPlanInput(Monday, AttendanceStatus.Office, null),
            new DayPlanInput(Wednesday, AttendanceStatus.Remote, null));

        Result cleared = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new ClearAttendanceCommand(team.Id, [Monday]), token));

        cleared.IsSuccess.Should().BeTrue();

        Result<IReadOnlyList<DayPlanResponse>> plan = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetMyPlanQuery(team.Id, Monday, Sunday), token));

        plan.Value.Select(d => d.Date).Should().Equal(Wednesday);
    }

    [Fact]
    public async Task ClearAttendance_IsHarmlessWhenNothingWasPlanned()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        Result result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new ClearAttendanceCommand(team.Id, [Monday]), token));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetTeamSchedule_DefaultsToTheWeekContainingToday()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        Result<TeamScheduleResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetTeamScheduleQuery(team.Id, null, null), token));

        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(Monday);
        result.Value.End.Should().Be(Sunday);
        result.Value.Days.Should().HaveCount(7);
        result.Value.Today.Should().Be(harness.Clock.Today);
    }

    [Fact]
    public async Task GetTeamSchedule_CountsEveryMemberAndFlagsWeekends()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await TeamUseCaseTests.TeamWithSecondMemberAsync(harness);

        // The second member plans Monday; the owner does not.
        await SetAsync(harness, team.Id, new DayPlanInput(Monday, AttendanceStatus.Office, null));

        Result<TeamScheduleResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetTeamScheduleQuery(team.Id, Monday, Sunday), token));

        DayTotalsResponse monday = result.Value.Totals.Single(t => t.Date == Monday);
        monday.InOffice.Should().Be(1);
        monday.NotPlanned.Should().Be(1);
        monday.IsWeekend.Should().BeFalse();

        result.Value.Totals.Single(t => t.Date == Sunday).IsWeekend.Should().BeTrue();
        result.Value.Rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTeamSchedule_ReturnsSparseRowsForUnplannedMembers()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await TeamUseCaseTests.TeamWithSecondMemberAsync(harness);

        Result<TeamScheduleResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetTeamScheduleQuery(team.Id, Monday, Sunday), token));

        result.Value.Rows.Should().OnlyContain(row => row.Days.Count == 0);
        result.Value.Totals.Should().OnlyContain(t => t.NotPlanned == 2);
    }

    [Fact]
    public async Task GetTeamSchedule_RejectsARangeLongerThanAllowed()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        Result<TeamScheduleResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(
                new GetTeamScheduleQuery(team.Id, Monday, Monday.AddDays(DateRange.MaxDays)),
                token));

        result.Error.Should().Be(DateRangeErrors.TooLong);
    }

    [Fact]
    public async Task GetTeamSchedule_IsRefusedForSomeoneOutsideTheTeam()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        await AuthenticationUseCaseTests.RegisterAsync(harness, "stranger@example.com", "Stranger");

        Result<TeamScheduleResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetTeamScheduleQuery(team.Id, Monday, Sunday), token));

        result.Error.Should().Be(TeamErrors.NotMember);
    }

    [Fact]
    public async Task GetDayRoster_ListsEveryMemberIncludingThoseWithoutAPlan()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await TeamUseCaseTests.TeamWithSecondMemberAsync(harness);

        await SetAsync(harness, team.Id, new DayPlanInput(Wednesday, AttendanceStatus.Office, "Sprint review"));

        Result<DayRosterResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetDayRosterQuery(team.Id, Wednesday), token));

        result.Value.Entries.Should().HaveCount(2);
        result.Value.Entries.Count(e => e.Status is null).Should().Be(1);
        result.Value.Entries.Single(e => e.Status == AttendanceStatus.Office).Note.Should().Be("Sprint review");
        result.Value.Totals.InOffice.Should().Be(1);
        result.Value.Totals.NotPlanned.Should().Be(1);
    }

    [Fact]
    public async Task GetMyPlan_DefaultsToTheMonthContainingToday()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await OwnedTeamAsync(harness);

        await SetAsync(
            harness,
            team.Id,
            new DayPlanInput(new DateOnly(2026, 8, 3), AttendanceStatus.Office, null),
            new DayPlanInput(new DateOnly(2026, 9, 2), AttendanceStatus.Office, null));

        Result<IReadOnlyList<DayPlanResponse>> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetMyPlanQuery(team.Id, null, null), token));

        result.Value.Select(d => d.Date).Should().Equal(new DateOnly(2026, 8, 3));
    }

    [Fact]
    public async Task GetMyPlan_ReturnsOnlyTheCallersOwnDays()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await TeamUseCaseTests.TeamWithSecondMemberAsync(harness);

        // Harness is currently the second member.
        await SetAsync(harness, team.Id, new DayPlanInput(Monday, AttendanceStatus.Remote, "Mine"));

        Guid ownerId = team.Members.Single(m => m.Role == TeamRole.Owner).UserId;
        harness.CurrentUser.UserId = ownerId;
        await SetAsync(harness, team.Id, new DayPlanInput(Monday, AttendanceStatus.Office, "Theirs"));

        Result<IReadOnlyList<DayPlanResponse>> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetMyPlanQuery(team.Id, Monday, Sunday), token));

        result.Value.Should().HaveCount(1);
        result.Value[0].Note.Should().Be("Theirs");
    }

    private static async Task<TeamDetailResponse> OwnedTeamAsync(UseCaseHarness harness)
    {
        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");

        return (await TeamUseCaseTests.CreateTeamAsync(harness)).Value;
    }

    private static Task<Result<IReadOnlyList<DayPlanResponse>>> SetAsync(
        UseCaseHarness harness,
        Guid teamId,
        params DayPlanInput[] days) =>
        harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new SetAttendanceCommand(teamId, days), token));
}
