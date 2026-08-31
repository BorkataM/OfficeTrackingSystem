using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication.Contracts;

namespace OfficeSystem.Application.Features.Authentication.Register;

public sealed record RegisterCommand(string Email, string DisplayName, string Password)
    : ICommand<AuthenticationResponse>;
