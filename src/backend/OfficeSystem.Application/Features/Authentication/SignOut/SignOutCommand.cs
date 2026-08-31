using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication.SignOut;

/// <summary>Revokes the presented refresh token, or every token when none is given.</summary>
public sealed record SignOutCommand(string? RefreshToken) : ICommand;

internal sealed class SignOutCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    ITokenProvider tokenProvider,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<SignOutCommand>
{
    public async Task<Result> HandleAsync(SignOutCommand command, CancellationToken cancellationToken)
    {
        User? user = await users.FindByIdAsync(currentUser.RequireUserId(), cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound);
        }

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            user.RevokeAllRefreshTokens(clock.UtcNow);
        }
        else
        {
            user.RevokeRefreshToken(tokenProvider.HashRefreshToken(command.RefreshToken), clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
