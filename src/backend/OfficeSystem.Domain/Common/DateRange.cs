namespace OfficeSystem.Domain.Common;

/// <summary>
/// An inclusive range of calendar days. Bounded so a single schedule query can
/// never ask the database for an unbounded amount of rows.
/// </summary>
public sealed class DateRange : ValueObject
{
    public const int MaxDays = 120;

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public int LengthInDays => End.DayNumber - Start.DayNumber + 1;

    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            return DateRangeErrors.EndBeforeStart;
        }

        if (end.DayNumber - start.DayNumber + 1 > MaxDays)
        {
            return DateRangeErrors.TooLong;
        }

        return new DateRange(start, end);
    }

    /// <summary>Creates the ISO week (Monday..Sunday) that contains <paramref name="anchor"/>.</summary>
    public static DateRange WeekOf(DateOnly anchor)
    {
        int offsetToMonday = ((int)anchor.DayOfWeek + 6) % 7;
        DateOnly monday = anchor.AddDays(-offsetToMonday);

        return new DateRange(monday, monday.AddDays(6));
    }

    /// <summary>Creates the calendar month that contains <paramref name="anchor"/>.</summary>
    public static DateRange MonthOf(DateOnly anchor)
    {
        DateOnly first = new(anchor.Year, anchor.Month, 1);

        return new DateRange(first, first.AddMonths(1).AddDays(-1));
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public IEnumerable<DateOnly> Days()
    {
        for (DateOnly day = Start; day <= End; day = day.AddDays(1))
        {
            yield return day;
        }
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Start;
        yield return End;
    }
}

public static class DateRangeErrors
{
    public static readonly Error EndBeforeStart = Error.Validation(
        "dateRange.endBeforeStart",
        "The end date must not be earlier than the start date.");

    public static readonly Error TooLong = Error.Validation(
        "dateRange.tooLong",
        $"A date range may not span more than {DateRange.MaxDays} days.");
}
