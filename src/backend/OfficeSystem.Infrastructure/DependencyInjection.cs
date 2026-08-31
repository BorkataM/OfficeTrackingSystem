using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeSystem.Application.Abstractions.Authentication;
using OfficeSystem.Application.Abstractions.Data;
using OfficeSystem.Application.Abstractions.Time;
using OfficeSystem.Infrastructure.Authentication;
using OfficeSystem.Infrastructure.Persistence;
using OfficeSystem.Infrastructure.Persistence.ReadRepositories;
using OfficeSystem.Infrastructure.Persistence.Repositories;
using OfficeSystem.Infrastructure.Time;

namespace OfficeSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services
            .AddPersistence(configuration)
            .AddAuthenticationServices(configuration)
            .AddTimeServices(configuration);
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
                                  ?? "Data Source=officesystem.db";

        services.AddDbContext<OfficeSystemDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OfficeSystemDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();

        services.AddScoped<ITeamReadRepository, TeamReadRepository>();
        services.AddScoped<IAttendanceReadRepository, AttendanceReadRepository>();

        services.AddScoped<DatabaseInitialiser>();

        return services;
    }

    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenProvider, JwtTokenProvider>();

        return services;
    }

    private static IServiceCollection AddTimeServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OfficeTimeOptions>()
            .Bind(configuration.GetSection(OfficeTimeOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
