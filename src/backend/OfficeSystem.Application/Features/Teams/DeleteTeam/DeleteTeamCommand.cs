using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.DeleteTeam;

public sealed record DeleteTeamCommand(Guid TeamId) : ICommand;

internal sealed class DeleteTeamCommandHandler(
    ITeamRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteTeamCommand>
{
    public async Task<Result> HandleAsync(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        Team? team = await teams.FindByIdAsync(command.TeamId, cancellationToken).ConfigureAwait(false);

        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound);
        }

        Result authorized = team.EnsureIsOwner(currentUser.RequireUserId());

        if (authorized.IsFailure)
        {
            return authorized;
        }

        // Memberships and attendance are cascade-deleted by the schema.
        teams.Remove(team);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
