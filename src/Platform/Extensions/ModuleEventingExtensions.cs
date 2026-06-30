using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.Eventing;
using Platform.Eventing.Persistence;

namespace Platform.Extensions;

public static class ModuleEventingExtensions
{
    public static IServiceCollection AddModuleEventing<TDbContext>(
        this IServiceCollection services,
        Action<ModuleEventingOptions>? configure = null)
        where TDbContext : class, IModuleEventDbContext
    {
        var options = new ModuleEventingOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<ModuleEventTypeRegistry>();
        services.TryAddScoped<IModuleEventDbContext>(provider => provider.GetRequiredService<TDbContext>());
        services.TryAddScoped<IModuleEventPublisher, ModuleEventPublisher>();
        services.TryAddScoped<IModuleEventDispatcher, ModuleEventDispatcher>();

        return services;
    }

    public static IServiceCollection AddModuleEventHandler<TPayload, THandler>(this IServiceCollection services)
        where TPayload : notnull
        where THandler : class, IModuleEventHandler<TPayload>
    {
        services.AddTransient<IModuleEventHandler<TPayload>, THandler>();
        return services;
    }
}
