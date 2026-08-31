using FluentValidation;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Attendance.Contracts;
using OfficeSystem.Domain.Attendance;

namespace OfficeSystem.Application.Features.Attendance.SetAttendance;

public sealed record DayPlanInput(DateOnly Date, AttendanceStatus Status, string? Note);

/// <summary>
/// Upserts the caller's own plan for one or more days. Sending several days at once
/// is what makes "mark the whole week" a single, atomic action.
/// </summary>
public sealed record SetAttendanceCommand(Guid TeamId, IReadOnlyList<DayPlanInput> Days)
    : ICommand<IReadOnlyList<DayPlanResponse>>;

internal sealed class SetAttendanceCommandValidator : AbstractValidator<SetAttendanceCommand>
{
    public SetAttendanceCommandValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();

        RuleFor(c => c.Days)
            .NotEmpty().WithMessage("At least one day must be supplied.")
            .Must(days => days is null || days.Count <= AttendanceEntry.MaxDaysPerBulkRequest)
                .WithMessage($"At most {AttendanceEntry.MaxDaysPerBulkRequest} days can be set at once.")
            .Must(days => days is null || days.Select(d => d.Date).Distinct().Count() == days.Count)
                .WithMessage("The same day was listed more than once.");

        RuleForEach(c => c.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.Status).IsInEnum().WithMessage("That attendance status is not recognised.");
            day.RuleFor(d => d.Note).MaximumLength(AttendanceEntry.NoteMaxLength);
        });
    }
}
