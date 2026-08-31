using System.Collections.Concurrent;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Application.Abstractions.Messaging;

/// <summary>
/// The validation stage of the request pipeline: shape-level rules live in
/// FluentValidation validators, business rules stay in the domain.
/// </summary>
internal sealed class RequestValidator(IServiceProvider serviceProvider)
{
    private static readonly ConcurrentDictionary<Type, Type> ValidatorTypes = new();

    public async Task<Result> ValidateAsync(object request, CancellationToken cancellationToken)
    {
        Type validatorType = ValidatorTypes.GetOrAdd(
            request.GetType(),
            static requestType => typeof(IValidator<>).MakeGenericType(requestType));

        IEnumerable<object?> validators = serviceProvider.GetServices(validatorType);

        List<ValidationFailure> failures = [];

        foreach (object? candidate in validators)
        {
            if (candidate is not IValidator validator)
            {
                continue;
            }

            ValidationContext<object> context = new(request);
            ValidationResult result = await validator.ValidateAsync(context, cancellationToken).ConfigureAwait(false);

            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count == 0)
        {
            return Result.Success();
        }

        Dictionary<string, string[]> grouped = failures
            .GroupBy(f => ToCamelCase(f.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray(), StringComparer.Ordinal);

        return Result.Failure(new ValidationError(grouped));
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        string[] segments = propertyName.Split('.');

        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];

            if (segment.Length > 0 && char.IsUpper(segment[0]))
            {
                segments[i] = char.ToLowerInvariant(segment[0]) + segment[1..];
            }
        }

        return string.Join('.', segments);
    }
}
