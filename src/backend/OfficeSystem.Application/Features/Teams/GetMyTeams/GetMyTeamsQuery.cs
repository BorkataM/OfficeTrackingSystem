using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Application.Features.Teams.GetMyTeams;

public sealed record GetMyTeamsQuery : IQuery<IReadOnlyList<TeamListItemResponse>>;

internal sealed class GetMyTeamsQueryHandler(ITeamReadRepository teams, ICurrentUser currentUser)
    : IQueryHandler<GetMyTeamsQuery, IReadOnlyList<TeamListItemResponse>>
{
    public async Task<Result<IReadOnlyList<TeamListItemResponse>>> HandleAsync(GetMyTeamsQuery query, CancellationToken cancellationToken)
        => Result.Success(await teams
            .ListForUserAsync(currentUser.RequireUserId(), cancellationToken)
            .ConfigureAwait(false));
}
