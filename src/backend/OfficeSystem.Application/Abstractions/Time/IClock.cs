namespace OfficeSystem.Application.Abstractions.Time;

/// <summary>
/// Time as a dependency, so use cases stay deterministic under test.
/// <see cref="Today"/> is the office's local calendar day — attendance is planned
/// against office days, not UTC days.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}
