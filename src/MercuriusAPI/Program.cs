using Imageflow.Server;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Extensions;
using Mercurius.LAN.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

namespace Mercurius.LAN.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                             .AddEnvironmentVariables("Mercurius.LAN.API_");

        builder.Services.AddDbContext<MercuriusDBContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("MercuriusDB")));

        builder.Services.AddValidation();


        builder.Services.ConfigureVersionedSwagger();
        builder.Services.AddServiceDependencies();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        var auth0Settings = builder.Configuration.GetSection("Auth0");
        var auth0Authority = auth0Settings["Authority"]?.TrimEnd('/');

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = auth0Authority;
            options.Audience = auth0Settings["Audience"];
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                NameClaimType = "sub",
                RoleClaimType = auth0Settings["RoleClaimType"],
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
            };
        });
        builder.Services.AddAuthorization();

        var jwtBuilder = new JWTBuilder(builder);
        jwtBuilder.AddJWTSecuredSwaggerGen(options =>
        {
            options.IncludeXMLComments = true;
            options.UseEnumSchemaFilter = true;
        });
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowMercuriusAalst", policy =>
            {
                policy.WithOrigins("https://*.mercurius-aalst.be")
            .SetIsOriginAllowedToAllowWildcardSubdomains()
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
        });

        var app = builder.Build();
        app.UseCors("AllowMercuriusAalst");
        // Apply pending migrations on startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
            dbContext.Database.Migrate();
        }

        app.UseExceptionHandler();

        app.UseHttpsRedirection();


        // Add ImageFlow middleware to serve and optimize images
        var imgflowOptions = new ImageflowMiddlewareOptions
        {
            AllowDiskCaching = true, // Enable disk caching
            AllowCaching = true, // Enable stream caching
            DefaultCacheControlString = "public, max-age=31536000" // Cache images for 1 year
        }.MapPath("/images", app.Configuration["FileStorage:Location"]);

        app.UseImageflow(imgflowOptions);
        app.UseStaticFiles();


        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSecuredSwaggerUI();

        app.MapGameEndpoints();
        app.MapMatchEndpoints();
        app.MapTeamEndpoints();
        app.MapSponsorEndpoints();
        app.MapUserEndpoints();

        app.Run();
    }
}
