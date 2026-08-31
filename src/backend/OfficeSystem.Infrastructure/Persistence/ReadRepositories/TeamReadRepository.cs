using Microsoft.EntityFrameworkCore;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Infrastructure.Persistence.ReadRepositories;

internal sealed class TeamReadRepository(OfficeSystemDbContext context) : ITeamReadRepository
{
    // Ordering happens before the projection throughout: EF cannot sort by a member
    // of a record it is still constructing.
    public async Task<IReadOnlyList<TeamListItemResponse>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.TeamMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                context.Teams.AsNoTracking(),
                m => m.TeamId,
                t => t.Id,
                (Membership, Team) => new { Membership, Team })
            .OrderBy(x => x.Team.Name)
            .Select(x => new TeamListItemResponse(
                x.Team.Id,
                x.Team.Name,
                x.Team.Description,
                x.Membership.Role,
                x.Team.Memberships.Count,
                x.Team.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<TeamRole?> GetRoleAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Projected into a list so "not a member" stays distinct from "role Member (0)".
        List<TeamRole> roles = await context.TeamMemberships
            .AsNoTracking()
            .Where(m => m.TeamId == teamId && m.UserId == userId)
            .Select(m => m.Role)
            .Take(1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return roles.Count == 0 ? null : roles[0];
    }

    public async Task<TeamDetailResponse?> GetDetailAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        TeamRole? role = await GetRoleAsync(teamId, userId, cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return null;
        }

        var team = await context.Teams
            .AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                JoinCode = t.JoinCode.Value,
                t.CreatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (team is null)
        {
            return null;
        }

        IReadOnlyList<TeamMemberResponse> members = await ListMembersAsync(teamId, cancellationToken).ConfigureAwait(false);

        bool canSeeJoinCode = role is TeamRole.Admin or TeamRole.Owner;

        return new TeamDetailResponse(
            team.Id,
            team.Name,
            team.Description,
            role.Value,
            canSeeJoinCode ? team.JoinCode : null,
            team.CreatedAtUtc,
            members);
    }

    public async Task<IReadOnlyList<TeamMemberResponse>> ListMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
        => await context.TeamMemberships
            .AsNoTracking()
            .Where(m => m.TeamId == teamId)
            .Join(
                context.Users.AsNoTracking(),
                m => m.UserId,
                u => u.Id,
                (Membership, User) => new { Membership, User })
            // Owner first, then admins, then members alphabetically: the grid's reading order.
            .OrderByDescending(x => x.Membership.Role)
            .ThenBy(x => x.User.DisplayName)
            .Select(x => new TeamMemberResponse(
                x.User.Id,
                x.User.DisplayName,
                x.User.Email.Value,
                x.User.AccentColor,
                x.Membership.Role,
                x.Membership.JoinedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
