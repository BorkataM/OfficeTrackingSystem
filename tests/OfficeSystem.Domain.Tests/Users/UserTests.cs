using FluentAssertions;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Domain.Tests.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    [Fact]
    public void Register_NormalisesTheDisplayNameAndPicksAnAccentColour()
    {
        User user = Register("  Ada Lovelace  ");

        user.DisplayName.Should().Be("Ada Lovelace");
        user.AccentColor.Should().MatchRegex("^#[0-9a-f]{6}$");
        user.CreatedAtUtc.Should().Be(Now);
        user.RefreshTokens.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_RejectsABlankDisplayName(string? displayName)
    {
        Result<User> result = User.Register(Email.Create("ada@example.com").Value, displayName, "hash", Now);

        result.Error.Should().Be(UserErrors.DisplayNameEmpty);
    }

    [Fact]
    public void Register_RejectsADisplayNameOverTheLimit()
    {
        Result<User> result = User.Register(
            Email.Create("ada@example.com").Value,
            new string('x', User.DisplayNameMaxLength + 1),
            "hash",
            Now);

        result.Error.Should().Be(UserErrors.DisplayNameTooLong);
    }

    [Fact]
    public void IssueRefreshToken_AddsAnActiveToken()
    {
        User user = Register();

        RefreshToken token = user.IssueRefreshToken("hash-1", Now, Lifetime);

        token.IsActiveAt(Now).Should().BeTrue();
        token.ExpiresAtUtc.Should().Be(Now.Add(Lifetime));
        user.RefreshTokens.Should().HaveCount(1);
    }

    [Fact]
    public void RotateRefreshToken_RevokesThePresentedTokenAndIssuesANewOne()
    {
        User user = Register();
        user.IssueRefreshToken("hash-1", Now, Lifetime);

        Result<RefreshToken> result = user.RotateRefreshToken("hash-1", "hash-2", Now, Lifetime);

        result.IsSuccess.Should().BeTrue();
        result.Value.TokenHash.Should().Be("hash-2");
        user.RefreshTokens.Single(t => t.TokenHash == "hash-1").IsActiveAt(Now).Should().BeFalse();
        user.RefreshTokens.Single(t => t.TokenHash == "hash-2").IsActiveAt(Now).Should().BeTrue();
    }

    [Fact]
    public void RotateRefreshToken_RefusesAReplayOfAnAlreadyRotatedToken()
    {
        User user = Register();
        user.IssueRefreshToken("hash-1", Now, Lifetime);
        user.RotateRefreshToken("hash-1", "hash-2", Now, Lifetime);

        Result<RefreshToken> replay = user.RotateRefreshToken("hash-1", "hash-3", Now, Lifetime);

        replay.Error.Should().Be(UserErrors.RefreshTokenInvalid);
        user.RefreshTokens.Should().NotContain(t => t.TokenHash == "hash-3");
    }

    [Fact]
    public void RotateRefreshToken_RefusesAnExpiredToken()
    {
        User user = Register();
        user.IssueRefreshToken("hash-1", Now, Lifetime);

        Result<RefreshToken> result = user.RotateRefreshToken(
            "hash-1",
            "hash-2",
            Now.Add(Lifetime).AddSeconds(1),
            Lifetime);

        result.Error.Should().Be(UserErrors.RefreshTokenInvalid);
    }

    [Fact]
    public void RotateRefreshToken_RefusesAnUnknownToken()
    {
        User user = Register();
        user.IssueRefreshToken("hash-1", Now, Lifetime);

        Result<RefreshToken> result = user.RotateRefreshToken("never-issued", "hash-2", Now, Lifetime);

        result.Error.Should().Be(UserErrors.RefreshTokenInvalid);
    }

    [Fact]
    public void RevokeAllRefreshTokens_LeavesNothingUsable()
    {
        User user = Register();
        user.IssueRefreshToken("hash-1", Now, Lifetime);
        user.IssueRefreshToken("hash-2", Now, Lifetime);

        user.RevokeAllRefreshTokens(Now);

        user.RefreshTokens.Should().OnlyContain(t => !t.IsActiveAt(Now));
    }

    [Fact]
    public void PruneExpiredRefreshTokens_KeepsOnlyTheUsableOnes()
    {
        User user = Register();
        user.IssueRefreshToken("expired", Now, TimeSpan.FromDays(1));
        user.IssueRefreshToken("active", Now, Lifetime);

        user.PruneExpiredRefreshTokens(Now.AddDays(2));

        user.RefreshTokens.Select(t => t.TokenHash).Should().Equal("active");
    }

    [Fact]
    public void Rename_TrimsAndAppliesTheNewName()
    {
        User user = Register();

        Result result = user.Rename("  Ada King  ");

        result.IsSuccess.Should().BeTrue();
        user.DisplayName.Should().Be("Ada King");
    }

    private static User Register(string displayName = "Ada Lovelace")
        => User.Register(Email.Create("ada@example.com").Value, displayName, "hash", Now).Value;
}
