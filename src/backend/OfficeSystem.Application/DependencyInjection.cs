using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OfficeSystem.Application.Abstractions.Messaging;
using OfficeSystem.Application.Features.Authentication;

namespace OfficeSystem.Application;

public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>)
    ];

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Assembly assembly = typeof(DependencyInjection).Assembly;

        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<RequestValidator>();
        services.AddScoped<AuthenticationSessionFactory>();

        RegisterClosedGenerics(services, assembly, HandlerInterfaces);
        RegisterClosedGenerics(services, assembly, [typeof(IValidator<>)]);

        return services;
    }

    /// <summary>
    /// Registers every concrete type in the assembly against each closed form of the
    /// given open generic interfaces, so adding a use case needs no DI wiring.
    /// </summary>
    private static void RegisterClosedGenerics(IServiceCollection services, Assembly assembly, Type[] openGenerics)
    {
        foreach (Type type in assembly.GetTypes().Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }))
        {
            foreach (Type contract in type.GetInterfaces()
                         .Where(i => i.IsGenericType && openGenerics.Contains(i.GetGenericTypeDefinition())))
            {
                services.AddScoped(contract, type);
            }
        }
    }
}
