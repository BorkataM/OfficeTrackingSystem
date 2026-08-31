using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Teams;

/// <summary>
/// A person's place in a team. Lives inside the <see cref="Team"/> aggregate, so it
/// is only ever created or mutated through the team's own methods.
/// </summary>
public sealed class TeamMembership : Entity
{
    private TeamMembership(Guid id, Guid teamId, Guid userId, TeamRole role, DateTimeOffset joinedAtUtc)
        : base(id)
    {
        TeamId = teamId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }

    private TeamMembership()
    {
    }

    public Guid TeamId { get; private set; }

    public Guid UserId { get; private set; }

    public TeamRole Role { get; private set; }

    public DateTimeOffset JoinedAtUtc { get; private set; }

    public bool CanManageMembers => Role is TeamRole.Admin or TeamRole.Owner;

    internal static TeamMembership Create(Guid teamId, Guid userId, TeamRole role, DateTimeOffset joinedAtUtc)
        => new(Guid.CreateVersion7(), teamId, userId, role, joinedAtUtc);

    internal void ChangeRole(TeamRole role) => Role = role;
}
