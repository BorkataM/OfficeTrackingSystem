using OfficeSystem.Api.Infrastructure;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Teams.ChangeMemberRole;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Application.Features.Teams.CreateTeam;
using OfficeSystem.Application.Features.Teams.DeleteTeam;
using OfficeSystem.Application.Features.Teams.GetMyTeams;
using OfficeSystem.Application.Features.Teams.GetTeam;
using OfficeSystem.Application.Features.Teams.JoinTeam;
using OfficeSystem.Application.Features.Teams.LeaveTeam;
using OfficeSystem.Application.Features.Teams.RegenerateJoinCode;
using OfficeSystem.Application.Features.Teams.RemoveMember;
using OfficeSystem.Application.Features.Teams.TransferOwnership;
using OfficeSystem.Application.Features.Teams.UpdateTeam;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Api.Endpoints;

internal sealed class TeamEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        RouteGroupBuilder group = routes.MapGroup("/api/teams")
            .WithTags("Teams")
            .RequireAuthorization();

        group.MapGet("/", async (
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(new GetMyTeamsQuery(), cancellationToken)).ToHttpResult())
            .WithSummary("Lists the teams the caller belongs to.")
            .Produces<IReadOnlyList<TeamListItemResponse>>();

        group.MapPost("/", async (
                CreateTeamCommand command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(command, cancellationToken))
                    .ToCreatedResult(team => $"/api/teams/{team.Id}"))
            .WithSummary("Creates a team with the caller as its owner.")
            .Produces<TeamDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPost("/join", async (
                JoinTeamCommand command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(command, cancellationToken)).ToHttpResult())
            .WithSummary("Joins a team using its invite code.")
            .Produces<TeamDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{teamId:guid}", async (
                Guid teamId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(new GetTeamQuery(teamId), cancellationToken)).ToHttpResult())
            .WithSummary("Returns a team with its members.")
            .Produces<TeamDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{teamId:guid}", async (
                Guid teamId,
                UpdateTeamRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(
                    new UpdateTeamCommand(teamId, request.Name, request.Description),
                    cancellationToken)).ToHttpResult())
            .WithSummary("Renames a team or changes its description. Admins and owners only.")
            .Produces<TeamDetailResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapDelete("/{teamId:guid}", async (
                Guid teamId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(new DeleteTeamCommand(teamId), cancellationToken)).ToHttpResult())
            .WithSummary("Deletes a team and everything in it. Owner only.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{teamId:guid}/join-code", async (
                Guid teamId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(new RegenerateJoinCodeCommand(teamId), cancellationToken)).ToHttpResult())
            .WithSummary("Issues a new invite code, invalidating the previous one. Admins and owners only.")
            .Produces<string>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{teamId:guid}/leave", async (
                Guid teamId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(new LeaveTeamCommand(teamId), cancellationToken)).ToHttpResult())
            .WithSummary("Leaves a team. The owner must transfer ownership first.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        MapMemberEndpoints(group);
    }

    private static void MapMemberEndpoints(RouteGroupBuilder group)
    {
        group.MapPut("/{teamId:guid}/members/{userId:guid}/role", async (
                Guid teamId,
                Guid userId,
                ChangeMemberRoleRequest request,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(
                    new ChangeMemberRoleCommand(teamId, userId, request.Role),
                    cancellationToken)).ToHttpResult())
            .WithSummary("Promotes or demotes a member between Member and Admin.")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{teamId:guid}/members/{userId:guid}/ownership", async (
                Guid teamId,
                Guid userId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(
                    new TransferOwnershipCommand(teamId, userId),
                    cancellationToken)).ToHttpResult())
            .WithSummary("Hands ownership to another member. The previous owner becomes an admin.")
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapDelete("/{teamId:guid}/members/{userId:guid}", async (
                Guid teamId,
                Guid userId,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(new RemoveMemberCommand(teamId, userId), cancellationToken)).ToHttpResult())
            .WithSummary("Removes a member and their attendance from the team.")
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    internal sealed record UpdateTeamRequest(string Name, string? Description);

    internal sealed record ChangeMemberRoleRequest(TeamRole Role);
}
