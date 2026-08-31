using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.RegenerateJoinCode;

public sealed record RegenerateJoinCodeCommand(Guid TeamId) : ICommand<string>;

internal sealed class RegenerateJoinCodeCommandHandler(
    ITeamRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<RegenerateJoinCodeCommand, string>
{
    public async Task<Result<string>> HandleAsync(RegenerateJoinCodeCommand command, CancellationToken cancellationToken)
    {
        Team? team = await teams.FindByIdAsync(command.TeamId, cancellationToken).ConfigureAwait(false);

        if (team is null)
        {
            return Result.Failure<string>(TeamErrors.NotFound);
        }

        Result<JoinCode> regenerated = team.RegenerateJoinCode(currentUser.RequireUserId());

        if (regenerated.IsFailure)
        {
            return Result.Failure<string>(regenerated.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return regenerated.Value.Value;
    }
}
