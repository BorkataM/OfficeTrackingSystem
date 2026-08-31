using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication;

/// <summary>
/// Issues a fresh session for a user. Shared by register, login and refresh so the
/// three paths cannot drift apart.
/// </summary>
internal sealed class AuthenticationSessionFactory(ITokenProvider tokenProvider, IClock clock)
{
    public AuthenticationResponse StartSession(User user)
    {
        user.PruneExpiredRefreshTokens(clock.UtcNow);

        RefreshTokenPair refreshToken = tokenProvider.CreateRefreshToken();
        user.IssueRefreshToken(refreshToken.Hash, clock.UtcNow, tokenProvider.RefreshTokenLifetime);

        return Build(user, tokenProvider.CreateAccessToken(user), refreshToken.Raw);
    }

    public AuthenticationResponse ContinueSession(User user, string rawRefreshToken)
        => Build(user, tokenProvider.CreateAccessToken(user), rawRefreshToken);

    private static AuthenticationResponse Build(User user, AccessToken accessToken, string rawRefreshToken)
        => new(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            Describe(user));

    public static AuthenticatedUserResponse Describe(User user)
        => new(user.Id, user.Email.Value, user.DisplayName, user.AccentColor);
}
