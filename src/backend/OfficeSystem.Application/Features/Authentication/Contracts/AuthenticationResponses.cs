namespace OfficeSystem.Application.Features.Authentication.Contracts;

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string AccentColor);

public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    AuthenticatedUserResponse User);
