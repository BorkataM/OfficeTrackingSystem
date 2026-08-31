using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Attendance;

public static class AttendanceErrors
{
    public static readonly Error NoteTooLong = Error.Validation(
        "attendance.note.tooLong", $"A note may not exceed {AttendanceEntry.NoteMaxLength} characters.");

    public static readonly Error StatusUnknown = Error.Validation(
        "attendance.status.unknown", "The attendance status is not recognised.");

    public static readonly Error NotFound = Error.NotFound(
        "attendance.notFound", "There is no attendance entry for that day.");

    public static readonly Error TooFarInThePast = Error.Validation(
        "attendance.tooFarInThePast",
        $"Attendance can only be changed up to {AttendanceEntry.MaxDaysInThePast} days into the past.");

    public static readonly Error TooFarInTheFuture = Error.Validation(
        "attendance.tooFarInTheFuture",
        $"Attendance can only be planned up to {AttendanceEntry.MaxDaysInTheFuture} days ahead.");

    public static readonly Error DuplicateDaysInRequest = Error.Validation(
        "attendance.duplicateDays", "The same day was listed more than once.");

    public static readonly Error NoDaysInRequest = Error.Validation(
        "attendance.noDays", "At least one day must be supplied.");

    public static readonly Error TooManyDaysInRequest = Error.Validation(
        "attendance.tooManyDays",
        $"At most {AttendanceEntry.MaxDaysPerBulkRequest} days can be set in one request.");
}
