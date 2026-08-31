using FluentValidation;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(Guid TeamId, Guid UserId, TeamRole Role) : ICommand;

internal sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Role).IsInEnum().WithMessage("That role is not recognised.");
    }
}

internal sealed class ChangeMemberRoleCommandHandler(
    ITeamRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeMemberRoleCommand>
{
    public async Task<Result> HandleAsync(ChangeMemberRoleCommand command, CancellationToken cancellationToken)
    {
        Team? team = await teams.FindByIdAsync(command.TeamId, cancellationToken).ConfigureAwait(false);

        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound);
        }

        Result changed = team.ChangeRole(command.UserId, command.Role, currentUser.RequireUserId());

        if (changed.IsFailure)
        {
            return changed;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
