using OfficeSystem.Api.Infrastructure;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Attendance.ClearAttendance;
using OfficeSystem.Application.Features.Attendance.Contracts;
using OfficeSystem.Application.Features.Attendance.GetDayRoster;
using OfficeSystem.Application.Features.Attendance.GetMyPlan;
using OfficeSystem.Application.Features.Attendance.GetTeamSchedule;
using OfficeSystem.Application.Features.Attendance.SetAttendance;

namespace OfficeSystem.Api.Endpoints;

internal sealed class AttendanceEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        RouteGroupBuilder group = routes.MapGroup("/api/teams/{teamId:guid}")
            .WithTags("Attendance")
            .RequireAuthorization();

        group.MapGet("/schedule", async (
                Guid teamId,
                DateOnly? from,
                DateOnly? to,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(
                    new GetTeamScheduleQuery(teamId, from, to),
                    cancellationToken)).ToHttpResult())
            .WithSummary("The team's schedule grid. Defaults to the week containing today.")
            .Produces<TeamScheduleResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/schedule/{date}", async (
                Guid teamId,
                DateOnly date,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(new GetDayRosterQuery(teamId, date), cancellationToken)).ToHttpResult())
            .WithSummary("Who is in on one day, grouped by status.")
            .Produces<DayRosterResponse>();

        group.MapGet("/attendance/me", async (
                Guid teamId,
                DateOnly? from,
                DateOnly? to,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(new GetMyPlanQuery(teamId, from, to), cancellationToken)).ToHttpResult())
            .WithSummary("The caller's own plan. Defaults to the month containing today.")
            .Produces<IReadOnlyList<DayPlanResponse>>();

        group.MapPut("/attendance/me", async (
                Guid teamId,
                SetAttendanceRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(
                    new SetAttendanceCommand(teamId, request.Days),
                    cancellationToken)).ToHttpResult())
            .WithSummary("Sets the caller's plan for one or more days.")
            .Produces<IReadOnlyList<DayPlanResponse>>()
            .ProducesValidationProblem();

        // A POST rather than a DELETE: the list of days belongs in a body, and
        // request bodies on DELETE are unreliable through proxies.
        group.MapPost("/attendance/me/clear", async (
                Guid teamId,
                ClearAttendanceRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(
                    new ClearAttendanceCommand(teamId, request.Dates),
                    cancellationToken)).ToHttpResult())
            .WithSummary("Clears the caller's plan for the given days.")
            .ProducesValidationProblem();
    }

    internal sealed record SetAttendanceRequest(IReadOnlyList<DayPlanInput> Days);

    internal sealed record ClearAttendanceRequest(IReadOnlyList<DateOnly> Dates);
}
