using MercuriusAPI.Data;
using MercuriusAPI.Services.LAN.GameServices;
using MercuriusAPI.Services.LAN.MatchServices;
using MercuriusAPI.Services.LAN.MatchServices.BracketTypes;
using MercuriusAPI.Services.LAN.ParticipantServices;
using MercuriusAPI.Services.LAN.PlayerServices;
using MercuriusAPI.Services.LAN.TeamServices;
using Microsoft.EntityFrameworkCore;

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
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddTransient<IPlayerService, PlayerService>();
            builder.Services.AddTransient<ITeamService, TeamService>();
            builder.Services.AddTransient<IGameService, GameService>();
            builder.Services.AddTransient<IParticipantService, ParticipantService>();

            builder.Services.AddTransient<IMatchGeneratorFactory, MatchGeneratorFactory>();
            builder.Services.AddTransient<SingleEliminationMatchGenerator>();
            builder.Services.AddTransient<DoubleEliminationMatchGenerator>();
            builder.Services.AddTransient<SwissStageMatchGenerator>();
            builder.Services.AddTransient<RoundRobinMatchGenerator>();


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
