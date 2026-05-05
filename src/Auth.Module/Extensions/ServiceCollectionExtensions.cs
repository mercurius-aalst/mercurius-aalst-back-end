using Auth.Module.Services;
using Auth.Module.Services.Login;
using Auth.Module.Services.External;
using Auth.Module.Services.Token;
using Auth.Module.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Module.Extensions;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddOptions<GoogleOidcOptions>()
            .BindConfiguration(GoogleOidcOptions.SectionName);
        services.AddSingleton<IOidcStateStore, OidcStateStore>();
        services.AddHttpClient<IExternalAuthService, ExternalAuthService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ILoginAttemptService>(_ => new LoginAttemptService(5, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)));
        services.AddTransient<IAuthService, AuthService>();
        services.Decorate<IAuthService, AuthValidationService>();

        services.AddTransient<IAuthUserService, AuthUserService>();
        services.Decorate<IAuthUserService, AuthUserValidationService>();

        return services;
    }
}
