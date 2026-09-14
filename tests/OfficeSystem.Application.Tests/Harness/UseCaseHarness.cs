using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OfficeSystem.Application;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Infrastructure;
using OfficeSystem.Infrastructure.Persistence;

namespace OfficeSystem.Application.Tests.Harness;

/// <summary>
/// Runs use cases through the real dispatcher, the real validators and the real EF
/// mappings against a private SQLite database. Fakes are limited to the two things
/// a test must control: the clock and who is calling.
/// </summary>
public sealed class UseCaseHarness : IAsyncDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly ServiceProvider _services;

    private UseCaseHarness(SqliteConnection keepAlive, ServiceProvider services)
    {
        _keepAlive = keepAlive;
        _services = services;
    }

    public TestClock Clock { get; private set; } = null!;

    public TestCurrentUser CurrentUser { get; private set; } = null!;

    public static async Task<UseCaseHarness> CreateAsync()
    {
        // A named in-memory database lives only while a connection to it is open,
        // so one is held for the lifetime of the harness.
        string connectionString = $"Data Source=file:{Guid.CreateVersion7():N}?mode=memory&cache=shared";
        SqliteConnection keepAlive = new(connectionString);
        await keepAlive.OpenAsync();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connectionString,
                ["Database:Provider"] = DatabaseProviders.Sqlite,
                ["Jwt:SigningKey"] = "test-signing-key-that-is-long-enough-for-hmac-sha256",
                ["Jwt:Issuer"] = "office-system-tests",
                ["Jwt:Audience"] = "office-system-tests",
                ["OfficeTime:TimeZone"] = "UTC",
            })
            .Build();

        TestClock clock = new();
        TestCurrentUser currentUser = new();

        ServiceCollection services = new();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Registered after the infrastructure so these win.
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<ICurrentUser>(currentUser);

        ServiceProvider provider = services.BuildServiceProvider();

        await using (AsyncServiceScope scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<OfficeSystemDbContext>().Database.EnsureCreatedAsync();
        }

        return new UseCaseHarness(keepAlive, provider) { Clock = clock, CurrentUser = currentUser };
    }

    /// <summary>
    /// Each call gets its own scope, mirroring one HTTP request: a use case never
    /// sees another use case's change tracker.
    /// </summary>
    public async Task<T> DispatchAsync<T>(Func<IDispatcher, CancellationToken, Task<T>> action)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();

        return await action(scope.ServiceProvider.GetRequiredService<IDispatcher>(), CancellationToken.None);
    }

    /// <summary>Direct database access, for arranging state and asserting on it.</summary>
    public async Task<T> QueryAsync<T>(Func<OfficeSystemDbContext, Task<T>> query)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();

        return await query(scope.ServiceProvider.GetRequiredService<OfficeSystemDbContext>());
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _keepAlive.DisposeAsync();
    }
}

public sealed class TestClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);

    public DateOnly Today { get; set; } = new(2026, 8, 28);

    public void Advance(TimeSpan by)
    {
        UtcNow = UtcNow.Add(by);
        Today = DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }
}

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public bool IsAuthenticated => UserId is not null;

    public Guid RequireUserId() => UserId ?? throw new InvalidOperationException("No user set on the test harness.");
}
