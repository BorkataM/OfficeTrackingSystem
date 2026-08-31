using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Users;

public sealed class User : Entity, IAggregateRoot
{
    public const int DisplayNameMaxLength = 80;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    private readonly List<RefreshToken> _refreshTokens = [];

    private User(Guid id, Email email, string displayName, string passwordHash, string accentColor, DateTimeOffset createdAtUtc)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        AccentColor = accentColor;
        CreatedAtUtc = createdAtUtc;
    }

    // Required by EF Core's materialisation.
    private User()
    {
        Email = null!;
        DisplayName = null!;
        PasswordHash = null!;
        AccentColor = null!;
    }

    public Email Email { get; private set; }

    public string DisplayName { get; private set; }

    public string PasswordHash { get; private set; }

    /// <summary>Deterministic avatar colour, derived once at registration so the UI stays stable.</summary>
    public string AccentColor { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static Result<User> Register(Email email, string? displayName, string passwordHash, DateTimeOffset nowUtc)
    {
        Result<string> name = NormalizeDisplayName(displayName);

        if (name.IsFailure)
        {
            return Result.Failure<User>(name.Error);
        }

        Guid id = Guid.CreateVersion7();

        return new User(id, email, name.Value, passwordHash, AvatarPalette.ColorFor(id), nowUtc);
    }

    public Result Rename(string? displayName)
    {
        Result<string> name = NormalizeDisplayName(displayName);

        if (name.IsFailure)
        {
            return Result.Failure(name.Error);
        }

        DisplayName = name.Value;

        return Result.Success();
    }

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset nowUtc, TimeSpan lifetime)
    {
        RefreshToken token = RefreshToken.Issue(Id, tokenHash, nowUtc, lifetime);
        _refreshTokens.Add(token);

        return token;
    }

    public Result<RefreshToken> RotateRefreshToken(string presentedTokenHash, string replacementTokenHash, DateTimeOffset nowUtc, TimeSpan lifetime)
    {
        RefreshToken? existing = _refreshTokens.SingleOrDefault(t => t.TokenHash == presentedTokenHash);

        if (existing is null || !existing.IsActiveAt(nowUtc))
        {
            return UserErrors.RefreshTokenInvalid;
        }

        existing.Revoke(nowUtc);

        return IssueRefreshToken(replacementTokenHash, nowUtc, lifetime);
    }

    public void RevokeRefreshToken(string tokenHash, DateTimeOffset nowUtc)
        => _refreshTokens.SingleOrDefault(t => t.TokenHash == tokenHash)?.Revoke(nowUtc);

    public void RevokeAllRefreshTokens(DateTimeOffset nowUtc)
    {
        foreach (RefreshToken token in _refreshTokens.Where(t => t.IsActiveAt(nowUtc)))
        {
            token.Revoke(nowUtc);
        }
    }

    /// <summary>Drops tokens that can no longer be used, so the table does not grow forever.</summary>
    public void PruneExpiredRefreshTokens(DateTimeOffset nowUtc)
        => _refreshTokens.RemoveAll(t => !t.IsActiveAt(nowUtc));

    private static Result<string> NormalizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return UserErrors.DisplayNameEmpty;
        }

        string trimmed = displayName.Trim();

        return trimmed.Length > DisplayNameMaxLength
            ? UserErrors.DisplayNameTooLong
            : trimmed;
    }
}
