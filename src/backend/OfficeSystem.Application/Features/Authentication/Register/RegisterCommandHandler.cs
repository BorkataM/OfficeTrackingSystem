using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    AuthenticationSessionFactory sessionFactory,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<RegisterCommand, AuthenticationResponse>
{
    public async Task<Result<AuthenticationResponse>> HandleAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        Result<Email> email = Email.Create(command.Email);

        if (email.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(email.Error);
        }

        if (await users.EmailExistsAsync(email.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<AuthenticationResponse>(UserErrors.EmailAlreadyInUse);
        }

        Result<User> user = User.Register(
            email.Value,
            command.DisplayName,
            passwordHasher.Hash(command.Password),
            clock.UtcNow);

        if (user.IsFailure)
        {
            return Result.Failure<AuthenticationResponse>(user.Error);
        }

        AuthenticationResponse session = sessionFactory.StartSession(user.Value);

        users.Add(user.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session;
    }
}
