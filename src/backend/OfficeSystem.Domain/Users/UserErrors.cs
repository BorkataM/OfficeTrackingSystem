using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailEmpty = Error.Validation(
        "user.email.empty", "An email address is required.");

    public static readonly Error EmailTooLong = Error.Validation(
        "user.email.tooLong", $"An email address may not exceed {Email.MaxLength} characters.");

    public static readonly Error EmailInvalid = Error.Validation(
        "user.email.invalid", "That does not look like a valid email address.");

    public static readonly Error EmailAlreadyInUse = Error.Conflict(
        "user.email.alreadyInUse", "An account with that email address already exists.");

    public static readonly Error DisplayNameEmpty = Error.Validation(
        "user.displayName.empty", "A display name is required.");

    public static readonly Error DisplayNameTooLong = Error.Validation(
        "user.displayName.tooLong", $"A display name may not exceed {User.DisplayNameMaxLength} characters.");

    public static readonly Error PasswordTooShort = Error.Validation(
        "user.password.tooShort", $"A password must be at least {User.PasswordMinLength} characters long.");

    public static readonly Error PasswordTooLong = Error.Validation(
        "user.password.tooLong", $"A password may not exceed {User.PasswordMaxLength} characters.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "user.credentials.invalid", "The email address or password is incorrect.");

    public static readonly Error NotFound = Error.NotFound(
        "user.notFound", "The user could not be found.");

    public static readonly Error RefreshTokenInvalid = Error.Unauthorized(
        "user.refreshToken.invalid", "The refresh token is invalid or has expired. Please sign in again.");
}
