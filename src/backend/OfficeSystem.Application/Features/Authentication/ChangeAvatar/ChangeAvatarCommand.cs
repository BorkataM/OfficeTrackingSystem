using FluentValidation;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication.ChangeAvatar;

public sealed record ChangeAvatarCommand(string? Avatar) : ICommand<AuthenticatedUserResponse>;

internal sealed class ChangeAvatarCommandValidator : AbstractValidator<ChangeAvatarCommand>
{
    public ChangeAvatarCommandValidator()
        => RuleFor(c => c.Avatar).MaximumLength(AvatarChoice.MaxLength);
}

internal sealed class ChangeAvatarCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<ChangeAvatarCommand, AuthenticatedUserResponse>
{
    public async Task<Result<AuthenticatedUserResponse>> HandleAsync(ChangeAvatarCommand command, CancellationToken cancellationToken)
    {
        User? user = await users.FindByIdAsync(currentUser.RequireUserId(), cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<AuthenticatedUserResponse>(UserErrors.NotFound);
        }

        Result changed = user.ChangeAvatar(command.Avatar);

        if (changed.IsFailure)
        {
            return Result.Failure<AuthenticatedUserResponse>(changed.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return AuthenticationSessionFactory.Describe(user);
    }
}
