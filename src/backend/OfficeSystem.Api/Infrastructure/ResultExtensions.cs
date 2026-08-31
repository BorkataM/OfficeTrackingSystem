using OfficeSystem.Domain.Common;

namespace OfficeSystem.Api.Infrastructure;

/// <summary>
/// The single place where a domain <see cref="Error"/> becomes an HTTP status code.
/// Endpoints never choose a status themselves.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error);
    }

    public static IResult ToHttpResult<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);
    }

    public static IResult ToCreatedResult<TValue>(this Result<TValue> result, Func<TValue, string> location)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(location);

        return result.IsSuccess
            ? Results.Created(location(result.Value), result.Value)
            : Problem(result.Error);
    }

    private static IResult Problem(Error error)
    {
        if (error is ValidationError validation)
        {
            return Results.ValidationProblem(
                validation.Failures.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal),
                detail: validation.Description,
                title: "One or more fields need attention.",
                extensions: CodeExtension(error));
        }

        (int status, string title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "That request could not be accepted."),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not found."),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "That conflicts with the current state."),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Not signed in."),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Not allowed."),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong.")
        };

        return Results.Problem(
            detail: error.Description,
            statusCode: status,
            title: title,
            extensions: CodeExtension(error));
    }

    /// <summary>Ships the stable machine-readable code alongside the human-readable text.</summary>
    private static Dictionary<string, object?> CodeExtension(Error error)
        => new(StringComparer.Ordinal) { ["code"] = error.Code };
}
