using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

namespace Mercurius.Platform.Swagger;

public static class MercuriusSwaggerExtensions
{
    public static IServiceCollection AddMercuriusSwagger(
        this IServiceCollection services,
        IHostEnvironment environment,
        bool includeXmlComments,
        bool useEnumSchemaFilter)
    {
        services.AddEndpointsApiExplorer();
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader());
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.ConfigureSwaggerGen(options =>
        {
            if (useEnumSchemaFilter)
                options.SchemaFilter<EnumSchemaFilter>();

            if (!includeXmlComments)
                return;

            var xmlDocFile = Path.Combine(AppContext.BaseDirectory, $"{environment.ApplicationName}.xml");
            if (File.Exists(xmlDocFile))
                options.IncludeXmlComments(xmlDocFile);
        });

        services.AddSwaggerGen(options =>
        {
            options.DocumentFilter<SecurityTrimming>();
            options.AddSecurityDefinition("BearerDefinition", new OpenApiSecurityScheme
            {
                Name = environment.ApplicationName,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your security token."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    []
                }
            });
        });

        return services;
    }

    public static WebApplication UseMercuriusSwaggerUI(this WebApplication app)
    {
        var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "staticfiles")),
            RequestPath = "/staticfiles"
        });
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in apiVersionProvider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }

            options.InjectJavascript("/staticfiles/swagger-custom.js");
        });

        return app;
    }
}
