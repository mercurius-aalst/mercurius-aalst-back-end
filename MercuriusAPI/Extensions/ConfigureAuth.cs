using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Security.Cryptography.X509Certificates;

namespace MercuriusAPI.Extensions
{

    public static class ConfigureAuth
    {

        public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));
            return services;
        }
        public static IServiceCollection ConfigureAuthorizationWithDynamicPolicies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthorization(options =>
            {              
                var roles = configuration.GetSection("AzureAd:Roles").Get<string[]>() ?? Array.Empty<string>();
               
                foreach(var roleName in roles)
                {
                    var policyName = $"Require{roleName}Role";
                    options.AddPolicy(policyName, policy =>
                        policy.RequireRole(roleName)
                    );
                }
            });
            return services;
        }
    }
}
