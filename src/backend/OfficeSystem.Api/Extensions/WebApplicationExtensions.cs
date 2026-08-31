using OfficeSystem.Infrastructure.Persistence;

namespace OfficeSystem.Api.Extensions;

internal static class WebApplicationExtensions
{
    /// <summary>
    /// Applies migrations at startup, and seeds demo data only outside production.
    /// Migrating in-process suits SQLite and a single instance; a clustered
    /// deployment would move this to a release step instead.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        DatabaseInitialiser initialiser = scope.ServiceProvider.GetRequiredService<DatabaseInitialiser>();

        await initialiser.MigrateAsync().ConfigureAwait(false);

        bool seedDemoData = app.Configuration.GetValue("SeedDemoData", !app.Environment.IsProduction());

        if (seedDemoData)
        {
            await initialiser.SeedDemoDataAsync().ConfigureAwait(false);
        }
    }
}
