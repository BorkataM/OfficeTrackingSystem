using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication.Login;

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    AuthenticationSessionFactory sessionFactory,
    IUnitOfWork unitOfWork) : ICommandHandler<LoginCommand, AuthenticationResponse>
{
    public async Task<Result<AuthenticationResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        Result<Email> email = Email.Create(command.Email);

        // A malformed address and a wrong password answer identically, so the
        // endpoint cannot be used to probe which accounts exist.
        if (email.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(UserErrors.InvalidCredentials);
        }

        User? user = await users.FindByEmailAsync(email.Value, cancellationToken).ConfigureAwait(false);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failure<AuthenticationResponse>(UserErrors.InvalidCredentials);
        }

        AuthenticationResponse session = sessionFactory.StartSession(user);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }
}
