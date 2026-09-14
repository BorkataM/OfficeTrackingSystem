using OfficeSystem.Application.Abstractions.Authentication;

namespace OfficeSystem.Infrastructure.Authentication;

/// <summary>BCrypt with a per-hash salt and an explicit work factor.</summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 10;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A stored hash we cannot parse is treated as a failed login, never as a crash.
            return false;
        }
    }
}
