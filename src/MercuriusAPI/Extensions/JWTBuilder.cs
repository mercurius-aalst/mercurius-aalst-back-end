using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace MercuriusAPI.Extensions;

public class JWTBuilder(WebApplicationBuilder appBuilder) : IJWTBuilder
{
    public IJWTBuilder AddJWTSecuredSwaggerGen(Action<SecuredSwaggerOptions> options)
    {
        appBuilder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        var optionsValues = new SecuredSwaggerOptions();
        options(optionsValues);

        if (optionsValues.IncludeXMLComments)
        {
            appBuilder.Services.ConfigureSwaggerGen(options =>
            {
                if (optionsValues.UseEnumSchemaFilter)
                    options.SchemaFilter<EnumSchemaFilter>();

                var xmlDocFile = Path.Combine(AppContext.BaseDirectory, $"{appBuilder.Environment.ApplicationName}.xml");
                if (File.Exists(xmlDocFile))
                {
                    options.IncludeXmlComments(xmlDocFile);
                }
            });
        }
        else if (optionsValues.UseEnumSchemaFilter)
        {
            appBuilder.Services.ConfigureSwaggerGen(options =>
            {
                options.SchemaFilter<EnumSchemaFilter>();
            });
        }

        appBuilder.Services.AddSwaggerGen(c =>
        {
            c.DocumentFilter<SecurityTrimming>();
            c.AddSecurityDefinition("BearerDefinition", new OpenApiSecurityScheme()
            {
                Name = $"{appBuilder.Environment.ApplicationName}",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your security token."
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "BearerDefinition"
                        }
                    },
                    new List<string>()
                }
            });
        });

        return this;
    }
}
