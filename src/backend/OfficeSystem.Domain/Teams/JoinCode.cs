using System.Security.Cryptography;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Teams;

/// <summary>
/// A short, human-shareable team invite code. The alphabet deliberately omits
/// characters that get confused when read aloud or copied by hand (0/O, 1/I/L).
/// </summary>
public sealed class JoinCode : ValueObject
{
    public const int Length = 8;

    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private JoinCode(string value) => Value = value;

    public string Value { get; }

    public static JoinCode NewCode()
    {
        char[] buffer = new char[Length];

        for (int i = 0; i < Length; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new JoinCode(new string(buffer));
    }

    public static Result<JoinCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TeamErrors.JoinCodeEmpty;
        }

        string normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length != Length || normalized.Any(c => !Alphabet.Contains(c)))
        {
            return TeamErrors.JoinCodeInvalid;
        }

        return new JoinCode(normalized);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }
}
