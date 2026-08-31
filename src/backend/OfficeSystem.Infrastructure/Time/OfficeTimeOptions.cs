namespace OfficeSystem.Infrastructure.Time;

public sealed class OfficeTimeOptions
{
    public const string SectionName = "OfficeTime";

    /// <summary>
    /// The zone whose calendar day counts as "today" for attendance. An IANA id
    /// ("Europe/Berlin") or a Windows id both resolve on .NET 10.
    /// </summary>
    public string TimeZone { get; init; } = "Europe/Berlin";
}
