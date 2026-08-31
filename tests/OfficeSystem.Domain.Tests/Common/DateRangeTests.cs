using FluentAssertions;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Tests.Common;

public sealed class DateRangeTests
{
    [Fact]
    public void Create_AcceptsASingleDay()
    {
        DateOnly day = new(2026, 8, 28);

        DateRange range = DateRange.Create(day, day).Value;

        range.LengthInDays.Should().Be(1);
        range.Days().Should().Equal(day);
    }

    [Fact]
    public void Create_RejectsAnInvertedRange()
    {
        Result<DateRange> result = DateRange.Create(new DateOnly(2026, 8, 28), new DateOnly(2026, 8, 27));

        result.Error.Should().Be(DateRangeErrors.EndBeforeStart);
    }

    [Fact]
    public void Create_AcceptsTheLongestAllowedRange()
    {
        DateOnly start = new(2026, 1, 1);

        Result<DateRange> result = DateRange.Create(start, start.AddDays(DateRange.MaxDays - 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.LengthInDays.Should().Be(DateRange.MaxDays);
    }

    [Fact]
    public void Create_RejectsARangeOverTheLimit()
    {
        DateOnly start = new(2026, 1, 1);

        Result<DateRange> result = DateRange.Create(start, start.AddDays(DateRange.MaxDays));

        result.Error.Should().Be(DateRangeErrors.TooLong);
    }

    [Theory]
    // Every day of one week must resolve to the same Monday.
    [InlineData("2026-08-24")] // Monday
    [InlineData("2026-08-26")] // Wednesday
    [InlineData("2026-08-30")] // Sunday
    public void WeekOf_AlwaysStartsOnMondayAndSpansSevenDays(string anchor)
    {
        DateRange week = DateRange.WeekOf(DateOnly.Parse(anchor, null));

        week.Start.Should().Be(new DateOnly(2026, 8, 24));
        week.End.Should().Be(new DateOnly(2026, 8, 30));
        week.Start.DayOfWeek.Should().Be(DayOfWeek.Monday);
        week.LengthInDays.Should().Be(7);
    }

    [Fact]
    public void MonthOf_CoversTheWholeCalendarMonth()
    {
        DateRange month = DateRange.MonthOf(new DateOnly(2026, 8, 15));

        month.Start.Should().Be(new DateOnly(2026, 8, 1));
        month.End.Should().Be(new DateOnly(2026, 8, 31));
    }

    [Fact]
    public void MonthOf_HandlesFebruaryInALeapYear()
    {
        DateRange month = DateRange.MonthOf(new DateOnly(2028, 2, 10));

        month.End.Should().Be(new DateOnly(2028, 2, 29));
    }

    [Fact]
    public void Contains_IsInclusiveOnBothEnds()
    {
        DateRange week = DateRange.WeekOf(new DateOnly(2026, 8, 26));

        week.Contains(week.Start).Should().BeTrue();
        week.Contains(week.End).Should().BeTrue();
        week.Contains(week.Start.AddDays(-1)).Should().BeFalse();
        week.Contains(week.End.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void Equality_ComparesTheEndpoints()
    {
        DateRange left = DateRange.WeekOf(new DateOnly(2026, 8, 24));
        DateRange right = DateRange.WeekOf(new DateOnly(2026, 8, 30));

        left.Should().Be(right);
    }
}
