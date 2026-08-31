using FluentValidation;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.TransferOwnership;

public sealed record TransferOwnershipCommand(Guid TeamId, Guid NewOwnerId) : ICommand;

internal sealed class TransferOwnershipCommandValidator : AbstractValidator<TransferOwnershipCommand>
{
    public TransferOwnershipCommandValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.NewOwnerId).NotEmpty();
    }
}

internal sealed class TransferOwnershipCommandHandler(
    ITeamRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<TransferOwnershipCommand>
{
    public async Task<Result> HandleAsync(TransferOwnershipCommand command, CancellationToken cancellationToken)
    {
        Team? team = await teams.FindByIdAsync(command.TeamId, cancellationToken).ConfigureAwait(false);

        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound);
        }

        Result transferred = team.TransferOwnership(command.NewOwnerId, currentUser.RequireUserId());

        if (transferred.IsFailure)
        {
            return transferred;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
