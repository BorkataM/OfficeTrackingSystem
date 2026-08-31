using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.LeaveTeam;

public sealed record LeaveTeamCommand(Guid TeamId) : ICommand;

internal sealed class LeaveTeamCommandHandler(
    ITeamRepository teams,
    IAttendanceRepository attendance,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<LeaveTeamCommand>
{
    public async Task<Result> HandleAsync(LeaveTeamCommand command, CancellationToken cancellationToken)
    {
        Team? team = await teams.FindByIdAsync(command.TeamId, cancellationToken).ConfigureAwait(false);

        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound);
        }

        Guid userId = currentUser.RequireUserId();
        Result left = team.Leave(userId);

        if (left.IsFailure)
        {
            return left;
        }

        await attendance.RemoveAllForMemberAsync(command.TeamId, userId, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
