using FluentValidation;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication.UpdateProfile;

public sealed record UpdateProfileCommand(string DisplayName) : ICommand<AuthenticatedUserResponse>;

internal sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
        => RuleFor(c => c.DisplayName)
            .NotEmpty().WithMessage("A display name is required.")
            .MaximumLength(User.DisplayNameMaxLength);
}

internal sealed class UpdateProfileCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateProfileCommand, AuthenticatedUserResponse>
{
    public async Task<Result<AuthenticatedUserResponse>> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        User? user = await users.FindByIdAsync(currentUser.RequireUserId(), cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<AuthenticatedUserResponse>(UserErrors.NotFound);
        }

        Result renamed = user.Rename(command.DisplayName);

        if (renamed.IsFailure)
        {
            return Result.Failure<AuthenticatedUserResponse>(renamed.Error);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return AuthenticationSessionFactory.Describe(user);
    }
}
