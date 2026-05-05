using Auth.Module.Services;
using Auth.Module.Services.Login;
using Auth.Module.Services.External;
using Auth.Module.Services.Token;
using Auth.Module.Services.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Module.Extensions;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddSingleton<IOidcStateStore, OidcStateStore>();
        services.AddHttpClient();
        services.AddSingleton<IOidcProviderRegistry>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var providers = configuration.GetSection(OidcProviderOptions.SectionName)
                .GetChildren()
                .Select(section =>
                {
                    var options = section.Get<OidcProviderOptions>() ?? new OidcProviderOptions();
                    return (ProviderName: section.Key, Options: OidcProviderDefaults.Apply(section.Key, options));
                })
                .Where(provider => provider.Options.IsEnabled)
                .Select(provider => (IOidcProviderStrategy)new StandardOidcProvider(provider.ProviderName, provider.Options, httpClientFactory.CreateClient()))
                .ToArray();

            return new OidcProviderRegistry(providers);
        });
        services.AddTransient<IExternalAuthService, ExternalAuthService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ILoginAttemptService>(_ => new LoginAttemptService(5, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)));
        services.AddTransient<IAuthService, AuthService>();
        services.Decorate<IAuthService, AuthValidationService>();

        services.AddTransient<IAuthUserService, AuthUserService>();
        services.Decorate<IAuthUserService, AuthUserValidationService>();

        return services;
    }
}
