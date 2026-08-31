using FluentAssertions;
using OfficeSystem.Application.Features.Teams.ChangeMemberRole;
using OfficeSystem.Application.Features.Teams.Contracts;
using OfficeSystem.Application.Features.Teams.CreateTeam;
using OfficeSystem.Application.Features.Teams.GetMyTeams;
using OfficeSystem.Application.Features.Teams.GetTeam;
using OfficeSystem.Application.Features.Teams.JoinTeam;
using OfficeSystem.Application.Features.Teams.RegenerateJoinCode;
using OfficeSystem.Application.Features.Teams.UpdateTeam;
using OfficeSystem.Application.Tests.Authentication;
using OfficeSystem.Application.Tests.Harness;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Tests.Teams;

public sealed class TeamUseCaseTests
{
    [Fact]
    public async Task CreateTeam_MakesTheCallerTheOwnerAndReturnsTheJoinCode()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        await AuthenticationUseCaseTests.RegisterAsync(harness);

        Result<TeamDetailResponse> result = await CreateTeamAsync(harness);

        result.IsSuccess.Should().BeTrue();
        result.Value.MyRole.Should().Be(TeamRole.Owner);
        result.Value.JoinCode.Should().NotBeNull().And.HaveLength(JoinCode.Length);
        result.Value.Members.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyTeams_ListsOnlyTheTeamsTheCallerBelongsTo()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");
        await CreateTeamAsync(harness, "Platform Team");

        await AuthenticationUseCaseTests.RegisterAsync(harness, "grace@example.com", "Grace");
        await CreateTeamAsync(harness, "Design Guild");

        Result<IReadOnlyList<TeamListItemResponse>> mine = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetMyTeamsQuery(), token));

        mine.Value.Select(t => t.Name).Should().Equal("Design Guild");
    }

    [Fact]
    public async Task GetTeam_ReportsNotFoundForANonMember()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");
        TeamDetailResponse team = (await CreateTeamAsync(harness)).Value;

        // A team you are not in is indistinguishable from one that does not exist.
        await AuthenticationUseCaseTests.RegisterAsync(harness, "stranger@example.com", "Stranger");

        Result<TeamDetailResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetTeamQuery(team.Id), token));

        result.Error.Should().Be(TeamErrors.NotFound);
    }

    [Fact]
    public async Task JoinTeam_AddsTheCallerAsAMemberAndHidesTheJoinCode()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");
        TeamDetailResponse team = (await CreateTeamAsync(harness)).Value;

        await AuthenticationUseCaseTests.RegisterAsync(harness, "grace@example.com", "Grace");

        Result<TeamDetailResponse> joined = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new JoinTeamCommand(team.JoinCode!), token));

        joined.IsSuccess.Should().BeTrue();
        joined.Value.MyRole.Should().Be(TeamRole.Member);
        joined.Value.Members.Should().HaveCount(2);
        joined.Value.JoinCode.Should().BeNull("a plain member must not see the invite secret");
    }

    [Fact]
    public async Task JoinTeam_AcceptsALowerCaseCode()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");
        TeamDetailResponse team = (await CreateTeamAsync(harness)).Value;

        await AuthenticationUseCaseTests.RegisterAsync(harness, "grace@example.com", "Grace");

        Result<TeamDetailResponse> joined = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new JoinTeamCommand(team.JoinCode!.ToLowerInvariant()), token));

        joined.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task JoinTeam_RejectsAnUnknownCode()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        await AuthenticationUseCaseTests.RegisterAsync(harness);

        Result<TeamDetailResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new JoinTeamCommand("ZZZZZZZZ"), token));

        result.Error.Should().Be(TeamErrors.JoinCodeNotRecognised);
    }

    [Fact]
    public async Task JoinTeam_RejectsASecondJoin()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");
        TeamDetailResponse team = (await CreateTeamAsync(harness)).Value;

        Result<TeamDetailResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new JoinTeamCommand(team.JoinCode!), token));

        result.Error.Should().Be(TeamErrors.AlreadyMember);
    }

    [Fact]
    public async Task UpdateTeam_IsRefusedForAPlainMember()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await TeamWithSecondMemberAsync(harness);

        Result<TeamDetailResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new UpdateTeamCommand(team.Id, "Hijacked", null), token));

        result.Error.Should().Be(TeamErrors.AdminRequired);
    }

    [Fact]
    public async Task RegenerateJoinCode_IsRefusedForAPlainMember()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await TeamWithSecondMemberAsync(harness);

        Result<string> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RegenerateJoinCodeCommand(team.Id), token));

        result.Error.Should().Be(TeamErrors.AdminRequired);
    }

    [Fact]
    public async Task RegenerateJoinCode_InvalidatesThePreviousCode()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");
        TeamDetailResponse team = (await CreateTeamAsync(harness)).Value;
        string original = team.JoinCode!;

        Result<string> reissued = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RegenerateJoinCodeCommand(team.Id), token));

        reissued.IsSuccess.Should().BeTrue();
        reissued.Value.Should().NotBe(original);

        await AuthenticationUseCaseTests.RegisterAsync(harness, "grace@example.com", "Grace");

        Result<TeamDetailResponse> withOldCode = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new JoinTeamCommand(original), token));

        withOldCode.Error.Should().Be(TeamErrors.JoinCodeNotRecognised);
    }

    [Fact]
    public async Task ChangeMemberRole_PromotesAMemberAndThenAllowsThemToManage()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        TeamDetailResponse team = await TeamWithSecondMemberAsync(harness);

        Guid memberId = harness.CurrentUser.UserId!.Value;
        Guid ownerId = team.Members.Single(m => m.Role == TeamRole.Owner).UserId;

        harness.CurrentUser.UserId = ownerId;
        Result promoted = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new ChangeMemberRoleCommand(team.Id, memberId, TeamRole.Admin), token));

        promoted.IsSuccess.Should().BeTrue();

        harness.CurrentUser.UserId = memberId;
        Result<TeamDetailResponse> renamed = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new UpdateTeamCommand(team.Id, "Platform & Tools", null), token));

        renamed.IsSuccess.Should().BeTrue();
        renamed.Value.Name.Should().Be("Platform & Tools");
        renamed.Value.JoinCode.Should().NotBeNull("an admin may see the invite code");
    }

    [Fact]
    public async Task ChangeMemberRole_RejectsAnUnknownRoleAtTheValidationStage()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");
        TeamDetailResponse team = (await CreateTeamAsync(harness)).Value;

        Result result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(
                new ChangeMemberRoleCommand(team.Id, Guid.CreateVersion7(), (TeamRole)42),
                token));

        result.Error.Should().BeOfType<ValidationError>();
    }

    [Fact]
    public async Task GetTeam_OrdersMembersOwnerFirstThenAdminsThenAlphabetically()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        await AuthenticationUseCaseTests.RegisterAsync(harness, "zoe@example.com", "Zoe Owner");
        TeamDetailResponse team = (await CreateTeamAsync(harness)).Value;
        Guid ownerId = harness.CurrentUser.UserId!.Value;

        await AuthenticationUseCaseTests.RegisterAsync(harness, "bob@example.com", "Bob Admin");
        Guid adminId = harness.CurrentUser.UserId!.Value;
        await harness.DispatchAsync((dispatcher, token) => dispatcher.SendAsync(new JoinTeamCommand(team.JoinCode!), token));

        await AuthenticationUseCaseTests.RegisterAsync(harness, "amy@example.com", "Amy Member");
        await harness.DispatchAsync((dispatcher, token) => dispatcher.SendAsync(new JoinTeamCommand(team.JoinCode!), token));

        harness.CurrentUser.UserId = ownerId;
        await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new ChangeMemberRoleCommand(team.Id, adminId, TeamRole.Admin), token));

        Result<TeamDetailResponse> reloaded = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.QueryAsync(new GetTeamQuery(team.Id), token));

        reloaded.Value.Members.Select(m => m.DisplayName)
            .Should().Equal("Zoe Owner", "Bob Admin", "Amy Member");
    }

    internal static Task<Result<TeamDetailResponse>> CreateTeamAsync(
        UseCaseHarness harness,
        string name = "Platform Team") =>
        harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new CreateTeamCommand(name, "Keeps the lights on."), token));

    /// <summary>Owner + one plain member; leaves the harness acting as the plain member.</summary>
    internal static async Task<TeamDetailResponse> TeamWithSecondMemberAsync(UseCaseHarness harness)
    {
        await AuthenticationUseCaseTests.RegisterAsync(harness, "ada@example.com", "Ada");
        TeamDetailResponse team = (await CreateTeamAsync(harness)).Value;

        await AuthenticationUseCaseTests.RegisterAsync(harness, "grace@example.com", "Grace");
        Result<TeamDetailResponse> joined = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new JoinTeamCommand(team.JoinCode!), token));

        joined.IsSuccess.Should().BeTrue();

        return team;
    }
}
