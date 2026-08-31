using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Teams.GetTeam;

public sealed record GetTeamQuery(Guid TeamId) : IQuery<TeamDetailResponse>;

internal sealed class GetTeamQueryHandler(ITeamReadRepository teams, ICurrentUser currentUser)
    : IQueryHandler<GetTeamQuery, TeamDetailResponse>
{
    public async Task<Result<TeamDetailResponse>> HandleAsync(GetTeamQuery query, CancellationToken cancellationToken)
    {
        TeamDetailResponse? detail = await teams
            .GetDetailAsync(query.TeamId, currentUser.RequireUserId(), cancellationToken)
            .ConfigureAwait(false);

        // A team the caller is not in is indistinguishable from one that does not exist.
        return detail is null
            ? Result.Failure<TeamDetailResponse>(TeamErrors.NotFound)
            : detail;
    }
}
