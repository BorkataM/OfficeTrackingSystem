using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Users;

/// <summary>
/// Only the hash of a refresh token is persisted; the raw value exists solely in
/// the response to the client.
/// </summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    private RefreshToken() => TokenHash = null!;

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    internal static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset nowUtc, TimeSpan lifetime)
        => new(Guid.CreateVersion7(), userId, tokenHash, nowUtc, nowUtc.Add(lifetime));

    public bool IsActiveAt(DateTimeOffset nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    internal void Revoke(DateTimeOffset nowUtc) => RevokedAtUtc ??= nowUtc;
}
