using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Abstractions.Data;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Loads the user together with their refresh tokens, for rotation.</summary>
    Task<User?> FindByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    void Add(User user);
}
