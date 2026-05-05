using Mercurius.LAN.API.Services.Auth;
using Mercurius.LAN.API.Services.Auth.Login;
using Mercurius.LAN.API.Services.Auth.Token;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Module.Extensions;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ILoginAttemptService>(_ => new LoginAttemptService(5, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)));
        services.AddTransient<IAuthService, AuthService>();
        services.Decorate<IAuthService, AuthValidationService>();

        return services;
    }
}
