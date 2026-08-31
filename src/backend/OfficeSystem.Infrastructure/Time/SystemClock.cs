using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeSystem.Application.Abstractions.Time;

namespace OfficeSystem.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    private readonly TimeZoneInfo _officeZone;

    public SystemClock(IOptions<OfficeTimeOptions> options, ILogger<SystemClock> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        string configured = options.Value.TimeZone;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(configured, out TimeZoneInfo? zone))
        {
            _officeZone = zone;
        }
        else
        {
            logger.LogWarning(
                "Office time zone '{TimeZone}' was not found on this machine; falling back to UTC.",
                configured);

            _officeZone = TimeZoneInfo.Utc;
        }
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, _officeZone).DateTime);
}
