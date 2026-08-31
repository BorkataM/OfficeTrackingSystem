using FluentAssertions;
using OfficeSystem.Domain.Attendance;
using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Tests.Attendance;

public sealed class AttendanceEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 8, 28);
    private static readonly Guid TeamId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    [Fact]
    public void Create_StoresTheDayAsGiven()
    {
        AttendanceEntry entry = Create(Today, AttendanceStatus.Office, "At a desk").Value;

        entry.TeamId.Should().Be(TeamId);
        entry.UserId.Should().Be(UserId);
        entry.Date.Should().Be(Today);
        entry.Status.Should().Be(AttendanceStatus.Office);
        entry.Note.Should().Be("At a desk");
        entry.UpdatedAtUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_TreatsABlankNoteAsNoNote(string? note)
    {
        AttendanceEntry entry = Create(Today, AttendanceStatus.Remote, note).Value;

        entry.Note.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsTheNote()
    {
        AttendanceEntry entry = Create(Today, AttendanceStatus.Remote, "  Focus day  ").Value;

        entry.Note.Should().Be("Focus day");
    }

    [Fact]
    public void Create_RejectsANoteOverTheLimit()
    {
        Result<AttendanceEntry> result = Create(Today, AttendanceStatus.Office, new string('x', AttendanceEntry.NoteMaxLength + 1));

        result.Error.Should().Be(AttendanceErrors.NoteTooLong);
    }

    [Fact]
    public void Create_RejectsAnUnknownStatus()
    {
        Result<AttendanceEntry> result = Create(Today, (AttendanceStatus)99, null);

        result.Error.Should().Be(AttendanceErrors.StatusUnknown);
    }

    [Fact]
    public void Create_AcceptsTheOldestEditableDay()
    {
        Result<AttendanceEntry> result = Create(Today.AddDays(-AttendanceEntry.MaxDaysInThePast), AttendanceStatus.Office, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_RejectsADayBeyondThePastWindow()
    {
        Result<AttendanceEntry> result = Create(Today.AddDays(-AttendanceEntry.MaxDaysInThePast - 1), AttendanceStatus.Office, null);

        result.Error.Should().Be(AttendanceErrors.TooFarInThePast);
    }

    [Fact]
    public void Create_AcceptsTheFurthestPlannableDay()
    {
        Result<AttendanceEntry> result = Create(Today.AddDays(AttendanceEntry.MaxDaysInTheFuture), AttendanceStatus.Office, null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_RejectsADayBeyondTheFutureWindow()
    {
        Result<AttendanceEntry> result = Create(Today.AddDays(AttendanceEntry.MaxDaysInTheFuture + 1), AttendanceStatus.Office, null);

        result.Error.Should().Be(AttendanceErrors.TooFarInTheFuture);
    }

    [Fact]
    public void Update_ReplacesTheStatusNoteAndTimestamp()
    {
        AttendanceEntry entry = Create(Today, AttendanceStatus.Office, "First").Value;
        DateTimeOffset later = Now.AddHours(3);

        Result result = entry.Update(AttendanceStatus.Away, "Second", Today, later);

        result.IsSuccess.Should().BeTrue();
        entry.Status.Should().Be(AttendanceStatus.Away);
        entry.Note.Should().Be("Second");
        entry.UpdatedAtUtc.Should().Be(later);
    }

    [Fact]
    public void Update_LeavesTheEntryUntouchedWhenRejected()
    {
        AttendanceEntry entry = Create(Today, AttendanceStatus.Office, "Keep me").Value;

        Result result = entry.Update((AttendanceStatus)99, "Changed", Today, Now.AddHours(1));

        result.Error.Should().Be(AttendanceErrors.StatusUnknown);
        entry.Status.Should().Be(AttendanceStatus.Office);
        entry.Note.Should().Be("Keep me");
        entry.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Update_IsRejectedOnceTheDayHasAgedOutOfTheWindow()
    {
        DateOnly longAgo = Today.AddDays(-10);
        AttendanceEntry entry = Create(longAgo, AttendanceStatus.Office, null).Value;

        // Same entry, but "today" has moved far enough that the day is now locked.
        Result result = entry.Update(AttendanceStatus.Remote, null, longAgo.AddDays(AttendanceEntry.MaxDaysInThePast + 1), Now);

        result.Error.Should().Be(AttendanceErrors.TooFarInThePast);
        entry.Status.Should().Be(AttendanceStatus.Office);
    }

    private static Result<AttendanceEntry> Create(DateOnly date, AttendanceStatus status, string? note)
        => AttendanceEntry.Create(TeamId, UserId, date, status, note, Today, Now);
}
