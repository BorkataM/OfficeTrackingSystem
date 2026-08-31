using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Teams;

public sealed class Team : Entity, IAggregateRoot
{
    public const int NameMaxLength = 80;
    public const int DescriptionMaxLength = 280;

    private readonly List<TeamMembership> _memberships = [];

    private Team(Guid id, string name, string? description, JoinCode joinCode, DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        Description = description;
        JoinCode = joinCode;
        CreatedAtUtc = createdAtUtc;
    }

    private Team()
    {
        Name = null!;
        JoinCode = null!;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public JoinCode JoinCode { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<TeamMembership> Memberships => _memberships.AsReadOnly();

    /// <summary>Creates a team with <paramref name="ownerId"/> as its owner and first member.</summary>
    public static Result<Team> Create(string? name, string? description, Guid ownerId, DateTimeOffset nowUtc)
    {
        Result<string> normalizedName = NormalizeName(name);

        if (normalizedName.IsFailure)
        {
            return Result.Failure<Team>(normalizedName.Error);
        }

        Result<string?> normalizedDescription = NormalizeDescription(description);

        if (normalizedDescription.IsFailure)
        {
            return Result.Failure<Team>(normalizedDescription.Error);
        }

        Team team = new(
            Guid.CreateVersion7(),
            normalizedName.Value,
            normalizedDescription.Value,
            JoinCode.NewCode(),
            nowUtc);

        team._memberships.Add(TeamMembership.Create(team.Id, ownerId, TeamRole.Owner, nowUtc));

        return team;
    }

    public TeamMembership? MembershipOf(Guid userId) => _memberships.SingleOrDefault(m => m.UserId == userId);

    public bool HasMember(Guid userId) => MembershipOf(userId) is not null;

    public TeamMembership Owner => _memberships.Single(m => m.Role == TeamRole.Owner);

    public int MemberCount => _memberships.Count;

    public Result<TeamMembership> Join(Guid userId, DateTimeOffset nowUtc)
    {
        if (HasMember(userId))
        {
            return TeamErrors.AlreadyMember;
        }

        TeamMembership membership = TeamMembership.Create(Id, userId, TeamRole.Member, nowUtc);
        _memberships.Add(membership);

        return membership;
    }

    public Result Rename(string? name, Guid actingUserId)
    {
        Result authorized = EnsureCanManage(actingUserId);

        if (authorized.IsFailure)
        {
            return authorized;
        }

        Result<string> normalized = NormalizeName(name);

        if (normalized.IsFailure)
        {
            return Result.Failure(normalized.Error);
        }

        Name = normalized.Value;

        return Result.Success();
    }

    public Result Describe(string? description, Guid actingUserId)
    {
        Result authorized = EnsureCanManage(actingUserId);

        if (authorized.IsFailure)
        {
            return authorized;
        }

        Result<string?> normalized = NormalizeDescription(description);

        if (normalized.IsFailure)
        {
            return Result.Failure(normalized.Error);
        }

        Description = normalized.Value;

        return Result.Success();
    }

    public Result<JoinCode> RegenerateJoinCode(Guid actingUserId)
    {
        Result authorized = EnsureCanManage(actingUserId);

        if (authorized.IsFailure)
        {
            return Result.Failure<JoinCode>(authorized.Error);
        }

        JoinCode = JoinCode.NewCode();

        return JoinCode;
    }

    public Result ChangeRole(Guid targetUserId, TeamRole role, Guid actingUserId)
    {
        Result authorized = EnsureCanManage(actingUserId);

        if (authorized.IsFailure)
        {
            return authorized;
        }

        TeamMembership? target = MembershipOf(targetUserId);

        if (target is null)
        {
            return Result.Failure(TeamErrors.MemberNotFound);
        }

        if (target.Role == TeamRole.Owner || role == TeamRole.Owner)
        {
            // Ownership moves only through TransferOwnership, which keeps the
            // "exactly one owner" invariant in a single step.
            return Result.Failure(TeamErrors.CannotDemoteOwner);
        }

        target.ChangeRole(role);

        return Result.Success();
    }

    public Result TransferOwnership(Guid newOwnerId, Guid actingUserId)
    {
        TeamMembership? actor = MembershipOf(actingUserId);

        if (actor is null)
        {
            return Result.Failure(TeamErrors.NotMember);
        }

        if (actor.Role != TeamRole.Owner)
        {
            return Result.Failure(TeamErrors.OwnerRequired);
        }

        if (newOwnerId == actingUserId)
        {
            return Result.Failure(TeamErrors.CannotTransferToSelf);
        }

        TeamMembership? target = MembershipOf(newOwnerId);

        if (target is null)
        {
            return Result.Failure(TeamErrors.MemberNotFound);
        }

        actor.ChangeRole(TeamRole.Admin);
        target.ChangeRole(TeamRole.Owner);

        return Result.Success();
    }

    public Result RemoveMember(Guid targetUserId, Guid actingUserId)
    {
        Result authorized = EnsureCanManage(actingUserId);

        if (authorized.IsFailure)
        {
            return authorized;
        }

        TeamMembership? target = MembershipOf(targetUserId);

        if (target is null)
        {
            return Result.Failure(TeamErrors.MemberNotFound);
        }

        if (target.Role == TeamRole.Owner)
        {
            return Result.Failure(TeamErrors.CannotRemoveOwner);
        }

        _memberships.Remove(target);

        return Result.Success();
    }

    public Result Leave(Guid userId)
    {
        TeamMembership? membership = MembershipOf(userId);

        if (membership is null)
        {
            return Result.Failure(TeamErrors.NotMember);
        }

        if (membership.Role == TeamRole.Owner)
        {
            return Result.Failure(TeamErrors.OwnerCannotLeave);
        }

        _memberships.Remove(membership);

        return Result.Success();
    }

    public Result EnsureCanManage(Guid userId)
    {
        TeamMembership? membership = MembershipOf(userId);

        if (membership is null)
        {
            return Result.Failure(TeamErrors.NotMember);
        }

        return membership.CanManageMembers
            ? Result.Success()
            : Result.Failure(TeamErrors.AdminRequired);
    }

    public Result EnsureIsOwner(Guid userId)
    {
        TeamMembership? membership = MembershipOf(userId);

        if (membership is null)
        {
            return Result.Failure(TeamErrors.NotMember);
        }

        return membership.Role == TeamRole.Owner
            ? Result.Success()
            : Result.Failure(TeamErrors.OwnerRequired);
    }

    private static Result<string> NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TeamErrors.NameEmpty;
        }

        string trimmed = name.Trim();

        return trimmed.Length > NameMaxLength ? TeamErrors.NameTooLong : trimmed;
    }

    private static Result<string?> NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Success<string?>(null);
        }

        string trimmed = description.Trim();

        return trimmed.Length > DescriptionMaxLength
            ? Result.Failure<string?>(TeamErrors.DescriptionTooLong)
            : Result.Success<string?>(trimmed);
    }
}
