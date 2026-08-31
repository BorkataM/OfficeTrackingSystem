using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Attendance;

/// <summary>
/// One person's plan for one day in one team. The absence of an entry means
/// "not planned yet" — it is never stored as a status of its own.
/// </summary>
public sealed class AttendanceEntry : Entity, IAggregateRoot
{
    public const int NoteMaxLength = 200;
    public const int MaxDaysInThePast = 30;
    public const int MaxDaysInTheFuture = 365;
    public const int MaxDaysPerBulkRequest = 62;

    private AttendanceEntry(
        Guid id,
        Guid teamId,
        Guid userId,
        DateOnly date,
        AttendanceStatus status,
        string? note,
        DateTimeOffset updatedAtUtc)
        : base(id)
    {
        TeamId = teamId;
        UserId = userId;
        Date = date;
        Status = status;
        Note = note;
        UpdatedAtUtc = updatedAtUtc;
    }

    private AttendanceEntry()
    {
    }

    public Guid TeamId { get; private set; }

    public Guid UserId { get; private set; }

    public DateOnly Date { get; private set; }

    public AttendanceStatus Status { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Result<AttendanceEntry> Create(
        Guid teamId,
        Guid userId,
        DateOnly date,
        AttendanceStatus status,
        string? note,
        DateOnly today,
        DateTimeOffset nowUtc)
    {
        Result validated = Validate(date, status, note, today);

        if (validated.IsFailure)
        {
            return Result.Failure<AttendanceEntry>(validated.Error);
        }

        return new AttendanceEntry(
            Guid.CreateVersion7(),
            teamId,
            userId,
            date,
            status,
            NormalizeNote(note),
            nowUtc);
    }

    public Result Update(AttendanceStatus status, string? note, DateOnly today, DateTimeOffset nowUtc)
    {
        Result validated = Validate(Date, status, note, today);

        if (validated.IsFailure)
        {
            return validated;
        }

        Status = status;
        Note = NormalizeNote(note);
        UpdatedAtUtc = nowUtc;

        return Result.Success();
    }

    public static Result EnsureDateIsEditable(DateOnly date, DateOnly today)
    {
        int offset = date.DayNumber - today.DayNumber;

        if (offset < -MaxDaysInThePast)
        {
            return Result.Failure(AttendanceErrors.TooFarInThePast);
        }

        return offset > MaxDaysInTheFuture
            ? Result.Failure(AttendanceErrors.TooFarInTheFuture)
            : Result.Success();
    }

    private static Result Validate(DateOnly date, AttendanceStatus status, string? note, DateOnly today)
    {
        if (!Enum.IsDefined(status))
        {
            return Result.Failure(AttendanceErrors.StatusUnknown);
        }

        if (note is not null && note.Trim().Length > NoteMaxLength)
        {
            return Result.Failure(AttendanceErrors.NoteTooLong);
        }

        return EnsureDateIsEditable(date, today);
    }

    private static string? NormalizeNote(string? note)
        => string.IsNullOrWhiteSpace(note) ? null : note.Trim();
}
