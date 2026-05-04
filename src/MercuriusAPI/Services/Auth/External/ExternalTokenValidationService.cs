using System.IdentityModel.Tokens.Jwt;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Mercurius.LAN.API.Services.Auth.External;

public class ExternalTokenValidationService : IExternalTokenValidationService
{
    private readonly IConfiguration _configuration;

    public ExternalTokenValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<ExternalPrincipal> ValidateAsync(ExternalAuthProvider provider, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ValidationException("External token is required.");

        var audience = _configuration[$"Authentication:External:{provider}:Audience"];
        var issuer = _configuration[$"Authentication:External:{provider}:Issuer"];
        var signingKey = _configuration[$"Authentication:External:{provider}:SigningKey"];

        if (string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(signingKey))
            throw new ValidationException($"External provider '{provider}' is not configured.");

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };

        try
        {
            var principal = handler.ValidateToken(token, validationParameters, out _);
            var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(subject))
                throw new ValidationException("External token is missing subject claim.");

            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var emailVerifiedRaw = principal.FindFirst("email_verified")?.Value;
            var emailVerified = bool.TryParse(emailVerifiedRaw, out var parsed) && parsed;

            return Task.FromResult(new ExternalPrincipal
            {
                Subject = subject,
                Email = email,
                EmailVerified = emailVerified,
            });
        }
        catch (SecurityTokenException)
        {
            throw new ValidationException("Invalid external token.");
        }
    }
}
