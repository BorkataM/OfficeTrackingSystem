using FluentValidation;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication.Contracts;

namespace OfficeSystem.Application.Features.Authentication.RefreshSession;

public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<AuthenticationResponse>;

internal sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
        => RuleFor(c => c.RefreshToken).NotEmpty().WithMessage("A refresh token is required.");
}
