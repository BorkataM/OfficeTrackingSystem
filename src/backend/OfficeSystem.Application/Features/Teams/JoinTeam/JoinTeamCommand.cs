using FluentValidation;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.JoinTeam;

public sealed record JoinTeamCommand(string JoinCode) : ICommand<TeamDetailResponse>;

internal sealed class JoinTeamCommandValidator : AbstractValidator<JoinTeamCommand>
{
    public JoinTeamCommandValidator()
        => RuleFor(c => c.JoinCode)
            .NotEmpty().WithMessage("A join code is required.")
            .Length(Domain.Teams.JoinCode.Length)
                .WithMessage($"A join code is exactly {Domain.Teams.JoinCode.Length} characters.");
}

internal sealed class JoinTeamCommandHandler(
    ITeamRepository teams,
    ITeamReadRepository teamsRead,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<JoinTeamCommand, TeamDetailResponse>
{
    public async Task<Result<TeamDetailResponse>> HandleAsync(JoinTeamCommand command, CancellationToken cancellationToken)
    {
        Result<JoinCode> joinCode = JoinCode.Create(command.JoinCode);

        if (joinCode.IsFailure)
        {
            return Result.Failure<TeamDetailResponse>(joinCode.Error);
        }

        Team? team = await teams.FindByJoinCodeAsync(joinCode.Value, cancellationToken).ConfigureAwait(false);

        if (team is null)
        {
            return Result.Failure<TeamDetailResponse>(TeamErrors.JoinCodeNotRecognised);
        }

        Guid userId = currentUser.RequireUserId();
        Result<TeamMembership> joined = team.Join(userId, clock.UtcNow);

        if (joined.IsFailure)
        {
            return Result.Failure<TeamDetailResponse>(joined.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        TeamDetailResponse? detail = await teamsRead
            .GetDetailAsync(team.Id, userId, cancellationToken)
            .ConfigureAwait(false);

        return detail is null
            ? Result.Failure<TeamDetailResponse>(TeamErrors.NotFound)
            : detail;
    }
}
