using Imageflow.Server;
using MercuriusAPI.Data;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Extensions;
using MercuriusAPI.Services.UserServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

namespace MercuriusAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                             .AddEnvironmentVariables("MercuriusApi_");

        builder.Services.AddDbContext<MercuriusDBContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("MercuriusDB")));


        builder.Services.ConfigureVersionedSwagger();
        builder.Services.AddServiceDependencies();

        builder.Services.AddControllers(options =>
        {
            options.EnableEndpointRouting = false;
            options.Filters.Add<ExceptionFilter>();
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Add JWT authentication
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
            };
        });

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


        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen();

        var app = builder.Build();
        app.UseCors("AllowMercuriusAalst");
        // Apply pending migrations on startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
            dbContext.Database.Migrate();
            // Seed initial user
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            userService.SeedInitialUserAsync(app.Configuration).GetAwaiter().GetResult();
        }

        app.UseSwagger();
        app.UseSwaggerUI();

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

        app.MapControllers();

        app.Run();
    }
}
