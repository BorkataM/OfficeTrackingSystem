namespace OfficeSystem.Application.Abstractions.Authentication;

/// <summary>
/// The authenticated caller, supplied by the host. Use cases never touch
/// <c>HttpContext</c> themselves.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>The caller's id, or throws if the endpoint was reached unauthenticated.</summary>
    Guid RequireUserId();
}
