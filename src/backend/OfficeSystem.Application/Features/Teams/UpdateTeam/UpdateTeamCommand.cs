using FluentValidation;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.UpdateTeam;

public sealed record UpdateTeamCommand(Guid TeamId, string Name, string? Description) : ICommand<TeamDetailResponse>;

internal sealed class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamCommandValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("A team name is required.")
            .MaximumLength(Team.NameMaxLength);

        RuleFor(c => c.Description).MaximumLength(Team.DescriptionMaxLength);
    }
}

internal sealed class UpdateTeamCommandHandler(
    ITeamRepository teams,
    ITeamReadRepository teamsRead,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTeamCommand, TeamDetailResponse>
{
    public async Task<Result<TeamDetailResponse>> HandleAsync(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        Team? team = await teams.FindByIdAsync(command.TeamId, cancellationToken).ConfigureAwait(false);

        if (team is null)
        {
            return Result.Failure<TeamDetailResponse>(TeamErrors.NotFound);
        }

        Guid actorId = currentUser.RequireUserId();

        Result renamed = team.Rename(command.Name, actorId);

        if (renamed.IsFailure)
        {
            return Result.Failure<TeamDetailResponse>(renamed.Error);
        }

        Result described = team.Describe(command.Description, actorId);

        if (described.IsFailure)
        {
            return Result.Failure<TeamDetailResponse>(described.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        TeamDetailResponse? detail = await teamsRead.GetDetailAsync(team.Id, actorId, cancellationToken).ConfigureAwait(false);

        return detail is null
            ? Result.Failure<TeamDetailResponse>(TeamErrors.NotFound)
            : detail;
    }
}
