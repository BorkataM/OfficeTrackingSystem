using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Application.Features.Authentication.Login;
using OfficeSystem.Application.Features.Authentication.RefreshSession;
using OfficeSystem.Application.Features.Authentication.Register;
using OfficeSystem.Application.Tests.Harness;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Tests.Authentication;

public sealed class AuthenticationUseCaseTests
{
    [Fact]
    public async Task Register_PersistsTheUserAndReturnsASession()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        Result<AuthenticationResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RegisterCommand("Ada@Example.com", "  Ada Lovelace  ", "Passw0rd!"), token));

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be("ada@example.com");
        result.Value.User.DisplayName.Should().Be("Ada Lovelace");
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

        int stored = await harness.QueryAsync(db => db.Users.CountAsync());
        stored.Should().Be(1);
    }

    [Fact]
    public async Task Register_StoresAHashRatherThanThePassword()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        await RegisterAsync(harness);

        User user = await harness.QueryAsync(db => db.Users.SingleAsync());

        user.PasswordHash.Should().NotContain("Passw0rd!");
        user.PasswordHash.Should().StartWith("$2");
    }

    [Fact]
    public async Task Register_StoresOnlyTheHashOfTheRefreshToken()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        AuthenticationResponse session = await RegisterAsync(harness);

        List<string> hashes = await harness.QueryAsync(db => db.Set<RefreshToken>().Select(t => t.TokenHash).ToListAsync());

        hashes.Should().HaveCount(1);
        hashes[0].Should().NotBe(session.RefreshToken);
    }

    [Fact]
    public async Task Register_RejectsAnEmailAlreadyInUseRegardlessOfCasing()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        await RegisterAsync(harness);

        Result<AuthenticationResponse> second = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RegisterCommand("ADA@EXAMPLE.COM", "Impostor", "Passw0rd!"), token));

        second.Error.Should().Be(UserErrors.EmailAlreadyInUse);
    }

    [Fact]
    public async Task Register_RunsTheValidationStageBeforeTheHandler()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();

        Result<AuthenticationResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RegisterCommand("ada@example.com", "Ada", "short"), token));

        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).Failures.Should().ContainKey("password");

        int stored = await harness.QueryAsync(db => db.Users.CountAsync());
        stored.Should().Be(0);
    }

    [Fact]
    public async Task Login_SucceedsWithADifferentlyCasedEmail()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        await RegisterAsync(harness);

        Result<AuthenticationResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new LoginCommand("  ADA@example.COM ", "Passw0rd!"), token));

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be("ada@example.com");
    }

    [Fact]
    public async Task Login_ReportsTheSameErrorForAWrongPasswordAndAnUnknownAccount()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        await RegisterAsync(harness);

        Result<AuthenticationResponse> wrongPassword = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new LoginCommand("ada@example.com", "NotMyPassword1!"), token));

        Result<AuthenticationResponse> unknownAccount = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new LoginCommand("nobody@example.com", "Passw0rd!"), token));

        Result<AuthenticationResponse> malformedEmail = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new LoginCommand("not-an-email", "Passw0rd!"), token));

        wrongPassword.Error.Should().Be(UserErrors.InvalidCredentials);
        unknownAccount.Error.Should().Be(UserErrors.InvalidCredentials);
        malformedEmail.Error.Should().Be(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Login_IssuesAnAdditionalRefreshTokenPerSession()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        await RegisterAsync(harness);

        await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new LoginCommand("ada@example.com", "Passw0rd!"), token));

        int tokens = await harness.QueryAsync(db => db.Set<RefreshToken>().CountAsync());

        // One from registering, one from signing in: a second device does not evict the first.
        tokens.Should().Be(2);
    }

    [Fact]
    public async Task Refresh_RotatesTheTokenAndRefusesAReplay()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        AuthenticationResponse session = await RegisterAsync(harness);

        Result<AuthenticationResponse> rotated = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RefreshSessionCommand(session.RefreshToken), token));

        rotated.IsSuccess.Should().BeTrue();
        rotated.Value.RefreshToken.Should().NotBe(session.RefreshToken);

        Result<AuthenticationResponse> replay = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RefreshSessionCommand(session.RefreshToken), token));

        replay.Error.Should().Be(UserErrors.RefreshTokenInvalid);
    }

    [Fact]
    public async Task Refresh_RefusesATokenThatHasExpired()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        AuthenticationResponse session = await RegisterAsync(harness);

        harness.Clock.Advance(TimeSpan.FromDays(400));

        Result<AuthenticationResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RefreshSessionCommand(session.RefreshToken), token));

        result.Error.Should().Be(UserErrors.RefreshTokenInvalid);
    }

    [Fact]
    public async Task Refresh_RefusesATokenThatWasNeverIssued()
    {
        await using UseCaseHarness harness = await UseCaseHarness.CreateAsync();
        await RegisterAsync(harness);

        Result<AuthenticationResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RefreshSessionCommand("a-token-nobody-issued"), token));

        result.Error.Should().Be(UserErrors.RefreshTokenInvalid);
    }

    internal static async Task<AuthenticationResponse> RegisterAsync(
        UseCaseHarness harness,
        string email = "ada@example.com",
        string displayName = "Ada Lovelace")
    {
        Result<AuthenticationResponse> result = await harness.DispatchAsync((dispatcher, token) =>
            dispatcher.SendAsync(new RegisterCommand(email, displayName, "Passw0rd!"), token));

        result.IsSuccess.Should().BeTrue($"registering {email} should succeed but failed with {result.Error.Code}");
        harness.CurrentUser.UserId = result.Value.User.Id;

        return result.Value;
    }
}
