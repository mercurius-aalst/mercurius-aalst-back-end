
namespace MercuriusAPI.Extensions;

/// <summary>
/// Represents a builder for JWT (JSON Web Token) authentication configuration.
/// </summary>
public interface IJWTBuilder
{
    /// <summary>
    /// Adds JWT secured SwaggerGen configuration.
    /// </summary>
    /// <param name="options">The function that returns the secured Swagger options.</param>
    /// <returns>The JWT builder instance.</returns>
    IJWTBuilder AddJWTSecuredSwaggerGen(Action<SecuredSwaggerOptions> options);

}
