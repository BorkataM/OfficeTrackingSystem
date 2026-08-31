using OfficeSystem.Api.Infrastructure;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication.Contracts;
using OfficeSystem.Application.Features.Authentication.GetCurrentUser;
using OfficeSystem.Application.Features.Authentication.Login;
using OfficeSystem.Application.Features.Authentication.RefreshSession;
using OfficeSystem.Application.Features.Authentication.Register;
using OfficeSystem.Application.Features.Authentication.SignOut;
using OfficeSystem.Application.Features.Authentication.UpdateProfile;

namespace OfficeSystem.Api.Endpoints;

internal sealed class AuthenticationEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        RouteGroupBuilder group = routes.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (
                RegisterCommand command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(command, cancellationToken)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingPolicies.Authentication)
            .WithSummary("Creates an account and starts a session.")
            .Produces<AuthenticationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (
                LoginCommand command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(command, cancellationToken)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingPolicies.Authentication)
            .WithSummary("Signs in with an email address and password.")
            .Produces<AuthenticationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (
                RefreshSessionCommand command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(command, cancellationToken)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingPolicies.Authentication)
            .WithSummary("Exchanges a refresh token for a new session.")
            .Produces<AuthenticationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/sign-out", async (
                SignOutCommand command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(command, cancellationToken)).ToHttpResult())
            .RequireAuthorization()
            .WithSummary("Revokes the given refresh token, or every token when none is supplied.");

        group.MapGet("/me", async (
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.QueryAsync(new GetCurrentUserQuery(), cancellationToken)).ToHttpResult())
            .RequireAuthorization()
            .WithSummary("Returns the signed-in user.")
            .Produces<AuthenticatedUserResponse>();

        group.MapPut("/me", async (
                UpdateProfileCommand command,
                IDispatcher dispatcher,
                CancellationToken cancellationToken) =>
                (await dispatcher.SendAsync(command, cancellationToken)).ToHttpResult())
            .RequireAuthorization()
            .WithSummary("Updates the signed-in user's display name.")
            .Produces<AuthenticatedUserResponse>()
            .ProducesValidationProblem();
    }
}
