namespace OfficeSystem.Domain.Attendance;

public enum AttendanceStatus
{
    /// <summary>In the office.</summary>
    Office = 1,

    /// <summary>Working, but not from the office.</summary>
    Remote = 2,

    /// <summary>Working off-site: customer visit, conference, another location.</summary>
    Travelling = 3,

    /// <summary>Not working: holiday, sick leave, public holiday.</summary>
    Away = 4
}
