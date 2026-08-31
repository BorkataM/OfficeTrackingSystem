using Microsoft.EntityFrameworkCore;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(OfficeSystemDbContext context) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default)
        => context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> FindByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash), cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
        => context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}
