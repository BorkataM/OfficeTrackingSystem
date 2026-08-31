using FluentValidation;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.CreateTeam;

public sealed record CreateTeamCommand(string Name, string? Description) : ICommand<TeamDetailResponse>;

internal sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("A team name is required.")
            .MaximumLength(Team.NameMaxLength);

        RuleFor(c => c.Description)
            .MaximumLength(Team.DescriptionMaxLength);
    }
}

internal sealed class CreateTeamCommandHandler(
    ITeamRepository teams,
    ITeamReadRepository teamsRead,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<CreateTeamCommand, TeamDetailResponse>
{
    private const int JoinCodeAttempts = 5;

    public async Task<Result<TeamDetailResponse>> HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        Guid ownerId = currentUser.RequireUserId();

        Result<Team> team = Team.Create(command.Name, command.Description, ownerId, clock.UtcNow);

        if (team.IsFailure)
        {
            return Result.Failure<TeamDetailResponse>(team.Error);
        }

        Result unique = await EnsureUniqueJoinCodeAsync(team.Value, ownerId, cancellationToken).ConfigureAwait(false);

        if (unique.IsFailure)
        {
            return Result.Failure<TeamDetailResponse>(unique.Error);
        }

        teams.Add(team.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        TeamDetailResponse? detail = await teamsRead
            .GetDetailAsync(team.Value.Id, ownerId, cancellationToken)
            .ConfigureAwait(false);

        return detail is null
            ? Result.Failure<TeamDetailResponse>(TeamErrors.NotFound)
            : detail;
    }

    private async Task<Result> EnsureUniqueJoinCodeAsync(Team team, Guid ownerId, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < JoinCodeAttempts; attempt++)
        {
            if (!await teams.JoinCodeExistsAsync(team.JoinCode, cancellationToken).ConfigureAwait(false))
            {
                return Result.Success();
            }

            team.RegenerateJoinCode(ownerId);
        }

        return Result.Failure(Error.Failure(
            "team.joinCode.exhausted",
            "Could not allocate a unique join code. Please try again."));
    }
}
