using Microsoft.EntityFrameworkCore;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Infrastructure.Persistence.Repositories;

internal sealed class TeamRepository(OfficeSystemDbContext context) : ITeamRepository
{
    public Task<Team?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => WithMemberships().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Team?> FindByJoinCodeAsync(JoinCode joinCode, CancellationToken cancellationToken = default)
        => WithMemberships().FirstOrDefaultAsync(t => t.JoinCode == joinCode, cancellationToken);

    public Task<bool> JoinCodeExistsAsync(JoinCode joinCode, CancellationToken cancellationToken = default)
        => context.Teams.AnyAsync(t => t.JoinCode == joinCode, cancellationToken);

    public void Add(Team team) => context.Teams.Add(team);

    public void Remove(Team team) => context.Teams.Remove(team);

    /// <summary>The aggregate is never loaded without its memberships: every invariant needs them.</summary>
    private IQueryable<Team> WithMemberships() => context.Teams.Include(t => t.Memberships);
}
