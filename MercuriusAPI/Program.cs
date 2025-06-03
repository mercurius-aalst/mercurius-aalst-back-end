using MercuriusAPI.Data;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Extensions;
using MercuriusAPI.Services.LAN.GameServices;
using MercuriusAPI.Services.LAN.MatchServices;
using MercuriusAPI.Services.LAN.MatchServices.BracketTypes;
using MercuriusAPI.Services.LAN.ParticipantServices;
using MercuriusAPI.Services.LAN.PlayerServices;
using MercuriusAPI.Services.LAN.TeamServices;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace MercuriusAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            builder.Services.AddDbContext<MercuriusDBContext>(options =>
                                                                options.UseNpgsql(builder.Configuration.GetConnectionString("MercuriusDB")))
                            .ConfigureVersionedSwagger()
                            .AddServiceDependencies()
                            .ConfigureAuthentication(builder.Configuration)
                            .ConfigureAuthorizationWithDynamicPolicies(builder.Configuration);


            builder.Services.AddControllers(options =>
            {
                options.EnableEndpointRouting = false;
                options.Filters.Add<ExceptionFilter>();
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            var app = builder.Build();

            if(app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
