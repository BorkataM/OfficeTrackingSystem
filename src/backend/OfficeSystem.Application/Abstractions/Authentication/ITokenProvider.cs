using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Abstractions.Authentication;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// The raw refresh token goes to the client exactly once; only <see cref="Hash"/>
/// is ever persisted.
/// </summary>
public sealed record RefreshTokenPair(string Raw, string Hash);

public interface ITokenProvider
{
    AccessToken CreateAccessToken(User user);

    RefreshTokenPair CreateRefreshToken();

    string HashRefreshToken(string rawRefreshToken);

    TimeSpan RefreshTokenLifetime { get; }
}
