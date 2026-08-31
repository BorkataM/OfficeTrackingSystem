using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Abstractions.Data;

/// <summary>
/// Read side for teams: projects straight to response contracts so queries never
/// hydrate an aggregate they are not going to change.
/// </summary>
public interface ITeamReadRepository
{
    Task<IReadOnlyList<TeamListItemResponse>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<TeamRole?> GetRoleAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    Task<TeamDetailResponse?> GetDetailAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamMemberResponse>> ListMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
}
