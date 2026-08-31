using FluentValidation;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Domain.Attendance;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;

namespace OfficeSystem.Application.Features.Attendance.ClearAttendance;

/// <summary>Removes the caller's plan for the given days, back to "not planned yet".</summary>
public sealed record ClearAttendanceCommand(Guid TeamId, IReadOnlyList<DateOnly> Dates) : ICommand;

internal sealed class ClearAttendanceCommandValidator : AbstractValidator<ClearAttendanceCommand>
{
    public ClearAttendanceCommandValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();

        RuleFor(c => c.Dates)
            .NotEmpty().WithMessage("At least one day must be supplied.")
            .Must(dates => dates is null || dates.Count <= AttendanceEntry.MaxDaysPerBulkRequest)
                .WithMessage($"At most {AttendanceEntry.MaxDaysPerBulkRequest} days can be cleared at once.");
    }
}

internal sealed class ClearAttendanceCommandHandler(
    IAttendanceRepository attendance,
    ITeamReadRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : ICommandHandler<ClearAttendanceCommand>
{
    public async Task<Result> HandleAsync(ClearAttendanceCommand command, CancellationToken cancellationToken)
    {
        Guid userId = currentUser.RequireUserId();

        TeamRole? role = await teams.GetRoleAsync(command.TeamId, userId, cancellationToken).ConfigureAwait(false);

        if (role is null)
        {
            return Result.Failure(TeamErrors.NotMember);
        }

        DateOnly today = clock.Today;

        foreach (DateOnly date in command.Dates.Distinct())
        {
            Result editable = AttendanceEntry.EnsureDateIsEditable(date, today);

            if (editable.IsFailure)
            {
                return editable;
            }
        }

        IReadOnlyList<AttendanceEntry> entries = await attendance
            .ListForUserAsync(command.TeamId, userId, [.. command.Dates.Distinct()], cancellationToken)
            .ConfigureAwait(false);

        foreach (AttendanceEntry entry in entries)
        {
            attendance.Remove(entry);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
