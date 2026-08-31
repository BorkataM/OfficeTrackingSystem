using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Abstractions.Data;

public interface ITeamRepository
{
    /// <summary>Loads the aggregate with its memberships, ready to be mutated.</summary>
    Task<Team?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Team?> FindByJoinCodeAsync(JoinCode joinCode, CancellationToken cancellationToken = default);

    Task<bool> JoinCodeExistsAsync(JoinCode joinCode, CancellationToken cancellationToken = default);

    void Add(Team team);

    void Remove(Team team);
}
