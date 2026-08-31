using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.RemoveMember;

public sealed record RemoveMemberCommand(Guid TeamId, Guid UserId) : ICommand;

internal sealed class RemoveMemberCommandHandler(
    ITeamRepository teams,
    IAttendanceRepository attendance,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveMemberCommand>
{
    public async Task<Result> HandleAsync(RemoveMemberCommand command, CancellationToken cancellationToken)
    {
        Team? team = await teams.FindByIdAsync(command.TeamId, cancellationToken).ConfigureAwait(false);

        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound);
        }

        Result removed = team.RemoveMember(command.UserId, currentUser.RequireUserId());

        if (removed.IsFailure)
        {
            return removed;
        }

        await attendance.RemoveAllForMemberAsync(command.TeamId, command.UserId, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
