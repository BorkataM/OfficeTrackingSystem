using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Domain.Attendance;
using OfficeSystem.Domain.Common;
using OfficeSystem.Domain.Teams;
using OfficeSystem.Domain.Users;

namespace OfficeSystem.Infrastructure.Persistence;

/// <summary>
/// Brings the database up to date and, on an empty database, plants a demo team so
/// the app has something to show on first run.
/// </summary>
public sealed class DatabaseInitialiser(
    OfficeSystemDbContext context,
    IPasswordHasher passwordHasher,
    IClock clock,
    ILogger<DatabaseInitialiser> logger)
{
    private const string DemoPassword = "Office123!";

    private static readonly (string Email, string Name, TeamRole Role)[] DemoPeople =
    [
        ("ada@example.com", "Ada Lovelace", TeamRole.Owner),
        ("grace@example.com", "Grace Hopper", TeamRole.Admin),
        ("alan@example.com", "Alan Turing", TeamRole.Member),
        ("katherine@example.com", "Katherine Johnson", TeamRole.Member),
        ("linus@example.com", "Linus Torvalds", TeamRole.Member)
    ];

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Database schema is up to date.");
    }

    public async Task SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Database already contains users; skipping demo seed.");
            return;
        }

        string passwordHash = passwordHasher.Hash(DemoPassword);
        Dictionary<string, User> users = [];

        foreach ((string email, string name, _) in DemoPeople)
        {
            Result<User> user = User.Register(Email.Create(email).Value, name, passwordHash, clock.UtcNow);
            users[email] = user.Value;
            context.Users.Add(user.Value);
        }

        Team team = Team.Create(
            "Platform Team",
            "Backend, infrastructure and everything that keeps the lights on.",
            users[DemoPeople[0].Email].Id,
            clock.UtcNow).Value;

        foreach ((string email, _, TeamRole role) in DemoPeople.Where(p => p.Role != TeamRole.Owner))
        {
            team.Join(users[email].Id, clock.UtcNow);

            if (role != TeamRole.Member)
            {
                team.ChangeRole(users[email].Id, role, users[DemoPeople[0].Email].Id);
            }
        }

        context.Teams.Add(team);
        context.AttendanceEntries.AddRange(BuildDemoAttendance(team, users));

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Seeded demo team '{Team}' with {MemberCount} members. Join code: {JoinCode}. Password for every demo account: {Password}",
            team.Name,
            team.MemberCount,
            team.JoinCode.Value,
            DemoPassword);
    }

    /// <summary>
    /// A deterministic but varied pattern over the current and next week, so the
    /// grid is not empty and the per-day totals differ from each other.
    /// </summary>
    private IEnumerable<AttendanceEntry> BuildDemoAttendance(Team team, Dictionary<string, User> users)
    {
        AttendanceStatus[][] patterns =
        [
            [AttendanceStatus.Office, AttendanceStatus.Office, AttendanceStatus.Remote, AttendanceStatus.Office, AttendanceStatus.Remote],
            [AttendanceStatus.Remote, AttendanceStatus.Office, AttendanceStatus.Office, AttendanceStatus.Remote, AttendanceStatus.Away],
            [AttendanceStatus.Office, AttendanceStatus.Travelling, AttendanceStatus.Travelling, AttendanceStatus.Office, AttendanceStatus.Office],
            [AttendanceStatus.Away, AttendanceStatus.Away, AttendanceStatus.Office, AttendanceStatus.Office, AttendanceStatus.Remote],
            [AttendanceStatus.Remote, AttendanceStatus.Remote, AttendanceStatus.Office, AttendanceStatus.Travelling, AttendanceStatus.Office]
        ];

        DateOnly monday = DateRange.WeekOf(clock.Today).Start;

        for (int person = 0; person < DemoPeople.Length; person++)
        {
            Guid userId = users[DemoPeople[person].Email].Id;

            for (int week = 0; week < 2; week++)
            {
                for (int weekday = 0; weekday < 5; weekday++)
                {
                    DateOnly date = monday.AddDays((week * 7) + weekday);
                    AttendanceStatus status = patterns[person][(weekday + week) % 5];

                    Result<AttendanceEntry> entry = AttendanceEntry.Create(
                        team.Id,
                        userId,
                        date,
                        status,
                        note: null,
                        today: clock.Today,
                        nowUtc: clock.UtcNow);

                    if (entry.IsSuccess)
                    {
                        yield return entry.Value;
                    }
                }
            }
        }
    }
}
