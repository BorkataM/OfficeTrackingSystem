using OfficeSystem.Domain.Common;

namespace OfficeSystem.Domain.Teams;

public static class TeamErrors
{
    public static readonly Error NameEmpty = Error.Validation(
        "team.name.empty", "A team name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "team.name.tooLong", $"A team name may not exceed {Team.NameMaxLength} characters.");

    public static readonly Error DescriptionTooLong = Error.Validation(
        "team.description.tooLong", $"A team description may not exceed {Team.DescriptionMaxLength} characters.");

    public static readonly Error JoinCodeEmpty = Error.Validation(
        "team.joinCode.empty", "A join code is required.");

    public static readonly Error JoinCodeInvalid = Error.Validation(
        "team.joinCode.invalid", $"A join code is {JoinCode.Length} characters long and uses letters and digits only.");

    public static readonly Error NotFound = Error.NotFound(
        "team.notFound", "The team could not be found.");

    public static readonly Error JoinCodeNotRecognised = Error.NotFound(
        "team.joinCode.notRecognised", "No team matches that join code.");

    public static readonly Error AlreadyMember = Error.Conflict(
        "team.alreadyMember", "You are already a member of this team.");

    public static readonly Error NotMember = Error.Forbidden(
        "team.notMember", "You are not a member of this team.");

    public static readonly Error MemberNotFound = Error.NotFound(
        "team.member.notFound", "That person is not a member of this team.");

    public static readonly Error AdminRequired = Error.Forbidden(
        "team.adminRequired", "Only team admins can do that.");

    public static readonly Error OwnerRequired = Error.Forbidden(
        "team.ownerRequired", "Only the team owner can do that.");

    public static readonly Error CannotDemoteOwner = Error.Conflict(
        "team.cannotDemoteOwner", "The team owner's role cannot be changed. Transfer ownership first.");

    public static readonly Error CannotRemoveOwner = Error.Conflict(
        "team.cannotRemoveOwner", "The team owner cannot be removed. Transfer ownership first.");

    public static readonly Error OwnerCannotLeave = Error.Conflict(
        "team.ownerCannotLeave", "Transfer ownership to another member before leaving the team.");

    public static readonly Error CannotTransferToSelf = Error.Validation(
        "team.cannotTransferToSelf", "You already own this team.");
}
