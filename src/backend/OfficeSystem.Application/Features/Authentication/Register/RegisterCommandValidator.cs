using FluentValidation;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Application.Features.Authentication.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("An email address is required.")
            .MaximumLength(Email.MaxLength);

        RuleFor(c => c.DisplayName)
            .NotEmpty().WithMessage("A display name is required.")
            .MaximumLength(User.DisplayNameMaxLength);

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("A password is required.")
            .MinimumLength(User.PasswordMinLength)
                .WithMessage($"Use at least {User.PasswordMinLength} characters.")
            .MaximumLength(User.PasswordMaxLength)
            .Must(HasEnoughVariety)
                .WithMessage("Mix letters with a number or a symbol.");
    }

    private static bool HasEnoughVariety(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        bool hasLetter = password.Any(char.IsLetter);
        bool hasNonLetter = password.Any(c => !char.IsLetter(c));

        return hasLetter && hasNonLetter;
    }
}
