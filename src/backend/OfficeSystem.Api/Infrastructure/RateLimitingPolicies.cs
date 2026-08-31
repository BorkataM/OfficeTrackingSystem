namespace OfficeSystem.Api.Infrastructure;

public static class RateLimitingPolicies
{
    /// <summary>
    /// Applied to the anonymous auth endpoints, which are the only ones an attacker
    /// can hammer without a token.
    /// </summary>
    public const string Authentication = "authentication";
}
