using MercuriusAPI.Data;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Imageflow.Server;
using MercuriusAPI.Services.Files;
using MercuriusAPI.Controllers.LAN;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace MercuriusAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
                };
            });

            // Add CORS policy to allow all origins
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Register FileService
            builder.Services.AddTransient<IFileService, FileService>();

                        builder.Services.AddEndpointsApiExplorer();

                        builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Apply pending migrations on startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
                dbContext.Database.Migrate();
            }

            app.UseSwagger();
            app.UseSwaggerUI();          

                        if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            // Add ImageFlow middleware to serve and optimize images
            var imgflowOptions = new ImageflowMiddlewareOptions
            {
                AllowDiskCaching = true, // Enable disk caching
                AllowCaching = true, // Enable stream caching
                DefaultCacheControlString = "public, max-age=31536000" // Cache images for 1 year
            }.MapPath("/images", app.Configuration["FileStorage:Location"]);

            app.UseImageflow(imgflowOptions);
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
