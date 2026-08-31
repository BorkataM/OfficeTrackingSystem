using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Infrastructure.Authentication;

internal sealed class JwtTokenProvider : ITokenProvider
{
    private const int RefreshTokenBytes = 32;

    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenProvider(IOptions<JwtOptions> options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _clock = clock;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken CreateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTimeOffset expiresAt = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = _signingCredentials,
            IssuedAt = _clock.UtcNow.UtcDateTime,
            NotBefore = _clock.UtcNow.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email.Value,
                [ClaimTypes.Name] = user.DisplayName
            }
        };

        JsonWebTokenHandler handler = new();

        return new AccessToken(handler.CreateToken(descriptor), expiresAt);
    }

    public RefreshTokenPair CreateRefreshToken()
    {
        string raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

        return new RefreshTokenPair(raw, HashRefreshToken(raw));
    }

    /// <summary>
    /// A plain SHA-256 is the right tool here: the token is already 256 bits of
    /// entropy, so there is nothing for a slow KDF to protect against.
    /// </summary>
    public string HashRefreshToken(string rawRefreshToken)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));
}
