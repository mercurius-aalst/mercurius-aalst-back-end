using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Platform.Eventing;
using Platform.Eventing.Persistence;

namespace Platform.Extensions;

public static class ModuleEventingExtensions
{
    public static IServiceCollection AddModuleEventing<TDbContext>(
        this IServiceCollection services,
        IConfiguration? configuration = null)
        where TDbContext : class, IModuleEventDbContext
    {
        var options = services.AddOptions<ModuleEventingOptions>();
        if (configuration is not null)
            options.Bind(configuration.GetSection(ModuleEventingOptions.SectionName));

        options
            .Validate(value => value.DispatchBatchSize is >= 1 and <= 1000,
                "ModuleEventing:DispatchBatchSize must be between 1 and 1000.")
            .Validate(value => value.PollInterval > TimeSpan.Zero && value.PollInterval <= TimeSpan.FromDays(1),
                "ModuleEventing:PollInterval must be greater than zero and no more than one day.")
            .Validate(value => value.LeaseDuration >= TimeSpan.FromMilliseconds(60) && value.LeaseDuration <= TimeSpan.FromHours(1),
                "ModuleEventing:LeaseDuration must be between 60 milliseconds and one hour.")
            .Validate(value => value.MaxAttempts is >= 1 and <= 100,
                "ModuleEventing:MaxAttempts must be between 1 and 100.")
            .Validate(value => value.RetryBaseDelay > TimeSpan.Zero && value.RetryBaseDelay <= TimeSpan.FromDays(1),
                "ModuleEventing:RetryBaseDelay must be greater than zero and no more than one day.")
            .Validate(value => value.RetryMaxDelay >= value.RetryBaseDelay && value.RetryMaxDelay <= TimeSpan.FromDays(7),
                "ModuleEventing:RetryMaxDelay must be at least RetryBaseDelay and no more than seven days.")
            .Validate(value => value.SuccessfulRetention > TimeSpan.Zero && value.SuccessfulRetention <= TimeSpan.FromDays(3650),
                "ModuleEventing:SuccessfulRetention must be greater than zero and no more than 3650 days.")
            .Validate(value => value.DeadLetterRetention > TimeSpan.Zero && value.DeadLetterRetention <= TimeSpan.FromDays(3650),
                "ModuleEventing:DeadLetterRetention must be greater than zero and no more than 3650 days.")
            .Validate(value => value.CleanupBatchSize is >= 1 and <= 1000,
                "ModuleEventing:CleanupBatchSize must be between 1 and 1000.")
            .Validate(value => value.CleanupInterval > TimeSpan.Zero && value.CleanupInterval <= TimeSpan.FromDays(7),
                "ModuleEventing:CleanupInterval must be greater than zero and no more than seven days.")
            .ValidateOnStart();

        services.TryAddScoped<IModuleEventDbContext>(provider => provider.GetRequiredService<TDbContext>());
        services.TryAddScoped<IModuleEventPublisher, ModuleEventPublisher>();
        services.TryAddScoped<IModuleEventDispatcher, ModuleEventDispatcher>();
        services.TryAddSingleton<ModuleEventClaimGate>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ModuleEventDispatchWorker>());

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
