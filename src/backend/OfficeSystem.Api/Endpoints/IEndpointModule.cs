using System.Reflection;

namespace OfficeSystem.Api.Endpoints;

/// <summary>
/// One vertical slice of the HTTP surface. Modules are discovered by reflection, so
/// adding a feature never means editing <c>Program.cs</c>.
/// </summary>
public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder routes);
}

public static class EndpointModuleExtensions
{
    public static IServiceCollection AddEndpointModules(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (Type type in DiscoverModules())
        {
            services.AddSingleton(typeof(IEndpointModule), type);
        }

        return services;
    }

    public static void MapEndpointModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        foreach (IEndpointModule module in app.Services.GetServices<IEndpointModule>())
        {
            module.MapEndpoints(app);
        }
    }

    private static IEnumerable<Type> DiscoverModules()
        => Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IEndpointModule).IsAssignableFrom(t));
}
