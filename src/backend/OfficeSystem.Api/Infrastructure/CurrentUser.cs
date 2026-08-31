using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using OfficeSystem.Application.Abstractions.Authentication;

namespace OfficeSystem.Api.Infrastructure;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;

            string? subject = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                              ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(subject, out Guid id) ? id : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;

    public Guid RequireUserId()
        => UserId ?? throw new InvalidOperationException(
            "No authenticated user on this request. The endpoint is missing RequireAuthorization().");
}
