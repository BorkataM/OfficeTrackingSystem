using System.Text.RegularExpressions;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Users;

public sealed partial class Email : ValueObject
{
    public const int MaxLength = 256;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return UserErrors.EmailEmpty;
        }

        string normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            return UserErrors.EmailTooLong;
        }

        if (!EmailPattern().IsMatch(normalized))
        {
            return UserErrors.EmailInvalid;
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
