using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication.RefreshSession;

/// <summary>
/// Rotates the refresh token on every use: the presented token is revoked and a new
/// one issued, so a stolen token is single-use at best.
/// </summary>
internal sealed class RefreshSessionCommandHandler(
    IUserRepository users,
    ITokenProvider tokenProvider,
    AuthenticationSessionFactory sessionFactory,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<RefreshSessionCommand, AuthenticationResponse>
{
    public async Task<Result<AuthenticationResponse>> HandleAsync(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        string presentedHash = tokenProvider.HashRefreshToken(command.RefreshToken);

        User? user = await users.FindByRefreshTokenHashAsync(presentedHash, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<AuthenticationResponse>(UserErrors.RefreshTokenInvalid);
        }

        RefreshTokenPair replacement = tokenProvider.CreateRefreshToken();

        Result<RefreshToken> rotated = user.RotateRefreshToken(
            presentedHash,
            replacement.Hash,
            clock.UtcNow,
            tokenProvider.RefreshTokenLifetime);

        if (rotated.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(rotated.Error);
        }

        user.PruneExpiredRefreshTokens(clock.UtcNow);

        AuthenticationResponse session = sessionFactory.ContinueSession(user, replacement.Raw);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }
}
