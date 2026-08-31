namespace OfficeSystem.Domain.Teams;

public enum TeamRole
{
    /// <summary>Can see the schedule and manage their own attendance.</summary>
    Member = 0,

    /// <summary>Can additionally manage members and team settings.</summary>
    Admin = 1,

    /// <summary>Full control, including deleting the team. Every team has exactly one.</summary>
    Owner = 2
}
