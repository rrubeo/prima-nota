using System.Reflection;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace PrimaNota.Application;

/// <summary>Registers Application-layer services (MediatR, FluentValidation, Mapster).</summary>
public static class DependencyInjection
{
    /// <summary>Adds MediatR, FluentValidation and Mapster to the DI container.</summary>
    /// <param name="services">The DI container.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
