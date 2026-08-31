using FluentAssertions;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Domain.Tests.Teams;

public sealed class TeamTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Owner = Guid.CreateVersion7();
    private static readonly Guid Member = Guid.CreateVersion7();
    private static readonly Guid Stranger = Guid.CreateVersion7();

    [Fact]
    public void Create_MakesTheCreatorTheOwnerAndOnlyMember()
    {
        Team team = CreateTeam();

        team.MemberCount.Should().Be(1);
        team.Owner.UserId.Should().Be(Owner);
        team.MembershipOf(Owner)!.Role.Should().Be(TeamRole.Owner);
    }

    [Fact]
    public void Create_TrimsTheNameAndDropsAnEmptyDescription()
    {
        Team team = Team.Create("  Platform Team  ", "   ", Owner, Now).Value;

        team.Name.Should().Be("Platform Team");
        team.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsABlankName(string? name)
    {
        Result<Team> result = Team.Create(name, null, Owner, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TeamErrors.NameEmpty);
    }

    [Fact]
    public void Create_RejectsANameOverTheLimit()
    {
        Result<Team> result = Team.Create(new string('x', Team.NameMaxLength + 1), null, Owner, Now);

        result.Error.Should().Be(TeamErrors.NameTooLong);
    }

    [Fact]
    public void Create_IssuesAJoinCode()
    {
        Team team = CreateTeam();

        team.JoinCode.Value.Should().HaveLength(JoinCode.Length);
        JoinCode.Create(team.JoinCode.Value).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Join_AddsTheUserAsAPlainMember()
    {
        Team team = CreateTeam();

        Result<TeamMembership> result = team.Join(Member, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(TeamRole.Member);
        team.MemberCount.Should().Be(2);
    }

    [Fact]
    public void Join_IsRejectedForSomeoneAlreadyInTheTeam()
    {
        Team team = CreateTeam();
        team.Join(Member, Now);

        Result<TeamMembership> result = team.Join(Member, Now);

        result.Error.Should().Be(TeamErrors.AlreadyMember);
        team.MemberCount.Should().Be(2);
    }

    [Fact]
    public void ChangeRole_PromotesAMemberToAdmin()
    {
        Team team = TeamWithMember();

        Result result = team.ChangeRole(Member, TeamRole.Admin, Owner);

        result.IsSuccess.Should().BeTrue();
        team.MembershipOf(Member)!.Role.Should().Be(TeamRole.Admin);
    }

    [Fact]
    public void ChangeRole_CannotBeUsedToGrantOwnership()
    {
        Team team = TeamWithMember();

        Result result = team.ChangeRole(Member, TeamRole.Owner, Owner);

        result.Error.Should().Be(TeamErrors.CannotDemoteOwner);
        team.MembershipOf(Member)!.Role.Should().Be(TeamRole.Member);
    }

    [Fact]
    public void ChangeRole_CannotDemoteTheOwner()
    {
        Team team = TeamWithMember();
        team.ChangeRole(Member, TeamRole.Admin, Owner);

        Result result = team.ChangeRole(Owner, TeamRole.Member, Member);

        result.Error.Should().Be(TeamErrors.CannotDemoteOwner);
        team.Owner.UserId.Should().Be(Owner);
    }

    [Fact]
    public void ChangeRole_IsRefusedForAPlainMember()
    {
        Team team = TeamWithMember();

        Result result = team.ChangeRole(Member, TeamRole.Admin, Member);

        result.Error.Should().Be(TeamErrors.AdminRequired);
    }

    [Fact]
    public void ChangeRole_IsRefusedForSomeoneOutsideTheTeam()
    {
        Team team = TeamWithMember();

        Result result = team.ChangeRole(Member, TeamRole.Admin, Stranger);

        result.Error.Should().Be(TeamErrors.NotMember);
    }

    [Fact]
    public void TransferOwnership_SwapsTheRolesInOneStep()
    {
        Team team = TeamWithMember();

        Result result = team.TransferOwnership(Member, Owner);

        result.IsSuccess.Should().BeTrue();
        team.MembershipOf(Member)!.Role.Should().Be(TeamRole.Owner);
        team.MembershipOf(Owner)!.Role.Should().Be(TeamRole.Admin);
        team.Memberships.Count(m => m.Role == TeamRole.Owner).Should().Be(1);
    }

    [Fact]
    public void TransferOwnership_IsRefusedForANonOwner()
    {
        Team team = TeamWithMember();

        Result result = team.TransferOwnership(Owner, Member);

        result.Error.Should().Be(TeamErrors.OwnerRequired);
    }

    [Fact]
    public void TransferOwnership_IsRefusedToTheCurrentOwner()
    {
        Team team = TeamWithMember();

        Result result = team.TransferOwnership(Owner, Owner);

        result.Error.Should().Be(TeamErrors.CannotTransferToSelf);
    }

    [Fact]
    public void TransferOwnership_IsRefusedForSomeoneOutsideTheTeam()
    {
        Team team = TeamWithMember();

        Result result = team.TransferOwnership(Stranger, Owner);

        result.Error.Should().Be(TeamErrors.MemberNotFound);
    }

    [Fact]
    public void RemoveMember_DropsTheMembership()
    {
        Team team = TeamWithMember();

        Result result = team.RemoveMember(Member, Owner);

        result.IsSuccess.Should().BeTrue();
        team.HasMember(Member).Should().BeFalse();
    }

    [Fact]
    public void RemoveMember_CannotRemoveTheOwner()
    {
        Team team = TeamWithMember();
        team.ChangeRole(Member, TeamRole.Admin, Owner);

        Result result = team.RemoveMember(Owner, Member);

        result.Error.Should().Be(TeamErrors.CannotRemoveOwner);
        team.HasMember(Owner).Should().BeTrue();
    }

    [Fact]
    public void Leave_RemovesAPlainMember()
    {
        Team team = TeamWithMember();

        Result result = team.Leave(Member);

        result.IsSuccess.Should().BeTrue();
        team.MemberCount.Should().Be(1);
    }

    [Fact]
    public void Leave_IsRefusedForTheOwner()
    {
        Team team = TeamWithMember();

        Result result = team.Leave(Owner);

        result.Error.Should().Be(TeamErrors.OwnerCannotLeave);
        team.HasMember(Owner).Should().BeTrue();
    }

    [Fact]
    public void Leave_IsRefusedForSomeoneOutsideTheTeam()
    {
        Team team = TeamWithMember();

        Result result = team.Leave(Stranger);

        result.Error.Should().Be(TeamErrors.NotMember);
    }

    [Fact]
    public void RegenerateJoinCode_ProducesADifferentCodeForAnAdmin()
    {
        Team team = TeamWithMember();
        team.ChangeRole(Member, TeamRole.Admin, Owner);
        string original = team.JoinCode.Value;

        Result<JoinCode> result = team.RegenerateJoinCode(Member);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().NotBe(original);
    }

    [Fact]
    public void RegenerateJoinCode_IsRefusedForAPlainMember()
    {
        Team team = TeamWithMember();
        string original = team.JoinCode.Value;

        Result<JoinCode> result = team.RegenerateJoinCode(Member);

        result.Error.Should().Be(TeamErrors.AdminRequired);
        team.JoinCode.Value.Should().Be(original);
    }

    [Fact]
    public void Rename_IsRefusedForAPlainMember()
    {
        Team team = TeamWithMember();

        Result result = team.Rename("Hijacked", Member);

        result.Error.Should().Be(TeamErrors.AdminRequired);
        team.Name.Should().Be("Platform Team");
    }

    [Fact]
    public void Describe_RejectsADescriptionOverTheLimit()
    {
        Team team = CreateTeam();

        Result result = team.Describe(new string('x', Team.DescriptionMaxLength + 1), Owner);

        result.Error.Should().Be(TeamErrors.DescriptionTooLong);
    }

    private static Team CreateTeam() => Team.Create("Platform Team", "Keeps the lights on.", Owner, Now).Value;

    private static Team TeamWithMember()
    {
        Team team = CreateTeam();
        team.Join(Member, Now);

        return team;
    }
}
