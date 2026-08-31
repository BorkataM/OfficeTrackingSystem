using FluentValidation;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication.Contracts;

namespace OfficeSystem.Application.Features.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthenticationResponse>;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().WithMessage("An email address is required.");
        RuleFor(c => c.Password).NotEmpty().WithMessage("A password is required.");
    }
}
